using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        // Only the native boundary is controlled: all result classification,
        // cancellation boundaries and resource ownership remain production code.
        private sealed class AgentServiceNativeFixture : ISshAgentServiceNative, IDisposable
        {
            internal readonly List<string> Calls = new List<string>();
            internal readonly List<uint> ManagerRights = new List<uint>(), ServiceRights = new List<uint>();
            internal readonly List<IntPtr> Closed = new List<IntPtr>();
            internal readonly HashSet<IntPtr> Allocations = new HashSet<IntPtr>();
            internal Action<string> After;
            internal string ThrowAt;
            internal int ManagerError, StatusOpenError, ConfigOpenError, StatusQueryError, ConfigProbeError = 122, ConfigReadError;
            internal uint State = 4, Start = 3, ProbeBytes = (uint)SshAgentServiceStatus.MinimumConfigurationBytes;
            internal uint? ReadBytes;
            internal bool ProbeSuccess, LeaveStartUnwritten, NullAllocation;
            internal int Allocated, Freed;
            internal readonly IntPtr Manager = new IntPtr(101), StatusService = new IntPtr(201), ConfigService = new IntPtr(301);

            private void Called(string stage)
            {
                Calls.Add(stage);
                if (ThrowAt == stage) throw new InvalidOperationException("PRIVATE_NATIVE_FIXTURE_FAILURE");
                if (After != null) After(stage);
            }
            public IntPtr OpenManager(uint access, out int error)
            { ManagerRights.Add(access); error = ManagerError; Called("manager"); return error == 0 ? Manager : IntPtr.Zero; }
            public IntPtr OpenService(IntPtr manager, string name, uint access, out int error)
            {
                if (manager != Manager || name != "ssh-agent") throw new Exception("Unexpected SCM service selection.");
                ServiceRights.Add(access); bool status = access == 4;
                error = status ? StatusOpenError : ConfigOpenError;
                Called(status ? "status-open" : "config-open"); return error == 0 ? status ? StatusService : ConfigService : IntPtr.Zero;
            }
            public bool QueryStatus(IntPtr service, out uint state, out int error)
            {
                if (service != StatusService) throw new Exception("Wrong status handle.");
                state = State; error = StatusQueryError; Called("status-query"); return error == 0;
            }
            public bool QueryConfig(IntPtr service, IntPtr ptr, uint size, out uint needed, out int error)
            {
                if (service != ConfigService) throw new Exception("Wrong configuration handle.");
                if (ptr == IntPtr.Zero) {
                    if (size != 0) throw new Exception("Unexpected configuration probe buffer.");
                    needed = ProbeBytes; error = ConfigProbeError; Called("config-probe"); return ProbeSuccess;
                }
                if (!Allocations.Contains(ptr) || size != ProbeBytes) throw new Exception("Unexpected configuration allocation.");
                needed = ReadBytes ?? ProbeBytes; error = ConfigReadError;
                if (!LeaveStartUnwritten) Marshal.WriteInt32(ptr, 4, unchecked((int)Start));
                Called("config-read"); return error == 0;
            }
            public IntPtr Allocate(int bytes)
            {
                Called("allocate"); if (NullAllocation) return IntPtr.Zero;
                IntPtr ptr = Marshal.AllocHGlobal(bytes); Allocations.Add(ptr); Allocated++;
                // Deterministic bytes make a missing native write observable.
                for (int i = 0; i < bytes; i++) Marshal.WriteByte(ptr, i, 0);
                return ptr;
            }
            public void Free(IntPtr ptr)
            {
                Calls.Add("free"); if (!Allocations.Remove(ptr)) throw new Exception("Configuration buffer freed twice.");
                Marshal.FreeHGlobal(ptr); Freed++;
            }
            public void Close(IntPtr handle)
            {
                Calls.Add("close"); if (Closed.Contains(handle)) throw new Exception("SCM handle closed twice.");
                Closed.Add(handle);
            }
            internal bool Clean
            {
                get {
                    return Allocations.Count == 0 && Allocated == Freed && Closed.Distinct().Count() == Closed.Count &&
                        (ManagerError != 0 || !Calls.Contains("manager") || Closed.Contains(Manager)) &&
                        (StatusOpenError != 0 || !Calls.Contains("status-open") || Closed.Contains(StatusService)) &&
                        (ConfigOpenError != 0 || !Calls.Contains("config-open") || Closed.Contains(ConfigService));
                }
            }
            public void Dispose()
            { foreach (var ptr in Allocations.ToArray()) Marshal.FreeHGlobal(ptr); Allocations.Clear(); }
        }

        private static bool AgentError(SshAgentServiceError error, SshAgentServiceStage stage, int? code,
            SshAgentServiceErrorKind kind = SshAgentServiceErrorKind.NativeFailure)
        { return error != null && error.Stage == stage && error.Win32Code == code && error.Kind == kind; }

        private static void SshAgentServiceWorkflow()
        {
            using (var native = new AgentServiceNativeFixture { State = 4, Start = 4 }) {
                var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                Check(actual.RuntimeState == SshAgentRuntimeState.Running && actual.StartMode == SshAgentStartMode.Disabled &&
                    actual.RawRuntimeState == 4 && actual.RawStartMode == 4 && actual.StatusError == null && actual.ConfigurationError == null && native.Clean,
                    "SCM running agent and disabled startup policy remain independent facts with owned cleanup");
                Check(actual.ToDisplay().Contains("Running") && actual.ToDisplay().Contains("Disabled") &&
                    !actual.ToDisplay().Contains("аутентифицирован") && !actual.ToDisplay().Contains("нужный ключ готов"),
                    "agent display includes both running state and disabled policy without claiming selected-key authentication");
                Check(native.ManagerRights.SequenceEqual(new uint[] { 1 }) && native.ServiceRights.SequenceEqual(new uint[] { 4, 1 }) &&
                    native.Calls.Count(s => s == "status-query") == 1 && native.Calls.Count(s => s == "config-probe") == 1 && native.Calls.Count(s => s == "config-read") == 1,
                    "agent inspection requests only SCM connect and separate query-status query-config rights");
            }
            using (var native = new AgentServiceNativeFixture { State = 1, Start = 3 }) {
                var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                Check(actual.RuntimeState == SshAgentRuntimeState.Stopped && actual.StartMode == SshAgentStartMode.Manual && native.Clean,
                    "SCM stopped agent with manual startup remains a valid read-only result");
                Check(actual.ToDisplay().Contains("Stopped") && actual.ToDisplay().Contains("Manual") &&
                    !actual.ToDisplay().Contains("требует прав администратора") && !actual.ToDisplay().Contains("требует согласия владельца и прав администратора"),
                    "stopped manual agent does not invent a requirement for administrator rights");
            }
            bool states = true;
            foreach (uint state in new uint[] { 2, 3, 5, 6, 7 }) using (var native = new AgentServiceNativeFixture { State = state }) {
                var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                states &= (uint)actual.RuntimeState == state && actual.RawRuntimeState == state && actual.StatusError == null && native.Clean;
            }
            Check(states, "SCM preserves each start stop continue pause pending and paused runtime state");
            bool modes = true;
            foreach (uint mode in new uint[] { 0, 1, 2, 3, 4 }) using (var native = new AgentServiceNativeFixture { Start = mode }) {
                var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                modes &= (int)actual.StartMode == mode && actual.RawStartMode == mode && actual.ConfigurationError == null && native.Clean;
            }
            Check(modes, "SCM startup policies boot system automatic manual and disabled retain native values");
            using (var native = new AgentServiceNativeFixture { State = 99, Start = 99 }) {
                var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                Check(actual.RuntimeState == SshAgentRuntimeState.Unknown && actual.StartMode == SshAgentStartMode.Unknown &&
                    actual.RawRuntimeState == 99 && actual.RawStartMode == 99 &&
                    AgentError(actual.StatusError, SshAgentServiceStage.QueryStatus, null, SshAgentServiceErrorKind.InvalidData) &&
                    AgentError(actual.ConfigurationError, SshAgentServiceStage.QueryConfig, null, SshAgentServiceErrorKind.InvalidData) && native.Clean,
                    "unrecognised SCM values stay unknown with separate invalid-data errors and no invented native code");
            }
            AgentServiceFailures();
            AgentServiceCancellation();
            AgentServiceNativeOracle();
        }

        private static void AgentServiceFailures()
        {
            using (var native = new AgentServiceNativeFixture { ManagerError = 5 }) {
                var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                Check(AgentError(actual.StatusError, SshAgentServiceStage.OpenManager, 5) &&
                    AgentError(actual.ConfigurationError, SshAgentServiceStage.OpenManager, 5) && actual.RawRuntimeState == null && actual.RawStartMode == null &&
                    native.ServiceRights.Count == 0 && native.Clean,
                    "SCM access denial keeps exact manager stage and Win32 code without service access");
            }
            using (var native = new AgentServiceNativeFixture { StatusOpenError = 1060, ConfigOpenError = 1060 }) {
                var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                Check(actual.RuntimeState == SshAgentRuntimeState.Missing &&
                    AgentError(actual.StatusError, SshAgentServiceStage.OpenStatusService, 1060) && native.Clean && native.Allocated == 0,
                    "missing ssh-agent is distinguished from access denial and does not allocate a configuration buffer");
            }
            using (var native = new AgentServiceNativeFixture { StatusOpenError = 5, Start = 2 }) {
                var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                Check(actual.RuntimeState == SshAgentRuntimeState.Unknown && actual.StartMode == SshAgentStartMode.Automatic &&
                    actual.RawRuntimeState == null && AgentError(actual.StatusError, SshAgentServiceStage.OpenStatusService, 5) && actual.ConfigurationError == null && native.Clean,
                    "denied runtime query preserves separately readable startup policy");
            }
            using (var native = new AgentServiceNativeFixture { StatusQueryError = 1722, Start = 3 }) {
                var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                Check(actual.RuntimeState == SshAgentRuntimeState.Unknown && actual.StartMode == SshAgentStartMode.Manual &&
                    AgentError(actual.StatusError, SshAgentServiceStage.QueryStatus, 1722) && native.Clean,
                    "runtime RPC error retains its query stage while independent configuration remains available");
            }
            using (var native = new AgentServiceNativeFixture { ConfigOpenError = 5 }) {
                var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                Check(actual.RuntimeState == SshAgentRuntimeState.Running && actual.StatusError == null && actual.StartMode == SshAgentStartMode.Unknown &&
                    actual.RawStartMode == null && AgentError(actual.ConfigurationError, SshAgentServiceStage.OpenConfigService, 5) && native.Clean,
                    "denied configuration access cannot erase a confirmed running agent");
            }
            using (var native = new AgentServiceNativeFixture { ConfigProbeError = 5 }) {
                var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                Check(actual.RuntimeState == SshAgentRuntimeState.Running && AgentError(actual.ConfigurationError, SshAgentServiceStage.QueryConfigSize, 5) &&
                    native.Allocated == 0 && native.Clean, "configuration size probe preserves access error rather than trusting its supplied size");
            }
            using (var native = new AgentServiceNativeFixture { ProbeSuccess = true, ConfigProbeError = 0 }) {
                var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                Check(AgentError(actual.ConfigurationError, SshAgentServiceStage.QueryConfigSize, null, SshAgentServiceErrorKind.InvalidData) &&
                    native.Allocated == 0 && native.Clean, "unexpected successful zero-buffer probe is invalid data without allocating");
            }
            foreach (uint bytes in new uint[] { 0, (uint)SshAgentServiceStatus.MinimumConfigurationBytes - 1, 8193 })
                using (var native = new AgentServiceNativeFixture { ProbeBytes = bytes }) {
                    var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                    Check(actual.StartMode == SshAgentStartMode.Unknown &&
                        AgentError(actual.ConfigurationError, SshAgentServiceStage.QueryConfigSize, null, SshAgentServiceErrorKind.InvalidData) && native.Allocated == 0 && native.Clean,
                        "configuration probe rejects undersized or over-limit native allocation: " + bytes);
                }
            using (var native = new AgentServiceNativeFixture { ProbeBytes = 8192 }) {
                var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                Check(actual.StartMode == SshAgentStartMode.Manual && actual.ConfigurationError == null && native.Allocated == 1 && native.Freed == 1 && native.Clean,
                    "configuration accepts the documented 8192-byte boundary and frees it exactly once");
            }
            using (var native = new AgentServiceNativeFixture { ConfigReadError = 122, ReadBytes = 8193 }) {
                var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                Check(actual.StartMode == SshAgentStartMode.Unknown && AgentError(actual.ConfigurationError, SshAgentServiceStage.QueryConfig, 122) &&
                    native.Calls.Count(s => s == "config-read") == 1 && native.Allocated == 1 && native.Clean,
                    "configuration growth error keeps exact native code and never starts an unbounded realloc loop");
            }
            bool successSizeIndependent = true;
            foreach (uint bytes in new uint[] { 0, (uint)SshAgentServiceStatus.MinimumConfigurationBytes - 1, 8193 })
                using (var native = new AgentServiceNativeFixture { ReadBytes = bytes }) {
                    var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                    // Microsoft documents pcbBytesNeeded only on failure with
                    // ERROR_INSUFFICIENT_BUFFER; its success value is not data.
                    successSizeIndependent &= actual.StartMode == SshAgentStartMode.Manual && actual.ConfigurationError == null && native.Clean;
                }
            Check(successSizeIndependent, "successful native configuration relies on its valid buffer rather than undefined bytes-needed output");
            using (var native = new AgentServiceNativeFixture { LeaveStartUnwritten = true }) {
                var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                Check(actual.StartMode == SshAgentStartMode.Unknown &&
                    AgentError(actual.ConfigurationError, SshAgentServiceStage.QueryConfig, null, SshAgentServiceErrorKind.InvalidData) && native.Clean,
                    "native success without writing startup type cannot turn uninitialised memory into Boot policy");
            }
            foreach (string stage in new[] { "status-query", "config-probe", "allocate", "config-read" })
                using (var native = new AgentServiceNativeFixture { ThrowAt = stage }) {
                    var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                    var error = stage == "status-query" ? actual.StatusError : actual.ConfigurationError;
                    var expected = stage == "status-query" ? SshAgentServiceStage.QueryStatus : stage == "config-probe" ? SshAgentServiceStage.QueryConfigSize :
                        stage == "allocate" ? SshAgentServiceStage.AllocateConfiguration : SshAgentServiceStage.QueryConfig;
                    Check(AgentError(error, expected, null, SshAgentServiceErrorKind.Unavailable) &&
                        !actual.ToDisplay().Contains("PRIVATE_NATIVE_FIXTURE_FAILURE") && native.Clean,
                        "SCM pipeline releases every acquired handle and buffer after a managed boundary failure: " + stage);
                }
            using (var native = new AgentServiceNativeFixture { NullAllocation = true }) {
                var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                Check(actual.StartMode == SshAgentStartMode.Unknown && actual.ConfigurationError != null && native.Allocated == 0 && native.Clean,
                    "failed configuration allocation leaves typed unknown policy and settled handle ownership");
            }
        }

        private static void AgentServiceCancellation()
        {
            using (var cancellation = new CancellationTokenSource())
            using (var native = new AgentServiceNativeFixture()) {
                cancellation.Cancel(); bool cancelled = false;
                try { SshAgentServiceStatus.Read(cancellation.Token, native); } catch (OperationCanceledException) { cancelled = true; }
                Check(cancelled && native.Calls.Count == 0 && native.Clean, "cancelled agent query performs no native call or allocation");
            }
            foreach (string stage in new[] { "manager", "status-open", "status-query", "config-open", "config-probe", "allocate", "config-read" })
                using (var cancellation = new CancellationTokenSource())
                using (var native = new AgentServiceNativeFixture()) {
                    native.After = current => { if (current == stage) cancellation.Cancel(); };
                    bool cancelled = false;
                    try { SshAgentServiceStatus.Read(cancellation.Token, native); } catch (OperationCanceledException) { cancelled = true; }
                    int at = native.Calls.IndexOf(stage);
                    Check(cancelled && at >= 0 && native.Calls.Skip(at + 1).All(s => s == "free" || s == "close") && native.Clean,
                        "cancellation after native boundary stops later query work and settles acquired ownership: " + stage);
                }
            using (var native = new AgentServiceNativeFixture()) {
                bool clean = true;
                for (int i = 0; i < 24; i++) {
                    native.Calls.Clear(); native.Closed.Clear();
                    var actual = SshAgentServiceStatus.Read(CancellationToken.None, native);
                    clean &= actual.RuntimeState == SshAgentRuntimeState.Running && actual.StartMode == SshAgentStartMode.Manual && native.Clean;
                }
                Check(clean && native.Allocated == 24 && native.Freed == 24,
                    "repeated agent inspection releases every native configuration buffer and service handle");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AgentOracleStatus { internal uint Type, State, Controls, ExitCode, ServiceExitCode, Checkpoint, WaitHint; }
        [StructLayout(LayoutKind.Sequential)]
        private struct AgentOracleConfiguration
        {
            internal uint Type, Start, ErrorControl;
            internal IntPtr Binary, Group;
            internal uint Tag;
            internal IntPtr Dependencies, Account, Display;
        }
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "OpenSCManagerW")]
        private static extern IntPtr AgentOracleOpenManager(string machine, string database, uint access);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "OpenServiceW")]
        private static extern IntPtr AgentOracleOpenService(IntPtr manager, string name, uint access);
        [DllImport("advapi32.dll", SetLastError = true, EntryPoint = "QueryServiceStatus")]
        private static extern bool AgentOracleQueryStatus(IntPtr service, out AgentOracleStatus status);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "QueryServiceConfigW")]
        private static extern bool AgentOracleQueryConfig(IntPtr service, IntPtr buffer, uint size, out uint needed);
        [DllImport("advapi32.dll", EntryPoint = "CloseServiceHandle")]
        private static extern bool AgentOracleClose(IntPtr handle);

        private static void AgentServiceNativeOracle()
        {
            // Read the isolated CI service without starting it or changing policy.
            // Unlike production, this independent oracle uses one fixed maximum
            // buffer and a complete native struct, not its size-probe/parser path.
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") return;
            IntPtr manager = IntPtr.Zero, statusService = IntPtr.Zero, configService = IntPtr.Zero, buffer = IntPtr.Zero;
            uint state = 0, start = UInt32.MaxValue; int managerError = 0, statusError = 0, configError = 0;
            bool statusKnown = false, configKnown = false;
            var statusStage = SshAgentServiceStage.OpenStatusService;
            try {
                manager = AgentOracleOpenManager(null, null, 1); managerError = manager == IntPtr.Zero ? Marshal.GetLastWin32Error() : 0;
                if (manager != IntPtr.Zero) {
                    statusService = AgentOracleOpenService(manager, "ssh-agent", 4);
                    if (statusService == IntPtr.Zero) statusError = Marshal.GetLastWin32Error();
                    else {
                        statusStage = SshAgentServiceStage.QueryStatus;
                        AgentOracleStatus status;
                        statusKnown = AgentOracleQueryStatus(statusService, out status);
                        if (statusKnown) state = status.State; else statusError = Marshal.GetLastWin32Error();
                    }
                    configService = AgentOracleOpenService(manager, "ssh-agent", 1);
                    if (configService == IntPtr.Zero) configError = Marshal.GetLastWin32Error();
                    else {
                        buffer = Marshal.AllocHGlobal(8192); uint needed;
                        configKnown = AgentOracleQueryConfig(configService, buffer, 8192, out needed);
                        if (configKnown) start = ((AgentOracleConfiguration)Marshal.PtrToStructure(buffer, typeof(AgentOracleConfiguration))).Start;
                        else configError = Marshal.GetLastWin32Error();
                    }
                }
            } finally {
                if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
                if (configService != IntPtr.Zero) AgentOracleClose(configService);
                if (statusService != IntPtr.Zero) AgentOracleClose(statusService);
                if (manager != IntPtr.Zero) AgentOracleClose(manager);
            }
            var actual = SshAgentServiceStatus.Read(CancellationToken.None);
            bool same;
            if (managerError != 0) same = AgentError(actual.StatusError, SshAgentServiceStage.OpenManager, managerError) &&
                AgentError(actual.ConfigurationError, SshAgentServiceStage.OpenManager, managerError);
            else {
                bool sameStatus = statusKnown ? actual.StatusError == null && actual.RawRuntimeState == state :
                    actual.RawRuntimeState == null && AgentError(actual.StatusError, statusStage, statusError);
                bool sameConfig = configKnown ? actual.ConfigurationError == null && actual.RawStartMode == start :
                    actual.RawStartMode == null && actual.ConfigurationError != null && actual.ConfigurationError.Win32Code == configError;
                same = sameStatus && sameConfig;
            }
            Check(same,
                "production agent snapshot agrees with independent read-only native SCM runtime and startup oracle");
            Console.WriteLine("INFO: read-only SCM oracle runtime=" + state + "; startup=" + start + "; statusError=" + statusError + "; configError=" + configError);
        }
    }
}
