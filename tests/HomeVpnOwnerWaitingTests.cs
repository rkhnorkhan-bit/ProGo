using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    // Real production dispatch and owned Windows process trees; only SCP and
    // the SSH executable are replaced. No live VPS or owner credentials are used.
    internal static class HomeVpnOwnerWaitingTests
    {
        private const string Variable = "PROGO_OWNER_WAIT_FIXTURE";
        private const string OldId = "aaaaaaaaaaaaaaaaaaaaaaaa";
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        private static Action<bool, string> check;
        private static string folder, replacement, sharePath;
        private static string[] inputPaths;
        private static int copies;
        private static HomeVpnOwner Owner { get { return new HomeVpnOwner { Host = "vpn.example.org", Login = "root", Port = 22, KeyFile = "" }; } }
        private static string At(string name) { return Path.Combine(folder, name); }
        private static string PendingPath { get { return Path.Combine(HomeVpnPrivateFiles.Root, "admin-request.dat"); } }
        private static string SharePendingPath { get { return Path.Combine(HomeVpnPrivateFiles.Root, "share-request.dat"); } }

        internal static bool Fixture(string[] args)
        {
            if (args.Length == 1 && args[0] == "owner-wait-unrelated") { Thread.Sleep(300000); return true; }
            if (args.Length == 2 && args[0] == "owner-wait-output") {
                string root = args[1];
                HomeVpnFixtureFiles.Publish(Path.Combine(root, "pid"), Process.GetCurrentProcess().Id.ToString());
                uint inputMode, errorMode;
                File.WriteAllText(Path.Combine(root, "console"), GetConsoleCP() + ":" + GetConsoleMode(GetStdHandle(-10), out inputMode) + ":" + GetConsoleMode(GetStdHandle(-12), out errorMode));
                if (File.ReadAllText(Path.Combine(root, "mode")) == "raw-overflow") {
                    Process.Start(new ProcessStartInfo(Application.ExecutablePath, "owner-wait-child " + HomeVpnService.Argument(root)) { UseShellExecute = false, CreateNoWindow = true }).Dispose();
                    var watch = Stopwatch.StartNew();
                    while (!File.Exists(Path.Combine(root, "child"))) { Thread.Sleep(10); if (watch.ElapsedMilliseconds > 5000) throw new Exception("Output fixture child did not start"); }
                }
                byte[] bytes = File.ReadAllBytes(Path.Combine(root, "raw-input"));
                using (var stream = Console.OpenStandardOutput()) stream.Write(bytes, 0, bytes.Length);
                return true;
            }
            bool child = args.Length == 2 && args[0] == "owner-wait-child";
            bool copy = args.Length == 2 && args[0] == "owner-wait-copy";
            string path = child || copy ? args[1] : Environment.GetEnvironmentVariable(Variable);
            if (!child && !copy && (args.Length == 0 || args[0] != "-o" || String.IsNullOrEmpty(path))) return false;
            if (child) { HomeVpnFixtureFiles.Publish(Path.Combine(path, "child"), Process.GetCurrentProcess().Id.ToString()); Thread.Sleep(60000); return true; }
            string command = copy ? "" : args.Last();
            string kind = copy ? "copy" : new[] { "revoke", "repair", "share", "invite" }.FirstOrDefault(action => command.Contains("home_vpn_setup.py " + action + " "));
            if (kind == null || (!copy && (!args.Contains("-T") || !args.Contains("root@vpn.example.org") || !args.Contains("22")
                || !command.Contains("trap 'rm -rf -- /tmp/") || !command.Contains(" --output /tmp/")))) throw new Exception("Unexpected production owner SSH dispatch");
            File.AppendAllText(Path.Combine(path, "calls"), kind + "\n");
            File.AppendAllText(Path.Combine(path, "argv"), Json.Serialize(args) + "\n");
            HomeVpnFixtureFiles.Publish(Path.Combine(path, "pid"), Process.GetCurrentProcess().Id.ToString());
            string token = File.ReadAllText(Path.Combine(path, "response"));
            var access = HomeVpnAccess.Parse(token);
            if (!copy && (command.Contains(token) || command.Contains(access.Password) || command.Contains(access.PrivateKey)
                || args.Any(value => value == "StrictHostKeyChecking=no"))) throw new Exception("Owner dispatch exposed a secret or disabled host-key checking");
            if (kind == "invite") {
                var request = HomeVpnAdminRecovery.Pending();
                var ids = Regex.Matches(command, @"--request-id ([0-9a-f]{32})").Cast<Match>().Select(match => match.Groups[1].Value).ToArray();
                if (request == null || request.SourceInviteId != OldId || request.Name != "Друг" || ids.Length != 3 || ids.Any(id => id != request.RequestId)
                    || !command.Contains(" --id " + OldId) || !command.Contains(" --name 'Друг'")
                    || command.IndexOf("home_vpn_setup.py invite ", StringComparison.Ordinal) >= command.IndexOf("home_vpn_setup.py operation-status ", StringComparison.Ordinal)
                    || command.IndexOf("home_vpn_setup.py operation-status ", StringComparison.Ordinal) >= command.IndexOf("home_vpn_setup.py operation-result ", StringComparison.Ordinal))
                    throw new Exception("Reissue lost the acknowledged source ID or frozen production receipt command");
            } else if (kind == "share") {
                var request = HomeVpnShareRecovery.Pending();
                var ids = Regex.Matches(command, @"--request-id ([0-9a-f]{32})").Cast<Match>().Select(match => match.Groups[1].Value).ToArray();
                if (request == null || request.Domain != "qr.example.org" || ids.Length != 3 || ids.Any(id => id != request.RequestId)
                    || !command.Contains("operation-status") || !command.Contains("operation-result")) throw new Exception("Share lost its saved request or production receipt framing");
            } else if (!copy && (command.Contains("--request-id") || command.Contains("operation-status") || command.Contains("operation-result")))
                throw new Exception("An ordinary owner action fabricated a recovery request");
            if (kind == "revoke" && !command.Contains(" --id " + OldId)) throw new Exception("Revoke did not retain the selected ID");
            if (kind == "share" && !command.Contains(" --domain 'qr.example.org'")) throw new Exception("Share did not retain its normalized domain");
            string mode = File.ReadAllText(Path.Combine(path, "mode"));
            if (mode == "hold" && kind != "invite") {
                using (var descendant = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "owner-wait-child " + HomeVpnService.Argument(path)) { UseShellExecute = false, CreateNoWindow = true })) {
                    var watch = Stopwatch.StartNew();
                    while (!File.Exists(Path.Combine(path, "child"))) { Thread.Sleep(10); if (watch.ElapsedMilliseconds > 5000) throw new Exception("Owner fixture child did not start"); }
                    Thread.Sleep(60000);
                }
                return true;
            }
            if (mode == "failure") { Console.Error.WriteLine("synthetic-private-owner-error"); Environment.Exit(7); return true; }
            Console.OutputEncoding = new UTF8Encoding(false);
            if (mode == "oversized") { Console.Write(new string('x', 1000000)); return true; }
            if (kind == "invite") {
                var request = HomeVpnAdminRecovery.Pending();
                Console.WriteLine(Json.Serialize(new Dictionary<string, object> { { "Version", 1 }, { "RequestId", request.RequestId }, { "Action", "invite" },
                    { "State", "succeeded" }, { "Started", "2026-01-01T00:00:00+00:00" }, { "Finished", "2026-01-01T00:00:01+00:00" }, { "ResultAvailable", true } }));
                Console.WriteLine(token);
            } else if (kind == "share") {
                var request = HomeVpnShareRecovery.Pending();
                Console.WriteLine(Json.Serialize(new Dictionary<string, object> { { "Version", 1 }, { "RequestId", request.RequestId }, { "Action", "share" },
                    { "State", "succeeded" }, { "Started", "2026-01-01T00:00:00+00:00" }, { "Finished", "2026-01-01T00:00:01+00:00" }, { "ResultAvailable", true } }));
                Console.Write("https://qr.example.org");
            } else Console.Write(kind == "revoke" ? "Access revoked." : "VPN network rules refreshed. Reconnect the phone and test internet access.");
            return true;
        }

        internal static void Run(Action<bool, string> assert, string token, ClipboardService clipboard, string work)
        {
            check = assert; folder = Path.Combine(work, "owner-wait"); HomeVpnPrivateFiles.SecureDirectory(folder);
            replacement = Replacement(token); sharePath = Path.Combine(HomeVpnPrivateFiles.Root, "share-" + HomeVpnAccess.Parse(token).ServerId + ".dat");
            var saved = new Dictionary<string, byte[]>();
            foreach (string name in new[] { "access", "owner", "home-address", "setup-request", "admin-request", "share-request" }) Save(saved, Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat"));
            Save(saved, sharePath);
            string helpers = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "home-vpn", "server"); Directory.CreateDirectory(helpers);
            foreach (string file in new[] { "home_vpn_setup.py", "ikev2_relay.py", "install-ikev2-relay.sh", "profile_share_setup.py", "profile_share.py", "qrcodegen.py", "QR_LICENSE.txt" }) {
                string target = Path.Combine(helpers, file); Save(saved, target);
                File.Copy(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Application.ExecutablePath)), "server", file), target, true);
            }
            inputPaths = saved.Keys.ToArray();
            string previous = Environment.GetEnvironmentVariable(Variable); Exception threadFailure = null;
            ThreadExceptionEventHandler handler = (sender, args) => { threadFailure = args.Exception; }; Application.ThreadException += handler;
            using (var unrelated = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "owner-wait-unrelated") { UseShellExecute = false, CreateNoWindow = true })) {
                try {
                    File.Delete(Path.Combine(HomeVpnPrivateFiles.Root, "setup-request.dat")); File.Delete(PendingPath); File.Delete(SharePendingPath);
                    HomeVpnPrivateFiles.Save("access", token); HomeVpnPrivateFiles.Save("owner", Json.Serialize(Owner));
                    Environment.SetEnvironmentVariable(Variable, folder);
                    OwnedRepair(token, clipboard);
                    foreach (string action in new[] { "revoke", "repair", "share" }) Entry(token, clipboard, action, "button", false, false);
                    foreach (string route in new[] { "escape", "close", "dispose", "deadline" }) Entry(token, clipboard, "repair", route, false, false);
                    Entry(token, clipboard, "reissue", "button", false, false);
                    Entry(token, clipboard, "reissue", "complete", false, true);
                    foreach (string action in new[] { "revoke", "reissue", "share" }) Entry(token, clipboard, action, "button", true, false);
                    CommandOutputLimits(); Outputs(saved); AcceptedCancellation();
                    check(threadFailure == null, "ordinary owner waiting and UI completion cause no thread exception");
                    check(!unrelated.HasExited, "owner cancellation and deadlines preserve an unrelated process");
                    check(Directory.GetDirectories(HomeVpnPrivateFiles.Root, "admin-*").Length == 0, "owner waits settle their private dispatch work");
                } finally {
                    Application.ThreadException -= handler; Environment.SetEnvironmentVariable(Variable, previous); KillFixtures();
                    if (!unrelated.HasExited) { unrelated.Kill(); unrelated.WaitForExit(2000); }
                    foreach (var item in saved) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); }
                }
            }
        }
        private static async Task HeldRepairCopy(Form owner, Action<HomeVpnPreparationForm> publish, Action<CancellationToken> read)
        {
            using (var wait = new HomeVpnPreparationForm(token => Task.Run(() => read(token)))) {
                publish(wait); wait.ShowDialog(owner); await wait.Completion;
            }
        }
        private static Exception RepairFailure(Task task)
        { try { task.GetAwaiter().GetResult(); return null; } catch (Exception ex) { return ex; } }
        private static void OwnedRepair(string token, ClipboardService clipboard)
        {
            foreach (string mode in new[] { "default-refusal", "complete", "cancel-retry", "stop-copy", "dispose-copy", "owner-dispose", "stop-command", "cleanup-lock" }) {
                Reset(mode == "stop-command" ? "hold" : "complete");
                using (var relay = new Ikev2RelayService())
                using (var service = new HomeVpnService(relay))
                using (var entered = new ManualResetEventSlim())
                using (var release = new ManualResetEventSlim()) {
                    service.UseToken(token, Owner);
                    var before = Snapshot(inputPaths);
                    string sibling = Path.Combine(HomeVpnPrivateFiles.Root, "admin-unrelated-" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(sibling); File.WriteAllText(Path.Combine(sibling, "keep"), "unrelated");
                    var directories = Directory.GetDirectories(HomeVpnPrivateFiles.Root);
                    string coreWork = null; FileStream held = null; HomeVpnPreparationForm copyWindow = null;
                    HomeVpnWizardForm wizard = null; Task<string> operation = null, cancelled = null; Task<bool> stop = null;
                    Exception cancellation = null; int phase = 0, turns = 0, worker = 0, attempts = 0, retained = 0, closedPublications = 0;
                    int ui = Thread.CurrentThread.ManagedThreadId; bool closing = mode == "owner-dispose", helperRead = false;
                    try {
                        if (mode == "default-refusal") {
                            service.RetainAdminCleanup(() => retained++);
                            // A broken legacy binding fails validation locally;
                            // this negative probe must never start real OpenSSH.
                            service.Owner.KeyFile = Path.Combine(folder, "missing-owned-repair-key");
                        }
                        wizard = new HomeVpnWizardForm(service, clipboard); HomeVpnFixtureFiles.ConfigureReady(wizard);
                        typeof(HomeVpnWizardForm).GetMethod("ShowStep", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(wizard, new object[] { 4 });
                        var dispatcher = (HomeProfileUiDispatcher)Field(wizard, "httpDispatcher");
                        var button = Controls(wizard).OfType<Button>().Single(b => b.Text == "Исправить выход VPN в интернет");
                        if (mode != "default-refusal") wizard.Admin = (owner, action, label, id, report) => {
                            check(action == "repair", "owned repair slice cannot dispatch another Admin action");
                            operation = service.RepairAdminOwnedAsync(owner, wizard, dispatcher, report, (executable, arguments) => {
                                check(Thread.CurrentThread.ManagedThreadId == ui && executable == "scp.exe" && arguments.Contains(" -P 22 "), "owned repair copy starts on its explicit UI owner with production SSH options");
                                if (++attempts != 1) return Task.FromResult(0);
                                return HeldRepairCopy(wizard, value => copyWindow = value, ct => {
                                    Interlocked.Exchange(ref worker, Thread.CurrentThread.ManagedThreadId);
                                    coreWork = Directory.GetDirectories(HomeVpnPrivateFiles.Root).Except(directories).Single();
                                    string helper = Directory.GetFiles(coreWork, "home_vpn_setup.py", SearchOption.AllDirectories).Single();
                                    helperRead = File.ReadAllText(helper).Length != 0;
                                    if (mode == "cleanup-lock") held = File.Open(helper, FileMode.Open, FileAccess.Read, FileShare.Read);
                                    entered.Set(); if (!release.Wait(5000)) throw new TimeoutException("Owned repair copy fixture deadline");
                                    ct.ThrowIfCancellationRequested();
                                });
                            }, Application.ExecutablePath, 20000);
                            return operation;
                        };
                        HomeVpnWaitingTests.ModalWizardCase(wizard, delegate {
                            if (phase == 0) {
                                check(wizard.Modal, "owned repair executes the real wizard button inside ShowDialog: " + mode);
                                button.PerformClick(); phase = 1; return false;
                            }
                            if (mode == "default-refusal") {
                                if ((bool)Field(wizard, "busy")) return false;
                                check(((Label)Field(wizard, "status")).Text.Contains("Очистка прежней команды") && attempts == 0 && !File.Exists(At("pid")),
                                    "default production repair binding refuses retained cleanup before key I/O or transport");
                                if (stop == null) { stop = service.StopAsync(); return false; }
                                if (!stop.IsCompleted) return false;
                                check(stop.GetAwaiter().GetResult() && retained == 1 && !service.HasOwnedResources, "default repair refusal preserves only its exact cleanup owner");
                                check(true, "owned production repair slice PASS: " + mode);
                                return true;
                            }
                            if (phase == 1) {
                                if (!entered.IsSet) { if (operation.IsCompleted) operation.GetAwaiter().GetResult(); return false; }
                                if (++turns < 3) return false;
                                check(helperRead && worker != ui && service.IsAdminWorking && !operation.IsCompleted && (bool)Field(wizard, "busy") && !button.Enabled && copyWindow.Owner == wizard,
                                    "actual repair core and copy keep ownership and native heartbeat without parallel UI admission: " + mode);
                                button.PerformClick(); check(attempts == 1, "pending repair refuses a duplicate button invocation: " + mode);
                                if (mode == "cancel-retry") ((Button)Field(copyWindow, "cancel")).PerformClick();
                                if (mode == "stop-copy" || mode == "dispose-copy" || closing) {
                                    var watch = Stopwatch.StartNew();
                                    if (mode == "dispose-copy" || closing) service.Dispose();
                                    stop = service.StopAsync();
                                    if (closing) { wizard.Dispose(); closedPublications = dispatcher.PublicationCount; }
                                    check(watch.ElapsedMilliseconds < 500 && !stop.IsCompleted && service.HasOwnedResources,
                                        "Stop Dispose returns promptly while retaining actual repair I/O: " + mode);
                                }
                                turns = 0; phase = 2; return false;
                            }
                            if (phase == 2) {
                                if (++turns < 3) return false;
                                check(!operation.IsCompleted && (stop == null || !stop.IsCompleted), "repair cancellation and Stop cannot settle before their held real work: " + mode);
                                release.Set(); phase = 3; return false;
                            }
                            if (phase == 3 && mode == "stop-command") {
                                if (!File.Exists(At("child"))) { if (operation.IsCompleted) operation.GetAwaiter().GetResult(); return false; }
                                var wait = wizard.OwnedForms.OfType<HomeVpnPreparationForm>().Single();
                                stop = service.StopAsync(); check(!stop.IsCompleted && service.IsAdminWorking && wait.Owner == wizard, "Stop retains already admitted real native repair process tree");
                                ((Button)Field(wait, "cancel")).PerformClick(); phase = 4; return false;
                            }
                            if (phase == 3 || phase == 4) {
                                if (!operation.IsCompleted || (bool)Field(wizard, "busy") || (stop != null && !stop.IsCompleted)) return false;
                                var failure = RepairFailure(operation);
                                if (mode == "cleanup-lock") {
                                    check(failure is HomeVpnOwnerUnconfirmedException && service.HasPendingAdminCleanup && Directory.Exists(coreWork), "actual repair keeps its typed unconfirmed outcome and exact locked cleanup");
                                    stop = service.StopAsync(); phase = 5; return false;
                                }
                                if (mode == "cancel-retry" && cancelled == null) {
                                    cancelled = operation; cancellation = failure;
                                    check(failure is HomeVpnPreparationCancelledException && Calls().Length == 0 && !Directory.Exists(coreWork) && wizard.Modal && button.Enabled,
                                        "cancelled repair stays open with cleanup complete and explicit Retry before SSH");
                                    button.PerformClick(); phase = 4; return false;
                                }
                                bool success = mode == "complete" || mode == "cancel-retry";
                                check(success ? failure == null && ((Label)Field(wizard, "status")).Text.Contains("Правила выхода VPN обновлены")
                                    : mode == "stop-command" ? failure is HomeVpnOwnerUnconfirmedException
                                    : failure is HomeVpnPreparationCancelledException || failure is OperationCanceledException,
                                    "repair reports the actual accepted outcome without inventing completion or replay: " + mode);
                                if (cancelled != null) check(Object.ReferenceEquals(cancellation, RepairFailure(cancelled)) && attempts == 2, "cancelled repair result cannot overwrite its successful explicit Retry");
                                if (stop != null) check(stop.GetAwaiter().GetResult(), "repair Stop drains its body and local cleanup: " + mode);
                                if (closing) check(dispatcher.PublicationCount == closedPublications, "disposed wizard receives no late repair UI publication");
                                phase = 7;
                            }
                            if (phase == 5) {
                                if (!stop.IsCompleted) return false;
                                check(!stop.GetAwaiter().GetResult() && service.HasPendingAdminCleanup && service.HasOwnedResources, "locked repair cleanup keeps honest failed Stop and owner");
                                var refused = service.RepairAdminOwnedAsync(Owner, wizard, dispatcher, delegate { });
                                check(RepairFailure(refused) is HomeVpnLocalCleanupException && attempts == 1 && Calls().SequenceEqual(new[] { "repair" }), "retained repair cleanup refuses a new command without native replay");
                                held.Dispose(); held = null; stop = service.StopAsync(); phase = 6; return false;
                            }
                            if (phase == 6) {
                                if (!stop.IsCompleted) return false;
                                check(stop.GetAwaiter().GetResult() && !service.HasPendingAdminCleanup && RepairFailure(operation) is HomeVpnOwnerUnconfirmedException, "explicit Stop retries only unlocked repair deletion and preserves the original outcome"); phase = 7;
                            }
                            if (phase != 7) return false;
                            int expectedCommands = mode == "complete" || mode == "cancel-retry" || mode == "stop-command" || mode == "cleanup-lock" ? 1 : 0;
                            check(Calls().Length == expectedCommands && Calls().All(value => value == "repair") && Unchanged(before)
                                && File.ReadAllText(Path.Combine(sibling, "keep")) == "unrelated" && !service.HasOwnedResources && !Directory.Exists(coreWork)
                                && (!File.Exists(At("pid")) || Gone(Id("pid"))) && (!File.Exists(At("child")) || Gone(Id("child"))),
                                "owned repair preserves protected bytes unrelated files and processes with no automatic command replay: " + mode);
                            check(true, "owned production repair slice PASS: " + mode); return true;
                        }, () => closing);
                    } finally {
                        release.Set(); if (held != null) held.Dispose();
                        if (copyWindow != null) copyWindow.Dispose(); if (wizard != null) wizard.Dispose();
                        KillFixtures(); service.Dispose(); var cleanup = service.StopAsync(); var watch = Stopwatch.StartNew();
                        while (!cleanup.IsCompleted || (operation != null && !operation.IsCompleted)) {
                            Application.DoEvents(); Thread.Sleep(10); if (watch.ElapsedMilliseconds > 5000) throw new TimeoutException("Owned repair fixture cleanup did not settle");
                        }
                        if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Owned repair fixture retained local cleanup");
                        if (operation != null) RepairFailure(operation);
                        Directory.Delete(sibling, true);
                    }
                }
            }
        }
        private static string Replacement(string token)
        {
            string encoded = token.Substring(7).Replace('-', '+').Replace('_', '/'); encoded = encoded.PadRight((encoded.Length + 3) / 4 * 4, '=');
            var value = Json.Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(Convert.FromBase64String(encoded)));
            value["InviteId"] = new string('c', 24); value["User"] = "pgv" + new string('c', 24);
            return "PROGO1." + Convert.ToBase64String(Encoding.UTF8.GetBytes(Json.Serialize(value))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
        private static void Save(Dictionary<string, byte[]> saved, string path) { saved[path] = File.Exists(path) ? File.ReadAllBytes(path) : null; }
        private static Dictionary<string, byte[]> Snapshot(IEnumerable<string> paths)
        { var saved = new Dictionary<string, byte[]>(); foreach (string path in paths) Save(saved, path); return saved; }
        private static bool Unchanged(Dictionary<string, byte[]> saved)
        { return saved.All(item => item.Value == null ? !File.Exists(item.Key) : File.Exists(item.Key) && item.Value.SequenceEqual(File.ReadAllBytes(item.Key))); }
        private static object Field(object target, string name) { return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target); }
        private static IEnumerable<Control> Controls(Control target)
        { foreach (Control child in target.Controls) { yield return child; foreach (var nested in Controls(child)) yield return nested; } }
        private static bool Gone(int id) { try { using (var process = Process.GetProcessById(id)) return process.HasExited; } catch (ArgumentException) { return true; } }
        private static int Id(string name) { return Int32.Parse(File.ReadAllText(At(name))); }
        private static string[] Calls() { return File.Exists(At("calls")) ? File.ReadAllLines(At("calls")) : new string[0]; }
        private static void KillFixtures()
        {
            foreach (string name in new[] { "pid", "child" }) {
                if (!File.Exists(At(name))) continue;
                try { using (var process = Process.GetProcessById(Id(name))) { if (!process.HasExited) { process.Kill(); process.WaitForExit(2000); } } } catch (ArgumentException) { }
            }
        }
        private static void Reset(string mode)
        { KillFixtures(); foreach (string file in Directory.GetFiles(folder)) File.Delete(file); File.Delete(PendingPath); File.Delete(SharePendingPath); copies = 0; File.WriteAllText(At("mode"), mode); File.WriteAllText(At("response"), replacement); }
        private static Task<string> Admin(HomeVpnOwner owner, string action, string label, string id, Action<string> progress, int timeout, bool realCopy)
        {
            return HomeVpnService.AdminAsync(owner, action, label, id, progress, (executable, arguments) => {
                copies++; check(executable == "scp.exe" && arguments.Contains("ConnectTimeout=15") && arguments.Contains(" -P 22 "), "owner dispatch keeps production OpenSSH copy options");
                return realCopy ? HomeVpnPreparationForm.CopyAsync(Form.ActiveForm, Application.ExecutablePath, "owner-wait-copy " + HomeVpnService.Argument(folder)) : Task.FromResult(0);
            }, Application.ExecutablePath, timeout);
        }
        private static void Shot(Form form, string name)
        { using (var image = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(Path.Combine(Path.GetDirectoryName(folder), name + ".png")); } }
        private sealed class Observation { internal int Ticks, Root, Child; internal bool Waiting; internal string Status; }
        // Own the outer loop so closing a nested waiting modal cannot end a
        // modeless Pump and lose the button handler's captured UI continuation.
        private static Observation Loop(Form main, Action start, Func<bool> done, string route, string shot, Func<string> status)
        {
            var result = new Observation(); bool started = false; Exception failure = null; var watch = Stopwatch.StartNew();
            using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) {
                main.Shown += delegate { main.BeginInvoke(new Action(delegate { try { HomeVpnFixtureFiles.ConfigureReady(main); started = true; start(); } catch (Exception ex) { started = true; failure = ex; } })); };
                timer.Tick += delegate {
                    result.Ticks++;
                    try {
                        if (!started) return;
                        var wait = Application.OpenForms.OfType<HomeVpnPreparationForm>().FirstOrDefault();
                        if (!result.Waiting && wait != null && File.Exists(At("child")) && result.Ticks >= 3) {
                            result.Waiting = true; result.Root = Id("pid"); result.Child = Id("child");
                            var cancel = (Button)Field(wait, "cancel"); var text = (Label)Field(wait, "status");
                            check(cancel.Enabled && wait.CancelButton == cancel && wait.AcceptButton == null && text.AccessibilityObject.Description == text.Text,
                                "owner waiting exposes an accessible responsive cancellation control");
                            if (!Calls().Contains("copy")) check(text.Text.Contains("5 минут") && (Calls().Contains("share") ? text.Text.Contains("HTTPS-запрос сохранён")
                                : !text.Text.Contains("запрос сохранён") && !text.Text.Contains("Запрос сохранён")), "owner progress describes its actual ordinary or recoverable wait");
                            if (shot != null) { Shot(wait, shot + "-waiting"); wait.ClientSize = new Size(440, 270); Application.DoEvents();
                                check(wait.RectangleToClient(cancel.RectangleToScreen(cancel.ClientRectangle)).Bottom <= wait.ClientSize.Height, "minimum owner wait retains its actual cancellation button"); Shot(wait, shot + "-minimum"); }
                            if (route == "button") cancel.PerformClick();
                            else if (route == "escape") typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(wait, new object[] { Keys.Escape });
                            else if (route == "close") wait.Close(); else if (route == "dispose") wait.Dispose();
                        }
                        if (failure == null && watch.ElapsedMilliseconds > 20000) failure = new TimeoutException("Owner UI fixture did not settle");
                        if (failure != null) {
                            timer.Stop(); if (wait != null && !wait.IsDisposed) wait.Dispose(); KillFixtures(); main.Dispose(); Application.ExitThread(); return;
                        }
                        if (done()) {
                            result.Status = status == null ? "" : status(); if (shot != null) Shot(main, shot + "-cancelled"); timer.Stop(); main.Close();
                        }
                    } catch (Exception ex) { failure = ex; }
                };
                timer.Start(); Application.Run(main);
            }
            if (failure != null) throw new Exception("Owner native UI controller failed (" + route + "): " + failure.GetType().Name + ": " + failure.Message, failure);
            return result;
        }
        private static void Entry(string token, ClipboardService clipboard, string action, string route, bool realCopy, bool success)
        {
            Reset(success ? "complete" : "hold"); int verified = 0, shown = 0; HomeVpnInvitation first = null, second = null;
            using (var relay = new Ikev2RelayService())
            using (var service = new HomeVpnService(relay)) {
                service.UseToken(token, Owner); var before = Snapshot(inputPaths.Where(path => action != "share" || realCopy || path != SharePendingPath));
                Func<HomeVpnOwner, string, string, string, Action<string>, Task<string>> admin = (owner, kind, label, id, progress) => Admin(owner, kind, label, id, progress, route == "deadline" ? 5000 : 20000, realCopy);
                Form main; Button button; Func<bool> done; Func<string> status;
                if (action == "repair") {
                    var wizard = new HomeVpnWizardForm(service, clipboard); HomeVpnFixtureFiles.ConfigureReady(wizard); wizard.Admin = admin;
                    typeof(HomeVpnWizardForm).GetMethod("ShowStep", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(wizard, new object[] { 4 });
                    main = wizard; button = Controls(wizard).OfType<Button>().Single(control => control.Text == "Исправить выход VPN в интернет");
                    done = () => !(bool)Field(wizard, "busy"); status = () => {
                        check(wizard.Visible && (int)Field(wizard, "step") == 4 && ((Control)Field(wizard, "body")).Enabled,
                            "cancelled repair restores the existing wizard step without a queued close or completion claim");
                        return ((Label)Field(wizard, "status")).Text;
                    };
                } else if (action == "share") {
                    main = HomeProfileShare.CreateConfigureForm(service, admin, (origin, access) => { verified++; return Task.FromResult(0); });
                    Controls(main).OfType<TextBox>().Single().Text = "QR.EXAMPLE.ORG";
                    button = Controls(main).OfType<Button>().Single(control => control.Text == "Настроить HTTPS на VPS");
                    var address = Controls(main).OfType<TextBox>().Single(); done = () => address.Enabled;
                    status = () => {
                        check(main.Visible && main.DialogResult == DialogResult.None
                            && Controls(main).OfType<Button>().Single(control => control.Text == "Настроить HTTPS на VPS").Enabled == realCopy
                            && Controls(main).OfType<Button>().Single(control => control.Text == "Адрес уже настроен — проверить").Enabled == realCopy
                            && Controls(main).OfType<Button>().Single(control => control.Text == "Проверить прежнюю настройку HTTPS").Visible != realCopy,
                            "cancelled share distinguishes pre-SSH preparation from its retained HTTPS request");
                        return Controls(main).OfType<Label>().Single(control => control.AccessibleName == "Результат настройки HTTPS-выдачи").Text;
                    };
                } else {
                    first = new HomeVpnInvitation { Id = OldId, Name = "Друг" }; second = new HomeVpnInvitation { Id = new string('d', 24), Name = "Друг" };
                    var friends = new HomeInvitationsForm(new[] { first, second }, (kind, label, id) => admin(Owner, kind, label, id, delegate { }), clipboard);
                    friends.Confirm = text => true; friends.ShowToken = value => { shown++; check(value == replacement, "acknowledged phase-one reissue presents only its new verified token"); };
                    var list = (HomeInvitationList)Field(friends, "list"); ((ListBox)Field(list, "list")).SelectedIndex = 0;
                    ((TextBox)Field(friends, "name")).Text = "Другой черновик";
                    main = friends; button = (Button)Field(friends, action == "reissue" ? "reissue" : "revoke");
                    done = () => !(bool)Field(friends, "working"); status = () => {
                        if (!success) check(friends.Visible && list.Selected == first && ((TextBox)Field(friends, "name")).Text == "Другой черновик"
                            && ((Button)Field(friends, "create")).Enabled == realCopy && ((Button)Field(friends, "revoke")).Enabled == realCopy
                            && ((Button)Field(friends, "reissue")).Enabled == realCopy,
                            "friends retain selection and draft, and distinguish pre-SSH cancellation from uncertain remote mutation: " + action);
                        return ((Label)Field(friends, "status")).Text;
                    };
                }
                using (main) {
                    var result = Loop(main, () => button.PerformClick(), done, route,
                        route == "button" && !realCopy && action != "reissue" ? "vps-owner-" + action : null, status);
                    if (success) check(shown == 1 && first.Revoked && !second.Revoked && Calls().SequenceEqual(new[] { "revoke", "invite" })
                        && copies == 2 && !File.Exists(PendingPath) && Unchanged(before), "real phase-one ACK permits exactly one source-bound replacement and consumes only its completed handoff");
                    else {
                        check(result.Waiting && result.Ticks >= 3 && Gone(result.Root) && Gone(result.Child) && copies == 1
                            && Calls().SequenceEqual(new[] { realCopy ? "copy" : action == "reissue" ? "revoke" : action }) && shown == 0 && verified == 0 && Unchanged(before),
                            "native owner cancellation settles its tree, preserves private files and makes one attempt: " + action + "/" + route + "/copy=" + realCopy);
                        check(Directory.GetDirectories(HomeVpnPrivateFiles.Root, "admin-*").Length == 0 && !File.Exists(PendingPath), "ordinary cancellation creates no issuance journal and cleans local work");
                        if (realCopy) check(result.Status.Contains("не запускались"), "pre-dispatch copy cancellation reports that server commands did not start: " + action);
                        else check(result.Status.Contains("подтвержд") && (action == "share" ? result.Status.Contains("Запрос сохранён")
                            : !result.Status.Contains("запрос сохранён") && !result.Status.Contains("Запрос сохранён"))
                            && !result.Status.Contains("Правила выхода VPN обновлены"), "owner cancellation reports uncertainty without fabricated recovery or success: " + action + "/" + route);
                        if (action == "share" && !realCopy) check(HomeVpnShareRecovery.Pending().Domain == "qr.example.org"
                            && !Encoding.UTF8.GetString(File.ReadAllBytes(SharePendingPath)).Contains("qr.example.org"), "cancelled share retains its frozen DPAPI-protected request");
                        if (action == "repair" && !realCopy) check(result.Status.Contains(route == "deadline" ? "Время ожидания SSH истекло" : "Ожидание SSH прервано"),
                            "ordinary waiting distinguishes the deadline from requested cancellation: " + route);
                        if (first != null) check(!first.Revoked && !second.Revoked, "unconfirmed phase-one revoke never publishes a revoked row or mutates its duplicate");
                    }
                    check(!relay.IsRunning && !Directory.GetDirectories(HomeVpnPrivateFiles.Root, "session-*").Any(), "owner management does not start a tunnel");
                }
            }
        }
        private static void Outputs(Dictionary<string, byte[]> paths)
        {
            foreach (string mode in new[] { "complete", "failure", "oversized" })
            foreach (string action in new[] { "revoke", "repair", "share" }) {
                Reset(mode); var before = Snapshot(paths.Keys.Where(path => action != "share" || path != SharePendingPath)); Task<string> task = null;
                using (var host = new Form()) {
                    // Leave the unchanged 20-second controller enough time for
                    // the operation deadline, owned-tree settlement and private cleanup.
                    var watch = Stopwatch.StartNew();
                    var result = Loop(host, () => task = Admin(Owner, action, null, action == "revoke" ? OldId : action == "share" ? "QR.EXAMPLE.ORG" : null, delegate { }, 10000, false), () => task != null && task.IsCompleted, "output:" + action + "/" + mode, null, null);
                    Exception failure = null; string text = null; try { text = task.GetAwaiter().GetResult(); } catch (Exception ex) { failure = ex; }
                    check(copies == 1 && Calls().SequenceEqual(new[] { action }) && Gone(Id("pid")) && Unchanged(before) && Directory.GetDirectories(HomeVpnPrivateFiles.Root, "admin-*").Length == 0,
                        "production ordinary output makes one attempt and preserves private inputs/helpers: " + action + "/" + mode);
                    if (mode == "complete") check(failure == null && text == (action == "revoke" ? "Access revoked." : action == "share" ? "https://qr.example.org" : "VPN network rules refreshed. Reconnect the phone and test internet access."),
                        "ordinary production SSH returns its actual successful output: " + action);
                    else check((action == "share" ? failure is HomeVpnSharePendingException : failure is HomeVpnOwnerUnconfirmedException)
                        && failure.Message.Contains("подтвержд") && !failure.Message.Contains("synthetic-private")
                        && (action == "share" || !failure.Message.Contains("запрос сохранён")), "owner nonzero/oversized output preserves its correct uncertainty and recovery boundary: " + action + "/" + mode);
                    if (mode == "oversized") check(result.Ticks >= 3 && watch.ElapsedMilliseconds < 10000 && !failure.Message.Contains("Время ожидания"),
                        "oversized owner output settles before the SSH deadline with a responsive UI: " + action);
                    if (action == "share") check(HomeVpnShareRecovery.HasPending(), "share output alone never consumes its HTTPS request before verification and save");
                }
            }
        }
        private static void CommandOutputLimits()
        {
            foreach (bool sharing in new[] { false, true })
            foreach (bool overflow in new[] { false, true }) {
                Reset(overflow ? "raw-overflow" : "raw-complete");
                int limit = sharing ? 65536 : 32768;
                var payload = new byte[limit + (overflow ? 1 : 0)];
                byte[] seed = Encoding.UTF8.GetBytes("Ж\U0001F512");
                int offset = 0;
                while (offset + seed.Length <= payload.Length) { Buffer.BlockCopy(seed, 0, payload, offset, seed.Length); offset += seed.Length; }
                while (offset < payload.Length) payload[offset++] = (byte)'x';
                File.WriteAllBytes(At("raw-input"), payload); Task task = null;
                string output = At("raw-output"), route = "raw-output:share=" + sharing + "/overflow=" + overflow;
                var watch = Stopwatch.StartNew();
                using (var host = new Form()) {
                    var observed = Loop(host, () => task = sharing
                        ? HomeVpnPreparationProcess.ShareAsync(Application.ExecutablePath, "owner-wait-output " + HomeVpnService.Argument(folder), output, 10000, CancellationToken.None)
                        : HomeVpnPreparationProcess.OwnerAsync(Application.ExecutablePath, "owner-wait-output " + HomeVpnService.Argument(folder), output, 10000, CancellationToken.None),
                        () => task != null && task.IsCompleted, route, null, null);
                    Exception failure = null; try { task.GetAwaiter().GetResult(); } catch (Exception ex) { failure = ex; }
                    byte[] captured = File.ReadAllBytes(output);
                    check(Gone(Id("pid")) && observed.Ticks >= 3 && watch.ElapsedMilliseconds < 15000
                        && File.ReadAllText(At("console")).Split(':')[0] != "0"
                        && File.ReadAllText(At("console")).EndsWith(":True:True"), "raw owner output retains real console stdin/stderr and settles its root: " + route);
                    if (!overflow) check(failure == null && payload.SequenceEqual(captured), "exact-limit UTF-8 owner output retains all original bytes: " + route);
                    else check((sharing ? failure is HomeVpnSharePendingException : failure is HomeVpnOwnerUnconfirmedException)
                        && !failure.Message.Contains("Время ожидания") && captured.Length <= limit && Gone(Id("child")),
                        "one-byte output overflow is refused with bounded storage and settled descendant: " + route);
                }
            }
        }
        [DllImport("kernel32.dll")] private static extern uint GetConsoleCP();
        [DllImport("kernel32.dll")] private static extern IntPtr GetStdHandle(int value);
        [DllImport("kernel32.dll")] private static extern bool GetConsoleMode(IntPtr handle, out uint mode);
        private static void AcceptedCancellation()
        {
            var ready = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var wait = new HomeVpnPreparationForm(token => ready.Task, HomeVpnWaitPurpose.OwnerCommand)) {
                bool started = false; Exception failure = null; var watch = Stopwatch.StartNew();
                using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) {
                    timer.Tick += delegate {
                        if (watch.ElapsedMilliseconds > 5000) { failure = new TimeoutException("Queued owner completion did not settle"); timer.Stop(); wait.Dispose(); return; }
                        if (started) return; started = true; ((Button)Field(wait, "cancel")).PerformClick(); ready.SetResult(null);
                    };
                    timer.Start(); Application.Run(wait);
                }
                if (failure != null || !wait.Completion.IsCompleted) throw new Exception("Queued owner cancellation fixture did not settle", failure);
                try { wait.Completion.GetAwaiter().GetResult(); } catch (Exception ex) { failure = ex; }
                check(started && failure is HomeVpnOwnerUnconfirmedException, "accepted owner cancellation cannot publish a queued successful command completion");
            }
        }
    }
}
