using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void AsyncSettingsWorkflow(SettingsService ownerSettings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: asynchronous settings transactions require isolated native CI"); return;
            }
            if (File.Exists(SystemProxyService.BackupPath) || File.Exists(CliProxyEnvironmentService.BackupPath))
                throw new Exception("asynchronous settings fixture is not isolated from existing ownership");
            var original = ownerSettings.Current.Clone();
            try {
                AsyncSettingsConditionalCommit();
                AsyncSettingsColdStart();
                AsyncSettingsColdStartHeartbeat();
                AsyncSettingsExactRollback();
                AsyncSettingsPreservesInterveningEdits();
                AsyncSettingsPreservesChangedOwnershipJournal();
                AsyncSettingsRetainsFailedCleanupEndpoints();
                AsyncSettingsFormHeartbeat();
                AsyncSettingsFormCancel();
                AsyncSettingsFormAcceptedCompletion();
                AsyncSettingsShutdownDeadline();
                AsyncSettingsNativeColdShutdownDeadline();
                AsyncSettingsDisposedSettlement(false);
                AsyncSettingsDisposedSettlement(true);
                AsyncSettingsRestartMismatch();
            } finally { ownerSettings.Save(original); }
        }

        // Fixtures execute real HKCU, file replacement and listener operations.
        // Cleanup deliberately restores the isolated fixture's raw snapshots;
        // it does not use the production transaction to hide rollback failures.
        private sealed class AsyncSettingsFixture : IDisposable
        {
            internal readonly SettingsService Settings;
            internal readonly CliProxyBridgeService Bridge;
            internal readonly UpdateAwareTrayApplicationContext Context;
            internal readonly MainWindow Main;
            private readonly SystemProxyBackup windows;
            private readonly Dictionary<string, WindowsProxyValue> environment;
            private readonly Dictionary<string, byte[]> files;
            private readonly byte[] settingsBytes;
            private readonly System.Net.Sockets.TcpListener socks;
            private readonly ProxyService proxy;
            private readonly Ikev2RelayService relay;
            private readonly HomeVpnService home;
            private readonly ClipboardService clipboard;
            private readonly ConnectionHealthMonitor health;
            internal AsyncSettingsFixture(Action beforeReplace = null, Action<BridgeTransitionPhase> transition = null, bool owner = false,
                int settingsShutdownTimeoutMilliseconds = 3000, Action beforeNativeCleanup = null)
            {
                windows = SystemProxyService.ReadCurrent();
                environment = AsyncEnvironmentValues();
                files = new[] { CliProxyEnvironmentService.BackupPath, SystemProxyService.BackupPath, CodexProxyService.LauncherPath }
                    .ToDictionary(p => p, p => File.Exists(p) ? File.ReadAllBytes(p) : null);
                settingsBytes = File.Exists(AppPaths.SettingsPath) ? File.ReadAllBytes(AppPaths.SettingsPath) : null;
                Settings = new SettingsService(beforeReplace);
                socks = Occupy(0); AnswerFixtureSocks(socks);
                var configured = AppSettings.Defaults(); configured.SshProfile = "fixture";
                configured.SocksHost = "127.0.0.1"; configured.SocksPort = Number(socks);
                configured.AutoCliProxy = configured.AutoSystemProxy = configured.AutoStartSocks = false;
                configured.AutoHttpProxyPort = true; configured.TrayCloseExplained = true;
                using (var unused = new ListenerScope()) configured.HttpProxyPort = unused.Port;
                Settings.Save(configured);
                Bridge = new CliProxyBridgeService(Settings, transition);
                if (owner) {
                    proxy = new ProxyService(() => Settings.Current, s => Settings.Save(s), "unused-test-ssh", () => DateTime.UtcNow, false);
                    relay = new Ikev2RelayService(); home = new HomeVpnService(relay); clipboard = new ClipboardService(Settings);
                    health = new ConnectionHealthMonitor(() => Settings.Current);
                    Context = new UpdateAwareTrayApplicationContext(Settings, proxy, Bridge, home, clipboard, false, health,
                        delegate {
                            if (beforeNativeCleanup != null) beforeNativeCleanup();
                            return SystemProxyService.RestoreOwned();
                        }, null, null, 3000, null, delegate {
                            if (beforeNativeCleanup != null) beforeNativeCleanup();
                            CliProxyEnvironmentService.ClearUserEnvironmentIfOwned();
                        }, null, settingsShutdownTimeoutMilliseconds);
                    Context.RequestShowStatus(); Application.DoEvents(); Main = (MainWindow)Field(Context, "mainWindow");
                }
            }
            internal void Start()
            {
                string message; Check(Bridge.Start(out message), "async settings fixture starts its actual HTTP listener");
            }
            internal void OwnIntegrations()
            {
                CliProxyEnvironmentService.ApplyUserEnvironment(Bridge.Port);
                string message; Check(SystemProxyService.Apply(Settings.Current, out message), "async settings fixture owns real Windows proxy fields");
                Directory.CreateDirectory(Path.GetDirectoryName(CodexProxyService.LauncherPath));
                File.WriteAllText(CodexProxyService.LauncherPath, CodexProxyService.LauncherContent(Bridge.Port), Encoding.ASCII);
            }
            public void Dispose()
            {
                if (Context != null) {
                    Context.Dispose();
                    var pending = Context.SettingsWork;
                    if (pending != null && !pending.Wait(30000)) throw new Exception("async settings fixture did not settle after owner disposal");
                }
                using (var key = Registry.CurrentUser.CreateSubKey("Environment"))
                    foreach (var pair in environment) SystemProxyService.WriteValue(key, pair.Key, pair.Value);
                CliProxyEnvironmentService.BroadcastEnvironmentChange();
                SystemProxyService.RestoreSnapshot(windows);
                foreach (var pair in files) RestoreAsyncFile(pair.Key, pair.Value);
                RestoreAsyncFile(AppPaths.SettingsPath, settingsBytes);
                if (Bridge.CleanupPending) {
                    SettingsSaveError cleanupError = null;
                    var cleanup = Task.Run(() => Bridge.RetryPendingCleanup(out cleanupError));
                    if (!cleanup.Wait(30000) || !cleanup.Result) throw new Exception("isolated fixture retained endpoint cleanup did not settle");
                }
                Bridge.Dispose();
                if (clipboard != null) clipboard.Dispose();
                if (home != null) home.Dispose();
                if (relay != null) relay.Dispose();
                if (health != null) health.Dispose();
                if (proxy != null) proxy.Dispose();
                socks.Stop(); Settings.Dispose();
            }
        }
        private sealed class ListenerScope : IDisposable
        {
            private readonly System.Net.Sockets.TcpListener listener = Occupy(0);
            internal int Port { get { return Number(listener); } }
            public void Dispose() { listener.Stop(); }
        }
        private static void RestoreAsyncFile(string path, byte[] bytes)
        {
            if (bytes == null) { if (File.Exists(path)) File.Delete(path); }
            else { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, bytes); }
        }
        private static Dictionary<string, WindowsProxyValue> AsyncEnvironmentValues()
        {
            using (var key = Registry.CurrentUser.OpenSubKey("Environment"))
                return CliProxyEnvironmentService.Names.ToDictionary(n => n, n => SystemProxyService.ReadValue(key, n));
        }
        private static void AsyncSettingsWait(ManualResetEventSlim signal)
        {
            if (!signal.Wait(20000)) throw new TimeoutException("asynchronous settings release deadline");
        }
        private static void WaitAsyncSettings(Task operation)
        {
            PumpIntegrationUntil(() => operation.IsCompleted); operation.GetAwaiter().GetResult();
        }

        private static void AsyncSettingsConditionalCommit()
        {
            int writes = 0;
            using (var fixture = new AsyncSettingsFixture(() => Interlocked.Increment(ref writes))) {
                var settings = fixture.Settings; var before = settings.Capture();
                var candidate = before.Settings.Clone(); candidate.TestEndpoint = "https://candidate.example.org/check";
                SettingsCommitReceipt receipt;
                Check(settings.TrySave(before.Revision, candidate, out receipt) && receipt != null && settings.Revision == before.Revision + 1,
                    "conditional settings commit performs a real atomic replacement at the captured revision");
                var ownBytes = File.ReadAllBytes(AppPaths.SettingsPath); int ownWrites = Volatile.Read(ref writes);
                SettingsCommitReceipt rejected;
                Check(!settings.TrySave(before.Revision, before.Settings, out rejected) && rejected == null &&
                    Volatile.Read(ref writes) == ownWrites && File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(ownBytes),
                    "stale general settings commit rejects before any temporary write or publication");
                var newer = settings.Current.Clone(); newer.ClipboardClearSeconds = 155; settings.Save(newer);
                var newerBytes = File.ReadAllBytes(AppPaths.SettingsPath); long revision = settings.Revision; ownWrites = Volatile.Read(ref writes);
                Check(!settings.TryRollback(receipt) && settings.Revision == revision && Volatile.Read(ref writes) == ownWrites &&
                    File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(newerBytes) && settings.Current.ClipboardClearSeconds == 155,
                    "conditional rollback preserves a later independently committed preference and exact durable bytes");
                candidate = settings.Current.Clone(); candidate.TestEndpoint = "https://second.example.org/check";
                Check(settings.TrySave(revision, candidate, out receipt) && settings.TryRollback(receipt) &&
                    File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(newerBytes),
                    "fresh receipt can restore only its own complete committed settings change");
            }
        }

        private static void AsyncSettingsColdStart()
        {
            int writes = 0;
            using (var fixture = new AsyncSettingsFixture(() => Interlocked.Increment(ref writes))) {
                var before = fixture.Settings.Capture(); var bytes = File.ReadAllBytes(AppPaths.SettingsPath); int calls = Volatile.Read(ref writes);
                var operation = Task.Run(delegate { string error; return fixture.Bridge.Start(out error); }); WaitAsyncSettings(operation);
                Check(operation.Result && fixture.Bridge.IsRunning && fixture.Settings.Revision == before.Revision && Volatile.Read(ref writes) == calls &&
                    File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(bytes),
                    "cold bridge start at the unchanged actual port performs no settings replacement or revision change");
                AssertBridge(fixture.Bridge.Port);
            }
        }

        private static void AsyncSettingsColdStartHeartbeat()
        {
            int armed = 0, workerThread = 0, activeWrites = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncSettingsFixture(delegate {
                if (Volatile.Read(ref armed) == 0) return;
                Interlocked.Increment(ref activeWrites);
                workerThread = Thread.CurrentThread.ManagedThreadId; entered.Set(); AsyncSettingsWait(release);
            }, null, true))
            using (var occupied = new ListenerScope())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 15 }) {
                var configured = fixture.Settings.Current.Clone(); configured.HttpProxyPort = occupied.Port; fixture.Settings.Save(configured);
                var before = fixture.Settings.Capture(); var bytes = File.ReadAllBytes(AppPaths.SettingsPath);
                int ticks = 0, ownerThread = Thread.CurrentThread.ManagedThreadId; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                Volatile.Write(ref armed, 1); var watch = Stopwatch.StartNew();
                var operation = fixture.Context.EnsureBridgeAsync(CancellationToken.None);
                try {
                    Check(watch.ElapsedMilliseconds < 1000 && !operation.IsCompleted, "cold asynchronous bridge returns before actual blocked durable port replacement");
                    PumpUntil(() => entered.IsSet && ticks >= 3);
                    Check(workerThread != ownerThread && fixture.Context.SettingsPending && !operation.IsCompleted && fixture.Main.Visible &&
                        fixture.Settings.Revision == before.Revision && File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(bytes),
                        "native heartbeat and dashboard remain live while cold fallback port commit waits on the real replacement seam");
                    var duplicate = fixture.Context.EnsureBridgeAsync(CancellationToken.None);
                    Check(duplicate.IsCompleted && duplicate.Result != null && Object.ReferenceEquals(fixture.Context.SettingsWork, operation) &&
                        Volatile.Read(ref activeWrites) == 1 && !operation.IsCompleted,
                        "duplicate cold Ensure is refused without another actual writer or replacing the tracked active settings operation");
                    watch.Restart(); Call(fixture.Context, "ExecuteCommand", AppCommand.StopDesktop);
                    Check(watch.ElapsedMilliseconds < 1000 && !operation.IsCompleted && !fixture.Context.IntegrationWork.IsCompleted,
                        "desktop Stop returns immediately and queues safe cleanup while the actual cold settings writer is still blocked");
                    release.Set(); WaitAsyncSettings(operation);
                    Check(operation.Result == null && fixture.Bridge.Port != occupied.Port &&
                        fixture.Settings.Current.HttpProxyPort == fixture.Bridge.Port && fixture.Settings.Revision == before.Revision + 1,
                        "cold fallback publishes and persists its actual reserved endpoint once the worker has completed");
                    WaitIntegration(fixture.Context);
                    Check(!fixture.Context.SettingsPending && !fixture.Bridge.IsRunning,
                        "queued desktop Stop completes only after cold publication has safely settled");
                    AssertPortReleased(fixture.Settings.Current.HttpProxyPort, "queued desktop Stop releases the actual fallback endpoint");
                } finally { release.Set(); WaitAsyncSettings(operation); }
            }
        }

        private static void AsyncSettingsExactRollback()
        {
            int armed = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncSettingsFixture(null, delegate(BridgeTransitionPhase phase) {
                if (phase != BridgeTransitionPhase.BeforeCommit || Volatile.Read(ref armed) == 0) return;
                entered.Set(); AsyncSettingsWait(release);
            })) {
                fixture.Start(); fixture.OwnIntegrations();
                int oldPort = fixture.Bridge.Port; var before = fixture.Settings.Capture(); var settingsBytes = File.ReadAllBytes(AppPaths.SettingsPath);
                var native = SystemProxyService.ReadCurrent();
                var environment = AsyncEnvironmentValues();
                var files = new[] { CliProxyEnvironmentService.BackupPath, SystemProxyService.BackupPath, CodexProxyService.LauncherPath }.ToDictionary(p => p, File.ReadAllBytes);
                Volatile.Write(ref armed, 1); SettingsSaveError error = null;
                Task<bool> operation;
                using (var denied = new FileStream(AppPaths.SettingsPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    operation = Task.Run(() => fixture.Bridge.ReconfigureDetailed(fixture.Settings.Current.Clone(), true, out error));
                    try { PumpUntil(() => entered.IsSet); release.Set(); WaitAsyncSettings(operation); }
                    finally { release.Set(); WaitAsyncSettings(operation); }
                }
                Check(!operation.Result && error != null && error.Field == SettingsField.SettingsFile && fixture.Settings.Revision == before.Revision &&
                    File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(settingsBytes) && fixture.Bridge.Port == oldPort,
                    "real denied settings replacement rejects the port move without publishing memory or releasing its old listener");
                var actualEnvironment = AsyncEnvironmentValues();
                Check(environment.All(p => p.Value.Matches(actualEnvironment[p.Key])) &&
                    SystemProxyService.FieldNames.All(n => native.Values[n].Matches(SystemProxyService.ReadCurrent().Values[n])) &&
                    files.All(p => File.ReadAllBytes(p.Key).SequenceEqual(p.Value)) && !fixture.Bridge.CleanupPending,
                    "failed actual native move conditionally restores its own environment, typed Windows values and exact file bytes");
                AssertBridge(oldPort);
            }
        }

        private static void AsyncSettingsPreservesInterveningEdits()
        {
            int armed = 0, writes = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncSettingsFixture(() => Interlocked.Increment(ref writes), delegate(BridgeTransitionPhase phase) {
                if (phase != BridgeTransitionPhase.BeforeCommit || Volatile.Read(ref armed) == 0) return;
                entered.Set(); AsyncSettingsWait(release);
            })) {
                fixture.Start(); fixture.OwnIntegrations(); int oldPort = fixture.Bridge.Port;
                var before = fixture.Settings.Capture(); var proposed = before.Settings.Clone(); proposed.TestEndpoint = "https://stale.example.org/check";
                var json = new JavaScriptSerializer();
                var expectedJournals = new[] { CliProxyEnvironmentService.BackupPath, SystemProxyService.BackupPath }
                    .ToDictionary(p => p, p => json.Deserialize<Dictionary<string, object>>(File.ReadAllText(p)));
                SettingsSaveError error = null; Volatile.Write(ref armed, 1);
                var operation = Task.Run(() => fixture.Bridge.ReconfigureDetailed(proposed, true, out error));
                try {
                    PumpUntil(() => entered.IsSet);
                    const string externalProxy = "http://owner-edit.example.org:9017";
                    using (var key = Registry.CurrentUser.CreateSubKey("Environment"))
                        key.SetValue("HTTP_PROXY", externalProxy, RegistryValueKind.ExpandString);
                    var externalEnvironment = AsyncEnvironmentValues()["HTTP_PROXY"];
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings")) {
                        key.SetValue("ProxyServer", "%OWNER_PROXY_HOST%:9443", RegistryValueKind.ExpandString);
                        key.SetValue("ProxyOverride", "%USERPROFILE%;owner.internal", RegistryValueKind.ExpandString);
                    }
                    var externalNative = SystemProxyService.ReadCurrent();
                    var externalLauncher = Encoding.UTF8.GetBytes("@echo external owner launcher\r\n"); File.WriteAllBytes(CodexProxyService.LauncherPath, externalLauncher);
                    foreach (var path in new[] { CliProxyEnvironmentService.BackupPath, SystemProxyService.BackupPath }) {
                        var payload = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(path)); payload["OwnerNote"] = "changed after native move";
                        File.WriteAllBytes(path, Encoding.UTF8.GetBytes(json.Serialize(payload)));
                        expectedJournals[path]["OwnerNote"] = payload["OwnerNote"];
                    }
                    var fresh = Task.Run(() => fixture.Settings.SetAutoRestart(false)); WaitAsyncSettings(fresh);
                    var latest = fixture.Settings.Capture(); var latestBytes = File.ReadAllBytes(AppPaths.SettingsPath); int calls = Volatile.Read(ref writes);
                    release.Set(); WaitAsyncSettings(operation);
                    Check(!operation.Result && error != null && fixture.Settings.Revision == latest.Revision && Volatile.Read(ref writes) == calls &&
                        !fixture.Settings.Current.AutoRestartSocks && fixture.Settings.Current.TestEndpoint == before.Settings.TestEndpoint &&
                        File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(latestBytes),
                        "fresh independent recovery save makes the stale port proposal fail CAS without a stale write or blind settings rollback");
                    var actualNative = SystemProxyService.ReadCurrent();
                    Check(Environment.GetEnvironmentVariable("HTTP_PROXY", EnvironmentVariableTarget.User) == externalProxy &&
                        externalEnvironment.Matches(AsyncEnvironmentValues()["HTTP_PROXY"]) &&
                        externalNative.Values["ProxyServer"].Matches(actualNative.Values["ProxyServer"]) &&
                        externalNative.Values["ProxyOverride"].Matches(actualNative.Values["ProxyOverride"]) &&
                        File.ReadAllBytes(CodexProxyService.LauncherPath).SequenceEqual(externalLauncher) &&
                        expectedJournals.All(p => json.Serialize(p.Value) == json.Serialize(json.Deserialize<Dictionary<string, object>>(File.ReadAllText(p.Key)))),
                        "failed port transition preserves external raw environment/registry values, exact launcher bytes and extra journal metadata while recovering only its own matching markers");
                    Check(Environment.GetEnvironmentVariable("HTTPS_PROXY", EnvironmentVariableTarget.User) == CliProxyBridgeService.UrlFor(oldPort) &&
                        fixture.Bridge.IsRunning && fixture.Bridge.Port == oldPort && !fixture.Bridge.CleanupPending,
                        "conditional rollback aligns recovered ownership journals with unchanged own proxy values and keeps the usable old endpoint");
                    AssertBridge(oldPort);
                } finally { release.Set(); WaitAsyncSettings(operation); }
            }
        }

        private static void AsyncSettingsRetainsFailedCleanupEndpoints()
        {
            int armed = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncSettingsFixture(null, delegate(BridgeTransitionPhase phase) {
                if (phase != BridgeTransitionPhase.BeforeCommit || Volatile.Read(ref armed) == 0) return;
                entered.Set(); AsyncSettingsWait(release);
            })) {
                fixture.Start(); fixture.OwnIntegrations(); int oldPort = fixture.Bridge.Port; SettingsSaveError error = null;
                Volatile.Write(ref armed, 1);
                using (var deniedSettings = new FileStream(AppPaths.SettingsPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    var operation = Task.Run(() => fixture.Bridge.ReconfigureDetailed(fixture.Settings.Current.Clone(), true, out error));
                    try {
                        PumpUntil(() => entered.IsSet);
                        int candidatePort = new Uri(Environment.GetEnvironmentVariable("HTTP_PROXY", EnvironmentVariableTarget.User)).Port;
                        using (var deniedLauncher = new FileStream(CodexProxyService.LauncherPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                            release.Set(); WaitAsyncSettings(operation);
                            Check(!operation.Result && error != null && fixture.Bridge.CleanupPending && fixture.Bridge.Port == oldPort &&
                                fixture.Bridge.RetainedPorts.Contains(candidatePort) && File.ReadAllText(CodexProxyService.LauncherPath).Contains(CliProxyBridgeService.UrlFor(candidatePort)),
                                "actual denied launcher rollback leaves cleanup pending and retains the endpoint still named by the launcher");
                            AssertBridge(oldPort); AssertBridge(candidatePort);
                        }
                        SettingsSaveError cleanupError = null;
                        var cleanup = Task.Run(() => fixture.Bridge.RetryPendingCleanup(out cleanupError)); WaitAsyncSettings(cleanup);
                        Check(cleanup.Result && cleanupError == null && !fixture.Bridge.CleanupPending &&
                            !fixture.Bridge.RetainedPorts.Contains(candidatePort) && fixture.Bridge.Port == oldPort &&
                            File.ReadAllText(CodexProxyService.LauncherPath).Contains(CliProxyBridgeService.UrlFor(oldPort)),
                            "actual cleanup retry after unlocking the launcher restores its own remaining receipt without needing to replace denied settings");
                        AssertPortReleased(candidatePort, "settled cleanup releases only its reserved failed-transition endpoint");
                        AssertBridge(oldPort);
                    } finally { release.Set(); WaitAsyncSettings(operation); }
                }
            }
        }

        private static void AsyncSettingsPreservesChangedOwnershipJournal()
        {
            int armed = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncSettingsFixture(null, delegate(BridgeTransitionPhase phase) {
                if (phase != BridgeTransitionPhase.BeforeCommit || Volatile.Read(ref armed) == 0) return;
                entered.Set(); AsyncSettingsWait(release);
            })) {
                fixture.Start(); fixture.OwnIntegrations(); int oldPort = fixture.Bridge.Port;
                var json = new JavaScriptSerializer(); SettingsSaveError error = null; Volatile.Write(ref armed, 1);
                var operation = Task.Run(() => fixture.Bridge.ReconfigureDetailed(fixture.Settings.Current.Clone(), true, out error));
                try {
                    PumpUntil(() => entered.IsSet);
                    int candidatePort = new Uri(Environment.GetEnvironmentVariable("HTTP_PROXY", EnvironmentVariableTarget.User)).Port;
                    var foreign = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(CliProxyEnvironmentService.BackupPath));
                    foreign["ProGoAppliedPort"] = candidatePort == 31999 ? "32001" : "31999";
                    foreign["OwnerNote"] = "owner changed an ownership marker";
                    var foreignBytes = Encoding.UTF8.GetBytes(json.Serialize(foreign)); File.WriteAllBytes(CliProxyEnvironmentService.BackupPath, foreignBytes);
                    var fresh = Task.Run(() => fixture.Settings.SetAutoRestart(false)); WaitAsyncSettings(fresh);
                    var settingsBytes = File.ReadAllBytes(AppPaths.SettingsPath); long revision = fixture.Settings.Revision;
                    release.Set(); WaitAsyncSettings(operation);
                    Check(!operation.Result && error != null && error.Field == SettingsField.Applications && fixture.Bridge.CleanupPending &&
                        fixture.Bridge.RetainedPorts.Contains(candidatePort) && fixture.Bridge.Port == oldPort &&
                        File.ReadAllBytes(CliProxyEnvironmentService.BackupPath).SequenceEqual(foreignBytes) &&
                        fixture.Settings.Revision == revision && File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(settingsBytes),
                        "external ownership-marker edit remains byte-exact and leaves explicit cleanup pending with both endpoints retained and fresh settings preserved");
                    SettingsSaveError cleanupError = null;
                    var refused = Task.Run(() => fixture.Bridge.RetryPendingCleanup(out cleanupError)); WaitAsyncSettings(refused);
                    Check(!refused.Result && cleanupError != null && fixture.Bridge.CleanupPending &&
                        File.ReadAllBytes(CliProxyEnvironmentService.BackupPath).SequenceEqual(foreignBytes),
                        "repeated cleanup refuses to guess changed journal ownership or overwrite the external marker");
                    // Explicit fixture-owner repair restores only the disputed
                    // marker. Production retry must preserve the added metadata.
                    foreign["ProGoAppliedPort"] = candidatePort.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    File.WriteAllBytes(CliProxyEnvironmentService.BackupPath, Encoding.UTF8.GetBytes(json.Serialize(foreign)));
                    var cleanup = Task.Run(() => fixture.Bridge.RetryPendingCleanup(out cleanupError)); WaitAsyncSettings(cleanup);
                    var recovered = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(CliProxyEnvironmentService.BackupPath));
                    Check(cleanup.Result && cleanupError == null && !fixture.Bridge.CleanupPending &&
                        (string)recovered["ProGoAppliedPort"] == oldPort.ToString(System.Globalization.CultureInfo.InvariantCulture) &&
                        (string)recovered["OwnerNote"] == "owner changed an ownership marker" && !fixture.Settings.Current.AutoRestartSocks,
                        "explicit ownership-marker repair permits own rollback while preserving added journal metadata and the unrelated fresh preference");
                    AssertPortReleased(candidatePort, "recovered journal cleanup releases its retained candidate endpoint"); AssertBridge(oldPort);
                } finally { release.Set(); WaitAsyncSettings(operation); }
            }
        }

        private static void AsyncSettingsDisposedSettlement(bool committed)
        {
            int armed = 0, callbacks = 0, wrongThread = 0, ownerThread = Thread.CurrentThread.ManagedThreadId;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var cancellation = new CancellationTokenSource())
            using (var fixture = new AsyncSettingsFixture(null, delegate(BridgeTransitionPhase phase) {
                if (phase != (committed ? BridgeTransitionPhase.AfterCommit : BridgeTransitionPhase.BeforeCommit) || Volatile.Read(ref armed) == 0) return;
                entered.Set(); AsyncSettingsWait(release);
            }, true)) {
                fixture.Start(); fixture.OwnIntegrations();
                int oldPort = fixture.Bridge.Port; var before = fixture.Settings.Capture(); var bytes = File.ReadAllBytes(AppPaths.SettingsPath);
                var proposed = before.Settings.Clone(); proposed.TestEndpoint = "https://accepted.example.org/check";
                WatchIntegrationCommands(fixture.Context, delegate { callbacks++; if (Thread.CurrentThread.ManagedThreadId != ownerThread) wrongThread++; });
                WatchIntegrationControls(fixture.Main, delegate { callbacks++; if (Thread.CurrentThread.ManagedThreadId != ownerThread) wrongThread++; });
                Volatile.Write(ref armed, 1); var previousContext = SynchronizationContext.Current;
                try {
                    SynchronizationContext.SetSynchronizationContext(null);
                    var watch = Stopwatch.StartNew(); var operation = fixture.Context.ApplySettingsAsync(proposed, true, cancellation.Token);
                    Check(watch.ElapsedMilliseconds < 1000 && !operation.IsCompleted, "settings apply starts asynchronously before its native transition barrier");
                    PumpUntil(() => entered.IsSet); cancellation.Cancel();
                    watch.Restart(); fixture.Context.Dispose(); fixture.Bridge.Dispose(); int afterDispose = callbacks;
                    Check(watch.ElapsedMilliseconds < 1000 && !operation.IsCompleted && fixture.Main.IsDisposed && fixture.Bridge.IsRunning,
                        "forced owner disposal returns immediately and retains the active settings listener lease before actual settlement");
                    release.Set();
                    Check(operation.Wait(30000) && !fixture.Context.SettingsPending && callbacks == afterDispose && wrongThread == 0,
                        "lost settings owner callback settles without a surviving message pump or late control mutation");
                    if (committed) {
                        Check(operation.Result == null && fixture.Settings.Revision == before.Revision + 1 && fixture.Settings.Current.TestEndpoint == proposed.TestEndpoint &&
                            fixture.Bridge.IsRunning && fixture.Bridge.Port == fixture.Settings.Current.HttpProxyPort && fixture.Bridge.Port != oldPort,
                            "late Cancel after durable commit completes accepted endpoint publication even after the owner closes");
                        Check(CliProxyEnvironmentService.IsAppliedToUserEnvironment(fixture.Bridge.Port) && SystemProxyService.IsApplied(fixture.Settings.Current) &&
                            File.Exists(CliProxyEnvironmentService.BackupPath) && File.Exists(SystemProxyService.BackupPath) &&
                            File.ReadAllText(CodexProxyService.LauncherPath).Contains(fixture.Bridge.ProxyUrl),
                            "accepted disposed transition retains its real native routes and journals at the endpoint that still serves them");
                        AssertBridge(fixture.Bridge.Port); AssertPortReleased(oldPort, "accepted disposed transition releases its old unused endpoint");
                    } else {
                        Check(operation.Result != null && fixture.Settings.Revision == before.Revision && File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(bytes) &&
                            fixture.Bridge.IsRunning && fixture.Bridge.Port == oldPort && !fixture.Bridge.CleanupPending,
                            "precommit Cancel after owner closure restores only its own changes and preserves the uncommitted old endpoint and settings");
                        Check(CliProxyEnvironmentService.IsAppliedToUserEnvironment(oldPort) && SystemProxyService.IsApplied(fixture.Settings.Current) &&
                            File.ReadAllText(CodexProxyService.LauncherPath).Contains(CliProxyBridgeService.UrlFor(oldPort)),
                            "cancelled disposed transition restores actual native consumers to the retained old listening endpoint");
                        AssertBridge(oldPort);
                    }
                } finally { release.Set(); SynchronizationContext.SetSynchronizationContext(previousContext); }
            }
        }

        private static void AsyncSettingsFormHeartbeat()
        {
            int armed = 0, calls = 0, workerThread = 0, ownerThread = Thread.CurrentThread.ManagedThreadId;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncSettingsFixture(delegate {
                if (Volatile.Read(ref armed) == 0) return;
                workerThread = Thread.CurrentThread.ManagedThreadId; entered.Set(); AsyncSettingsWait(release);
            }, null, true))
            using (var form = new SshProfilesSettingsForm(fixture.Settings))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 15 }) {
                fixture.Start(); var before = fixture.Settings.Capture(); var bytes = File.ReadAllBytes(AppPaths.SettingsPath);
                SettingsApplyRequest captured = null;
                form.SaveRequestedAsync = delegate(SettingsApplyRequest request, CancellationToken token) {
                    calls++; captured = request; return fixture.Context.ApplySettingsAsync(request.Proposed, request.PickFree, token);
                };
                form.Show(); Application.DoEvents(); var endpoint = (TextBox)Field(form, "endpoint");
                endpoint.Text = "https://form-edit.example.org/check";
                int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start(); Volatile.Write(ref armed, 1);
                var watch = Stopwatch.StartNew(); ((Button)form.AcceptButton).PerformClick();
                var operation = form.SaveWork;
                try {
                    Check(watch.ElapsedMilliseconds < 1000 && operation != null && !operation.IsCompleted,
                        "native settings Save returns immediately before its real durable writer completes");
                    PumpUntil(() => entered.IsSet && ticks >= 3);
                    Check(form.IsSavePending && form.Visible && !((Button)form.AcceptButton).Enabled &&
                        !((TabControl)Field(form, "settingsTabs")).Enabled && workerThread != ownerThread &&
                        fixture.Settings.Revision == before.Revision && File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(bytes),
                        "settings controls show pending while the native heartbeat survives blocked settings replacement on a worker");
                    Call(form, "Save", null, EventArgs.Empty); ((Button)form.AcceptButton).PerformClick();
                    Check(calls == 1 && Object.ReferenceEquals(operation, form.SaveWork) && captured.Proposed.TrayCloseExplained,
                        "pending Save rejects duplicate activation and a form edit preserves the existing unrepresented tray preference");
                    release.Set(); WaitAsyncSettings(operation); PumpUntil(() => !form.Visible);
                    Check(form.DialogResult == DialogResult.OK && !form.IsSavePending && fixture.Settings.Current.TrayCloseExplained &&
                        fixture.Settings.Current.TestEndpoint == endpoint.Text && fixture.Settings.Revision == before.Revision + 1,
                        "completed native Save closes successfully after exactly one durable publication without resetting an unrelated setting");
                    AssertBridge(fixture.Bridge.Port);
                } finally { release.Set(); WaitAsyncSettings(operation); }
            }
        }

        private static void AsyncSettingsFormCancel()
        {
            int armed = 0, calls = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncSettingsFixture(null, delegate(BridgeTransitionPhase phase) {
                if (phase != BridgeTransitionPhase.BeforeCommit || Volatile.Read(ref armed) == 0) return;
                entered.Set(); AsyncSettingsWait(release);
            }, true))
            using (var form = new SshProfilesSettingsForm(fixture.Settings)) {
                fixture.Start(); int oldPort = fixture.Bridge.Port; var before = fixture.Settings.Capture(); var bytes = File.ReadAllBytes(AppPaths.SettingsPath);
                CancellationToken observedToken = CancellationToken.None;
                form.SaveRequestedAsync = delegate(SettingsApplyRequest request, CancellationToken token) {
                    calls++; observedToken = token; return fixture.Context.ApplySettingsAsync(request.Proposed, request.PickFree, token);
                };
                form.Show(); Application.DoEvents(); var endpoint = (TextBox)Field(form, "endpoint"); endpoint.Text = "https://cancelled-form.example.org/check";
                ((Button)Field(form, "pickPortButton")).PerformClick(); Volatile.Write(ref armed, 1); ((Button)form.AcceptButton).PerformClick();
                var operation = form.SaveWork;
                try {
                    PumpUntil(() => entered.IsSet); ((Button)form.CancelButton).PerformClick(); form.Close();
                    Check(observedToken.IsCancellationRequested && form.Visible && form.IsSavePending && !operation.IsCompleted && calls == 1,
                        "Cancel and X request one cancellation while the real native settings operation remains pending and its form stays open");
                    release.Set(); WaitAsyncSettings(operation); PumpUntil(() => !form.IsSavePending && ((Label)Field(form, "saveError")).Text.Contains("отмен"));
                    Check(operation.Result != null && form.Visible && form.DialogResult == DialogResult.None &&
                        endpoint.Text == "https://cancelled-form.example.org/check" && fixture.Settings.Revision == before.Revision &&
                        File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(bytes) && fixture.Bridge.Port == oldPort,
                        "settled precommit form cancellation preserves exact saved settings and staged edits for an explicit retry");
                    AssertBridge(oldPort); form.Close();
                } finally { release.Set(); WaitAsyncSettings(operation); }
            }
        }

        private static void AsyncSettingsFormAcceptedCompletion()
        {
            int armed = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var completed = new ManualResetEventSlim())
            using (var fixture = new AsyncSettingsFixture(null, delegate(BridgeTransitionPhase phase) {
                if (phase != BridgeTransitionPhase.AfterCommit || Volatile.Read(ref armed) == 0) return;
                entered.Set(); AsyncSettingsWait(release);
            }))
            using (var form = new SshProfilesSettingsForm(fixture.Settings)) {
                fixture.Start(); var before = fixture.Settings.Capture(); int calls = 0;
                var completion = new TaskCompletionSource<SettingsSaveError>(); Task native = null;
                form.SaveRequestedAsync = delegate(SettingsApplyRequest request, CancellationToken token) {
                    calls++;
                    native = Task.Run(delegate {
                        try {
                            SettingsSaveError error;
                            var prepared = fixture.Bridge.PrepareConfiguration(request.Proposed, request.PickFree, true, request.Expected, true, token, out error);
                            if (prepared != null) fixture.Bridge.PublishPrepared(prepared);
                            // The default TCS synchronously executes the registered
                            // ConfigureAwait(false) continuation before SetResult returns.
                            // Its queued owner completion remains deliberately unpumped.
                            completion.SetResult(error);
                        } catch (Exception ex) { completion.TrySetException(ex); }
                        finally { completed.Set(); }
                    });
                    return completion.Task;
                };
                form.Show(); Application.DoEvents(); ((TextBox)Field(form, "endpoint")).Text = "https://accepted-form.example.org/check";
                ((Button)Field(form, "pickPortButton")).PerformClick(); Volatile.Write(ref armed, 1); ((Button)form.AcceptButton).PerformClick();
                try {
                    PumpUntil(() => entered.IsSet); ((Button)form.CancelButton).PerformClick(); form.Close();
                    Check(form.Visible && form.IsSavePending && fixture.Settings.Revision == before.Revision + 1,
                        "late form Cancel waits while accepted durable settings still need endpoint completion");
                    release.Set();
                    Check(completed.Wait(30000) && form.SaveWork.IsCompleted && form.SaveWork.Result == null,
                        "actual accepted bridge and form save task complete while owner dialog completion is deliberately unpumped");
                    form.Close(); Call(form, "Save", null, EventArgs.Empty);
                    Check(form.Visible && form.IsSavePending && form.DialogResult == DialogResult.None && calls == 1,
                        "queued successful owner completion still guards X and duplicate Save until it can publish the accepted dialog result");
                    PumpUntil(() => !form.Visible);
                    Check(form.DialogResult == DialogResult.OK && !form.IsSavePending && fixture.Settings.Current.TrayCloseExplained &&
                        fixture.Settings.Current.TestEndpoint == "https://accepted-form.example.org/check",
                        "late cancellation cannot replace an already accepted save with a false Cancel dialog result");
                    AssertBridge(fixture.Bridge.Port);
                } finally {
                    release.Set(); if (native != null && !native.Wait(30000)) throw new Exception("accepted form native worker did not settle");
                }
            }
        }

        private static void AsyncSettingsShutdownDeadline()
        {
            int armed = 0, cleanupCalls = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncSettingsFixture(null, delegate(BridgeTransitionPhase phase) {
                if (phase != BridgeTransitionPhase.BeforeCommit || Volatile.Read(ref armed) == 0) return;
                entered.Set(); AsyncSettingsWait(release);
            }, true, 50, () => Interlocked.Increment(ref cleanupCalls)))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 15 }) {
                fixture.Start(); fixture.OwnIntegrations(); int oldPort = fixture.Bridge.Port;
                var before = fixture.Settings.Capture(); var settingsBytes = File.ReadAllBytes(AppPaths.SettingsPath);
                var native = SystemProxyService.ReadCurrent(); var environment = AsyncEnvironmentValues();
                var files = new[] { CliProxyEnvironmentService.BackupPath, SystemProxyService.BackupPath, CodexProxyService.LauncherPath }.ToDictionary(p => p, File.ReadAllBytes);
                var proposed = before.Settings.Clone(); proposed.TestEndpoint = "https://shutdown-staged.example.org/check";
                int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start(); Volatile.Write(ref armed, 1);
                var operation = fixture.Context.ApplySettingsAsync(proposed, true, CancellationToken.None);
                try {
                    PumpUntil(() => entered.IsSet && ticks >= 3);
                    int candidatePort = new Uri(Environment.GetEnvironmentVariable("HTTP_PROXY", EnvironmentVariableTarget.User)).Port;
                    var duplicateEnsure = fixture.Context.EnsureBridgeAsync(CancellationToken.None);
                    Check(duplicateEnsure.IsCompleted && duplicateEnsure.Result != null && Object.ReferenceEquals(fixture.Context.SettingsWork, operation),
                        "Ensure during an actual settings move cannot queue a second cancellation owner or replace the worker shutdown must await");
                    int pulses = ticks; var watch = Stopwatch.StartNew(); var shutdown = fixture.Context.RequestShutdownAsync();
                    Check(watch.ElapsedMilliseconds < 1000 && !shutdown.IsCompleted && Object.ReferenceEquals(shutdown, fixture.Context.RequestShutdownAsync()),
                        "duplicate shutdown shares one asynchronous wait for the actual blocked settings transition");
                    WaitAsyncSettings(shutdown); PumpUntil(() => ticks >= pulses + 3);
                    var listeners = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                    Check(!shutdown.Result && watch.ElapsedMilliseconds < 3000 && !operation.IsCompleted && fixture.Context.SettingsPending &&
                        !((bool)Field(fixture.Context, "shutdownPrepared")) && !((bool)Field(fixture.Context, "shutdownPreparing")) &&
                        fixture.Main.Visible && ((NotifyIcon)Field(fixture.Context, "tray")).Visible && Volatile.Read(ref cleanupCalls) == 0 &&
                        fixture.Bridge.IsRunning && fixture.Bridge.Port == oldPort && listeners.Any(p => p.Port == oldPort) && listeners.Any(p => p.Port == candidatePort),
                        "short settings shutdown deadline refuses exit with a live heartbeat, unchanged owner UI and both actual endpoints retained before any native off");
                    var retry = fixture.Context.RequestShutdownAsync(); WaitAsyncSettings(retry);
                    Check(!Object.ReferenceEquals(shutdown, retry) && !retry.Result && !operation.IsCompleted && Volatile.Read(ref cleanupCalls) == 0,
                        "a completed refused shutdown can be retried without hanging, reusing a stale pending task or starting premature cleanup");
                    release.Set(); WaitAsyncSettings(operation);
                    var actualEnvironment = AsyncEnvironmentValues(); var actualNative = SystemProxyService.ReadCurrent();
                    Check(operation.Result != null && !fixture.Context.SettingsPending && fixture.Settings.Revision == before.Revision &&
                        File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(settingsBytes) && fixture.Bridge.Port == oldPort &&
                        environment.All(p => p.Value.Matches(actualEnvironment[p.Key])) &&
                        SystemProxyService.FieldNames.All(n => native.Values[n].Matches(actualNative.Values[n])) && files.All(p => File.ReadAllBytes(p.Key).SequenceEqual(p.Value)),
                        "after the blocked worker returns, shutdown cancellation settles exact own native rollback and uncommitted settings before any off transaction");
                    AssertPortReleased(candidatePort, "cancelled shutdown staging releases the unused candidate listener");
                    var accepted = fixture.Context.RequestShutdownAsync(); WaitAsyncSettings(accepted);
                    Check(accepted.Result && ((bool)Field(fixture.Context, "shutdownPrepared")) && Volatile.Read(ref cleanupCalls) == 2 &&
                        !fixture.Context.SettingsPending && !fixture.Bridge.IsRunning && !File.Exists(CliProxyEnvironmentService.BackupPath) && !File.Exists(SystemProxyService.BackupPath),
                        "explicit shutdown retry performs one real CLI and Windows cleanup only after settings settlement and prepares a complete stop");
                    AssertPortReleased(oldPort, "accepted shutdown retry releases the old actual listening endpoint");
                } finally { release.Set(); WaitAsyncSettings(operation); }
            }
        }

        private static void AsyncSettingsNativeColdShutdownDeadline()
        {
            int armed = 0, cleanupCalls = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncSettingsFixture(null, delegate(BridgeTransitionPhase phase) {
                if (phase != BridgeTransitionPhase.BeforeCommit || Volatile.Read(ref armed) == 0) return;
                entered.Set(); AsyncSettingsWait(release);
            }, true, 50, () => Interlocked.Increment(ref cleanupCalls)))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 15 }) {
                var before = fixture.Settings.Capture(); var bytes = File.ReadAllBytes(AppPaths.SettingsPath);
                int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start(); Volatile.Write(ref armed, 1);
                // Start the actual J coordinator directly, without a route fixture
                // or modal warning wrapper hiding its cancellation result.
                var method = typeof(UpdateAwareTrayApplicationContext).GetMethod("QueueIntegration", PrivateInstance);
                var operation = (Task)method.Invoke(fixture.Context, new object[] { false, true, true, false });
                try {
                    PumpUntil(() => entered.IsSet && ticks >= 3);
                    Check(fixture.Context.SettingsWork == null && Object.ReferenceEquals(operation, fixture.Context.IntegrationWork) &&
                        !operation.IsCompleted && fixture.Context.IntegrationPending,
                        "real native enable owns a blocked cold bridge preparation independently of the settings dialog task");
                    int pulses = ticks; var watch = Stopwatch.StartNew(); var shutdown = fixture.Context.RequestShutdownAsync();
                    Check(watch.ElapsedMilliseconds < 1000 && !shutdown.IsCompleted, "shutdown returns while actual native cold bridge preparation has not settled");
                    WaitAsyncSettings(shutdown); PumpUntil(() => ticks >= pulses + 3);
                    Check(!shutdown.Result && watch.ElapsedMilliseconds < 3000 && !operation.IsCompleted && Volatile.Read(ref cleanupCalls) == 0 &&
                        fixture.Main.Visible && fixture.Context.IntegrationPending && !((bool)Field(fixture.Context, "shutdownPrepared")) &&
                        !File.Exists(SystemProxyService.BackupPath),
                        "bounded shutdown tracks actual integration work even with no SettingsWork and refuses premature native cleanup or exit");
                    release.Set(); PumpIntegrationUntil(() => operation.IsCompleted && !fixture.Context.IntegrationPending);
                    bool cancelled = false;
                    try { operation.GetAwaiter().GetResult(); }
                    catch (InvalidOperationException ex) { cancelled = ex.Message.Contains("отмен"); }
                    catch (OperationCanceledException) { cancelled = true; }
                    Check(cancelled && fixture.Settings.Revision == before.Revision && File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(bytes) &&
                        !fixture.Bridge.IsRunning && !File.Exists(SystemProxyService.BackupPath) && Volatile.Read(ref cleanupCalls) == 0,
                        "shutdown cancellation reaches the actual cold native preparation and settles its reserved listener before Windows apply begins");
                    AssertPortReleased(before.Settings.HttpProxyPort, "cancelled native cold preparation releases its actual reserved endpoint");
                    var accepted = fixture.Context.RequestShutdownAsync(); WaitAsyncSettings(accepted);
                    Check(accepted.Result && Volatile.Read(ref cleanupCalls) == 2 && !fixture.Context.IntegrationPending &&
                        ((bool)Field(fixture.Context, "shutdownPrepared")),
                        "explicit retry after cancelled native cold preparation completes the real coordinator cleanup and prepares shutdown");
                } finally {
                    release.Set(); PumpIntegrationUntil(() => operation.IsCompleted && !fixture.Context.IntegrationPending);
                }
            }
        }

        private static void AsyncSettingsRestartMismatch()
        {
            int writes = 0;
            using (var fixture = new AsyncSettingsFixture(() => Interlocked.Increment(ref writes), null, true))
            using (var formerEndpoint = new ListenerScope()) {
                var originalEnvironment = AsyncEnvironmentValues(); var originalWindows = SystemProxyService.ReadCurrent();
                if (fixture.Settings.Current.HttpProxyPort == formerEndpoint.Port) {
                    var separate = fixture.Settings.Current.Clone();
                    using (var unused = new ListenerScope()) separate.HttpProxyPort = unused.Port;
                    fixture.Settings.Save(separate);
                }
                var configured = fixture.Settings.Capture(); int oldPort = configured.Settings.HttpProxyPort;
                var moved = configured.Settings.Clone(); moved.HttpProxyPort = formerEndpoint.Port;
                CliProxyEnvironmentService.ApplyUserEnvironment(formerEndpoint.Port);
                string message; Check(SystemProxyService.Apply(moved, out message), "restart mismatch fixture owns actual CLI and Windows journals at a different port");
                var environment = AsyncEnvironmentValues(); var windows = SystemProxyService.ReadCurrent();
                var files = new[] { CliProxyEnvironmentService.BackupPath, SystemProxyService.BackupPath }.ToDictionary(p => p, File.ReadAllBytes);
                var settingsBytes = File.ReadAllBytes(AppPaths.SettingsPath); int calls = Volatile.Read(ref writes);
                var direct = Task.Run(delegate { string error; bool started = fixture.Bridge.Start(out error); return started ? null : error; }); WaitAsyncSettings(direct);
                var cold = fixture.Context.EnsureBridgeAsync(CancellationToken.None); WaitAsyncSettings(cold);
                Check(direct.Result != null && direct.Result.Contains("Отключить прокси на ПК") && cold.Result != null &&
                    cold.Result.Message.Contains("Отключить прокси на ПК") && cold.Result.Message.Contains(formerEndpoint.Port.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    "fresh cold Start and async Ensure refuse mismatched owned ports with readable explicit cleanup guidance");
                var actualEnvironment = AsyncEnvironmentValues(); var actualWindows = SystemProxyService.ReadCurrent();
                var json = new JavaScriptSerializer();
                var listeners = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                Check(fixture.Settings.Revision == configured.Revision && Volatile.Read(ref writes) == calls &&
                    json.Serialize(fixture.Settings.Current) == json.Serialize(configured.Settings) && File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(settingsBytes) &&
                    environment.All(p => p.Value.Matches(actualEnvironment[p.Key])) &&
                    SystemProxyService.FieldNames.All(n => windows.Values[n].Matches(actualWindows.Values[n])) && files.All(p => File.ReadAllBytes(p.Key).SequenceEqual(p.Value)) &&
                    !fixture.Bridge.IsRunning && fixture.Bridge.RetainedPorts.Length == 0 && !fixture.Bridge.CleanupPending && !listeners.Any(p => p.Port == oldPort),
                    "restart mismatch performs zero durable or native changes and creates no candidate listener at the saved old port");
                var observed = AppProxyConsumers.ReadConsumers(fixture.Settings.Current, oldPort);
                Check(observed.Known && !observed.Empty && observed.RecoveryMessage != null && observed.RecoveryMessage.Contains("Отключить прокси на ПК"),
                    "fresh consumer observation exposes restart recovery guidance and cannot classify mismatched journals as an empty bridge");
                Call(fixture.Context, "ExecuteCommand", AppCommand.StopDesktop); WaitIntegration(fixture.Context);
                actualEnvironment = AsyncEnvironmentValues(); actualWindows = SystemProxyService.ReadCurrent();
                Check(!File.Exists(CliProxyEnvironmentService.BackupPath) && !File.Exists(SystemProxyService.BackupPath) && !fixture.Bridge.IsRunning &&
                    originalEnvironment.All(p => p.Value.Matches(actualEnvironment[p.Key])) &&
                    SystemProxyService.FieldNames.All(n => originalWindows.Values[n].Matches(actualWindows.Values[n])),
                    "explicit normal desktop Stop uses fresh owned cleanup to recover the mismatched CLI and Windows journals exactly");
                var recovered = fixture.Context.EnsureBridgeAsync(CancellationToken.None); WaitAsyncSettings(recovered);
                Check(recovered.Result == null && fixture.Bridge.IsRunning && fixture.Bridge.Port == oldPort &&
                    fixture.Settings.Revision == configured.Revision && Volatile.Read(ref writes) == calls &&
                    File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(settingsBytes) &&
                    AppProxyConsumers.ReadConsumers(fixture.Settings.Current, oldPort).RecoveryMessage == null,
                    "cold bridge starts at the unchanged saved endpoint after explicit mismatch cleanup without a gratuitous settings save");
                AssertBridge(oldPort);
            }
        }
    }
}
