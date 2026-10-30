using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void SettingsLoadFailureWorkflow()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: settings initial-load preservation requires isolated native CI"); return;
            }
            int before = passed;
            using (var instance = new ApplicationInstance()) {
                Check(instance.IsOwner, "initial-load fixture owns the application instance before touching isolated settings");
                AppPaths.EnsureDirectories();
                var original = File.Exists(AppPaths.SettingsPath) ? File.ReadAllBytes(AppPaths.SettingsPath) : null;
                try {
                    SettingsLockedInitialLoad();
                    SettingsMalformedInitialLoad();
                    SettingsMissingInitialLoad();
                    SettingsMissingCreationRace();
                    SettingsDetachedReadFailure();
                } finally {
                    if (original == null) { if (File.Exists(AppPaths.SettingsPath)) File.Delete(AppPaths.SettingsPath); }
                    else File.WriteAllBytes(AppPaths.SettingsPath, original);
                }
            }
            Console.WriteLine("Settings initial-load preservation checks PASS: " + (passed - before));
        }
        private static byte[] SettingsLoadFixtureBytes()
        {
            var json = new JavaScriptSerializer();
            var settings = AppSettings.Defaults(); settings.SshProfile = "preserved.fixture";
            settings.SshProfiles = new List<SshProfileSetting> {
                new SshProfileSetting { Name = "Preserved", Target = "preserved.fixture" },
                new SshProfileSetting { Name = "Backup", Target = "backup.fixture" }
            };
            var values = json.Deserialize<Dictionary<string, object>>(json.Serialize(settings));
            values.Add("futureFlag", true);
            values.Add("futureOptions", new Dictionary<string, object> { { "mode", "preserved" }, { "steps", new[] { 1, 2, 3 } } });
            return System.Text.Encoding.UTF8.GetBytes(json.Serialize(values));
        }
        private static void SettingsLoadAssertPreserved(SettingsService service, byte[] bytes, AppSettings memory, long revision, string[] temporary, string label)
        {
            Check(File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(bytes) && Object.ReferenceEquals(service.Current, memory) && service.Revision == revision &&
                Directory.GetFiles(AppPaths.Root, "settings.json.*.tmp").OrderBy(p => p).SequenceEqual(temporary),
                "untrusted initial settings preserve source bytes memory revision and staging ownership after " + label);
        }
        private static void SettingsLoadWritesBlocked(SettingsService service, byte[] bytes, Func<int> writes)
        {
            var memory = service.Current; long revision = service.Revision;
            var temporary = Directory.GetFiles(AppPaths.Root, "settings.json.*.tmp").OrderBy(p => p).ToArray();
            var candidate = service.Current.Clone(); candidate.ClipboardClearSeconds = 123;
            var actions = new Dictionary<string, Action> {
                { "Save", () => service.Save(candidate) },
                { "TrySave", () => { SettingsCommitReceipt receipt; service.TrySave(revision, candidate, out receipt); } },
                { "tray preference", () => service.MarkTrayCloseExplained(CancellationToken.None) },
                { "rollback", () => service.TryRollback(new SettingsCommitReceipt(service.Capture(), revision)) },
                { "recovery preference", () => service.SetAutoRestart(!service.Current.AutoRestartSocks) },
                { "asynchronous Save", () => Task.Run(() => service.Save(candidate)).GetAwaiter().GetResult() }
            };
            foreach (var operation in actions) {
                bool blocked = false;
                try { operation.Value(); }
                catch (SettingsInitialLoadException ex) {
                    blocked = ex.Message.Contains("Исходные данные не изменены") && ex.Message.Contains("перезапустите ProGo") &&
                        !ex.Message.Contains(AppPaths.SettingsPath) && !ex.Message.Contains("preserved.fixture");
                }
                Check(blocked, "failed initial load rejects " + operation.Key + " with a fixed actionable nonsecret error");
                SettingsLoadAssertPreserved(service, bytes, memory, revision, temporary, operation.Key);
            }
            SettingsCommitReceipt rejected = null;
            bool failed = false;
            try { service.TrySave(revision, candidate, out rejected); } catch (SettingsInitialLoadException) { failed = true; }
            Check(failed && rejected == null && writes() == 0, "blocked conditional commits issue no receipt and never reach the real replacement boundary");
            Check(!service.TrySelectWorkingProfile(service.Capture(), "backup.fixture") && service.TryRollback(null),
                "nonmutating stale-profile and empty-rollback paths cannot create a write after an initial-load failure");
            SettingsLoadAssertPreserved(service, bytes, memory, revision, temporary, "nonmutating paths");
        }
        private static void SettingsLoadAssertProfilesAndUnknown(SettingsService service)
        {
            Check(service.Current.SshProfile == "preserved.fixture" && service.Current.SshProfiles.Count == 2 &&
                service.Current.SshProfiles[1].Target == "backup.fixture", "a fresh trusted service recovers both original profiles after the read fault is removed");
            var changed = service.Current.Clone(); changed.ClipboardClearSeconds = 124; service.Save(changed);
            var json = new JavaScriptSerializer();
            var saved = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(AppPaths.SettingsPath));
            var original = json.Deserialize<Dictionary<string, object>>(System.Text.Encoding.UTF8.GetString(SettingsLoadFixtureBytes()));
            Check((bool)saved["futureFlag"] && json.Serialize(saved["futureOptions"]) == json.Serialize(original["futureOptions"]) &&
                SettingsService.DeserializeSettings(File.ReadAllText(AppPaths.SettingsPath)).SshProfiles.Count == 2 && service.Current.ClipboardClearSeconds == 124,
                "a fresh trusted unrelated save preserves original profiles and unknown nested fields");
        }
        private static void SettingsLockedInitialLoad()
        {
            byte[] bytes = SettingsLoadFixtureBytes(); File.WriteAllBytes(AppPaths.SettingsPath, bytes);
            SettingsService failed; int writes = 0;
            using (var held = new FileStream(AppPaths.SettingsPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                failed = new SettingsService(() => Interlocked.Increment(ref writes));
                Check(failed.Current != null && failed.Current.SshProfile == "" && writes == 0,
                    "an actual exclusive file lock produces unsaved fallback settings without invoking a writer");
            }
            using (failed) {
                SettingsLoadWritesBlocked(failed, bytes, () => writes);
                var memory = failed.Current; long revision = failed.Revision;
                var detached = failed.Load();
                Check(detached.SshProfiles.Count == 2 && Object.ReferenceEquals(failed.Current, memory) && failed.Revision == revision,
                    "a successful later detached read does not publish data or silently rearm an untrusted instance");
                SettingsLoadWritesBlocked(failed, bytes, () => writes);
            }
            Check(File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(bytes), "disposing the failed-load instance cannot overwrite its unread original");
            using (var fresh = new SettingsService()) SettingsLoadAssertProfilesAndUnknown(fresh);
        }
        private static void SettingsMalformedInitialLoad()
        {
            foreach (string invalid in new[] { "{", "null" }) {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(invalid); File.WriteAllBytes(AppPaths.SettingsPath, bytes);
                int writes = 0;
                using (var failed = new SettingsService(() => Interlocked.Increment(ref writes))) {
                    SettingsLoadWritesBlocked(failed, bytes, () => writes);
                }
                Check(File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(bytes), "malformed or nonobject JSON remains byte-identical after attempted saves and disposal");
            }
        }
        private static void SettingsMissingInitialLoad()
        {
            File.Delete(AppPaths.SettingsPath); int writes = 0;
            using (var service = new SettingsService(() => Interlocked.Increment(ref writes))) {
                Check(File.Exists(AppPaths.SettingsPath) && service.Current.SshProfiles.Count == 0 && service.Revision == 1 && writes == 1,
                    "a genuinely missing settings file creates normal defaults through one exclusive initial publication");
                var changed = service.Current.Clone(); changed.ClipboardClearSeconds = 125; service.Save(changed);
                Check(writes == 2 && service.Current.ClipboardClearSeconds == 125 &&
                    SettingsService.DeserializeSettings(File.ReadAllText(AppPaths.SettingsPath)).ClipboardClearSeconds == 125,
                    "genuinely new settings remain writable after initial default publication");
            }
        }
        private static void SettingsMissingCreationRace()
        {
            File.Delete(AppPaths.SettingsPath); byte[] appeared = SettingsLoadFixtureBytes(); int writes = 0;
            using (var failed = new SettingsService(() => { Interlocked.Increment(ref writes); File.WriteAllBytes(AppPaths.SettingsPath, appeared); })) {
                Check(writes == 1 && File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(appeared) && failed.Revision == 0,
                    "settings appearing after a missing read are preserved rather than overwritten by initial defaults");
                int attemptsAfterConstruction = writes;
                SettingsLoadWritesBlocked(failed, appeared, () => writes - attemptsAfterConstruction);
            }
            using (var fresh = new SettingsService()) SettingsLoadAssertProfilesAndUnknown(fresh);
        }
        private static void SettingsDetachedReadFailure()
        {
            byte[] bytes = SettingsLoadFixtureBytes(); File.WriteAllBytes(AppPaths.SettingsPath, bytes);
            using (var trusted = new SettingsService()) {
                var memory = trusted.Current; long revision = trusted.Revision;
                using (var held = new FileStream(AppPaths.SettingsPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                    var detached = trusted.Load();
                    Check(detached.SshProfiles.Count == 0 && Object.ReferenceEquals(trusted.Current, memory) && trusted.Revision == revision,
                        "failed external detached Load does not replace or poison an already trusted memory publication");
                }
                SettingsLoadAssertProfilesAndUnknown(trusted);
                memory = trusted.Current; revision = trusted.Revision;
                File.Delete(AppPaths.SettingsPath);
                var missing = trusted.Load();
                Check(!File.Exists(AppPaths.SettingsPath) && missing.SshProfiles.Count == 0 && Object.ReferenceEquals(trusted.Current, memory) && trusted.Revision == revision,
                    "a detached missing read cannot create defaults or replace a trusted live publication");
                var retained = trusted.Current.Clone(); retained.ClipboardClearSeconds = 126; trusted.Save(retained);
                Check(File.Exists(AppPaths.SettingsPath) && trusted.Current.SshProfiles.Count == 2,
                    "an explicit later save from a trusted instance can republish its retained profiles");
            }
        }
    }
}
