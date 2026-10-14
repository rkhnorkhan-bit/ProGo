using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void StartupSnapshotAsyncWorkflow(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") return;
            string root = Path.Combine(Path.GetTempPath(), "ProGo-startup-async-" + Guid.NewGuid().ToString("N"));
            string install = Path.Combine(root, "app"), startup = Path.Combine(root, "Startup"), link = Path.Combine(startup, "ProGo.lnk");
            var original = settings.Current.Clone();
            try {
                Directory.CreateDirectory(install); File.WriteAllText(Path.Combine(install, "ProGo.exe"), "fixture; never executed");
                var shortcuts = new ApplicationShortcuts(install, Path.Combine(root, "Programs"), startup);
                var configured = AppSettings.Defaults(); configured.SshProfile = "fixture";
                configured.AutoStartSocks = configured.AutoCliProxy = configured.AutoSystemProxy = false; settings.Save(configured);
                shortcuts.ChangeStartup(shortcuts.ReadStartup(), true);
                StartupSnapshotPendingSave(settings, shortcuts, link);
                StartupSnapshotDeferredSave(settings, shortcuts, link);
                shortcuts.ChangeStartup(shortcuts.ReadStartup(), true);
                StartupSnapshotReadFailure(settings, shortcuts, link);
                StartupSnapshotOwnerClosed(settings, shortcuts, link, false);
                StartupSnapshotOwnerClosed(settings, shortcuts, link, true);
            } finally { settings.Save(original); if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
        private static void StartupSnapshotPendingSave(SettingsService settings, ApplicationShortcuts shortcuts, string link)
        {
            int calls = 0, worker = 0, owner = Thread.CurrentThread.ManagedThreadId;
            CancellationToken observed = CancellationToken.None; byte[] registration = File.ReadAllBytes(link);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var form = new SshProfilesSettingsForm(settings, SettingsSection.Connections, shortcuts, null, token => {
                observed = token; worker = Thread.CurrentThread.ManagedThreadId; Interlocked.Increment(ref calls); entered.Set(); AsyncSettingsWait(release);
            }))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 15 }) {
                var launch = (CheckBox)Field(form, "autoLaunch"); var endpoint = (TextBox)Field(form, "endpoint");
                Check(calls == 0 && form.StartupWork.IsCompleted && !launch.Enabled, "startup constructor performs no native snapshot read");
                int ticks = 0; pulse.Tick += delegate { ticks++; }; pulse.Start(); var watch = Stopwatch.StartNew(); form.Show();
                try {
                    Check(watch.ElapsedMilliseconds < 1000, "settings Show returns before its held startup read finishes");
                    endpoint.Text = "https://pending-startup.example.org/check"; PumpUntil(() => entered.IsSet && ticks >= 3);
                    Check(worker != owner && calls == 1 && form.IsStartupPending && !launch.Enabled && !launch.Checked &&
                        ((Label)Field(form, "startupNotice")).Text.Contains("Проверяем") && ((Button)form.AcceptButton).Enabled,
                        "startup read uses a worker while the native UI heartbeat and unrelated Save remain available");
                    Shot(form, "settings-startup-pending");
                    form.Hide(); form.Show(); Check(calls == 1, "showing the same settings form does not duplicate its held startup reader");
                    ((Button)form.AcceptButton).PerformClick(); WaitAsyncSettings(form.SaveWork); PumpUntil(() => !form.Visible);
                    Check(form.DialogResult == DialogResult.OK && settings.Current.TestEndpoint == endpoint.Text && registration.SequenceEqual(File.ReadAllBytes(link)),
                        "real Save while startup is unknown commits other edits without removing the existing login shortcut");
                    Check(observed.IsCancellationRequested && !form.IsStartupPending, "successful Save closes the startup reader's publication owner");
                    release.Set(); WaitAsyncSettings(form.StartupWork);
                    Check(!launch.Checked && registration.SequenceEqual(File.ReadAllBytes(link)) && settings.Current.TestEndpoint == endpoint.Text,
                        "a released read after accepted Save cannot publish stale startup UI or change saved values");
                } finally { release.Set(); WaitAsyncSettings(form.StartupWork); }
            }
        }
        private static void StartupSnapshotDeferredSave(SettingsService settings, ApplicationShortcuts shortcuts, string link)
        {
            byte[] registration = File.ReadAllBytes(link); SettingsApplyRequest captured = null;
            var save = new TaskCompletionSource<SettingsSaveError>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var form = new SshProfilesSettingsForm(settings, SettingsSection.Connections, shortcuts, null, token => {
                entered.Set(); AsyncSettingsWait(release);
            })) {
                form.SaveRequestedAsync = (request, token) => { captured = request; return save.Task; };
                form.Show(); PumpUntil(() => entered.IsSet);
                var launch = (CheckBox)Field(form, "autoLaunch"); var endpoint = (TextBox)Field(form, "endpoint");
                endpoint.Text = "https://deferred-startup.example.org/check"; string notice = ((Label)Field(form, "startupNotice")).Text;
                ((Button)form.AcceptButton).PerformClick();
                try {
                    Check(captured != null && captured.StartupSnapshot == null && form.IsSavePending, "pending startup Save explicitly carries no registration snapshot");
                    release.Set(); WaitAsyncSettings(form.StartupWork);
                    Check(form.IsStartupPending && form.IsSavePending && !launch.Checked && !launch.Enabled &&
                        ((Label)Field(form, "startupNotice")).Text == notice && endpoint.Text == "https://deferred-startup.example.org/check",
                        "completed startup read defers all field publication while Save is pending without replacing staged edits");
                    save.SetResult(new SettingsSaveError(SettingsField.SettingsFile, "Fixture write failed")); PumpUntil(() => !form.IsSavePending);
                    Check(!form.IsStartupPending && form.Visible && launch.Enabled && launch.Checked && endpoint.Text == "https://deferred-startup.example.org/check" &&
                        ((Label)Field(form, "saveError")).Text.Contains("Fixture write failed") && registration.SequenceEqual(File.ReadAllBytes(link)),
                        "failed Save publishes the deferred real startup state and retains the unsaved draft for retry");
                    launch.Checked = false; form.SaveRequestedAsync = null; ((Button)form.AcceptButton).PerformClick();
                    WaitAsyncSettings(form.SaveWork); PumpUntil(() => !form.Visible);
                    Check(form.DialogResult == DialogResult.OK && !File.Exists(link) && settings.Current.TestEndpoint == endpoint.Text,
                        "explicit retry uses the completed startup snapshot and applies the chosen login registration once");
                } finally { release.Set(); save.TrySetResult(new SettingsSaveError(SettingsField.General, "Fixture cleanup")); WaitAsyncSettings(form.StartupWork); if (form.SaveWork != null) WaitAsyncSettings(form.SaveWork); }
            }
        }
        private static void StartupSnapshotReadFailure(SettingsService settings, ApplicationShortcuts shortcuts, string link)
        {
            byte[] registration = File.ReadAllBytes(link);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var form = new SshProfilesSettingsForm(settings, SettingsSection.Connections, shortcuts, null, token => {
                entered.Set(); AsyncSettingsWait(release); throw new IOException("Fixture snapshot failure");
            })) {
                form.Show(); PumpUntil(() => entered.IsSet);
                var launch = (CheckBox)Field(form, "autoLaunch"); var endpoint = (TextBox)Field(form, "endpoint");
                try {
                    endpoint.Text = "https://failed-startup.example.org/check"; release.Set(); WaitAsyncSettings(form.StartupWork);
                    Check(!form.IsStartupPending && !launch.Enabled && ((Label)Field(form, "startupNotice")).Text.Contains("Не удалось проверить") &&
                        launch.AccessibleDescription == ((Label)Field(form, "startupNotice")).Text && endpoint.Text == "https://failed-startup.example.org/check",
                        "startup read failure leaves an honest accessible unavailable state rather than an unchecked editable default");
                    Shot(form, "settings-startup-read-failed"); ((Button)form.AcceptButton).PerformClick();
                    WaitAsyncSettings(form.SaveWork); PumpUntil(() => !form.Visible);
                    Check(form.DialogResult == DialogResult.OK && settings.Current.TestEndpoint == endpoint.Text && registration.SequenceEqual(File.ReadAllBytes(link)),
                        "failed startup discovery still permits a real unrelated settings commit and preserves exact registration bytes");
                } finally { release.Set(); WaitAsyncSettings(form.StartupWork); }
            }
        }
        private static void StartupSnapshotOwnerClosed(SettingsService settings, ApplicationShortcuts shortcuts, string link, bool dispose)
        {
            CancellationToken observed = CancellationToken.None; byte[] registration = File.ReadAllBytes(link), settingsBytes = File.ReadAllBytes(AppPaths.SettingsPath);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var form = new SshProfilesSettingsForm(settings, SettingsSection.Connections, shortcuts, null, token => {
                observed = token; entered.Set(); AsyncSettingsWait(release);
            })) {
                form.Show(); PumpUntil(() => entered.IsSet); var launch = (CheckBox)Field(form, "autoLaunch"); string notice = ((Label)Field(form, "startupNotice")).Text;
                try {
                    var watch = Stopwatch.StartNew(); if (dispose) form.Dispose(); else form.Close();
                    Check(watch.ElapsedMilliseconds < 1000 && observed.IsCancellationRequested && !form.IsStartupPending,
                        "startup " + (dispose ? "Dispose" : "Close") + " cancels publication immediately without waiting for the native reader");
                    release.Set(); WaitAsyncSettings(form.StartupWork);
                    Check(!launch.Checked && ((Label)Field(form, "startupNotice")).Text == notice && registration.SequenceEqual(File.ReadAllBytes(link)) &&
                        settingsBytes.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)),
                        "startup " + (dispose ? "Dispose" : "Close") + " ignores late completion and preserves both saved files");
                } finally { release.Set(); WaitAsyncSettings(form.StartupWork); }
            }
        }
    }
}
