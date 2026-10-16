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
        private sealed class SettingsFolderFixture : IDisposable
        {
            internal readonly string Root, Install, Programs, Startup, Profile, Executable, Link, Config;
            internal readonly ApplicationShortcuts Shortcuts;
            internal SettingsFolderFixture()
            {
                Root = Path.Combine(Path.GetTempPath(), "ProGo-settings-folders-" + Guid.NewGuid().ToString("N"));
                Install = Path.Combine(Root, "app"); Programs = Path.Combine(Root, "Programs");
                Startup = Path.Combine(Root, "Startup"); Profile = Path.Combine(Root, "Profile");
                Executable = Path.Combine(Install, "ProGo.exe"); Link = Path.Combine(Startup, "ProGo.lnk");
                Config = Path.Combine(Profile, ".ssh", "config");
                Directory.CreateDirectory(Install); Directory.CreateDirectory(Profile);
                File.WriteAllText(Executable, "fixture; never executed");
                Shortcuts = new ApplicationShortcuts(Install, Programs, Startup); Register();
            }
            internal void Register() { Shortcuts.ChangeStartup(Shortcuts.ReadStartup(), true); }
            internal string Folder(Environment.SpecialFolder folder)
            {
                if (folder == Environment.SpecialFolder.Programs) return Programs;
                if (folder == Environment.SpecialFolder.Startup) return Startup;
                if (folder == Environment.SpecialFolder.UserProfile) return Profile;
                throw new InvalidOperationException("Unexpected special-folder request: " + folder);
            }
            internal SshProfilesSettingsForm Form(SettingsService settings, Func<Environment.SpecialFolder, string> provider,
                bool injected = false, bool portable = false)
            {
                return new SshProfilesSettingsForm(settings, SettingsSection.Connections, injected ? Shortcuts : null,
                    null, null, provider, portable ? Path.Combine(Root, "portable", "ProGo.exe") : Executable, Install);
            }
            public void Dispose() { if (Directory.Exists(Root)) Directory.Delete(Root, true); }
        }
        private static void SettingsFolderWorkflow(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") return;
            var original = settings.Current.Clone();
            try {
                var configured = AppSettings.Defaults(); configured.SshProfile = "fixture";
                configured.AutoStartSocks = configured.AutoCliProxy = configured.AutoSystemProxy = false; settings.Save(configured);
                using (var fixture = new SettingsFolderFixture()) {
                    SettingsFoldersPendingSave(settings, fixture);
                    SettingsFoldersDeferredSave(settings, fixture);
                    SettingsFoldersIndependentProfile(settings, fixture);
                    fixture.Register();
                    SettingsFoldersFailure(settings, fixture, Environment.SpecialFolder.Programs, "throw", true);
                    SettingsFoldersFailure(settings, fixture, Environment.SpecialFolder.Programs, "", false);
                    SettingsFoldersFailure(settings, fixture, Environment.SpecialFolder.Programs, "relative-programs", false);
                    SettingsFoldersFailure(settings, fixture, Environment.SpecialFolder.Startup, "", false);
                    SettingsFoldersFailure(settings, fixture, Environment.SpecialFolder.UserProfile, "throw", false);
                    SettingsFoldersFailure(settings, fixture, Environment.SpecialFolder.UserProfile, "", false);
                    SettingsFoldersFailure(settings, fixture, Environment.SpecialFolder.UserProfile, "relative-profile", false);
                    SettingsFoldersFailure(settings, fixture, Environment.SpecialFolder.UserProfile, "\\relative-profile", false);
                    SettingsFoldersFailure(settings, fixture, Environment.SpecialFolder.UserProfile, "C:relative-profile", false);
                    SettingsFoldersOwnerClosed(settings, fixture, false);
                    SettingsFoldersOwnerClosed(settings, fixture, true);
                    SettingsFoldersInjectedAndPortable(settings, fixture, true);
                    SettingsFoldersInjectedAndPortable(settings, fixture, false);
                }
            } finally { settings.Save(original); }
        }
        private static void SettingsFoldersPendingSave(SettingsService settings, SettingsFolderFixture fixture)
        {
            int programs = 0, startup = 0, profile = 0, uiCalls = 0, owner = Thread.CurrentThread.ManagedThreadId;
            byte[] registration = File.ReadAllBytes(fixture.Link);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var form = fixture.Form(settings, folder => {
                if (Thread.CurrentThread.ManagedThreadId == owner) Interlocked.Increment(ref uiCalls);
                if (folder == Environment.SpecialFolder.Programs) { Interlocked.Increment(ref programs); entered.Set(); AsyncSettingsWait(release); }
                if (folder == Environment.SpecialFolder.Startup) Interlocked.Increment(ref startup);
                if (folder == Environment.SpecialFolder.UserProfile) Interlocked.Increment(ref profile);
                return fixture.Folder(folder);
            }))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 15 }) {
                var launch = (CheckBox)Field(form, "autoLaunch"); var notice = (Label)Field(form, "startupNotice");
                var path = (TextBox)Field(form, "sshConfigPath"); var endpoint = (TextBox)Field(form, "endpoint");
                Check(programs + startup + profile == 0 && form.StartupWork.IsCompleted && form.ProfilePathWork.IsCompleted &&
                    form.IsStartupPending && !launch.Enabled && notice.Text.Contains("Проверяем"),
                    "folder constructor does no special-folder lookup and keeps installed startup discovery pending");
                int ticks = 0; pulse.Tick += delegate { ticks++; }; pulse.Start(); var watch = Stopwatch.StartNew(); form.Show();
                long showMilliseconds = watch.ElapsedMilliseconds;
                try {
                    PumpUntil(() => entered.IsSet && form.ProfilePathWork.IsCompleted && ticks >= 3);
                    Check(showMilliseconds < 1000 && uiCalls == 0 && programs == 1 && startup == 0 && profile == 1 && path.Text == fixture.Config && path.ReadOnly &&
                        form.IsStartupPending && !launch.Enabled && !launch.Checked && ((Button)form.AcceptButton).Enabled,
                        "held startup folder lookup leaves the UI heartbeat, independent SSH path and unrelated Save available");
                    var startupWork = form.StartupWork; var profileWork = form.ProfilePathWork;
                    form.Hide(); form.Show(); Application.DoEvents();
                    Check(programs == 1 && profile == 1 && Object.ReferenceEquals(startupWork, form.StartupWork) && Object.ReferenceEquals(profileWork, form.ProfilePathWork),
                        "showing the same settings form does not duplicate either folder worker");
                    endpoint.Text = "https://pending-folders.example.org/check"; ((Button)form.AcceptButton).PerformClick();
                    WaitAsyncSettings(form.SaveWork); PumpUntil(() => !form.Visible);
                    Check(form.DialogResult == DialogResult.OK && settings.Current.TestEndpoint == endpoint.Text && registration.SequenceEqual(File.ReadAllBytes(fixture.Link)) &&
                        !startupWork.IsCompleted && !form.IsStartupPending && Field(form, "startupSnapshot") == null,
                        "real Save before default folder discovery preserves the registered shortcut and retains the raw held worker");
                    byte[] saved = File.ReadAllBytes(AppPaths.SettingsPath); string closedNotice = notice.Text; bool closedChecked = launch.Checked;
                    release.Set(); WaitAsyncSettings(startupWork);
                    Check(startup == 0 && notice.Text == closedNotice && launch.Checked == closedChecked && Field(form, "startupSnapshot") == null &&
                        saved.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)) && registration.SequenceEqual(File.ReadAllBytes(fixture.Link)),
                        "accepted Save cancels late default discovery before Startup lookup and cannot publish or write after closure");
                } finally { release.Set(); WaitAsyncSettings(form.StartupWork); WaitAsyncSettings(form.ProfilePathWork); }
            }
        }
        private static void SettingsFoldersDeferredSave(SettingsService settings, SettingsFolderFixture fixture)
        {
            SettingsApplyRequest captured = null; byte[] registration = File.ReadAllBytes(fixture.Link);
            var save = new TaskCompletionSource<SettingsSaveError>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var form = fixture.Form(settings, folder => {
                if (folder == Environment.SpecialFolder.Programs) { entered.Set(); AsyncSettingsWait(release); }
                return fixture.Folder(folder);
            })) {
                form.SaveRequestedAsync = (request, token) => { captured = request; return save.Task; };
                form.Show(); PumpUntil(() => entered.IsSet);
                var launch = (CheckBox)Field(form, "autoLaunch"); var endpoint = (TextBox)Field(form, "endpoint");
                var notice = (Label)Field(form, "startupNotice"); string pendingNotice = notice.Text;
                endpoint.Text = "https://deferred-folders.example.org/check"; ((Button)form.AcceptButton).PerformClick();
                try {
                    Check(captured != null && captured.Startup == null && captured.StartupSnapshot == null && form.IsSavePending,
                        "Save during default folder discovery carries neither a guessed shortcut manager nor a startup snapshot");
                    release.Set(); WaitAsyncSettings(form.StartupWork); WaitAsyncSettings(form.ProfilePathWork);
                    Check(form.IsSavePending && form.IsStartupPending && !launch.Enabled && !launch.Checked && notice.Text == pendingNotice &&
                        Field(form, "startupShortcuts") == null && Field(form, "startupSnapshot") == null && endpoint.Text == "https://deferred-folders.example.org/check",
                        "completed default discovery defers its entire manager and snapshot pair while Save owns the draft");
                    save.SetResult(new SettingsSaveError(SettingsField.SettingsFile, "Fixture folder Save failed")); PumpUntil(() => !form.IsSavePending);
                    var manager = (ApplicationShortcuts)Field(form, "startupShortcuts");
                    var snapshot = (ApplicationShortcuts.StartupSnapshot)Field(form, "startupSnapshot");
                    Check(manager != null && snapshot != null && Object.ReferenceEquals(snapshot.Owner, manager) && snapshot.Registered && launch.Checked && launch.Enabled &&
                        !form.IsStartupPending && endpoint.Text == "https://deferred-folders.example.org/check" &&
                        ((Label)Field(form, "saveError")).Text.Contains("Fixture folder Save failed") && registration.SequenceEqual(File.ReadAllBytes(fixture.Link)),
                        "failed Save publishes the matching real discovery owner and snapshot without losing the unsaved draft");
                    launch.Checked = false; form.SaveRequestedAsync = null; ((Button)form.AcceptButton).PerformClick();
                    WaitAsyncSettings(form.SaveWork); PumpUntil(() => !form.Visible);
                    Check(form.DialogResult == DialogResult.OK && !File.Exists(fixture.Link) && settings.Current.TestEndpoint == endpoint.Text,
                        "real Save retry after deferred discovery applies the registration change using its matching snapshot owner");
                    fixture.Register();
                } finally {
                    release.Set(); save.TrySetResult(new SettingsSaveError(SettingsField.General, "Fixture cleanup"));
                    WaitAsyncSettings(form.StartupWork); WaitAsyncSettings(form.ProfilePathWork); if (form.SaveWork != null) WaitAsyncSettings(form.SaveWork);
                }
            }
        }
        private static void SettingsFoldersIndependentProfile(SettingsService settings, SettingsFolderFixture fixture)
        {
            int profileCalls = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var form = fixture.Form(settings, folder => {
                if (folder == Environment.SpecialFolder.UserProfile) { Interlocked.Increment(ref profileCalls); entered.Set(); AsyncSettingsWait(release); }
                return fixture.Folder(folder);
            })) {
                form.Show();
                try {
                    PumpUntil(() => entered.IsSet && form.StartupWork.IsCompleted);
                    var launch = (CheckBox)Field(form, "autoLaunch"); var path = (TextBox)Field(form, "sshConfigPath");
                    var endpoint = (TextBox)Field(form, "endpoint"); endpoint.Text = "https://loaded-folders.example.org/check";
                    Check(launch.Enabled && launch.Checked && Field(form, "startupShortcuts") is ApplicationShortcuts && Field(form, "startupSnapshot") != null &&
                        form.IsProfilePathPending && !form.ProfilePathWork.IsCompleted && path.Text.Contains("Определяем") && profileCalls == 1,
                        "real default shortcut discovery completes independently while the diagnostic UserProfile provider is held");
                    ((TabControl)Field(form, "settingsTabs")).SelectedIndex = 3; Shot(form, "settings-folders-pending");
                    release.Set(); WaitAsyncSettings(form.ProfilePathWork);
                    Check(!form.IsProfilePathPending && path.Text == fixture.Config && path.ReadOnly && endpoint.Text == "https://loaded-folders.example.org/check" &&
                        launch.Enabled && launch.Checked && profileCalls == 1,
                        "released UserProfile publishes its absolute SSH path without replacing the unsaved settings draft");
                    launch.Checked = false; ((Button)form.AcceptButton).PerformClick(); WaitAsyncSettings(form.SaveWork); PumpUntil(() => !form.Visible);
                    Check(form.DialogResult == DialogResult.OK && !File.Exists(fixture.Link) && settings.Current.TestEndpoint == endpoint.Text,
                        "real Save uses the atomically discovered shortcut manager and snapshot to remove only the chosen login registration");
                } finally { release.Set(); WaitAsyncSettings(form.StartupWork); WaitAsyncSettings(form.ProfilePathWork); }
            }
        }
        private static void SettingsFoldersFailure(SettingsService settings, SettingsFolderFixture fixture, Environment.SpecialFolder failedFolder, string result, bool save)
        {
            byte[] registration = File.ReadAllBytes(fixture.Link);
            using (var form = fixture.Form(settings, folder => {
                if (folder != failedFolder) return fixture.Folder(folder);
                if (result == "throw") throw new IOException("Fixture folder lookup failure");
                return result;
            })) {
                form.Show(); WaitAsyncSettings(form.StartupWork); WaitAsyncSettings(form.ProfilePathWork);
                var launch = (CheckBox)Field(form, "autoLaunch"); var notice = (Label)Field(form, "startupNotice");
                var path = (TextBox)Field(form, "sshConfigPath"); bool profileFailure = failedFolder == Environment.SpecialFolder.UserProfile;
                Check(profileFailure
                    ? (!form.IsProfilePathPending && path.Text.Contains("Не удалось определить") && !path.Text.Contains(".ssh") &&
                        path.AccessibleDescription == path.Text && launch.Enabled && launch.Checked)
                    : (!form.IsStartupPending && !launch.Enabled && !launch.Checked && Field(form, "startupSnapshot") == null &&
                        notice.Text.Contains("Не удалось проверить") && !notice.Text.Contains("вне папки") && path.Text == fixture.Config),
                    "folder " + failedFolder + " " + (result.Length == 0 ? "empty" : result) + " is unavailable without a guessed path or failure in the independent reader");
                if (profileFailure && result == "throw") {
                    ((TabControl)Field(form, "settingsTabs")).SelectedIndex = 3; Shot(form, "settings-folders-failed");
                }
                if (save) {
                    var endpoint = (TextBox)Field(form, "endpoint"); endpoint.Text = "https://failed-folders.example.org/check";
                    ((Button)form.AcceptButton).PerformClick(); WaitAsyncSettings(form.SaveWork); PumpUntil(() => !form.Visible);
                    Check(form.DialogResult == DialogResult.OK && settings.Current.TestEndpoint == endpoint.Text && registration.SequenceEqual(File.ReadAllBytes(fixture.Link)),
                        "failed default startup folder discovery permits a real unrelated Save and preserves exact login shortcut bytes");
                } else form.Close();
            }
        }
        private static void SettingsFoldersOwnerClosed(SettingsService settings, SettingsFolderFixture fixture, bool dispose)
        {
            byte[] registration = File.ReadAllBytes(fixture.Link), saved = File.ReadAllBytes(AppPaths.SettingsPath);
            using (var startupEntered = new ManualResetEventSlim())
            using (var startupRelease = new ManualResetEventSlim())
            using (var profileEntered = new ManualResetEventSlim())
            using (var profileRelease = new ManualResetEventSlim())
            using (var form = fixture.Form(settings, folder => {
                if (folder == Environment.SpecialFolder.Programs) { startupEntered.Set(); AsyncSettingsWait(startupRelease); }
                if (folder == Environment.SpecialFolder.UserProfile) { profileEntered.Set(); AsyncSettingsWait(profileRelease); }
                return fixture.Folder(folder);
            })) {
                form.Show(); PumpUntil(() => startupEntered.IsSet && profileEntered.IsSet);
                var startupWork = form.StartupWork; var profileWork = form.ProfilePathWork;
                var launch = (CheckBox)Field(form, "autoLaunch"); var notice = (Label)Field(form, "startupNotice"); var path = (TextBox)Field(form, "sshConfigPath");
                int changes = 0; launch.CheckedChanged += delegate { changes++; }; notice.TextChanged += delegate { changes++; }; path.TextChanged += delegate { changes++; };
                try {
                    var watch = Stopwatch.StartNew(); if (dispose) form.Dispose(); else form.Close();
                    Check(watch.ElapsedMilliseconds < 1000 && !startupWork.IsCompleted && !profileWork.IsCompleted && !form.IsStartupPending && !form.IsProfilePathPending,
                        "folder " + (dispose ? "Dispose" : "Close") + " returns immediately while retaining both actual held worker lifetimes");
                    // Native teardown may change disposed Text caches before the
                    // readers are released; only later publication is forbidden.
                    int closedChanges = changes; bool closedChecked = launch.Checked; string closedNotice = notice.Text, closedPath = path.Text;
                    profileRelease.Set(); bool profileSettled = profileWork.Wait(5000);
                    Check(profileSettled && !startupWork.IsCompleted && path.Text == closedPath && notice.Text == closedNotice && launch.Checked == closedChecked && changes == closedChanges,
                        "closed folder diagnostic worker settles without a UI pump and independently of the held startup provider");
                    startupRelease.Set(); bool startupSettled = startupWork.Wait(5000);
                    // Settlement above must not need dispatch. Afterwards drain
                    // any queued callbacks before asserting no late publication.
                    Application.DoEvents();
                    Check(startupSettled && Field(form, "startupSnapshot") == null && changes == closedChanges && path.Text == closedPath && notice.Text == closedNotice &&
                        launch.Checked == closedChecked && saved.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)) && registration.SequenceEqual(File.ReadAllBytes(fixture.Link)),
                        "closed folder startup worker settles without a UI pump and cannot publish or mutate either saved file");
                } finally {
                    startupRelease.Set(); profileRelease.Set(); WaitAsyncSettings(startupWork); WaitAsyncSettings(profileWork);
                }
            }
        }
        private static void SettingsFoldersInjectedAndPortable(SettingsService settings, SettingsFolderFixture fixture, bool injected)
        {
            int programs = 0, startup = 0, profile = 0;
            using (var form = fixture.Form(settings, folder => {
                if (folder == Environment.SpecialFolder.Programs) Interlocked.Increment(ref programs);
                if (folder == Environment.SpecialFolder.Startup) Interlocked.Increment(ref startup);
                if (folder == Environment.SpecialFolder.UserProfile) Interlocked.Increment(ref profile);
                return fixture.Folder(folder);
            }, injected, !injected)) {
                form.Show(); WaitAsyncSettings(form.StartupWork); WaitAsyncSettings(form.ProfilePathWork);
                var launch = (CheckBox)Field(form, "autoLaunch"); var path = (TextBox)Field(form, "sshConfigPath");
                Check(programs == 0 && startup == 0 && profile == 1 && path.Text == fixture.Config &&
                    (injected ? launch.Enabled && launch.Checked && Object.ReferenceEquals(Field(form, "startupShortcuts"), fixture.Shortcuts)
                        : !launch.Enabled && !form.IsStartupPending && Field(form, "startupSnapshot") == null && ((Label)Field(form, "startupNotice")).Text.Contains("вне папки")),
                    injected ? "injected shortcut manager retains its real registration workflow without default Programs or Startup lookup"
                        : "portable settings skip Programs and Startup discovery while independently resolving the diagnostic SSH path");
                form.Close();
            }
        }
    }
}
