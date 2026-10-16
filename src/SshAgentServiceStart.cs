using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ProGo
{
    internal enum SshAgentStartState { Running, StartPending, Disabled, Missing, AccessDenied, NotRunning, Failed, Unavailable }

    internal sealed class SshAgentServiceStartResult
    {
        internal SshAgentStartState State = SshAgentStartState.Unavailable;
        internal SshAgentServiceSnapshot Snapshot;
        internal SshAgentServiceError Error;
        internal bool StartInvoked, StartAccepted;

        internal string ToDisplay()
        {
            string text;
            switch (State) {
                case SshAgentStartState.Running:
                    text = "Служба ssh-agent работает; нужный ключ и SSH-аутентификация ещё не проверены."; break;
                case SshAgentStartState.StartPending:
                    text = "Запуск ssh-agent ещё выполняется. Повторите проверку состояния позже."; break;
                case SshAgentStartState.Disabled:
                    text = "Служба ssh-agent отключена. Изменение режима запуска требует отдельного согласованного действия; приложение его не меняло."; break;
                case SshAgentStartState.Missing:
                    text = "Служба ssh-agent отсутствует. Проверьте установку Windows OpenSSH."; break;
                case SshAgentStartState.AccessDenied:
                    text = "Недостаточно прав для операции со службой ssh-agent. Обратитесь к администратору; повторного запроса UAC нет."; break;
                case SshAgentStartState.NotRunning:
                    text = "Работа ssh-agent не подтверждена. Проверьте текущее состояние службы и повторите проверку."; break;
                case SshAgentStartState.Failed:
                    text = "Не удалось запустить ssh-agent. Проверьте службу Windows OpenSSH и её системный журнал."; break;
                default:
                    text = "Результат операции со службой ssh-agent не подтверждён. Проверьте OpenSSH и доступ к службе."; break;
            }
            if (StartAccepted) text += " Windows приняла запрос запуска; это само по себе не подтверждает готовность агента.";
            if (Error != null) text += " Этап: " + Error.Stage + (Error.Win32Code.HasValue ? "; Win32 " + Error.Win32Code.Value : "; ответ Windows не подтверждён") + ".";
            if (Snapshot != null) text += " Последняя проверка: " + Snapshot.ToDisplay();
            return text;
        }
    }

    // Starting is a separate capability: read-only callers retain the smaller
    // ISshAgentServiceNative boundary. No configuration, stop, ACL or elevation API.
    internal interface ISshAgentServiceStartNative : ISshAgentServiceNative
    {
        bool Start(IntPtr service, out int error);
    }

    internal static class SshAgentServiceStart
    {
        // Runs only inside the shared owned worker. Native StartService can block
        // past the caller's deadline; cancellation cannot undo an accepted start.
        internal static SshAgentServiceStartResult TryStart(CancellationToken token, ISshAgentServiceStartNative native = null)
        {
            token.ThrowIfCancellationRequested();
            native = native ?? new WindowsNative();
            var result = new SshAgentServiceStartResult();
            IntPtr manager = IntPtr.Zero, service = IntPtr.Zero;
            var stage = SshAgentServiceStage.OpenManager;
            try {
                result.Snapshot = SshAgentServiceStatus.Read(token, native);
                token.ThrowIfCancellationRequested();
                if (!NeedsStart(result)) return result;

                stage = SshAgentServiceStage.OpenStartManager;
                int error;
                manager = native.OpenManager(1, out error); // SC_MANAGER_CONNECT.
                token.ThrowIfCancellationRequested();
                if (manager == IntPtr.Zero) { Fail(result, stage, error); return result; }
                stage = SshAgentServiceStage.OpenStartService;
                service = native.OpenService(manager, "ssh-agent", 0x14, out error); // START | QUERY_STATUS, never CHANGE_CONFIG.
                token.ThrowIfCancellationRequested();
                if (service == IntPtr.Zero) { Fail(result, stage, error); return result; }

                // Recheck with the same handle used for StartService: a service
                // may have started, stopped or begun transitioning since Read.
                stage = SshAgentServiceStage.QueryBeforeStart;
                if (!Observe(result, native, service, stage, token) || !NeedsStart(result)) return result;
                stage = SshAgentServiceStage.StartService;
                token.ThrowIfCancellationRequested();
                result.StartInvoked = true;
                bool accepted = native.Start(service, out error);
                result.StartAccepted = accepted;
                token.ThrowIfCancellationRequested();
                if (!accepted) {
                    Fail(result, stage, error);
                    // A concurrent external start is not proof of Running.
                    if (error != 1056) return result;
                }
                stage = SshAgentServiceStage.QueryAfterStart;
                if (Observe(result, native, service, stage, token)) ClassifyObservation(result);
                return result;
            } catch (OperationCanceledException) { throw; }
            catch (Exception) {
                result.State = SshAgentStartState.Unavailable;
                result.Error = new SshAgentServiceError { Stage = stage, Kind = SshAgentServiceErrorKind.Unavailable };
                return result;
            } finally {
                try { if (service != IntPtr.Zero) native.Close(service); }
                finally { if (manager != IntPtr.Zero) native.Close(manager); }
            }
        }

        private static bool NeedsStart(SshAgentServiceStartResult result)
        {
            var snapshot = result.Snapshot;
            if (snapshot.StatusError != null) {
                result.Error = snapshot.StatusError;
                result.State = FailureState(snapshot.StatusError.Win32Code);
                return false;
            }
            ClassifyObservation(result);
            if (snapshot.RuntimeState != SshAgentRuntimeState.Stopped) return false;
            if (snapshot.StartMode == SshAgentStartMode.Disabled) { result.State = SshAgentStartState.Disabled; return false; }
            // QUERY_CONFIG permission is not required for SERVICE_START. An
            // unreadable mode is left unknown; Windows enforces disabled policy.
            return true;
        }

        private static bool Observe(SshAgentServiceStartResult result, ISshAgentServiceStartNative native,
            IntPtr service, SshAgentServiceStage stage, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            uint raw; int error;
            bool success = native.QueryStatus(service, out raw, out error);
            token.ThrowIfCancellationRequested();
            var snapshot = result.Snapshot;
            snapshot.RawRuntimeState = success ? (uint?)raw : null;
            snapshot.RuntimeState = success ? SshAgentServiceStatus.RuntimeState(raw) : SshAgentRuntimeState.Unknown;
            snapshot.StatusError = null;
            if (!success) { Fail(result, stage, error); snapshot.StatusError = result.Error; return false; }
            if (snapshot.RuntimeState == SshAgentRuntimeState.Unknown) {
                result.State = SshAgentStartState.Unavailable;
                result.Error = snapshot.StatusError = new SshAgentServiceError { Stage = stage, Kind = SshAgentServiceErrorKind.InvalidData };
                return false;
            }
            return true;
        }

        private static void ClassifyObservation(SshAgentServiceStartResult result)
        {
            var state = result.Snapshot.RuntimeState;
            result.State = state == SshAgentRuntimeState.Running ? SshAgentStartState.Running :
                state == SshAgentRuntimeState.StartPending ? SshAgentStartState.StartPending :
                state == SshAgentRuntimeState.Missing ? SshAgentStartState.Missing :
                state == SshAgentRuntimeState.Unknown ? SshAgentStartState.Unavailable : SshAgentStartState.NotRunning;
        }
        private static SshAgentStartState FailureState(int? error)
        {
            return error == 5 ? SshAgentStartState.AccessDenied : error == 1058 ? SshAgentStartState.Disabled :
                error == 1060 ? SshAgentStartState.Missing : error.HasValue ? SshAgentStartState.Failed : SshAgentStartState.Unavailable;
        }
        private static void Fail(SshAgentServiceStartResult result, SshAgentServiceStage stage, int error)
        {
            result.State = FailureState(error);
            result.Error = new SshAgentServiceError { Stage = stage, Kind = SshAgentServiceErrorKind.NativeFailure, Win32Code = error };
        }

        internal sealed class WindowsNative : ISshAgentServiceStartNative
        {
            private readonly ISshAgentServiceNative query = new SshAgentServiceStatus.WindowsNative();
            public IntPtr OpenManager(uint access, out int error) { return query.OpenManager(access, out error); }
            public IntPtr OpenService(IntPtr manager, string name, uint access, out int error) { return query.OpenService(manager, name, access, out error); }
            public bool QueryStatus(IntPtr service, out uint state, out int error) { return query.QueryStatus(service, out state, out error); }
            public bool QueryConfig(IntPtr service, IntPtr buffer, uint size, out uint needed, out int error) { return query.QueryConfig(service, buffer, size, out needed, out error); }
            public IntPtr Allocate(int bytes) { return query.Allocate(bytes); }
            public void Free(IntPtr buffer) { query.Free(buffer); }
            public void Close(IntPtr handle) { query.Close(handle); }
            public bool Start(IntPtr service, out int error)
            {
                bool success = StartService(service, 0, IntPtr.Zero);
                error = success ? 0 : Marshal.GetLastWin32Error(); return success;
            }
        }
        [DllImport("advapi32.dll", EntryPoint = "StartServiceW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool StartService(IntPtr service, uint arguments, IntPtr values);
    }
}
