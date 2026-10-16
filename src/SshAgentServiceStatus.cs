using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ProGo
{
    internal enum SshAgentRuntimeState
    {
        Unknown = 0, Stopped = 1, StartPending = 2, StopPending = 3, Running = 4,
        ContinuePending = 5, PausePending = 6, Paused = 7, Missing = 8
    }
    internal enum SshAgentStartMode { Unknown = -1, Boot = 0, System = 1, Automatic = 2, Manual = 3, Disabled = 4 }
    internal enum SshAgentServiceStage { OpenManager, OpenStatusService, QueryStatus, OpenConfigService, QueryConfigSize, AllocateConfiguration, QueryConfig,
        OpenStartManager, OpenStartService, QueryBeforeStart, StartService, QueryAfterStart }
    internal enum SshAgentServiceErrorKind { NativeFailure, InvalidData, Unavailable }

    internal sealed class SshAgentServiceError
    {
        internal SshAgentServiceStage Stage;
        internal int? Win32Code;
        internal SshAgentServiceErrorKind Kind;
    }
    internal sealed class SshAgentServiceSnapshot
    {
        internal SshAgentRuntimeState RuntimeState = SshAgentRuntimeState.Unknown;
        internal SshAgentStartMode StartMode = SshAgentStartMode.Unknown;
        internal uint? RawRuntimeState, RawStartMode;
        internal SshAgentServiceError StatusError, ConfigurationError;

        internal string ToDisplay()
        {
            string state;
            if (RuntimeState == SshAgentRuntimeState.Running)
                state = "Running — запущен; загрузка нужного ключа ещё не проверена.";
            else if (RuntimeState == SshAgentRuntimeState.Stopped)
                state = StartMode == SshAgentStartMode.Disabled
                    ? "Disabled — режим запуска отключён; служба Stopped — остановлена."
                    : "Stopped — остановлен; запуск зависит от имеющихся прав.";
            else if (RuntimeState == SshAgentRuntimeState.Missing)
                state = "Служба отсутствует; проверьте установку Windows OpenSSH.";
            else if (RuntimeState == SshAgentRuntimeState.Paused)
                state = "Paused — приостановлен; доступность агента ещё не подтверждена.";
            else if (RuntimeState == SshAgentRuntimeState.Unknown)
                state = "Состояние агента не подтверждено.";
            else state = "Служба меняет состояние (" + RuntimeState + "); повторите проверку.";
            state += " Режим запуска: " + (StartMode == SshAgentStartMode.Unknown ? "не подтверждён" : StartMode.ToString()) + ".";
            if (StatusError != null) state += " Проверка состояния: " + DescribeError(StatusError) + ".";
            if (ConfigurationError != null) state += " Проверка режима запуска: " + DescribeError(ConfigurationError) + ".";
            return state;
        }
        private static string DescribeError(SshAgentServiceError error)
        {
            string reason = error.Kind == SshAgentServiceErrorKind.InvalidData ? "некорректный ответ Windows" :
                error.Kind == SshAgentServiceErrorKind.Unavailable ? "проверка недоступна" :
                error.Win32Code == 5 ? "нет прав на чтение" : error.Win32Code == 1060 ? "служба отсутствует" : "ошибка Windows";
            return error.Stage + " — " + reason + (error.Win32Code.HasValue ? " (Win32 " + error.Win32Code.Value + ")" : "");
        }
    }

    // This boundary exposes query and resource ownership only. It cannot start,
    // configure or remove a service, change ACLs, elevate, or access SSH keys.
    internal interface ISshAgentServiceNative
    {
        IntPtr OpenManager(uint access, out int error);
        IntPtr OpenService(IntPtr manager, string name, uint access, out int error);
        bool QueryStatus(IntPtr service, out uint state, out int error);
        bool QueryConfig(IntPtr service, IntPtr buffer, uint size, out uint needed, out int error);
        IntPtr Allocate(int bytes);
        void Free(IntPtr buffer);
        void Close(IntPtr handle);
    }
    internal static class SshAgentServiceStatus
    {
        internal const int MaximumConfigurationBytes = 8192;
        internal static readonly int MinimumConfigurationBytes = Marshal.SizeOf(typeof(Configuration));

        // Synchronous descriptor for an owned worker. Checkpoints do not interrupt
        // a native call already in progress and do not promise a hard deadline.
        internal static SshAgentServiceSnapshot Read(CancellationToken token, ISshAgentServiceNative native = null)
        {
            token.ThrowIfCancellationRequested();
            native = native ?? new WindowsNative();
            var result = new SshAgentServiceSnapshot();
            IntPtr manager = IntPtr.Zero;
            try {
                int error;
                try { manager = native.OpenManager(1, out error); } // SC_MANAGER_CONNECT only.
                catch (OperationCanceledException) { throw; }
                catch (Exception) {
                    result.StatusError = result.ConfigurationError = Error(SshAgentServiceStage.OpenManager, SshAgentServiceErrorKind.Unavailable);
                    return result;
                }
                token.ThrowIfCancellationRequested();
                if (manager == IntPtr.Zero) {
                    result.StatusError = result.ConfigurationError = Error(SshAgentServiceStage.OpenManager, SshAgentServiceErrorKind.NativeFailure, error);
                    return result;
                }
                ReadStatus(result, native, manager, token);
                ReadConfiguration(result, native, manager, token);
                token.ThrowIfCancellationRequested(); return result;
            } finally { if (manager != IntPtr.Zero) native.Close(manager); }
        }
        private static void ReadStatus(SshAgentServiceSnapshot result, ISshAgentServiceNative native, IntPtr manager, CancellationToken token)
        {
            IntPtr service = IntPtr.Zero;
            var stage = SshAgentServiceStage.OpenStatusService;
            try {
                token.ThrowIfCancellationRequested();
                int error;
                service = native.OpenService(manager, "ssh-agent", 4, out error); // SERVICE_QUERY_STATUS only.
                token.ThrowIfCancellationRequested();
                if (service == IntPtr.Zero) {
                    result.StatusError = Error(stage, SshAgentServiceErrorKind.NativeFailure, error);
                    if (error == 1060) result.RuntimeState = SshAgentRuntimeState.Missing;
                    return;
                }
                stage = SshAgentServiceStage.QueryStatus;
                uint state;
                bool success = native.QueryStatus(service, out state, out error);
                token.ThrowIfCancellationRequested();
                if (!success) { result.StatusError = Error(stage, SshAgentServiceErrorKind.NativeFailure, error); return; }
                result.RawRuntimeState = state;
                result.RuntimeState = RuntimeState(state);
                if (result.RuntimeState == SshAgentRuntimeState.Unknown)
                    result.StatusError = Error(stage, SshAgentServiceErrorKind.InvalidData);
            } catch (OperationCanceledException) { throw; }
            catch (Exception) { result.StatusError = Error(stage, SshAgentServiceErrorKind.Unavailable); }
            finally { if (service != IntPtr.Zero) native.Close(service); }
        }
        private static void ReadConfiguration(SshAgentServiceSnapshot result, ISshAgentServiceNative native, IntPtr manager, CancellationToken token)
        {
            IntPtr service = IntPtr.Zero, buffer = IntPtr.Zero;
            var stage = SshAgentServiceStage.OpenConfigService;
            try {
                token.ThrowIfCancellationRequested();
                int error;
                service = native.OpenService(manager, "ssh-agent", 1, out error); // SERVICE_QUERY_CONFIG only.
                token.ThrowIfCancellationRequested();
                if (service == IntPtr.Zero) { result.ConfigurationError = Error(stage, SshAgentServiceErrorKind.NativeFailure, error); return; }
                stage = SshAgentServiceStage.QueryConfigSize;
                uint needed;
                bool success = native.QueryConfig(service, IntPtr.Zero, 0, out needed, out error);
                token.ThrowIfCancellationRequested();
                if (!success && error != 122) { result.ConfigurationError = Error(stage, SshAgentServiceErrorKind.NativeFailure, error); return; }
                if (success || needed < MinimumConfigurationBytes || needed > MaximumConfigurationBytes) {
                    result.ConfigurationError = Error(stage, SshAgentServiceErrorKind.InvalidData); return;
                }
                stage = SshAgentServiceStage.AllocateConfiguration;
                buffer = native.Allocate((int)needed);
                token.ThrowIfCancellationRequested();
                if (buffer == IntPtr.Zero) { result.ConfigurationError = Error(stage, SshAgentServiceErrorKind.InvalidData); return; }
                // An unwritten/unknown start type cannot masquerade as Boot.
                Marshal.WriteInt32(buffer, 4, -1);
                stage = SshAgentServiceStage.QueryConfig;
                success = native.QueryConfig(service, buffer, needed, out needed, out error);
                token.ThrowIfCancellationRequested();
                if (!success) { result.ConfigurationError = Error(stage, SshAgentServiceErrorKind.NativeFailure, error); return; }
                uint mode = unchecked((uint)Marshal.ReadInt32(buffer, 4));
                result.RawStartMode = mode;
                if (mode <= 4) result.StartMode = (SshAgentStartMode)mode;
                else result.ConfigurationError = Error(stage, SshAgentServiceErrorKind.InvalidData);
            } catch (OperationCanceledException) { throw; }
            catch (Exception) { result.ConfigurationError = Error(stage, SshAgentServiceErrorKind.Unavailable); }
            finally {
                try { if (buffer != IntPtr.Zero) native.Free(buffer); }
                finally { if (service != IntPtr.Zero) native.Close(service); }
            }
        }
        internal static SshAgentRuntimeState RuntimeState(uint state)
        { return state >= 1 && state <= 7 ? (SshAgentRuntimeState)state : SshAgentRuntimeState.Unknown; }
        private static SshAgentServiceError Error(SshAgentServiceStage stage, SshAgentServiceErrorKind kind, int? code = null)
        { return new SshAgentServiceError { Stage = stage, Kind = kind, Win32Code = code }; }

        [StructLayout(LayoutKind.Sequential)] private struct Status
        { internal uint Type, State, Controls, ExitCode, ServiceExitCode, Checkpoint, WaitHint; }
        [StructLayout(LayoutKind.Sequential)] private struct Configuration
        {
            internal uint Type, StartType, ErrorControl;
            internal IntPtr BinaryPathName, LoadOrderGroup;
            internal uint Tag;
            internal IntPtr Dependencies, ServiceStartName, DisplayName;
        }
        internal sealed class WindowsNative : ISshAgentServiceNative
        {
            public IntPtr OpenManager(uint access, out int error)
            {
                IntPtr handle = OpenSCManager(null, null, access);
                error = handle == IntPtr.Zero ? Marshal.GetLastWin32Error() : 0; return handle;
            }
            public IntPtr OpenService(IntPtr manager, string name, uint access, out int error)
            {
                IntPtr handle = NativeOpenService(manager, name, access);
                error = handle == IntPtr.Zero ? Marshal.GetLastWin32Error() : 0; return handle;
            }
            public bool QueryStatus(IntPtr service, out uint state, out int error)
            {
                Status status; bool success = QueryServiceStatus(service, out status);
                error = success ? 0 : Marshal.GetLastWin32Error(); state = success ? status.State : 0; return success;
            }
            public bool QueryConfig(IntPtr service, IntPtr buffer, uint size, out uint needed, out int error)
            {
                bool success = QueryServiceConfig(service, buffer, size, out needed);
                error = success ? 0 : Marshal.GetLastWin32Error(); return success;
            }
            public IntPtr Allocate(int bytes) { return Marshal.AllocHGlobal(bytes); }
            public void Free(IntPtr buffer) { Marshal.FreeHGlobal(buffer); }
            public void Close(IntPtr handle) { CloseServiceHandle(handle); }
        }
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr OpenSCManager(string machine, string database, uint access);
        [DllImport("advapi32.dll", EntryPoint = "OpenServiceW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr NativeOpenService(IntPtr manager, string name, uint access);
        [DllImport("advapi32.dll", SetLastError = true)] private static extern bool QueryServiceStatus(IntPtr service, out Status status);
        [DllImport("advapi32.dll", EntryPoint = "QueryServiceConfigW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool QueryServiceConfig(IntPtr service, IntPtr config, uint size, out uint needed);
        [DllImport("advapi32.dll")] private static extern bool CloseServiceHandle(IntPtr handle);
    }
}
