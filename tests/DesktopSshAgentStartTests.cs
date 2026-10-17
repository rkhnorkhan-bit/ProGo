using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private sealed class AgentStartNativeFixture : ISshAgentServiceStartNative, IDisposable
        {
            internal readonly AgentServiceNativeFixture Read = new AgentServiceNativeFixture { State = 1 };
            internal readonly List<string> Calls = new List<string>();
            internal readonly List<uint> ManagerRights = new List<uint>(), StartRights = new List<uint>();
            internal Action<string> After;
            internal string ThrowAt;
            internal uint BeforeState = 1, AfterState = 4;
            internal int OpenManagerError, OpenServiceError, BeforeError, AfterError, StartError, Starts, Queries;
            private int managers;
            private bool managerOwned, serviceOwned, managerClosed, serviceClosed;
            private readonly IntPtr manager = new IntPtr(401), service = new IntPtr(501);
            internal AgentStartNativeFixture() { Read.After = s => Called("read-" + s); }
            private void Called(string stage)
            {
                Calls.Add(stage);
                if (ThrowAt == stage) throw new InvalidOperationException("PRIVATE_START_BOUNDARY_FAILURE");
                if (After != null) After(stage);
            }
            public IntPtr OpenManager(uint access, out int error)
            {
                ManagerRights.Add(access);
                if (++managers == 1) return Read.OpenManager(access, out error);
                error = OpenManagerError; Called("start-manager");
                managerOwned = error == 0; return managerOwned ? manager : IntPtr.Zero;
            }
            public IntPtr OpenService(IntPtr owner, string name, uint access, out int error)
            {
                if (owner != manager) return Read.OpenService(owner, name, access, out error);
                if (name != "ssh-agent") throw new Exception("Wrong service selection.");
                StartRights.Add(access); error = OpenServiceError; Called("start-open");
                serviceOwned = error == 0; return serviceOwned ? service : IntPtr.Zero;
            }
            public bool QueryStatus(IntPtr handle, out uint state, out int error)
            {
                if (handle != service) return Read.QueryStatus(handle, out state, out error);
                bool before = ++Queries == 1;
                state = before ? BeforeState : AfterState; error = before ? BeforeError : AfterError;
                Called(before ? "before-query" : "after-query"); return error == 0;
            }
            public bool QueryConfig(IntPtr handle, IntPtr ptr, uint size, out uint needed, out int error)
            { return Read.QueryConfig(handle, ptr, size, out needed, out error); }
            public IntPtr Allocate(int bytes) { return Read.Allocate(bytes); }
            public void Free(IntPtr ptr) { Read.Free(ptr); }
            public void Close(IntPtr handle)
            {
                if (handle == service) { if (serviceClosed) throw new Exception("Double start handle close."); serviceClosed = true; }
                else if (handle == manager) { if (managerClosed) throw new Exception("Double manager close."); managerClosed = true; }
                else Read.Close(handle);
            }
            public bool Start(IntPtr handle, out int error)
            {
                if (handle != service) throw new Exception("Wrong start handle.");
                Starts++; error = StartError; Called("start"); return error == 0;
            }
            internal bool Clean { get { return Read.Clean && (!managerOwned || managerClosed) && (!serviceOwned || serviceClosed); } }
            public void Dispose() { Read.Dispose(); }
        }

        private static SshAgentServiceStartResult AgentStart(AgentStartNativeFixture native)
        { return SshAgentServiceStart.TryStart(CancellationToken.None, native); }
        private static void SshAgentStartWorkflow()
        {
            Console.WriteLine("SSH-agent start checks BEGIN");
            int baseline = passed;
            AgentStartDecisions();
            AgentStartFailures();
            AgentStartCancellation();
            AgentStartOwnership();
            int error;
            bool accepted = new SshAgentServiceStart.WindowsNative().Start(IntPtr.Zero, out error);
            Check(!accepted && error == 6, "native StartService binding rejects a null handle without opening or changing any service; actual Win32=" + error);
            Console.WriteLine("SSH-agent start checks PASS: " + (passed - baseline));
        }
        private static void AgentStartDecisions()
        {
            foreach (uint state in new uint[] { 4, 2, 3, 5, 6, 7 }) using (var native = new AgentStartNativeFixture()) {
                native.Read.State = state;
                var result = AgentStart(native);
                Check(!result.StartInvoked && native.Starts == 0 && native.StartRights.Count == 0 && native.Clean &&
                    result.Snapshot.RawRuntimeState == state && result.State == (state == 4 ? SshAgentStartState.Running :
                        state == 2 ? SshAgentStartState.StartPending : SshAgentStartState.NotRunning),
                    "agent start skips an already running or transitioning/paused service without mutation rights: " + state);
            }
            using (var native = new AgentStartNativeFixture()) {
                native.Read.Start = 4; var result = AgentStart(native);
                Check(result.State == SshAgentStartState.Disabled && !result.StartInvoked && native.StartRights.Count == 0 && native.Clean,
                    "disabled stopped service is reported without changing startup policy or requesting start rights");
            }
            using (var native = new AgentStartNativeFixture()) {
                native.Read.State = native.Read.Start = 4; var result = AgentStart(native);
                Check(result.State == SshAgentStartState.Running && result.Snapshot.StartMode == SshAgentStartMode.Disabled && native.Starts == 0 && native.Clean,
                    "running service is usable even when its future startup policy is disabled");
            }
            foreach (uint mode in new uint[] { 2, 3 }) using (var native = new AgentStartNativeFixture()) {
                native.Read.Start = mode; var result = AgentStart(native);
                Check(result.State == SshAgentStartState.Running && result.StartInvoked && result.StartAccepted && native.Starts == 1 && native.Queries == 2 &&
                    result.Snapshot.RuntimeState == SshAgentRuntimeState.Running && (uint)result.Snapshot.StartMode == mode && native.Clean &&
                    native.ManagerRights.SequenceEqual(new uint[] { 1, 1 }) && native.Read.ServiceRights.SequenceEqual(new uint[] { 4, 1 }) &&
                    native.StartRights.SequenceEqual(new uint[] { 0x14 }), "agent starts once with only connect/start/query rights and verifies the resulting state: " + mode);
            }
            using (var native = new AgentStartNativeFixture()) {
                native.Read.ConfigOpenError = 5; var result = AgentStart(native);
                Check(result.State == SshAgentStartState.Running && native.Starts == 1 && result.Snapshot.StartMode == SshAgentStartMode.Unknown &&
                    result.Snapshot.ConfigurationError.Win32Code == 5 && native.Clean, "unreadable startup policy does not invent a missing SERVICE_START permission");
            }
            foreach (uint state in new uint[] { 4, 2, 3, 7 }) using (var native = new AgentStartNativeFixture { BeforeState = state }) {
                var result = AgentStart(native);
                Check(native.Starts == 0 && !result.StartInvoked && result.Snapshot.RawRuntimeState == state && native.Queries == 1 && native.Clean,
                    "same-handle recheck prevents a duplicate or inappropriate start after an external state change: " + state);
            }
            using (var native = new AgentStartNativeFixture { AfterState = 2 }) {
                var result = AgentStart(native);
                Check(result.State == SshAgentStartState.StartPending && result.StartAccepted && native.Starts == 1 && native.Clean &&
                    result.ToDisplay().Contains("ещё выполняется") && result.ToDisplay().Contains("не подтверждает готовность"),
                    "accepted start with pending status is not reported as a ready service or selected key");
            }
            using (var native = new AgentStartNativeFixture { AfterState = 1 }) {
                var result = AgentStart(native);
                Check(result.State == SshAgentStartState.NotRunning && result.StartAccepted && result.Snapshot.RuntimeState == SshAgentRuntimeState.Stopped && native.Clean,
                    "a service that stops after an accepted request is not declared running");
            }
        }
        private static void AgentStartFailures()
        {
            using (var native = new AgentStartNativeFixture()) {
                native.Read.StatusOpenError = 1060; var result = AgentStart(native);
                Check(result.State == SshAgentStartState.Missing && native.Starts == 0 && native.StartRights.Count == 0 &&
                    AgentError(result.Error, SshAgentServiceStage.OpenStatusService, 1060) && native.Clean, "missing service retains its actual read failure and receives no start request");
            }
            using (var native = new AgentStartNativeFixture()) {
                native.Read.StatusOpenError = 5; var result = AgentStart(native);
                Check(result.State == SshAgentStartState.AccessDenied && native.Starts == 0 && native.Clean &&
                    AgentError(result.Error, SshAgentServiceStage.OpenStatusService, 5), "status access denial is distinguished before any start rights are requested");
            }
            using (var native = new AgentStartNativeFixture()) {
                native.Read.State = 99; var result = AgentStart(native);
                Check(result.State == SshAgentStartState.Unavailable && native.Starts == 0 && native.Clean &&
                    AgentError(result.Error, SshAgentServiceStage.QueryStatus, null, SshAgentServiceErrorKind.InvalidData), "unknown initial native state cannot authorize a start");
            }
            using (var native = new AgentStartNativeFixture { OpenManagerError = 1722 }) {
                var result = AgentStart(native);
                Check(result.State == SshAgentStartState.Failed && AgentError(result.Error, SshAgentServiceStage.OpenStartManager, 1722) && native.Starts == 0 && native.Clean,
                    "start manager RPC failure retains its precise stage and Win32 code");
            }
            using (var native = new AgentStartNativeFixture { OpenServiceError = 5 }) {
                var result = AgentStart(native);
                Check(result.State == SshAgentStartState.AccessDenied && AgentError(result.Error, SshAgentServiceStage.OpenStartService, 5) && native.Starts == 0 && native.Clean,
                    "denied start handle does not elevate or retry with broader access rights");
            }
            using (var native = new AgentStartNativeFixture { BeforeError = 5 }) {
                var result = AgentStart(native);
                Check(result.State == SshAgentStartState.AccessDenied && AgentError(result.Error, SshAgentServiceStage.QueryBeforeStart, 5) && native.Starts == 0 && native.Clean,
                    "failed same-handle preflight prevents StartService and closes all owned handles");
            }
            foreach (int error in new int[] { 5, 1058, 1060, 1722, 1053 }) using (var native = new AgentStartNativeFixture { StartError = error }) {
                var result = AgentStart(native);
                var expected = error == 5 ? SshAgentStartState.AccessDenied : error == 1058 ? SshAgentStartState.Disabled :
                    error == 1060 ? SshAgentStartState.Missing : SshAgentStartState.Failed;
                Check(result.State == expected && result.StartInvoked && !result.StartAccepted && native.Starts == 1 && native.Queries == 1 && native.Clean &&
                    AgentError(result.Error, SshAgentServiceStage.StartService, error) && result.ToDisplay().Contains("Win32 " + error),
                    "StartService failure is classified without UAC, policy changes, retries or fabricated readiness: " + error);
            }
            foreach (uint state in new uint[] { 4, 2, 1 }) using (var native = new AgentStartNativeFixture { StartError = 1056, AfterState = state }) {
                var result = AgentStart(native);
                Check(result.State == (state == 4 ? SshAgentStartState.Running : state == 2 ? SshAgentStartState.StartPending : SshAgentStartState.NotRunning) &&
                    !result.StartAccepted && native.Starts == 1 && native.Queries == 2 && native.Clean && AgentError(result.Error, SshAgentServiceStage.StartService, 1056),
                    "already-running race is classified from fresh status rather than the error name: " + state);
            }
            using (var native = new AgentStartNativeFixture { AfterError = 1722 }) {
                var result = AgentStart(native);
                Check(result.State == SshAgentStartState.Failed && result.StartAccepted && result.Snapshot.RuntimeState == SshAgentRuntimeState.Unknown && native.Clean &&
                    AgentError(result.Error, SshAgentServiceStage.QueryAfterStart, 1722), "failed post-start query preserves accepted-request fact without a stale running claim");
            }
            using (var native = new AgentStartNativeFixture { AfterState = 99 }) {
                var result = AgentStart(native);
                Check(result.State == SshAgentStartState.Unavailable && result.StartAccepted && native.Clean &&
                    AgentError(result.Error, SshAgentServiceStage.QueryAfterStart, null, SshAgentServiceErrorKind.InvalidData), "invalid post-start state remains unknown without an invented Win32 code");
            }
            string[] phases = { "start-manager", "start-open", "before-query", "start", "after-query" };
            SshAgentServiceStage[] stages = { SshAgentServiceStage.OpenStartManager, SshAgentServiceStage.OpenStartService,
                SshAgentServiceStage.QueryBeforeStart, SshAgentServiceStage.StartService, SshAgentServiceStage.QueryAfterStart };
            for (int i = 0; i < phases.Length; i++) using (var native = new AgentStartNativeFixture { ThrowAt = phases[i] }) {
                var result = AgentStart(native);
                Check(result.State == SshAgentStartState.Unavailable && native.Clean && !result.ToDisplay().Contains("PRIVATE_") &&
                    AgentError(result.Error, stages[i], null, SshAgentServiceErrorKind.Unavailable), "unexpected native start failure is safely typed and cleaned: " + phases[i]);
            }
        }
        private static void AgentStartCancellation()
        {
            using (var native = new AgentStartNativeFixture()) using (var source = new CancellationTokenSource()) {
                source.Cancel(); bool cancelled = false;
                try { SshAgentServiceStart.TryStart(source.Token, native); } catch (OperationCanceledException) { cancelled = true; }
                Check(cancelled && native.Calls.Count == 0 && native.ManagerRights.Count == 0 && native.Clean, "pre-cancelled service start makes no native call");
            }
            foreach (string phase in new string[] { "read-manager", "read-status-query", "read-config-read", "start-manager", "start-open", "before-query", "start", "after-query" })
            using (var native = new AgentStartNativeFixture()) using (var source = new CancellationTokenSource()) {
                native.After = s => { if (s == phase) source.Cancel(); }; bool cancelled = false;
                try { SshAgentServiceStart.TryStart(source.Token, native); } catch (OperationCanceledException) { cancelled = true; }
                bool mayStart = phase == "start" || phase == "after-query";
                Check(cancelled && native.Clean && native.Starts == (mayStart ? 1 : 0) &&
                    (phase != "start" || native.Queries == 1), "cancellation stops subsequent service actions and retains cleanup at boundary: " + phase);
            }
        }
        private static void AgentStartOwnership()
        {
            using (var fixture = new AgentProbeFixture()) {
                var reading = fixture.Probe.ReadAsync(10000, CancellationToken.None);
                Check(fixture.Entered.Wait(3000), "existing read owns the shared gate before a competing service start");
                var start = fixture.Probe.StartAsync(1000, CancellationToken.None).GetAwaiter().GetResult();
                Check(start.State == SshAgentProbeState.Busy && fixture.Calls == 1 && !fixture.NativeToken.IsCancellationRequested,
                    "start cannot overlap or cancel an existing read owner");
                fixture.Release.Set(); AgentProbeSettled(reading);
            }
            AgentStartHeld(false);
            AgentStartHeld(true);
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) using (var source = new CancellationTokenSource()) {
                AgentStartNativeFixture observed = null; CancellationToken workerToken = CancellationToken.None;
                var probe = new SshAgentServiceProbe(token => null, token => {
                    workerToken = token; using (var native = new AgentStartNativeFixture()) {
                        observed = native; native.After = s => { if (s == "read-config-read") { entered.Set(); if (!release.Wait(10000)) throw new TimeoutException(); } };
                        return SshAgentServiceStart.TryStart(token, native);
                    }
                });
                try {
                    var start = probe.StartAsync(10000, source.Token);
                    Check(entered.Wait(3000), "startup service operation can be held before its first mutating native call");
                    source.Cancel();
                    Check(workerToken.IsCancellationRequested && !probe.PendingWork.IsCompleted,
                        "caller cancellation reaches the native token synchronously while its worker remains owned");
                    AgentProbeSettled(start); bool cancelled = false;
                    try { start.GetAwaiter().GetResult(); } catch (OperationCanceledException) { cancelled = true; }
                    release.Set(); AgentProbeSettled(probe.PendingWork);
                    Check(cancelled && observed.Starts == 0 && observed.Clean && !probe.PendingWork.IsFaulted,
                        "closing the caller during read prevents a late StartService after native read release");
                } finally { release.Set(); AgentProbeSettled(probe.PendingWork); }
            }
            int reads = 0;
            var readOnly = new SshAgentServiceProbe(token => { reads++; return new SshAgentServiceSnapshot(); });
            Check(readOnly.StartAsync(3000, CancellationToken.None).GetAwaiter().GetResult().State == SshAgentProbeState.Unavailable && reads == 0,
                "a probe without start capability never falls back to read or claims a successful start");
            bool budgets = true;
            foreach (int budget in new int[] { -1, 0, 30001 }) {
                bool rejected = false;
                try { readOnly.StartAsync(budget, CancellationToken.None).GetAwaiter().GetResult(); }
                catch (ArgumentOutOfRangeException) { rejected = true; }
                budgets &= rejected;
            }
            using (var cancelled = new CancellationTokenSource()) {
                cancelled.Cancel(); bool rejected = false;
                try { readOnly.StartAsync(3000, cancelled.Token).GetAwaiter().GetResult(); }
                catch (OperationCanceledException) { rejected = true; }
                Check(budgets && rejected && reads == 0 && readOnly.PendingWork.IsCompleted,
                    "start rejects invalid budgets and pre-cancelled callers before scheduling any native worker");
            }
            var previous = SynchronizationContext.Current; var context = new AgentProbeNoDispatchContext();
            int starts = 0;
            var independent = new SshAgentServiceProbe(token => { reads++; return new SshAgentServiceSnapshot(); }, token => {
                starts++; using (var native = new AgentStartNativeFixture()) return SshAgentServiceStart.TryStart(token, native);
            });
            try {
                SynchronizationContext.SetSynchronizationContext(context);
                var start = independent.StartAsync(3000, CancellationToken.None); AgentProbeSettled(start);
                var result = start.GetAwaiter().GetResult();
                Check(result.State == SshAgentProbeState.Completed && result.StartAttempt.State == SshAgentStartState.Running && starts == 1 && reads == 0 && context.Posts == 0,
                    "successful service start and its waiter settle without dispatching to a blocked caller synchronization context");
            } finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }
        private static void AgentStartHeld(bool inStart)
        {
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) {
                AgentStartNativeFixture observed = null; int calls = 0, reads = 0, worker = 0; bool background = false;
                CancellationToken nativeToken = CancellationToken.None;
                var probe = new SshAgentServiceProbe(token => { reads++; return new SshAgentServiceSnapshot(); }, token => {
                    int call = Interlocked.Increment(ref calls); worker = Thread.CurrentThread.ManagedThreadId; background = Thread.CurrentThread.IsBackground;
                    nativeToken = token;
                    using (var native = new AgentStartNativeFixture()) {
                        if (call == 1) observed = native;
                        native.After = s => { if (call == 1 && s == (inStart ? "start" : "before-query")) {
                            entered.Set(); if (!release.Wait(10000)) throw new TimeoutException("Start fixture release deadline.");
                        } };
                        return SshAgentServiceStart.TryStart(token, native);
                    }
                });
                try {
                    int owner = Thread.CurrentThread.ManagedThreadId;
                    var start = probe.StartAsync(1000, CancellationToken.None);
                    Check(entered.Wait(3000), "owned service start enters its controlled native hold: " + inStart);
                    var raw = probe.PendingWork; AgentProbeSettled(start); var result = start.GetAwaiter().GetResult();
                    Check(result.State == SshAgentProbeState.TimedOut && result.StartAttempt == null && nativeToken.IsCancellationRequested && !raw.IsCompleted &&
                        calls == 1 && worker != owner && background, "bounded start wait leaves native work owned on one background thread: " + inStart);
                    var retry = Task.WhenAll(Enumerable.Range(0, 12).Select(i => i % 2 == 0 ? probe.ReadAsync(1000, CancellationToken.None) : probe.StartAsync(1000, CancellationToken.None)));
                    AgentProbeSettled(retry);
                    Check(retry.GetAwaiter().GetResult().All(r => r.State == SshAgentProbeState.Busy) && calls == 1 && reads == 0 && Object.ReferenceEquals(raw, probe.PendingWork),
                        "timed-out start excludes concurrent read and start retries until actual settlement: " + inStart);
                    release.Set(); AgentProbeSettled(raw);
                    Check(observed.Clean && observed.Starts == (inStart ? 1 : 0) && observed.Queries == 1 && !raw.IsFaulted,
                        "late native return cleans ownership without another start or post-cancel status query: " + inStart);
                    var fresh = probe.StartAsync(3000, CancellationToken.None); AgentProbeSettled(fresh); var completed = fresh.GetAwaiter().GetResult();
                    Check(completed.State == SshAgentProbeState.Completed && completed.StartAttempt.State == SshAgentStartState.Running && calls == 2 && reads == 0 &&
                        completed.ToDisplay().Contains("нужный ключ") && !completed.ToDisplay().Contains("аутентифицирован"),
                        "fresh start after cleanup returns new typed data without treating service state as authentication: " + inStart);
                } finally { release.Set(); AgentProbeSettled(probe.PendingWork); }
            }
        }
    }
}
