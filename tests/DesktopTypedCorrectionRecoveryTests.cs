using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private const string TypedCorrectionsJournalKey = "ProGoPendingWindowsCorrections";
        private const string TypedCorrectionChildPass = "TYPED_CORRECTION_RETRY_PASS";

        private sealed class TypedCorrectionRetryRequest
        {
            public string UserRoot { get; set; }
            public string JournalPath { get; set; }
            public int ParentProcessId { get; set; }
            public Dictionary<string, WindowsProxyValue> Windows { get; set; }
            public Dictionary<string, string> EnvironmentValues { get; set; }
            public TypedCorrectionRetryRequest() { }
        }

        // A new process has no parent-side correction list, writer seam, or UI consumer.
        // It must recover solely from the actual user's persisted CLI journal.
        private static int TypedCorrectionRetryChild(string[] args)
        {
            try {
                if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true" || args.Length != 2)
                    throw new Exception("typed correction child requires isolated Windows CI and a request");
                var request = new JavaScriptSerializer().Deserialize<TypedCorrectionRetryRequest>(File.ReadAllText(args[1]));
                if (request == null || request.UserRoot != AppPaths.Root || request.JournalPath != CliProxyEnvironmentService.BackupPath ||
                    request.ParentProcessId == Process.GetCurrentProcess().Id || request.Windows == null || request.EnvironmentValues == null ||
                    request.Windows.Count != SystemProxyService.FieldNames.Length || request.EnvironmentValues.Count != CliProxyEnvironmentService.Names.Length ||
                    !File.Exists(CliProxyEnvironmentService.BackupPath) || File.Exists(SystemProxyService.BackupPath))
                    throw new Exception("typed correction child does not share the isolated parent user/journal");
                CliProxyEnvironmentService.ClearUserEnvironmentIfOwned();
                var actual = SystemProxyService.ReadCurrent();
                foreach (var name in SystemProxyService.FieldNames)
                    if (!actual.Values[name].Matches(request.Windows[name])) throw new Exception("child typed recovery mismatch: " + name);
                foreach (var name in CliProxyEnvironmentService.Names)
                    if (Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) != request.EnvironmentValues[name])
                        throw new Exception("child environment recovery mismatch: " + name);
                if (File.Exists(CliProxyEnvironmentService.BackupPath) || File.Exists(SystemProxyService.BackupPath))
                    throw new Exception("child cleanup retained or created a proxy ownership journal");
                Console.WriteLine(TypedCorrectionChildPass + " pid=" + Process.GetCurrentProcess().Id);
                return 0;
            } catch (Exception ex) { Console.WriteLine("FAIL: typed correction retry child: " + ex); return 1; }
        }

        private static void TypedCorrectionRecoveryWorkflow(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: durable typed correction recovery requires isolated Windows CI"); return;
            }
            if (File.Exists(SystemProxyService.BackupPath) || File.Exists(CliProxyEnvironmentService.BackupPath))
                throw new Exception("typed correction recovery fixture is not isolated from existing ownership");
            var original = SystemProxyService.ReadCurrent();
            var configuration = settings.Current.Clone();
            var environment = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            var socks = Occupy(0); AnswerFixtureSocks(socks);
            try {
                foreach (var name in CliProxyEnvironmentService.Names) Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.User);
                var configured = configuration.Clone();
                configured.SocksHost = "127.0.0.1"; configured.SocksPort = Number(socks);
                configured.AutoCliProxy = configured.AutoSystemProxy = configured.AutoStartSocks = false;
                configured.AutoHttpProxyPort = true; configured.TrayCloseExplained = true;
                settings.Save(configured);
                TypedCorrectionPrepareRefusal(true);
                TypedCorrectionPrepareRefusal(false);
                TypedCorrectionFileFailureAndChildRetry(settings, true, false);
                TypedCorrectionFileFailureAndChildRetry(settings, false, false);
                TypedCorrectionFileFailureAndChildRetry(settings, true, true);
                TypedCorrectionFileFailureAndChildRetry(settings, false, true);
            } finally {
                socks.Stop();
                if (File.Exists(CliProxyEnvironmentService.BackupPath)) File.Delete(CliProxyEnvironmentService.BackupPath);
                foreach (var pair in environment) Environment.SetEnvironmentVariable(pair.Key, pair.Value, EnvironmentVariableTarget.User);
                SystemProxyService.RestoreSnapshot(original);
                if (File.Exists(SystemProxyService.BackupPath)) File.Delete(SystemProxyService.BackupPath);
                settings.Save(configuration);
            }
        }

        private static List<WindowsProxyFieldBackup> TypedCorrectionGuards(byte[] bytes)
        {
            var json = new JavaScriptSerializer();
            var saved = json.Deserialize<Dictionary<string, string>>(System.Text.Encoding.UTF8.GetString(bytes));
            string corrections;
            if (!saved.TryGetValue(TypedCorrectionsJournalKey, out corrections)) throw new Exception("typed correction guards were not prepared before notification");
            return json.Deserialize<List<WindowsProxyFieldBackup>>(corrections);
        }

        private static void TypedCorrectionPrepareRefusal(bool enable)
        {
            string phase = enable ? "enable" : "off";
            CliProxyEnvironmentService.ApplyUserEnvironment(31881);
            if (enable) foreach (var name in CliProxyEnvironmentService.Names)
                Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.User);
            SeedExternalTypedRoute();
            var before = SystemProxyService.ReadCurrent();
            var environment = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            var journal = File.ReadAllBytes(CliProxyEnvironmentService.BackupPath);
            int notifications = 0, corrections = 0; bool refused = false;
            try {
                using (var held = File.Open(CliProxyEnvironmentService.BackupPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    Action<RegistryKey, string, WindowsProxyValue> writer = delegate(RegistryKey key, string name, WindowsProxyValue value) {
                        corrections++; SystemProxyService.WriteValue(key, name, value);
                    };
                    Action notification = delegate { notifications++; CliProxyEnvironmentService.BroadcastEnvironmentChange(); };
                    try {
                        if (enable) CliProxyEnvironmentService.ApplyUserEnvironment(31882, writer, notification);
                        else CliProxyEnvironmentService.ClearUserEnvironmentIfOwned(writer, notification);
                    } catch (IOException) { refused = true; }
                    Check(refused && notifications == 0 && corrections == 0, "locked initial typed guard save refuses CLI " + phase + " before setters or notification");
                    Check(CliProxyEnvironmentService.Names.All(n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User) == environment[n]),
                        "initial typed guard save failure preserves every user environment value: " + phase);
                    var actual = SystemProxyService.ReadCurrent();
                    Check(SystemProxyService.FieldNames.All(n => actual.Values[n].Matches(before.Values[n])) &&
                        File.ReadAllBytes(CliProxyEnvironmentService.BackupPath).SequenceEqual(journal),
                        "initial typed guard save failure preserves exact Windows types/data and previous journal bytes: " + phase);
                }
            } finally { CliProxyEnvironmentService.ClearUserEnvironmentIfOwned(); }
        }

        private static void TypedCorrectionFileFailureAndChildRetry(SettingsService settings, bool enable, bool differentExternal)
        {
            string phase = (enable ? "enable" : "off") + (differentExternal ? " / later external values" : " / matching normalization");
            bool armed = true, deny = true, settled = false;
            int ownerThread = Thread.CurrentThread.ManagedThreadId, workerThread = 0, denied = 0, wrongThread = 0, callbacks = 0;
            FileStream held = null; byte[] prepared = null; Exception preparationFailure = null;
            var expectedEnvironment = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim()) {
                Action<RegistryKey, string, WindowsProxyValue> writer = delegate(RegistryKey key, string name, WindowsProxyValue value) {
                    if (deny && (name == "AutoDetect" || name == "ProxyOverride" || name == "AutoConfigURL")) {
                        Interlocked.Increment(ref denied); throw new UnauthorizedAccessException("fixture typed correction writer denied");
                    }
                    SystemProxyService.WriteValue(key, name, value);
                };
                Action notification = delegate {
                    if (!armed) { CliProxyEnvironmentService.BroadcastEnvironmentChange(); return; }
                    workerThread = Thread.CurrentThread.ManagedThreadId;
                    List<WindowsProxyFieldBackup> guards = null;
                    try {
                        prepared = File.ReadAllBytes(CliProxyEnvironmentService.BackupPath);
                        guards = TypedCorrectionGuards(prepared);
                        if (guards.Count != 3 || guards.Any(f => !f.Pending || !SystemProxyService.IsTypedNormalization(f.Name, f.Original, f.Applied)))
                            throw new Exception("notification did not find the complete prepared typed correction receipt");
                        // Real Windows sharing rules deny atomic File.Replace and File.Delete,
                        // but still allow the helper and independent observer to read the receipt.
                        held = File.Open(CliProxyEnvironmentService.BackupPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    } catch (Exception ex) { preparationFailure = ex; }
                    // Pause even on a missing guard so the owner can fail and dispose
                    // without entering an unexpected modal warning while waiting.
                    entered.Set(); IntegrationWait(release);
                    if (preparationFailure != null) throw new Exception("typed correction notification preparation failed", preparationFailure);
                    CliProxyEnvironmentService.BroadcastEnvironmentChange();
                    using (var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true)) {
                        foreach (var guard in guards) SystemProxyService.WriteValue(key, guard.Name, guard.Applied);
                    }
                };
                using (var fixture = new IntegrationContextFixture(settings, null,
                    delegate { CliProxyEnvironmentService.ClearUserEnvironmentIfOwned(writer, notification); },
                    delegate(ProxyFeature feature, AppSettings captured) {
                        if (feature != ProxyFeature.Cli) throw new Exception("typed correction fixture expects only CLI");
                        CliProxyEnvironmentService.ApplyUserEnvironment(captured.HttpProxyPort, writer, notification);
                    }))
                using (var heartbeat = new System.Windows.Forms.Timer { Interval = 15 }) {
                    var context = fixture.Context; var bridge = fixture.Bridge;
                    var previousContext = SynchronizationContext.Current;
                    try {
                        if (!enable) {
                            string message; Check(bridge.Start(out message), "typed receipt off fixture starts its actual shared listener: " + phase);
                            CliProxyEnvironmentService.ApplyUserEnvironment(bridge.Port);
                        }
                        SeedExternalTypedRoute(); var original = SystemProxyService.ReadCurrent();
                        var expectedWindows = new Dictionary<string, WindowsProxyValue>(original.Values);
                        var consumers = (AppProxyConsumers)Field(context, "appConsumers"); consumers.Observe();
                        WatchIntegrationCommands(context, delegate { callbacks++; if (Thread.CurrentThread.ManagedThreadId != ownerThread) wrongThread++; });
                        WatchIntegrationControls(fixture.Main, delegate { if (Thread.CurrentThread.ManagedThreadId != ownerThread) wrongThread++; });
                        int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                        SynchronizationContext.SetSynchronizationContext(null);
                        var watch = Stopwatch.StartNew();
                        if (enable) Call(context, "EnableFeature", ProxyFeature.Cli); else IntegrationCommands(context)[AppCommand.StopCli].PerformClick();
                        Check(watch.ElapsedMilliseconds < 1000 && context.IntegrationPending, "typed receipt CLI action returns while its actual native helper runs: " + phase);
                        PumpIntegrationUntil(() => entered.IsSet && ticks >= 3);
                        if (preparationFailure != null) throw new Exception("typed correction guard assertion failed: " + phase, preparationFailure);
                        var guards = TypedCorrectionGuards(prepared);
                        Check(guards.All(f => f.Original.Matches(original.Values[f.Name])) && guards.Single(f => f.Name == "AutoDetect").Original.Data == "0" &&
                            !guards.Single(f => f.Name == "AutoDetect").Applied.Exists,
                            "durable notification guards contain exact fresh typed originals and anticipated normalization: " + phase);
                        consumers.ReleaseIfUnused();
                        Check(workerThread != ownerThread && bridge.IsRunning && !context.IntegrationWork.IsCompleted && ticks >= 3,
                            "null-context delayed CLI notification keeps UI heartbeat and its actual listener live: " + phase);
                        var warning = FinishIntegrationWarning(context, release.Set);
                        consumers.ReleaseIfUnused(); var normalized = SystemProxyService.ReadCurrent();
                        Check(denied == 3 && guards.All(f => normalized.Values[f.Name].Matches(f.Applied)) &&
                            File.ReadAllBytes(CliProxyEnvironmentService.BackupPath).SequenceEqual(prepared),
                            "denied corrections plus real atomic replacement failure preserve the complete durable receipt: " + phase);
                        Check(warning.Contains("терминал") && !warning.Contains("fixture typed") && consumers.CliCleanupPending && bridge.IsRunning &&
                            (enable ? CliProxyEnvironmentService.IsAppliedToUserEnvironment(bridge.Port) :
                            CliProxyEnvironmentService.Names.All(n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User) == expectedEnvironment[n])),
                            "failed actual CLI helper retains journal, pending consumer and shared listener: " + phase);
                        held.Dispose(); held = null;
                        if (differentExternal) {
                            using (var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true)) {
                                key.SetValue("ProxyOverride", "%TEMP%;later-durable.example.org", RegistryValueKind.ExpandString);
                                key.SetValue("AutoDetect", 1, RegistryValueKind.DWord);
                            }
                            var later = SystemProxyService.ReadCurrent();
                            expectedWindows["ProxyOverride"] = later.Values["ProxyOverride"];
                            expectedWindows["AutoDetect"] = later.Values["AutoDetect"];
                        }
                        TypedCorrectionRunChild(expectedWindows, expectedEnvironment, phase);
                        Check(bridge.IsRunning && consumers.CliCleanupPending, "separate retry process leaves the parent's pending listener available: " + phase);
                        armed = deny = false;
                        Call(context, "ExecuteCommand", AppCommand.StopCli); WaitIntegration(context); SettleConsumers(consumers, bridge);
                        Check(!consumers.CliCleanupPending && !bridge.IsRunning && !File.Exists(CliProxyEnvironmentService.BackupPath) &&
                            !File.Exists(SystemProxyService.BackupPath) && callbacks > 0 && wrongThread == 0,
                            "explicit UI cleanup settles the child-recovered receipt and releases only its actual listener on the owner thread: " + phase);
                        settled = true;
                    } finally {
                        armed = deny = false; release.Set();
                        if (held != null) { held.Dispose(); held = null; }
                        if (!settled) context.Dispose();
                        SynchronizationContext.SetSynchronizationContext(previousContext);
                    }
                }
            }
        }

        private static void TypedCorrectionRunChild(Dictionary<string, WindowsProxyValue> expectedWindows,
            Dictionary<string, string> expectedEnvironment, string phase)
        {
            string requestPath = Path.Combine(work, "typed-correction-retry-" + Guid.NewGuid().ToString("N") + ".json");
            var request = new TypedCorrectionRetryRequest {
                UserRoot = AppPaths.Root, JournalPath = CliProxyEnvironmentService.BackupPath, ParentProcessId = Process.GetCurrentProcess().Id,
                Windows = expectedWindows, EnvironmentValues = expectedEnvironment
            };
            File.WriteAllText(requestPath, new JavaScriptSerializer().Serialize(request));
            try {
                var start = new ProcessStartInfo(Application.ExecutablePath, "--typed-correction-retry-child \"" + requestPath + "\"") {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
                };
                using (var child = Process.Start(start)) {
                    if (child == null) throw new Exception("typed correction retry child did not start");
                    Task<string> output = child.StandardOutput.ReadToEndAsync(), error = child.StandardError.ReadToEndAsync();
                    try {
                        var watch = Stopwatch.StartNew();
                        while (!child.HasExited || !output.IsCompleted || !error.IsCompleted) {
                            if (watch.ElapsedMilliseconds > 45000) throw new TimeoutException("typed correction retry child deadline: " + phase);
                            Application.DoEvents(); Thread.Sleep(10);
                        }
                        string stdout = output.GetAwaiter().GetResult(), stderr = error.GetAwaiter().GetResult();
                        if (child.ExitCode != 0 || !stdout.Contains(TypedCorrectionChildPass) || stderr.Length != 0)
                            throw new Exception("typed correction retry child failed: " + phase + "; exit=" + child.ExitCode + "; stdout=" + stdout + "; stderr=" + stderr);
                        Check(child.Id != request.ParentProcessId, "separate executable process recovers exact Windows types/data and user environment from disk: " + phase);
                        var actual = SystemProxyService.ReadCurrent();
                        Check(SystemProxyService.FieldNames.All(n => actual.Values[n].Matches(expectedWindows[n])) &&
                            CliProxyEnvironmentService.Names.All(n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User) == expectedEnvironment[n]) &&
                            !File.Exists(CliProxyEnvironmentService.BackupPath),
                            "parent independently verifies restarted actual cleanup and preserves different external typed values: " + phase);
                    } finally {
                        if (!child.HasExited) { child.Kill(); if (!child.WaitForExit(5000)) throw new Exception("typed correction retry child did not terminate"); }
                    }
                }
            } finally { if (File.Exists(requestPath)) File.Delete(requestPath); }
        }
    }
}
