using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void TrayPreferenceAsyncWorkflow()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") return;
            byte[] original = File.Exists(AppPaths.SettingsPath) ? File.ReadAllBytes(AppPaths.SettingsPath) : null;
            var json = new JavaScriptSerializer(); string future = "FixtureTrayFuture" + Guid.NewGuid().ToString("N");
            var values = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(AppPaths.SettingsPath));
            values.Add(future, new Dictionary<string, object> { { "nested", new object[] { 1, "preserved" } } });
            try {
                File.WriteAllText(AppPaths.SettingsPath, json.Serialize(values));
                TrayPreferenceLatestCommit(future, json.Serialize(values[future]));
                TrayPreferenceFailureRetry();
                TrayPreferenceDisposedCommit();
            } finally { RestoreAsyncFile(AppPaths.SettingsPath, original); }
        }
        private static void TrayPreferenceLatestCommit(string future, string expectedUnknown)
        {
            int armed = 0, writes = 0, worker = 0, cleanup = 0, owner = Thread.CurrentThread.ManagedThreadId;
            using (var first = new ManualResetEventSlim())
            using (var releaseFirst = new ManualResetEventSlim())
            using (var second = new ManualResetEventSlim())
            using (var releaseSecond = new ManualResetEventSlim())
            using (var fixture = new AsyncSettingsFixture(delegate {
                if (Volatile.Read(ref armed) == 0) return;
                int write = Interlocked.Increment(ref writes);
                if (write == 1) { first.Set(); AsyncSettingsWait(releaseFirst); }
                if (write == 2) { worker = Thread.CurrentThread.ManagedThreadId; second.Set(); AsyncSettingsWait(releaseSecond); }
            }, null, true, 120, () => Interlocked.Increment(ref cleanup)))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 15 }) {
                var configured = fixture.Settings.Current.Clone(); configured.TrayCloseExplained = false; fixture.Settings.Save(configured);
                long revision = fixture.Settings.Revision; var settingsTask = fixture.Context.SettingsWork; var integrationTask = fixture.Context.IntegrationWork;
                var latest = fixture.Settings.Current.Clone(); latest.TestEndpoint = "https://latest-tray.example.org/check"; latest.ClipboardClearSeconds = 17;
                int ticks = 0; pulse.Tick += delegate { ticks++; }; pulse.Start(); Volatile.Write(ref armed, 1);
                var earlier = Task.Run(() => fixture.Settings.Save(latest)); Task preference = null;
                try {
                    PumpUntil(() => first.IsSet); var watch = Stopwatch.StartNew(); fixture.Main.Close(); preference = fixture.Context.TrayPreferenceWork;
                    Check(watch.ElapsedMilliseconds < 1000 && !preference.IsCompleted, "dashboard close returns immediately while its preference waits behind an actual earlier file writer");
                    PumpUntil(() => ticks >= 3); fixture.Context.RequestShowStatus(); ((MainWindow)Field(fixture.Context, "mainWindow")).Close();
                    Check(Object.ReferenceEquals(preference, fixture.Context.TrayPreferenceWork) && Volatile.Read(ref writes) == 1 &&
                        !fixture.Settings.Current.TrayCloseExplained && ((NotifyIcon)Field(fixture.Context, "tray")).Visible,
                        "duplicate dashboard close shares its pending preference worker while UI pulses and tray ownership remain alive");
                    releaseFirst.Set(); WaitAsyncSettings(earlier); PumpUntil(() => second.IsSet);
                    Check(worker != owner && fixture.Settings.Revision == revision + 1 && fixture.Settings.Current.TestEndpoint == latest.TestEndpoint &&
                        fixture.Settings.Current.ClipboardClearSeconds == 17 && !fixture.Settings.Current.TrayCloseExplained &&
                        Object.ReferenceEquals(settingsTask, fixture.Context.SettingsWork) && Object.ReferenceEquals(integrationTask, fixture.Context.IntegrationWork),
                        "preference writer runs off the owner thread after the competing publication without replacing settings or integration task tracking");
                    int before = ticks; watch.Restart(); var shutdown = fixture.Context.RequestShutdownAsync();
                    Check(!shutdown.IsCompleted && Object.ReferenceEquals(shutdown, fixture.Context.RequestShutdownAsync()), "duplicate shutdown shares one pending wait for the real tray preference write");
                    WaitAsyncSettings(shutdown); PumpUntil(() => ticks >= before + 3);
                    Check(!shutdown.Result && watch.ElapsedMilliseconds < 3000 && !preference.IsCompleted && cleanup == 0 &&
                        !(bool)Field(fixture.Context, "shutdownPrepared") && !(bool)Field(fixture.Context, "shutdownPreparing") &&
                        ((NotifyIcon)Field(fixture.Context, "tray")).Visible,
                        "bounded shutdown refuses handoff while the actual preference write is blocked and performs no premature native cleanup");
                    releaseSecond.Set(); WaitAsyncSettings(preference);
                    var raw = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(AppPaths.SettingsPath));
                    var saved = SettingsService.DeserializeSettings(File.ReadAllText(AppPaths.SettingsPath));
                    Check(fixture.Settings.Revision == revision + 2 && saved.TrayCloseExplained && fixture.Settings.Current.TrayCloseExplained &&
                        saved.TestEndpoint == latest.TestEndpoint && saved.ClipboardClearSeconds == 17 &&
                        new JavaScriptSerializer().Serialize(raw[future]) == expectedUnknown,
                        "field-only tray commit merges the latest known settings and preserves an unknown nested JSON field durably");
                    var accepted = fixture.Context.RequestShutdownAsync(); WaitAsyncSettings(accepted);
                    Check(accepted.Result && (bool)Field(fixture.Context, "shutdownPrepared") && cleanup == 2 && preference.IsCompleted &&
                        Object.ReferenceEquals(settingsTask, fixture.Context.SettingsWork),
                        "explicit shutdown retry prepares native cleanup only after the tracked tray preference has fully settled");
                } finally { releaseFirst.Set(); releaseSecond.Set(); WaitAsyncSettings(earlier); if (preference != null) WaitAsyncSettings(preference); }
            }
        }
        private static void TrayPreferenceFailureRetry()
        {
            int armed = 0, writes = 0;
            using (var fixture = new AsyncSettingsFixture(delegate {
                if (Volatile.Read(ref armed) != 0 && Interlocked.Increment(ref writes) == 1) throw new IOException("Fixture tray write failure");
            }, null, true)) {
                var configured = fixture.Settings.Current.Clone(); configured.TrayCloseExplained = false; fixture.Settings.Save(configured);
                long revision = fixture.Settings.Revision; byte[] bytes = File.ReadAllBytes(AppPaths.SettingsPath); Volatile.Write(ref armed, 1);
                fixture.Main.Close(); var failed = fixture.Context.TrayPreferenceWork; WaitAsyncSettings(failed);
                Check(!failed.IsFaulted && writes == 1 && !fixture.Settings.Current.TrayCloseExplained && fixture.Settings.Revision == revision &&
                    bytes.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)), "failed tray preference write settles without publishing a false explanation flag or changing durable bytes");
                fixture.Context.RequestShowStatus(); ((MainWindow)Field(fixture.Context, "mainWindow")).Close(); var retry = fixture.Context.TrayPreferenceWork; WaitAsyncSettings(retry);
                Check(!Object.ReferenceEquals(failed, retry) && writes == 2 && fixture.Settings.Revision == revision + 1 &&
                    fixture.Settings.Current.TrayCloseExplained && SettingsService.DeserializeSettings(File.ReadAllText(AppPaths.SettingsPath)).TrayCloseExplained,
                    "a later dashboard close explicitly retries the failed preference and commits once");
                fixture.Context.RequestShowStatus(); ((MainWindow)Field(fixture.Context, "mainWindow")).Close();
                Check(Object.ReferenceEquals(retry, fixture.Context.TrayPreferenceWork) && writes == 2 && fixture.Settings.Revision == revision + 1,
                    "a durably explained close queues no further preference task or disk replacement");
            }
        }
        private static void TrayPreferenceDisposedCommit()
        {
            int armed = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncSettingsFixture(delegate {
                if (Volatile.Read(ref armed) == 0) return; entered.Set(); AsyncSettingsWait(release);
            }, null, true)) {
                var configured = fixture.Settings.Current.Clone(); configured.TrayCloseExplained = false; fixture.Settings.Save(configured);
                long revision = fixture.Settings.Revision; byte[] bytes = File.ReadAllBytes(AppPaths.SettingsPath); Volatile.Write(ref armed, 1);
                fixture.Main.Close(); var preference = fixture.Context.TrayPreferenceWork;
                try {
                    PumpUntil(() => entered.IsSet); var watch = Stopwatch.StartNew(); fixture.Context.Dispose();
                    Check(watch.ElapsedMilliseconds < 1000 && !preference.IsCompleted, "forced owner disposal returns before the held tray writer settles");
                    release.Set(); WaitAsyncSettings(preference);
                    Check(!preference.IsFaulted && fixture.Settings.Revision == revision && !fixture.Settings.Current.TrayCloseExplained &&
                        bytes.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)) && !Directory.GetFiles(Path.GetDirectoryName(AppPaths.SettingsPath), Path.GetFileName(AppPaths.SettingsPath) + ".*.tmp").Any(),
                        "disposed preference owner cancels before atomic replacement, removes its temporary file and cannot commit late");
                } finally { release.Set(); WaitAsyncSettings(preference); }
            }
        }
    }
}
