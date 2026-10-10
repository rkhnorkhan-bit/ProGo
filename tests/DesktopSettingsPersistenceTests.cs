using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void SettingsPersistenceWorkflow()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") return;
            using (var instance = new ApplicationInstance())
            {
                Check(instance.IsOwner, "settings persistence fixture owns the real application instance");
                AppPaths.EnsureDirectories();
                var original = File.Exists(AppPaths.SettingsPath) ? File.ReadAllBytes(AppPaths.SettingsPath) : null;
                var json = new JavaScriptSerializer();
                Task first = null, second = null;
                using (var entered = new ManualResetEventSlim())
                using (var release = new ManualResetEventSlim())
                using (var secondEntered = new ManualResetEventSlim())
                try
                {
                    var initial = AppSettings.Defaults(); initial.SshProfile = "first"; initial.AutoSwitchSshProfile = true;
                    initial.SshProfiles = new List<SshProfileSetting> {
                        new SshProfileSetting { Name = "First", Target = "first" },
                        new SshProfileSetting { Name = "Second", Target = "second" },
                        new SshProfileSetting { Name = "Owner", Target = "owner-choice" }
                    };
                    var payload = json.Deserialize<Dictionary<string, object>>(json.Serialize(initial));
                    payload.Remove("SocksHost"); payload.Add("sockshost", "127.0.0.1");
                    payload.Add("FutureFlag", true); payload.Add("futureflag", false);
                    payload.Add("futureOptions", new Dictionary<string, object> { { "steps", new[] { 1, 2, 3 } }, { "mode", "kept" } });
                    payload.Add("AutoApplyProxy", true); payload.Add("AutoCodexProxy", true);
                    File.WriteAllText(AppPaths.SettingsPath, json.Serialize(payload));
                    var futureOptions = json.Serialize(payload["futureOptions"]);
                    int writes = 0, block = 0;
                    using (var settings = new SettingsService(delegate {
                        if (Interlocked.Increment(ref writes) == 1 && Volatile.Read(ref block) != 0) {
                            entered.Set(); release.Wait();
                        }
                    }))
                    using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
                    {
                        int pulses = 0; pulse.Tick += delegate { pulses++; }; pulse.Start();
                        var before = settings.Capture(); var beforeReference = settings.Current;
                        var beforeBytes = File.ReadAllBytes(AppPaths.SettingsPath);
                        var beforeTemporary = Directory.GetFiles(AppPaths.Root, "settings.json.*.tmp").OrderBy(p => p).ToArray();
                        var proposedA = before.Settings.Clone(); proposedA.TestEndpoint = "https://a.example.org/check";
                        var proposedB = before.Settings.Clone(); proposedB.TestEndpoint = "https://b.example.org/check"; proposedB.ClipboardClearSeconds = 90;
                        Volatile.Write(ref block, 1);
                        first = Task.Run(() => settings.Save(proposedA)); PumpUntil(() => entered.IsSet);
                        second = Task.Run(delegate { secondEntered.Set(); settings.Save(proposedB); });
                        PumpUntil(() => secondEntered.IsSet && pulses >= 3);
                        var watch = Stopwatch.StartNew();
                        for (int i = 0; i < 1000; i++) {
                            var current = settings.Current; var revision = settings.Revision;
                            if (current.TestEndpoint != before.Settings.TestEndpoint || revision != before.Revision) throw new Exception("uncommitted settings leaked");
                        }
                        var captured = settings.Capture(); watch.Stop();
                        Check(watch.ElapsedMilliseconds < 400 && pulses >= 3 && !first.IsCompleted && !second.IsCompleted && Volatile.Read(ref writes) == 1,
                            "native heartbeat and memory getters stay responsive while one real atomic settings writer stalls and the second waits");
                        Check(Object.ReferenceEquals(settings.Current, beforeReference) && captured.Revision == before.Revision &&
                            File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(beforeBytes),
                            "a staged temporary settings file is not published as committed memory or installed settings");
                        release.Set(); PumpUntil(() => first.IsCompleted && second.IsCompleted);
                        first.GetAwaiter().GetResult(); second.GetAwaiter().GetResult(); first = second = null;
                        Check(Volatile.Read(ref writes) == 2 && settings.Revision == before.Revision + 2 &&
                            settings.Current.TestEndpoint == proposedB.TestEndpoint && settings.Current.ClipboardClearSeconds == 90 &&
                            json.Serialize(SettingsService.DeserializeSettings(File.ReadAllText(AppPaths.SettingsPath))) == json.Serialize(settings.Current),
                            "serialized actual file replacements finish with the same complete B settings in durable storage and memory and two revisions");
                        Check(!Object.ReferenceEquals(settings.Current, proposedB), "successful publication owns a detached settings object");
                        proposedA.TestEndpoint = "https://changed-a.example.org/"; proposedB.TestEndpoint = "https://changed-b.example.org/";
                        Check(settings.Current.TestEndpoint == "https://b.example.org/check" &&
                            Directory.GetFiles(AppPaths.Root, "settings.json.*.tmp").OrderBy(p => p).SequenceEqual(beforeTemporary),
                            "later caller mutations cannot alter committed settings and completed writers leave no owned temporary files");
                        var saved = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(AppPaths.SettingsPath));
                        Check(saved.ContainsKey("SocksHost") && !saved.ContainsKey("sockshost") && (bool)saved["FutureFlag"] && !(bool)saved["futureflag"] &&
                            json.Serialize(saved["futureOptions"]) == futureOptions && !saved.ContainsKey("AutoApplyProxy") && !saved.ContainsKey("AutoCodexProxy") &&
                            !settings.Current.AutoCliProxy,
                            "canonical known fields preserve unknown top-level JSON and nested unknown values without reviving deprecated migrated flags");

                        Volatile.Write(ref block, 0);
                        var stable = settings.Capture(); var stableReference = settings.Current; var stableBytes = File.ReadAllBytes(AppPaths.SettingsPath);
                        var invalidInput = stable.Settings.Clone(); invalidInput.SocksHost = " "; invalidInput.SocksPort = -1; invalidInput.ClipboardClearSeconds = 1;
                        bool failed = false;
                        using (var locked = new FileStream(AppPaths.SettingsPath, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            first = Task.Run(delegate {
                                try { settings.Save(invalidInput); }
                                catch (IOException) { failed = true; }
                                catch (UnauthorizedAccessException) { failed = true; }
                            });
                            PumpUntil(() => first.IsCompleted); first.GetAwaiter().GetResult(); first = null;
                            Check(failed && Object.ReferenceEquals(settings.Current, stableReference) && settings.Revision == stable.Revision &&
                                json.Serialize(settings.Current) == json.Serialize(stable.Settings),
                                "actual denied file replacement leaves the old memory publication and revision intact");
                        }
                        Check(invalidInput.SocksHost == " " && invalidInput.SocksPort == -1 && invalidInput.ClipboardClearSeconds == 1 &&
                            File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(stableBytes) &&
                            Directory.GetFiles(AppPaths.Root, "settings.json.*.tmp").OrderBy(p => p).SequenceEqual(beforeTemporary),
                            "failed normalization and commit do not mutate caller values, installed bytes or leak a staged file");

                        var stale = settings.Capture();
                        var userEdit = stale.Settings.Clone(); userEdit.SshProfile = "owner-choice"; userEdit.TestEndpoint = "https://owner.example.org/check";
                        userEdit.ClipboardClearSeconds = 222; settings.Save(userEdit);
                        var user = settings.Capture(); var userBytes = File.ReadAllBytes(AppPaths.SettingsPath); int userWrites = Volatile.Read(ref writes);
                        Check(!settings.TrySelectWorkingProfile(stale, "second") && settings.Revision == user.Revision && Volatile.Read(ref writes) == userWrites &&
                            File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(userBytes) && json.Serialize(settings.Current) == json.Serialize(user.Settings),
                            "stale automatic SSH selection is rejected without a file write or reverting any newer user setting");

                        using (var proxy = new ProxyService(settings))
                        {
                            var owned = typeof(ProxyService).GetField("ownedSettingsSnapshot", BindingFlags.Instance | BindingFlags.NonPublic);
                            var select = typeof(ProxyService).GetMethod("SelectWorkingProfile", BindingFlags.Instance | BindingFlags.NonPublic);
                            var proxyGate = Field(proxy, "gate");
                            owned.SetValue(proxy, stale);
                            bool selected = true;
                            first = Task.Run(delegate { lock (proxyGate) selected = (bool)select.Invoke(proxy, new object[] { "second" }); });
                            PumpUntil(() => first.IsCompleted); first.GetAwaiter().GetResult(); first = null;
                            Check(!selected && Volatile.Read(ref writes) == userWrites && settings.Revision == user.Revision,
                                "production ProxyService auto-selection wiring rejects the captured stale revision while holding its real process gate");
                            owned.SetValue(proxy, user);
                            first = Task.Run(delegate { lock (proxyGate) selected = (bool)select.Invoke(proxy, new object[] { "second" }); });
                            PumpUntil(() => first.IsCompleted); first.GetAwaiter().GetResult(); first = null;
                            var expected = user.Settings.Clone(); expected.SshProfile = "second";
                            Check(selected && settings.Revision == user.Revision + 1 && json.Serialize(settings.Current) == json.Serialize(expected) &&
                                json.Serialize(SettingsService.DeserializeSettings(File.ReadAllText(AppPaths.SettingsPath))) == json.Serialize(expected),
                                "fresh production automatic selection commits only the successful profile and preserves all other latest settings");
                            Check(user.Settings.SshProfile == "owner-choice", "automatic selection does not mutate its captured snapshot");

                            var profileBefore = settings.Capture(); var profileReference = settings.Current;
                            var profileBytes = File.ReadAllBytes(AppPaths.SettingsPath); owned.SetValue(proxy, profileBefore);
                            bool profileFailure = false;
                            using (var locked = new FileStream(AppPaths.SettingsPath, FileMode.Open, FileAccess.Read, FileShare.None)) {
                                first = Task.Run(delegate {
                                    try { lock (proxyGate) select.Invoke(proxy, new object[] { "first" }); }
                                    catch (TargetInvocationException ex) {
                                        if (!(ex.InnerException is IOException) && !(ex.InnerException is UnauthorizedAccessException)) throw;
                                        profileFailure = true;
                                    }
                                });
                                PumpUntil(() => first.IsCompleted); first.GetAwaiter().GetResult(); first = null;
                                Check(profileFailure && Object.ReferenceEquals(settings.Current, profileReference) && settings.Revision == profileBefore.Revision &&
                                    json.Serialize(settings.Current) == json.Serialize(profileBefore.Settings),
                                    "failed production automatic selection cannot publish its target before durable replacement");
                            }
                            Check(File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(profileBytes), "failed automatic selection preserves actual settings bytes");

                            bool restartFailure = false;
                            using (var locked = new FileStream(AppPaths.SettingsPath, FileMode.Open, FileAccess.Read, FileShare.None)) {
                                first = Task.Run(delegate {
                                    try { proxy.SetAutoRestart(false); }
                                    catch (IOException) { restartFailure = true; }
                                    catch (UnauthorizedAccessException) { restartFailure = true; }
                                });
                                PumpUntil(() => first.IsCompleted); first.GetAwaiter().GetResult(); first = null;
                                Check(restartFailure && Object.ReferenceEquals(settings.Current, profileReference) && settings.Current.AutoRestartSocks &&
                                    settings.Revision == profileBefore.Revision, "failed production recovery toggle preserves the live preference and revision");
                            }
                            entered.Reset(); release.Reset(); Volatile.Write(ref writes, 0); Volatile.Write(ref block, 1);
                            int after = pulses;
                            first = Task.Run(() => proxy.SetAutoRestart(false)); PumpUntil(() => entered.IsSet && pulses >= after + 3);
                            watch.Restart(); var recovery = proxy.RecoveryStatus; var memory = settings.Current; var snapshot = settings.Capture(); watch.Stop();
                            Check(watch.ElapsedMilliseconds < 400 && !first.IsCompleted && memory.AutoRestartSocks && snapshot.Revision == profileBefore.Revision &&
                                recovery.Contains("Проверяем"),
                                "real proxy-to-settings writer order leaves native UI and opportunistic proxy status usable without a reverse gate callback");
                            release.Set(); PumpUntil(() => first.IsCompleted); first.GetAwaiter().GetResult(); first = null;
                            expected = profileBefore.Settings.Clone(); expected.AutoRestartSocks = false;
                            Check(settings.Revision == profileBefore.Revision + 1 && json.Serialize(settings.Current) == json.Serialize(expected) &&
                                json.Serialize(SettingsService.DeserializeSettings(File.ReadAllText(AppPaths.SettingsPath))) == json.Serialize(expected),
                                "successful recovery toggle publishes exactly the latest preference change after its actual atomic commit");
                        }
                        pulse.Stop();
                    }
                }
                finally
                {
                    release.Set();
                    try {
                        if (first != null) { PumpUntil(() => first.IsCompleted); first.GetAwaiter().GetResult(); }
                        if (second != null) { PumpUntil(() => second.IsCompleted); second.GetAwaiter().GetResult(); }
                    } finally {
                        if (original == null) { if (File.Exists(AppPaths.SettingsPath)) File.Delete(AppPaths.SettingsPath); }
                        else File.WriteAllBytes(AppPaths.SettingsPath, original);
                    }
                }
            }
        }
    }
}
