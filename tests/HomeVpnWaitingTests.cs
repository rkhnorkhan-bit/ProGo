using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static class HomeVpnWaitingTests
    {
        private static string marker;
        private static Action<bool, string> check;
        // This executable is also the owned SSH substitute, never a live SSH client.
        internal static bool Fixture(string[] args)
        {
            // An unrelated fixture outlives its owner's test scope, rather than
            // dying on an arbitrary wall clock during a still-bounded scenario.
            // Parent exit also closes this private pipe and releases the child.
            if (args.Length == 1 && args[0] == "unrelated-wait") { Console.In.ReadLine(); return true; }
            int index = Array.IndexOf(args, "-D");
            if (index < 0) return false;
            string folder = Environment.GetEnvironmentVariable("PROGO_HOME_WAIT_FIXTURE");
            if (String.IsNullOrEmpty(folder)) throw new Exception("Missing isolated fixture folder");
            HomeVpnFixtureFiles.Publish(Path.Combine(folder, "pid"), Process.GetCurrentProcess().Id.ToString());
            string mode = File.ReadAllText(Path.Combine(folder, "mode"));
            if (mode == "no-listener") { Thread.Sleep(60000); return true; }
            int port = Int32.Parse(args[index + 1].Split(':').Last());
            var listener = new TcpListener(IPAddress.Loopback, port); listener.Start();
            while (true) {
                var client = listener.AcceptTcpClient();
                Task.Run(delegate {
                    using (client) {
                        try {
                            var stream = client.GetStream(); byte[] greeting = Read(stream, 3);
                            if (greeting[0] != 5) return;
                            stream.Write(new byte[] { 5, 0 }, 0, 2);
                            byte[] request = Read(stream, 10); // Readiness probe closes after greeting.
                            if (request[0] != 5) return;
                            File.WriteAllText(Path.Combine(folder, "receiver"), "waiting");
                            if (mode == "silent") { while (stream.ReadByte() >= 0) { } return; }
                            stream.Write(new byte[] { 5, 0, 0, 1, 127, 0, 0, 1, 0, 0 }, 0, 10);
                            var hello = Read(stream, 6); stream.Write(hello, 0, hello.Length);
                        } catch (IOException) { } catch (SocketException) { }
                    }
                });
            }
        }
        private static byte[] Read(Stream stream, int count)
        {
            var bytes = new byte[count]; int offset = 0;
            while (offset < count) { int n = stream.Read(bytes, offset, count - offset); if (n == 0) throw new IOException(); offset += n; }
            return bytes;
        }
        internal static void Run(Action<bool, string> assert, string token, ClipboardService clipboard, string work)
        {
            check = assert; marker = Path.Combine(work, "channel-wait"); Directory.CreateDirectory(marker);
            var previous = Environment.GetEnvironmentVariable("PROGO_HOME_WAIT_FIXTURE");
            var saved = new Dictionary<string, byte[]>();
            foreach (string name in new[] { "access", "owner", "home-address" }) {
                var path = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat");
                saved[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            Environment.SetEnvironmentVariable("PROGO_HOME_WAIT_FIXTURE", marker);
            var unrelated = StartUnrelatedFixture();
            try {
                PublicationReadiness();
                StoredStateWorker(token, work);
                OwnedInitializationWorker(token);
                OwnedShareRefreshWorker(token);
                WizardShareRead(token, clipboard);
                ConfigureInitialization(token);
                PreparedHomeAddressWorker();
                OwnedHomeAddressWorker(token);
                WizardHomeAddressWrite(token, clipboard);
                foreach (string mode in new[] { "no-listener", "silent" }) {
                    foreach (string route in new[] { "button", "escape", "close", "dispose" }) NativeCancel(token, clipboard, work, mode, route);
                }
                ImportCancel(token, clipboard);
                CleanupFailure(token, clipboard);
                ServiceChecks(token);
                OwnedPreparationStop(token, false);
                OwnedPreparationStop(token, true);
                OwnedCleanupStop(token);
                foreach (string route in new[] { "stay", "close", "dispose" }) WizardOwnedStop(token, clipboard, route);
                RelayOwnedLifecycle(false);
                RelayOwnedLifecycle(true);
                ProtectedOperation(clipboard);
                check(!unrelated.HasExited, "all channel cancellation and cleanup paths preserve an unrelated process");
            }
            finally {
                try { KillFixture(); Environment.SetEnvironmentVariable("PROGO_HOME_WAIT_FIXTURE", previous); }
                finally {
                    try { StopUnrelatedFixture(unrelated); }
                    finally { foreach (var item in saved) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); } }
                }
            }
        }
        internal static Process StartUnrelatedFixture()
        {
            return Process.Start(new ProcessStartInfo(Application.ExecutablePath, "unrelated-wait") {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true });
        }
        internal static void StopUnrelatedFixture(Process process)
        {
            try {
                if (!process.HasExited) {
                    process.StandardInput.Close();
                    if (!process.WaitForExit(500)) {
                        process.Kill();
                        if (!process.WaitForExit(1500)) throw new TimeoutException("Owned unrelated fixture did not exit after pipe close or targeted cleanup");
                    }
                }
            } finally { process.Dispose(); }
        }
        private static HomeVpnService Service(Ikev2RelayService relay)
        {
            return new HomeVpnService(relay, Application.ExecutablePath, UdpPort(), UdpPort(), 17878);
        }
        private static void StoredStateWorker(string token, string work)
        {
            string shareName = "share-" + HomeVpnAccess.Parse(token).ServerId;
            var original = new Dictionary<string, byte[]>();
            foreach (var name in new[] { "access", "owner", "home-address", shareName }) {
                string path = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat");
                original[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            using (var form = new Form())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            {
                int ticks = 0, uiThread = Thread.CurrentThread.ManagedThreadId;
                heartbeat.Tick += delegate { ticks++; }; form.Show(); heartbeat.Start();
                try
                {
                    foreach (string mode in new[] { "clean", "cancel", "mutate" })
                    {
                        var owner = new HomeVpnOwner { Host = "fixture.example", Port = 2222, Login = "root", KeyFile = Path.Combine(work, "saved-owner-key") };
                        HomeVpnPrivateFiles.Save("access", token);
                        HomeVpnPrivateFiles.Save("owner", new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(owner));
                        HomeVpnPrivateFiles.Save("home-address", "home.example"); HomeVpnPrivateFiles.Save(shareName, "https://profiles.example");
                        var before = original.Keys.ToDictionary(path => path, File.ReadAllBytes);
                        using (var entered = new ManualResetEventSlim())
                        using (var release = new ManualResetEventSlim())
                        using (var cancellation = new CancellationTokenSource())
                        {
                            Task<HomeVpnStoredState> loading = null; int workerThread = 0;
                            try
                            {
                                var watch = Stopwatch.StartNew();
                                loading = HomeVpnStoredState.LoadAsync(cancellation.Token, delegate(string stage) {
                                    Interlocked.Exchange(ref workerThread, Thread.CurrentThread.ManagedThreadId);
                                    if (stage == "verify") { entered.Set(); if (!release.Wait(5000)) throw new TimeoutException("Stored-state read verification barrier"); }
                                });
                                check(watch.ElapsedMilliseconds < 500, "stored state read returns its actual worker task before any private DPAPI or disk wait: " + mode);
                                int startTicks = ticks; Pump(() => entered.IsSet && ticks >= startTicks + 3, 3000);
                                check(workerThread != uiThread && !loading.IsCompleted, "actual private reads and DPAPI parsing run outside the live native UI while ownership remains pending: " + mode);
                                if (mode == "cancel") {
                                    cancellation.Cancel(); startTicks = ticks; Pump(() => ticks >= startTicks + 3, 1000);
                                    check(!loading.IsCompleted, "stored-state cancellation retains a held real-path worker until it settles");
                                }
                                if (mode == "mutate") HomeVpnPrivateFiles.Save("home-address", "changed.example");
                                release.Set(); Pump(() => loading.IsCompleted, 5000);
                                if (mode == "clean") {
                                    var state = loading.GetAwaiter().GetResult(); var copiedAccess = state.CopyAccess(); var copiedOwner = state.CopyOwner();
                                    check(copiedAccess != null && copiedAccess.Host == HomeVpnAccess.Parse(token).Host && copiedOwner.Host == owner.Host
                                        && copiedOwner.KeyFile == owner.KeyFile && state.HomeAddress == "home.example" && state.ShareOrigin == "https://profiles.example",
                                        "actual protected access, owner, home and matching server share form one prepared state");
                                    copiedAccess.Host = "changed.example"; copiedOwner.KeyFile = "changed";
                                    check(state.CopyAccess().Host != copiedAccess.Host && state.CopyOwner().KeyFile == owner.KeyFile,
                                        "mutable consumer copies cannot alter the retained prepared identity or local owner metadata");
                                } else {
                                    bool refused = false;
                                    try { loading.GetAwaiter().GetResult(); }
                                    catch (OperationCanceledException) { refused = mode == "cancel"; }
                                    catch (InvalidOperationException) { refused = mode == "mutate"; }
                                    check(refused && (mode != "cancel" || loading.IsCanceled), "cancelled or concurrently changed private reads cannot return a publishable mixed state: " + mode);
                                }
                                foreach (var pair in before) {
                                    if (mode == "mutate" && Path.GetFileName(pair.Key) == "home-address.dat") continue;
                                    check(pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key)), "stored-state worker never rewrites protected source bytes: " + mode);
                                }
                                if (mode == "mutate") check(HomeVpnPrivateFiles.Load("home-address") == "changed.example", "read conflict preserves the actual concurrent change instead of repairing or retrying it");
                            }
                            finally {
                                release.Set();
                                if (loading != null) { Pump(() => loading.IsCompleted, 5000); try { loading.GetAwaiter().GetResult(); } catch { /* Primary outcome above reports errors; observe only this retained worker for cleanup. */ } }
                            }
                        }
                    }
                    Task<HomeVpnStoredState> blocked = null;
                    try {
                        using (var held = File.Open(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"), FileMode.Open, FileAccess.Read, FileShare.None)) {
                            blocked = HomeVpnStoredState.LoadAsync(CancellationToken.None);
                            Pump(() => blocked.IsCompleted, 5000); bool refused = false;
                            try { blocked.GetAwaiter().GetResult(); } catch (IOException) { refused = true; }
                            check(refused, "real protected-file sharing refusal fails the read without publishing partial state or rewriting the source");
                        }
                    } finally {
                        // Release the held native stream before the last bounded
                        // observation; private fixture restoration cannot race it.
                        if (blocked != null) { Pump(() => blocked.IsCompleted, 5000); try { blocked.GetAwaiter().GetResult(); } catch { } }
                    }
                    foreach (bool empty in new[] { false, true }) {
                        foreach (string name in new[] { "owner", "home-address", shareName }) File.Delete(Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat"));
                        if (empty) File.Delete(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"));
                        var missing = HomeVpnStoredState.LoadAsync(CancellationToken.None);
                        try {
                            Pump(() => missing.IsCompleted, 5000); var state = missing.GetAwaiter().GetResult();
                            check(state.CopyOwner() == null && state.HomeAddress == "" && (empty ? state.CopyAccess() == null && state.ShareOrigin == null
                                : state.CopyAccess() != null && state.ShareOrigin == HomeVpnAccess.Parse(token).ShareUrl),
                                "missing saved fields use only in-memory defaults and the frozen access share fallback: " + empty);
                            check(!File.Exists(Path.Combine(HomeVpnPrivateFiles.Root, "owner.dat")) && !File.Exists(Path.Combine(HomeVpnPrivateFiles.Root, "home-address.dat"))
                                && !File.Exists(Path.Combine(HomeVpnPrivateFiles.Root, shareName + ".dat")), "stored-state defaults never recreate missing source files");
                        } finally { Pump(() => missing.IsCompleted, 5000); try { missing.GetAwaiter().GetResult(); } catch { } }
                    }
                }
                finally {
                    heartbeat.Stop(); form.Close();
                    foreach (var pair in original) { if (pair.Value == null) File.Delete(pair.Key); else File.WriteAllBytes(pair.Key, pair.Value); }
                }
            }
        }
        private static int UdpPort() { using (var socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0))) return ((IPEndPoint)socket.Client.LocalEndPoint).Port; }
        private static void OwnedHomeAddressWorker(string token)
        { OwnedAddressWorker(false, token); OwnedAddressWorker(true, token); }
        private static Task<bool> SaveAddress(HomeVpnService service, bool share, string value, CancellationToken token, Action<string> beforeIo = null)
        { return share ? service.SetShareOriginAsync(service.Access, value, token, beforeIo) : service.SetHomeAddressAsync(value, token, beforeIo); }
        private static string SavedAddress(HomeVpnService service, bool share)
        { return share ? service.ShareOrigin : service.HomeAddress; }
        private static void OwnedAddressWorker(bool share, string token)
        {
            string serverId = HomeVpnAccess.Parse(token).ServerId;
            string name = share ? "share-" + serverId : "home-address";
            string path = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat");
            string beforeValue = share ? "https://before.example" : "before.example";
            string afterValue = share ? "https://after.example" : "after.example";
            var original = new Dictionary<string, byte[]>();
            foreach (string file in new[] { path, Path.Combine(HomeVpnPrivateFiles.Root, "access.dat") }) original[file] = File.Exists(file) ? File.ReadAllBytes(file) : null;
            using (var form = new Form())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            {
                int ticks = 0, uiThread = Thread.CurrentThread.ManagedThreadId;
                heartbeat.Tick += delegate { ticks++; }; form.Show(); heartbeat.Start();
                try {
                    HomeVpnPrivateFiles.Save("access", token);
                    foreach (string mode in share ? new[] { "save", "cancel", "stop", "dispose", "accepted-stop", "accepted-dispose", "cleanup-lock", "stale-server", "stale-access" } :
                        new[] { "save", "cancel", "stop", "dispose", "accepted-stop", "accepted-dispose", "cleanup-lock" })
                    {
                        HomeVpnPrivateFiles.Save(name, beforeValue); byte[] before = File.ReadAllBytes(path);
                        using (var relay = new Ikev2RelayService())
                        using (var service = new HomeVpnService(relay, Application.ExecutablePath, UdpPort(), UdpPort(), 17878, share))
                        using (var entered = new ManualResetEventSlim())
                        using (var release = new ManualResetEventSlim())
                        using (var accepted = new ManualResetEventSlim())
                        using (var publish = new ManualResetEventSlim())
                        using (var cancellation = new CancellationTokenSource())
                        {
                            Task<bool> saving = null, stop = null; FileStream held = null; int workerThread = 0;
                            try {
                                Pump(() => service.InitializationWork.IsCompleted, 5000); service.InitializationWork.GetAwaiter().GetResult();
                                var watch = Stopwatch.StartNew();
                                saving = SaveAddress(service, share, " after.example ", cancellation.Token, delegate(string stage) {
                                    Interlocked.Exchange(ref workerThread, Thread.CurrentThread.ManagedThreadId);
                                    if (stage == "prepared") { entered.Set(); if (!release.Wait(5000)) throw new TimeoutException("Owned address prepare fixture deadline"); }
                                    if (stage == "accepted" && mode.StartsWith("accepted-", StringComparison.Ordinal)) { accepted.Set(); if (!publish.Wait(5000)) throw new TimeoutException("Owned accepted address fixture deadline"); }
                                });
                                check(watch.ElapsedMilliseconds < 500 && Object.ReferenceEquals(saving, service.HomeAddressWork) && (share ? service.IsSavingShareOrigin && !service.IsSavingHomeAddress : service.IsSavingHomeAddress),
                                    "service publishes one owned home address write before private I/O can settle: " + (share ? "share/" : "") + mode);
                                int beforeTicks = ticks; Pump(() => entered.IsSet && ticks >= beforeTicks + 3, 3000);
                                check(workerThread != uiThread && !saving.IsCompleted && service.HasOwnedResources && SavedAddress(service, share) == beforeValue &&
                                    Object.ReferenceEquals(saving, SaveAddress(service, share, "after.example", CancellationToken.None)),
                                    "held real address write keeps heartbeat, original state and duplicate physical ownership: " + (share ? "share/" : "") + mode);
                                bool differentRefused = false; try { SaveAddress(service, share, "other.example", CancellationToken.None).GetAwaiter().GetResult(); } catch (InvalidOperationException) { differentRefused = true; }
                                check(differentRefused, "another address cannot replace or run beside the owned pending save: " + (share ? "share/" : "") + mode);
                                bool crossKindRefused = false;
                                try { SaveAddress(service, !share, "after.example", CancellationToken.None).GetAwaiter().GetResult(); } catch (InvalidOperationException) { crossKindRefused = true; }
                                check(crossKindRefused, "home and share address writes cannot have parallel physical ownership: " + (share ? "share/" : "") + mode);
                                if (share) {
                                    bool otherAccessRefused = false;
                                    try { service.SetShareOriginAsync(HomeVpnAccess.Parse(token), "after.example", CancellationToken.None).GetAwaiter().GetResult(); } catch (InvalidOperationException) { otherAccessRefused = true; }
                                    check(otherAccessRefused, "equal server ID on a different access object cannot join the captured write");
                                }
                                if (mode == "stale-server") service.Access.ServerId = new string(serverId[0] == 'b' ? 'c' : 'b', 32);
                                if (mode == "stale-access") typeof(HomeVpnService).GetProperty("Access", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(service, HomeVpnAccess.Parse(token), null);
                                if (mode.StartsWith("accepted-", StringComparison.Ordinal)) { release.Set(); Pump(() => accepted.IsSet, 3000); }
                                if (mode == "cancel" || mode == "cleanup-lock") cancellation.Cancel();
                                if (mode == "cleanup-lock") held = File.Open(((HomeVpnAddressWrite)Field(service, "addressWrite")).StagingPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                                if (mode == "stop" || mode == "accepted-stop") { watch.Restart(); stop = service.StopAsync(); check(watch.ElapsedMilliseconds < 500 && !stop.IsCompleted, "Stop joins actual private address I/O without blocking UI: " + (share ? "share/" : "") + mode); }
                                if (mode == "dispose" || mode == "accepted-dispose") { watch.Restart(); service.Dispose(); stop = service.StopWork; check(watch.ElapsedMilliseconds < 500 && !stop.IsCompleted, "Dispose retains actual private address I/O without late owner writes: " + (share ? "share/" : "") + mode); }
                                check(!saving.IsCompleted && before.SequenceEqual(File.ReadAllBytes(path)), "pending accepted or cancelled write cannot claim a physical disk result before it settles: " + (share ? "share/" : "") + mode);
                                release.Set(); publish.Set(); Pump(() => saving.IsCompleted && (stop == null || stop.IsCompleted), 5000);
                                Exception failure = null; bool saved = false; try { saved = saving.GetAwaiter().GetResult(); } catch (Exception ex) { failure = ex; }
                                bool committed = mode == "save" || mode.StartsWith("accepted-", StringComparison.Ordinal);
                                check(committed ? saved && failure == null && HomeVpnPrivateFiles.Load(name) == afterValue :
                                    (mode == "cleanup-lock" ? failure is HomeVpnLocalCleanupException : failure is OperationCanceledException && saving.IsCanceled) && before.SequenceEqual(File.ReadAllBytes(path)), "private save result distinguishes accepted commit from pre-admission cancellation: " + (share ? "share/" : "") + mode);
                                check(SavedAddress(service, share) == (committed && mode != "accepted-dispose" ? afterValue : beforeValue), "disposed address owner gets no late metadata update; accepted live scope gets its actual value: " + (share ? "share/" : "") + mode);
                                if (stop != null) check(stop.GetAwaiter().GetResult() && !service.HasOwnedResources, "Stop result owns and joins both actual write and private cleanup: " + (share ? "share/" : "") + mode);
                                if (mode == "cleanup-lock") {
                                    check(failure is HomeVpnLocalCleanupException && service.HasOwnedResources, "failed real stage deletion retains exact write ownership instead of reporting cancellation cleanup complete");
                                    stop = service.StopAsync(); Pump(() => stop.IsCompleted, 5000);
                                    check(!stop.GetAwaiter().GetResult() && service.HasOwnedResources, "locked pending address cleanup makes actual Stop fail honestly and remain retryable");
                                    held.Dispose(); held = null;
                                    stop = service.StopAsync(); Pump(() => stop.IsCompleted, 5000);
                                    check(stop.GetAwaiter().GetResult() && !service.HasOwnedResources, "unlock then explicit Stop retries only its retained address stage cleanup");
                                    var retry = SaveAddress(service, share, "after.example", CancellationToken.None); Pump(() => retry.IsCompleted, 5000);
                                    check(retry.GetAwaiter().GetResult() && SavedAddress(service, share) == afterValue, "new explicit save becomes available only after actual owned cleanup completes");
                                }
                            } finally {
                                release.Set(); publish.Set(); cancellation.Cancel(); if (held != null) held.Dispose(); service.Dispose();
                                var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted && service.HomeAddressWork.IsCompleted, 5000);
                                if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Owned home address fixture cleanup incomplete");
                                if (saving != null) try { saving.GetAwaiter().GetResult(); } catch (OperationCanceledException) { } catch (HomeVpnLocalCleanupException) { }
                            }
                        }
                    }
                } finally { heartbeat.Stop(); form.Close(); foreach (var item in original) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); } }
            }
        }
        private static void PreparedHomeAddressWorker()
        {
            PreparedAddressWorker(false); PreparedAddressWorker(true);
            foreach (string invalid in new[] { "", "../home-address", new string('a', 31), new string('a', 33), new string('A', 32) }) {
                bool refused = false;
                try { using (var unused = HomeVpnAddressWrite.ForShareOrigin(invalid, "profiles.example")) { } }
                catch (ArgumentException) { refused = true; }
                check(refused, "share-address factory refuses a noncanonical server identity before private I/O");
            }
            foreach (string invalid in new[] { "http://profiles.example", "https://profiles.example/path", "https://profiles.example:8443", "https://user@profiles.example" }) {
                bool refused = false;
                try { using (var unused = HomeVpnAddressWrite.ForShareOrigin(new string('a', 32), invalid)) { } }
                catch (ArgumentException) { refused = true; }
                check(refused, "share-address factory preserves existing HTTPS origin validation before private I/O");
            }
        }
        private static void PreparedAddressWorker(bool share)
        {
            string name = share ? "share-" + new string('a', 32) : "home-address";
            string afterValue = share ? "https://after.example" : "after.example";
            string beforeValue = share ? "https://before.example" : "before.example";
            string changedValue = share ? "https://changed.example" : "changed.example";
            string target = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat");
            var original = new Dictionary<string, byte[]>();
            string home = Path.Combine(HomeVpnPrivateFiles.Root, "home-address.dat");
            string other = Path.Combine(HomeVpnPrivateFiles.Root, "share-" + new string('b', 32) + ".dat");
            foreach (string path in new[] { target, home, other }) original[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            byte[] untouchedHome = original[home], untouchedOther = original[other];
            using (var form = new Form())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            {
                int ticks = 0, uiThread = Thread.CurrentThread.ManagedThreadId;
                heartbeat.Tick += delegate { ticks++; }; form.Show(); heartbeat.Start();
                try {
                    if (share) {
                        HomeVpnPrivateFiles.Save("home-address", "untouched-home.example");
                        HomeVpnPrivateFiles.Save("share-" + new string('b', 32), "https://untouched.example");
                        untouchedHome = File.ReadAllBytes(home); untouchedOther = File.ReadAllBytes(other);
                    }
                    foreach (string mode in new[] { "create", "replace", "refuse", "cancel", "locked", "changed", "stage-changed", "cleanup-lock", "accepted-cancel" })
                    using (var entered = new ManualResetEventSlim())
                    using (var release = new ManualResetEventSlim())
                    using (var cancellation = new CancellationTokenSource())
                    using (var acceptedEntered = new ManualResetEventSlim())
                    using (var publishRelease = new ManualResetEventSlim())
                    {
                        HomeVpnPrivateFiles.Save(name, beforeValue); if (mode == "create") File.Delete(target);
                        byte[] before = File.Exists(target) ? File.ReadAllBytes(target) : null, stagedBefore = null;
                        var operation = share ? HomeVpnAddressWrite.ForShareOrigin(new string('a', 32), " AFTER.example ") : new HomeVpnAddressWrite(" after.example ");
                        Task worker = null; FileStream held = null; int workerThread = 0, accepted = 0;
                        try {
                            worker = Task.Run(delegate {
                                try {
                                    operation.Prepare(cancellation.Token, delegate(string stage) {
                                        Interlocked.Exchange(ref workerThread, Thread.CurrentThread.ManagedThreadId);
                                        if (stage == "prepared") { entered.Set(); if (!release.Wait(5000)) throw new TimeoutException("Prepared home address fixture deadline"); }
                                    });
                                    operation.Commit(delegate {
                                        if (mode == "refuse" || cancellation.IsCancellationRequested) return false;
                                        Interlocked.Increment(ref accepted); return true;
                                    }, delegate(string stage) {
                                        if (mode == "accepted-cancel") { acceptedEntered.Set(); if (!publishRelease.Wait(5000)) throw new TimeoutException("Accepted address publication fixture deadline"); }
                                    });
                                } finally { operation.Dispose(); }
                            }, cancellation.Token);
                            int beforeTicks = ticks; Pump(() => entered.IsSet && ticks >= beforeTicks + 3, 3000);
                            stagedBefore = File.ReadAllBytes(operation.StagingPath);
                            check(workerThread != uiThread && !worker.IsCompleted && HomeVpnPrivateFiles.Load(Path.GetFileNameWithoutExtension(operation.StagingPath)) == afterValue &&
                                (before == null ? !File.Exists(target) : before.SequenceEqual(File.ReadAllBytes(target))),
                                "actual DPAPI staging is off UI, verified, owned and cannot replace the previous address before admission: " + (share ? "share/" : "") + mode);
                            if (mode == "cancel" || mode == "cleanup-lock") cancellation.Cancel();
                            if (mode == "locked") held = File.Open(target, FileMode.Open, FileAccess.Read, FileShare.Read);
                            if (mode == "cleanup-lock") held = File.Open(operation.StagingPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                            if (mode == "changed") { HomeVpnPrivateFiles.Save(name, changedValue); before = File.ReadAllBytes(target); }
                            if (mode == "stage-changed") File.WriteAllBytes(operation.StagingPath, new byte[] { 1, 2, 3 });
                            release.Set();
                            if (mode == "accepted-cancel") {
                                Pump(() => acceptedEntered.IsSet, 3000); cancellation.Cancel();
                                check(!worker.IsCompleted && before.SequenceEqual(File.ReadAllBytes(target)), "cancellation after admission retains actual pending atomic publication without claiming rollback");
                                publishRelease.Set();
                            }
                            Pump(() => worker.IsCompleted, 5000);
                            Exception failure = null; try { worker.GetAwaiter().GetResult(); } catch (Exception ex) { failure = ex; }
                            bool success = mode == "create" || mode == "replace" || mode == "accepted-cancel";
                            if (success) check(failure == null && accepted == 1 && HomeVpnPrivateFiles.Load(name) == afterValue,
                                "accepted protected address uses actual atomic " + (mode == "create" ? "Move" : "Replace") + " without losing its new value");
                            else {
                                check(failure != null && (before == null ? !File.Exists(target) : before.SequenceEqual(File.ReadAllBytes(target))),
                                    "cancel/refusal/conflict/real sharing failure preserves the actual previous or newer address bytes: " + (share ? "share/" : "") + mode);
                                check(accepted == (mode == "locked" ? 1 : 0), "failed private write reports its exact commit admission boundary: " + (share ? "share/" : "") + mode);
                            }
                            if (mode == "cleanup-lock" || mode == "stage-changed") {
                                check(File.Exists(operation.StagingPath), "failed cleanup retains this exact prepared object and its staging path for explicit retry: " + (share ? "share/" : "") + mode);
                                if (mode == "stage-changed") {
                                    check(File.ReadAllBytes(operation.StagingPath).SequenceEqual(new byte[] { 1, 2, 3 }), "cleanup refuses to erase a changed prepared file");
                                    File.WriteAllBytes(operation.StagingPath, stagedBefore); // Restore only this fixture's deliberate edit.
                                }
                                if (held != null) { held.Dispose(); held = null; }
                                var cleanup = Task.Run(() => operation.Dispose()); Pump(() => cleanup.IsCompleted, 5000); cleanup.GetAwaiter().GetResult();
                            }
                            if (share) {
                                check((untouchedHome == null ? !File.Exists(home) : untouchedHome.SequenceEqual(File.ReadAllBytes(home))) &&
                                    (untouchedOther == null ? !File.Exists(other) : untouchedOther.SequenceEqual(File.ReadAllBytes(other))),
                                    "captured per-server share write preserves the home address and another server's protected bytes: " + mode);
                            }
                            check(!File.Exists(operation.StagingPath), "prepared address worker or explicit cleanup retry removes only its own unique stage: " + (share ? "share/" : "") + mode);
                        } finally {
                            release.Set(); publishRelease.Set(); cancellation.Cancel(); if (held != null) held.Dispose();
                            if (worker != null) { Pump(() => worker.IsCompleted, 5000); try { worker.GetAwaiter().GetResult(); } catch { } }
                            if (mode == "stage-changed" && stagedBefore != null && File.Exists(operation.StagingPath)) File.WriteAllBytes(operation.StagingPath, stagedBefore);
                            var cleanup = Task.Run(() => operation.Dispose()); Pump(() => cleanup.IsCompleted, 5000); cleanup.GetAwaiter().GetResult();
                        }
                    }
                } finally {
                    heartbeat.Stop(); form.Close();
                    foreach (var item in original) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); }
                }
            }
        }
        internal static void ModalWizardCase(Form form, Func<bool> step, Func<bool> expectedClose)
        {
            Exception failure = null; bool done = false, inTick = false;
            var watch = Stopwatch.StartNew();
            using (var driver = new System.Windows.Forms.Timer { Interval = 20 }) {
                driver.Tick += delegate {
                    if (done) return;
                    if (watch.ElapsedMilliseconds > 15000) {
                        failure = new TimeoutException("Owned modal wizard fixture deadline"); done = true; form.Dispose(); return;
                    }
                    if (inTick) return;
                    inTick = true;
                    try { done = step(); }
                    catch (Exception ex) { failure = ex; done = true; }
                    finally { inTick = false; }
                    if (done) form.Dispose();
                };
                driver.Start();
                try {
                    form.ShowDialog();
                    if (!done && !expectedClose()) throw new Exception("Modal wizard exited before its owned scenario settled; cancellation must keep the modal open");
                    if (!done) Pump(() => done, 5000); // Explicit close/disposal still owns its actual background I/O.
                    if (failure != null) throw failure;
                } finally { driver.Stop(); form.Dispose(); }
            }
        }
        private static void WizardShareRead(string token, ClipboardService clipboard)
        {
            string shareName = "share-" + HomeVpnAccess.Parse(token).ServerId;
            var original = new Dictionary<string, byte[]>();
            foreach (string name in new[] { "access", "owner", "home-address", shareName }) {
                string path = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat");
                original[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            try {
                HomeVpnPrivateFiles.Save("access", token); HomeVpnPrivateFiles.Save("owner", "null");
                HomeVpnPrivateFiles.Save("home-address", "home.example"); HomeVpnPrivateFiles.Save(shareName, "https://profiles.example");
                foreach (string route in new[] { "cancel-retry", "close", "dispose" })
                using (var relay = new Ikev2RelayService())
                using (var service = new HomeVpnService(relay, Application.ExecutablePath, UdpPort(), UdpPort(), 17878, true))
                using (var entered = new ManualResetEventSlim())
                using (var release = new ManualResetEventSlim())
                using (var modalGuard = new System.Windows.Forms.Timer { Interval = 20 })
                {
                    HomeVpnWizardForm form = null; ProGoForm dialog = null;
                    bool shown = false, fresh = false, closeExpected = false, closed = false, closedAfterRead = false; int phase = 0, turns = 0, reads = 0, readThread = 0, uiThread = Thread.CurrentThread.ManagedThreadId;
                    byte[] retryBytes = null; var expected = original.Keys.ToDictionary(path => path, File.ReadAllBytes);
                    var existingForms = Application.OpenForms.Cast<Form>().ToArray();
                    try {
                        Pump(() => service.InitializationWork.IsCompleted, 5000); service.InitializationWork.GetAwaiter().GetResult();
                        form = new HomeVpnWizardForm(service, clipboard);
                        form.FormClosed += delegate { closed = true; closedAfterRead = service.ShareRefreshWork.IsCompleted && !(bool)Field(form, "busy"); };
                        form.BeforeShareRead = delegate(string stage) {
                            Interlocked.Exchange(ref readThread, Thread.CurrentThread.ManagedThreadId);
                            if (stage == "read") Interlocked.Increment(ref reads);
                            if (stage == "verify") { entered.Set(); if (!release.Wait(5000)) throw new TimeoutException("Wizard share reader fixture deadline"); }
                        };
                        Invoke(form, "ShowStep", 3);
                        modalGuard.Tick += delegate {
                            var child = Application.OpenForms.Cast<Form>().OfType<ProGoForm>().FirstOrDefault(f => f != form && !existingForms.Contains(f) && f.Text == "QR: адрес выдачи профиля");
                            if (child == null) return;
                            dialog = child; var initializing = child as HomeProfileConfigureForm;
                            if (initializing != null && !initializing.InitializationWork.IsCompleted) return;
                            shown = true; fresh = Controls(child).OfType<TextBox>().Any(t => t.Text == "https://retry.example");
                            child.Dispose(); // Only this action owns the new dialog; no SSH/HTTP button is clicked.
                        };
                        var button = Controls(form).OfType<Button>().Single(b => b.Text == "Настроить адрес выдачи QR…");
                        ModalWizardCase(form, delegate {
                            if (phase == 0) {
                                check(form.Modal, "actual configure cancellation runs inside ShowDialog: " + route);
                                button.PerformClick(); phase = 1; return false;
                            }
                            if (phase == 1) {
                                if (!entered.IsSet || ++turns < 3) return false;
                                check(readThread != uiThread && (bool)Field(form, "busy") && !service.ShareRefreshWork.IsCompleted && !shown && !button.Enabled &&
                                    ((Button)Field(form, "cancelWait")).DialogResult == DialogResult.None,
                                    "real modal configure waits responsively on owned read without implicit Cancel close: " + route);
                                turns = 0; phase = 2;
                                if (route == "cancel-retry") ((Button)Field(form, "cancelWait")).PerformClick();
                                else { closeExpected = true; if (route == "close") form.Close(); else form.Dispose(); }
                                return false;
                            }
                            if (phase == 2) {
                                if (++turns < 3) return false;
                                check(!service.ShareRefreshWork.IsCompleted && (route == "dispose" || form.Visible), "real modal cancel/close retains held read across native loop turns: " + route);
                                release.Set(); phase = 3; return false;
                            }
                            if (phase == 3) {
                                if (!service.ShareRefreshWork.IsCompleted || (bool)Field(form, "busy") || (route == "close" && form.Modal)) return false;
                                bool cancelled = false; try { service.ShareRefreshWork.GetAwaiter().GetResult(); } catch (OperationCanceledException) { cancelled = true; }
                                check(cancelled && !shown && !relay.IsRunning && !service.HasOwnedResources, "cancelled modal configure cannot open a late dialog or admit external work: " + route);
                                foreach (var pair in expected) check(pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key)), "modal configure cancellation preserves protected bytes: " + route);
                                // ShowDialog Close returns a hidden, reusable form;
                                // fixture Dispose below owns its final destruction.
                                if (route != "cancel-retry") { check(route == "close" ? closed && closedAfterRead && !form.Visible && !form.Modal : form.IsDisposed,
                                    "explicit modal close returns after actual read settlement; explicit disposal retains worker ownership: " + route); return true; }
                                check(form.Visible && button.Enabled && ((Label)Field(form, "status")).Text.Contains("Чтение адреса отменено"), "settled modal cancellation keeps explicit configure retry available");
                                HomeVpnPrivateFiles.Save(shareName, "https://retry.example"); retryBytes = File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, shareName + ".dat"));
                                phase = 4; modalGuard.Start(); button.PerformClick(); return false;
                            }
                            if (!shown || (bool)Field(form, "busy")) return false;
                            check(reads == 2 && fresh && form.Visible && button.Enabled && !service.InitializationFailed,
                                "actual modal Cancel then Retry rereads fresh address and opens only its matching configure dialog");
                            check(retryBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, shareName + ".dat"))), "cancelling the fresh modal address dialog never rewrites its source");
                            return true;
                        }, () => closeExpected);
                    } finally {
                        release.Set(); modalGuard.Stop(); if (dialog != null) { dialog.Dispose(); var configure = dialog as HomeProfileConfigureForm;
                            if (configure != null) Pump(() => configure.Work.IsCompleted && configure.InitializationWork.IsCompleted && configure.PendingReadWork.IsCompleted, 5000); } if (form != null) form.Dispose();
                        service.Dispose(); var cleanup = service.StopAsync();
                        Pump(() => cleanup.IsCompleted && service.ShareRefreshWork.IsCompleted && service.InitializationWork.IsCompleted, 5000);
                        if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Wizard share reader fixture cleanup incomplete");
                        try { service.ShareRefreshWork.GetAwaiter().GetResult(); } catch (OperationCanceledException) { }
                    }
                }
            } finally { foreach (var pair in original) { if (pair.Value == null) File.Delete(pair.Key); else File.WriteAllBytes(pair.Key, pair.Value); } }
        }
        private static void ConfigureInitialization(string token)
        {
            string share = "share-" + HomeVpnAccess.Parse(token).ServerId;
            var original = new Dictionary<string, byte[]>();
            foreach (string name in new[] { "access", "owner", share, HomeVpnShareRecovery.StorageName }) {
                string path = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat"); original[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            try {
                HomeVpnPrivateFiles.Save("access", token); HomeVpnPrivateFiles.Save("owner", "null"); HomeVpnPrivateFiles.Save(share, "https://profiles.example");
                File.Delete(Path.Combine(HomeVpnPrivateFiles.Root, HomeVpnShareRecovery.StorageName + ".dat"));
                var expected = original.Keys.ToDictionary(path => path, path => File.Exists(path) ? File.ReadAllBytes(path) : null);
                foreach (string route in new[] { "ready", "close", "dispose" })
                using (var relay = new Ikev2RelayService())
                using (var service = new HomeVpnService(relay, true))
                using (var entered = new ManualResetEventSlim())
                using (var release = new ManualResetEventSlim()) {
                    HomeProfileConfigureForm form = null; int phase = 0, turns = 0, workerThread = 0, actions = 0, updates = 0, ui = Thread.CurrentThread.ManagedThreadId;
                    bool closed = false, closedAfterRead = false; int disposedPublications = 0, nativeDisposeUpdates = 0;
                    try {
                        Pump(() => service.InitializationWork.IsCompleted, 5000); service.InitializationWork.GetAwaiter().GetResult();
                        var watch = Stopwatch.StartNew();
                        form = HomeProfileShare.CreateConfigureForm(service,
                            (owner, action, name, id, progress) => { actions++; throw new Exception("Unexpected initial-read SSH"); },
                            (origin, access, cancellation) => { actions++; throw new Exception("Unexpected initial-read HTTPS"); },
                            origin => { actions++; throw new Exception("Unexpected initial-read save"); }, delegate {
                                Interlocked.Exchange(ref workerThread, Thread.CurrentThread.ManagedThreadId); entered.Set();
                                if (!release.Wait(5000)) throw new TimeoutException("Configure actual pending-read fixture deadline");
                            });
                        check(watch.ElapsedMilliseconds < 500 && !form.InitializationWork.IsCompleted, "configure constructor publishes owned loading before physical private read settles: " + route);
                        form.FormClosed += delegate { closed = true; closedAfterRead = form.PendingReadWork.IsCompleted; };
                        ModalWizardCase(form, delegate {
                            if (phase == 0) {
                                if (!entered.IsSet || ++turns < 3) return false;
                                check(form.Modal && workerThread != ui && !form.PendingReadWork.IsCompleted && !Controls(form).OfType<TextBox>().Single().Enabled &&
                                    !Controls(form).OfType<Button>().Single(b => b.Text == "Адрес уже настроен — проверить").Enabled && ((Button)form.CancelButton).Enabled,
                                    "actual configure ShowDialog stays responsive with actions gated and explicit close during owned read: " + route);
                                phase = 1; turns = 0;
                                if (route == "close") form.Close();
                                if (route == "dispose") {
                                    disposedPublications = form.UiPublications; var controls = Controls(form).ToArray(); form.Dispose();
                                    foreach (var control in controls) { control.TextChanged += delegate { updates++; }; control.EnabledChanged += delegate { updates++; }; control.VisibleChanged += delegate { updates++; }; }
                                }
                                return false;
                            }
                            if (phase == 1) {
                                if (route == "dispose" && form.Modal) return false; // Wait for the real ShowDialog finally before releasing I/O.
                                if (++turns < 3) return false;
                                if (route == "dispose") {
                                    check(form.UiPublications == disposedPublications && !form.InitializationWork.IsCompleted, "disposed configure admits no product publication even while native ShowDialog unwinds");
                                    nativeDisposeUpdates = updates;
                                }
                                check(!form.PendingReadWork.IsCompleted && (route == "dispose" ? form.IsDisposed : form.Visible), "configure close/disposal retains its own pending worker across native message turns: " + route);
                                release.Set(); phase = 2; return false;
                            }
                            if (!form.InitializationWork.IsCompleted || !form.Work.IsCompleted || (route == "close" && form.Modal)) return false;
                            check(route == "ready" ? form.InitializationWork.GetAwaiter().GetResult() && form.Visible && Controls(form).OfType<TextBox>().Single().Text == "https://profiles.example" && Controls(form).OfType<TextBox>().Single().Enabled :
                                !form.InitializationWork.GetAwaiter().GetResult() && (route == "close" ? closed && closedAfterRead && !form.Visible && !form.Modal : form.IsDisposed && updates == nativeDisposeUpdates && form.UiPublications == disposedPublications),
                                "configure read publishes only to its live owner; Close waits for physical settlement and Dispose gets no late controls: " + route
                                + "; initialized=" + form.InitializationWork.GetAwaiter().GetResult() + "/disposed=" + form.IsDisposed + "/modal=" + form.Modal
                                + "/controlEvents=" + updates + "/nativeDisposeEvents=" + nativeDisposeUpdates + "/publications=" + form.UiPublications + "/disposedAt=" + disposedPublications);
                            check(actions == 0 && !relay.IsRunning, "initial configure read never admits save, SSH or HTTPS: " + route);
                            foreach (var item in expected) check(item.Value == null ? !File.Exists(item.Key) : item.Value.SequenceEqual(File.ReadAllBytes(item.Key)), "configure owned read/cancel preserves actual private source bytes: " + route);
                            return true;
                        }, () => route != "ready");
                    } finally {
                        release.Set();
                        try { if (form != null) { form.Dispose(); Pump(() => form.Work.IsCompleted && form.InitializationWork.IsCompleted && form.PendingReadWork.IsCompleted, 5000); } }
                        finally { service.Dispose(); var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 5000); if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Configure owned initial read cleanup incomplete"); }
                    }
                }
            } finally { foreach (var item in original) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); } }
        }
        private static void WizardHomeAddressWrite(string token, ClipboardService clipboard)
        {
            var original = new Dictionary<string, byte[]>();
            foreach (string name in new[] { "access", "owner", "home-address" }) {
                string path = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat"); original[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            string addressPath = Path.Combine(HomeVpnPrivateFiles.Root, "home-address.dat");
            try {
                HomeVpnPrivateFiles.Save("access", token); HomeVpnPrivateFiles.Save("owner", "null");
                foreach (string route in new[] { "cancel-retry", "close", "dispose", "accepted-cancel", "accepted-close", "accepted-dispose", "error-retry" })
                {
                    HomeVpnPrivateFiles.Save("home-address", "before.example"); byte[] before = File.ReadAllBytes(addressPath);
                    using (var relay = new Ikev2RelayService())
                    using (var service = new HomeVpnService(relay, Application.ExecutablePath, UdpPort(), UdpPort(), 17878, true))
                    using (var entered = new ManualResetEventSlim())
                    using (var release = new ManualResetEventSlim())
                    using (var accepted = new ManualResetEventSlim())
                    using (var publish = new ManualResetEventSlim())
                    {
                        HomeVpnWizardForm form = null; Task<bool> first = null; FileStream held = null; int phase = 0, turns = 0, workerThread = 0, uiThread = Thread.CurrentThread.ManagedThreadId;
                        bool afterAdmission = route.StartsWith("accepted-", StringComparison.Ordinal), closeExpected = false, closed = false, closedAfterWrite = false;
                        try {
                            Pump(() => service.InitializationWork.IsCompleted, 5000); service.InitializationWork.GetAwaiter().GetResult();
                            form = new HomeVpnWizardForm(service, clipboard);
                            form.FormClosed += delegate { closed = true; closedAfterWrite = first != null && first.IsCompleted && !(bool)Field(form, "busy"); };
                            form.BeforeHomeAddressIo = delegate(string stage) {
                                Interlocked.Exchange(ref workerThread, Thread.CurrentThread.ManagedThreadId);
                                if (stage == "prepared") { entered.Set(); if (!release.Wait(5000)) throw new TimeoutException("Wizard address prepare deadline"); }
                                if (stage == "accepted" && afterAdmission) { accepted.Set(); if (!publish.Wait(5000)) throw new TimeoutException("Wizard accepted address deadline"); }
                            };
                            Invoke(form, "ShowStep", 2); ((TextBox)Field(form, "home")).Text = "after.example";
                            ((CheckBox)Field(form, "routerCheck")).Checked = true;
                            var next = (Button)Field(form, "next");
                            ModalWizardCase(form, delegate {
                                if (phase == 0) { check(form.Modal, "actual address save cancellation uses ShowDialog: " + route); next.PerformClick(); first = service.HomeAddressWork; phase = 1; return false; }
                                if (phase == 1) {
                                    if (!entered.IsSet || ++turns < 3) return false;
                                    check(workerThread != uiThread && !first.IsCompleted && (bool)Field(form, "busy") && !next.Enabled &&
                                        ((Button)Field(form, "cancelWait")).DialogResult == DialogResult.None,
                                        "actual modal save remains responsive and cannot implicitly close or duplicate owned write: " + route);
                                    if (afterAdmission) { release.Set(); phase = 2; return false; }
                                    phase = 2;
                                }
                                if (phase == 2) {
                                    if (afterAdmission && !accepted.IsSet) return false;
                                    turns = 0; phase = 3;
                                    if (route == "error-retry") held = File.Open(addressPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                                    else if (route == "cancel-retry" || route == "accepted-cancel") ((Button)Field(form, "cancelWait")).PerformClick();
                                    else { closeExpected = true; if (route == "close" || route == "accepted-close") form.Close(); else form.Dispose(); }
                                    return false;
                                }
                                if (phase == 3) {
                                    if (++turns < 3) return false;
                                    check(!first.IsCompleted && before.SequenceEqual(File.ReadAllBytes(addressPath)) && (route.EndsWith("dispose", StringComparison.Ordinal) || form.Visible),
                                        "modal cancellation/close waits for actual address I/O across native message turns: " + route);
                                    release.Set(); publish.Set(); phase = 4; return false;
                                }
                                if (phase == 4) {
                                    if (!first.IsCompleted || (bool)Field(form, "busy") || ((route == "close" || route == "accepted-close") && form.Modal)) return false;
                                    bool cancelled = false, saved = false; Exception failure = null;
                                    try { saved = first.GetAwaiter().GetResult(); } catch (Exception ex) { failure = ex; cancelled = ex is OperationCanceledException; }
                                    check(route == "error-retry" ? failure is IOException && before.SequenceEqual(File.ReadAllBytes(addressPath)) :
                                        afterAdmission ? saved && !cancelled && HomeVpnPrivateFiles.Load("home-address") == "after.example" : cancelled && before.SequenceEqual(File.ReadAllBytes(addressPath)), "actual modal result distinguishes accepted persistence from confirmed cancellation: " + route);
                                    if (closeExpected) { check(route.EndsWith("dispose", StringComparison.Ordinal) ? form.IsDisposed : closed && closedAfterWrite && !form.Visible && !form.Modal,
                                        "explicit modal close returns after physical save settlement; disposal admits no late controls: " + route); return true; }
                                    check(form.Visible && next.Enabled && (int)Field(form, "step") == 2, "cancelled modal save stays open with an explicit next action: " + route);
                                    if (afterAdmission) { check(((Label)Field(form, "status")).Text.Contains("уже сохранён"), "late cancellation reports accepted address as saved without claiming rollback"); return true; }
                                    check(((Label)Field(form, "status")).Text.Contains(route == "error-retry" ? "Не удалось подтвердить" : "Сохранение адреса отменено"), "modal address cancellation or real file error gives an honest actionable retry result: " + route);
                                    if (held != null) { held.Dispose(); held = null; }
                                    phase = 5; next.PerformClick(); return false;
                                }
                                if ((bool)Field(form, "busy") || (int)Field(form, "step") != 3) return false;
                                check(!Object.ReferenceEquals(first, service.HomeAddressWork) && service.HomeAddressWork.GetAwaiter().GetResult() && service.HomeAddress == "after.example",
                                    "modal Cancel then Retry owns one fresh actual address write and advances only after its saved result");
                                return true;
                            }, () => closeExpected);
                        } finally {
                            release.Set(); publish.Set(); if (held != null) held.Dispose(); if (form != null) form.Dispose(); service.Dispose(); var cleanup = service.StopAsync();
                            Pump(() => cleanup.IsCompleted && service.HomeAddressWork.IsCompleted && service.InitializationWork.IsCompleted, 5000);
                            if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Modal address save fixture cleanup incomplete");
                            if (first != null) try { first.GetAwaiter().GetResult(); } catch (OperationCanceledException) { } catch (IOException) { }
                        }
                    }
                }
            } finally { foreach (var pair in original) { if (pair.Value == null) File.Delete(pair.Key); else File.WriteAllBytes(pair.Key, pair.Value); } }
        }
        private static void OwnedShareRefreshWorker(string token)
        {
            string shareName = "share-" + HomeVpnAccess.Parse(token).ServerId;
            var original = new Dictionary<string, byte[]>();
            foreach (string name in new[] { "access", "owner", "home-address", shareName }) {
                string path = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat");
                original[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            using (var form = new Form())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            {
                int ticks = 0, uiThread = Thread.CurrentThread.ManagedThreadId;
                heartbeat.Tick += delegate { ticks++; }; form.Show(); heartbeat.Start();
                try {
                    HomeVpnPrivateFiles.Save("access", token); HomeVpnPrivateFiles.Save("owner", "null");
                    HomeVpnPrivateFiles.Save("home-address", "home.example"); HomeVpnPrivateFiles.Save(shareName, "https://profiles.example");
                    foreach (string mode in new[] { "clean", "cancel", "stop", "dispose", "newer", "change" })
                    using (var relay = new Ikev2RelayService())
                    using (var entered = new ManualResetEventSlim())
                    using (var release = new ManualResetEventSlim())
                    using (var cancellation = new CancellationTokenSource())
                    using (var service = new HomeVpnService(relay, Application.ExecutablePath, UdpPort(), UdpPort(), 17878, true))
                    {
                        Task<bool> read = null, stop = null; int readThread = 0, reads = 0;
                        try {
                            Pump(() => service.InitializationWork.IsCompleted, 5000); service.InitializationWork.GetAwaiter().GetResult();
                            service.SetShareOrigin("https://profiles.example");
                            var expected = original.Keys.ToDictionary(path => path, File.ReadAllBytes);
                            var watch = Stopwatch.StartNew();
                            read = service.RefreshShareOriginAsync(cancellation.Token, delegate(string stage) {
                                Interlocked.Exchange(ref readThread, Thread.CurrentThread.ManagedThreadId);
                                if (stage == "read") Interlocked.Increment(ref reads);
                                if (stage == "verify") { entered.Set(); if (!release.Wait(5000)) throw new TimeoutException("Owned share read fixture deadline"); }
                            });
                            check(watch.ElapsedMilliseconds < 500 && Object.ReferenceEquals(read, service.ShareRefreshWork), "share retry publishes actual read ownership before returning: " + mode);
                            int beforeTicks = ticks; Pump(() => entered.IsSet && ticks >= beforeTicks + 3, 3000);
                            check(readThread != uiThread && !read.IsCompleted && reads == 1 &&
                                Object.ReferenceEquals(read, service.RefreshShareOriginAsync(CancellationToken.None)),
                                "held real share read keeps UI alive and duplicate retry joins one worker: " + mode);
                            if (mode == "cancel") cancellation.Cancel();
                            if (mode == "stop") { watch.Restart(); stop = service.StopAsync(); check(watch.ElapsedMilliseconds < 500 && !stop.IsCompleted, "stop retains and cancels actual share reader without waiting on UI"); }
                            if (mode == "dispose") { watch.Restart(); service.Dispose(); stop = service.StopWork; check(watch.ElapsedMilliseconds < 500 && !stop.IsCompleted, "dispose retains cancelled actual share read even after owner closes"); }
                            if (mode == "newer") service.SetShareOrigin("https://newer.example");
                            if (mode == "change") HomeVpnPrivateFiles.Save(shareName, "https://changed.example");
                            if (mode == "newer" || mode == "change") expected[Path.Combine(HomeVpnPrivateFiles.Root, shareName + ".dat")] = File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, shareName + ".dat"));
                            release.Set(); Pump(() => read.IsCompleted && (stop == null || stop.IsCompleted), 5000);
                            bool cancelled = false, stale = false; bool result = false;
                            try { result = read.GetAwaiter().GetResult(); } catch (OperationCanceledException) { cancelled = true; } catch (InvalidOperationException) { stale = true; }
                            if (mode == "cancel" || mode == "stop" || mode == "dispose") check(cancelled && !stale, "cancelled share reader cannot publish late cache or error: " + mode);
                            else if (mode == "newer") check(stale && service.ShareOrigin == "https://newer.example", "old actual read cannot overwrite a newer explicitly saved share value");
                            else if (mode == "change") {
                                bool warned = false; try { var ignored = service.ShareOrigin; } catch (InvalidOperationException) { warned = true; }
                                check(stale && warned, "external protected-file change refuses an incoherent share result with an honest cached warning");
                            } else check(result && service.ShareOrigin == "https://profiles.example", "clean owned share read publishes the verified value");
                            if (stop != null) check(stop.GetAwaiter().GetResult() && !service.HasOwnedResources, "stop settles only after cancelled real share reader exits: " + mode);
                            if (mode != "dispose") {
                                var retry = service.RefreshShareOriginAsync(CancellationToken.None); Pump(() => retry.IsCompleted, 5000);
                                check(!Object.ReferenceEquals(read, retry) && retry.GetAwaiter().GetResult() && !service.InitializationFailed &&
                                    service.ShareOrigin == (mode == "change" ? "https://changed.example" : mode == "newer" ? "https://newer.example" : "https://profiles.example"),
                                    "explicit retry after settled read restores fresh cache without parallel workers: " + mode);
                            }
                            foreach (var pair in expected) check(pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key)), "share read and retry never overwrite protected source bytes: " + mode);
                        } finally {
                            release.Set(); cancellation.Cancel(); service.Dispose(); var cleanup = service.StopAsync();
                            Pump(() => cleanup.IsCompleted && service.ShareRefreshWork.IsCompleted && service.InitializationWork.IsCompleted, 5000);
                            if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Owned share read fixture cleanup incomplete");
                            if (read != null) try { read.GetAwaiter().GetResult(); } catch (OperationCanceledException) { } catch (InvalidOperationException) { }
                            try { service.ShareRefreshWork.GetAwaiter().GetResult(); } catch (OperationCanceledException) { } catch (InvalidOperationException) { }
                        }
                    }
                    using (var relay = new Ikev2RelayService())
                    using (var service = new HomeVpnService(relay, Application.ExecutablePath, UdpPort(), UdpPort(), 17878, true)) {
                        try {
                            Pump(() => service.InitializationWork.IsCompleted, 5000); service.InitializationWork.GetAwaiter().GetResult();
                            Task<bool> refused;
                            using (var held = File.Open(Path.Combine(HomeVpnPrivateFiles.Root, shareName + ".dat"), FileMode.Open, FileAccess.Read, FileShare.None)) {
                                refused = service.RefreshShareOriginAsync(CancellationToken.None); Pump(() => refused.IsCompleted, 5000);
                                check(!refused.GetAwaiter().GetResult() && service.InitializationFailed, "real sharing violation leaves an observable failed share read");
                            }
                            var retry = service.RefreshShareOriginAsync(CancellationToken.None); Pump(() => retry.IsCompleted, 5000);
                            check(retry.GetAwaiter().GetResult() && !service.InitializationFailed && !String.IsNullOrEmpty(service.ShareOrigin), "unlock then explicit retry recovers the saved share without restarting or rewriting the service");
                        } finally { service.Dispose(); var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 5000); if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Share lock retry cleanup incomplete"); }
                    }
                } finally {
                    heartbeat.Stop(); form.Close();
                    foreach (var pair in original) { if (pair.Value == null) File.Delete(pair.Key); else File.WriteAllBytes(pair.Key, pair.Value); }
                }
            }
        }
        private static void OwnedInitializationWorker(string token)
        {
            string shareName = "share-" + HomeVpnAccess.Parse(token).ServerId;
            var original = new Dictionary<string, byte[]>();
            foreach (string name in new[] { "access", "owner", "home-address", shareName }) {
                string path = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat");
                original[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            using (var form = new Form())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            {
                int ticks = 0, uiThread = Thread.CurrentThread.ManagedThreadId;
                form.Show(); heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                try {
                    HomeVpnPrivateFiles.Save("access", token); HomeVpnPrivateFiles.Save("owner", "null");
                    HomeVpnPrivateFiles.Save("home-address", "home.example"); HomeVpnPrivateFiles.Save(shareName, "https://profiles.example");
                    var before = original.Keys.ToDictionary(path => path, File.ReadAllBytes);
                    foreach (string mode in new[] { "load", "stop", "dispose", "stop-dispose" })
                    using (var relay = new Ikev2RelayService())
                    using (var entered = new ManualResetEventSlim())
                    using (var release = new ManualResetEventSlim())
                    {
                        HomeVpnService service = null; Task start = null; Task<bool> stop = null;
                        int readThread = 0;
                        try {
                            var watch = Stopwatch.StartNew();
                            service = new HomeVpnService(relay, Application.ExecutablePath, UdpPort(), UdpPort(), 17878, true, delegate(string stage) {
                                Interlocked.Exchange(ref readThread, Thread.CurrentThread.ManagedThreadId);
                                if (stage == "verify") { entered.Set(); if (!release.Wait(5000)) throw new TimeoutException("Owned initialization read barrier"); }
                            });
                            check(watch.ElapsedMilliseconds < 500 && service.IsInitializing, "async service constructor returns a published initialization owner before private I/O settles: " + mode);
                            int beforeTicks = ticks; Pump(() => entered.IsSet && ticks >= beforeTicks + 3, 3000);
                            check(readThread != uiThread && service.Access == null && !service.InitializationWork.IsCompleted,
                                "held actual initialization keeps UI alive and cannot expose a partially read identity: " + mode);
                            if (mode == "stop" || mode == "stop-dispose") {
                                start = service.StartAsync(); check(Object.ReferenceEquals(start, service.StartAsync()), "start before load retains one physical startup task");
                                watch.Restart(); stop = service.StopAsync();
                                check(watch.ElapsedMilliseconds < 500 && !stop.IsCompleted && !start.IsCompleted && !service.HasOwnedResources,
                                    "phone stop retains pending initialization and cancelled startup without private session or native launch");
                            }
                            if (mode == "dispose" || mode == "stop-dispose") {
                                watch.Restart(); service.Dispose(); stop = service.StopWork;
                                check(watch.ElapsedMilliseconds < 500 && !stop.IsCompleted && !service.InitializationWork.IsCompleted,
                                    "dispose cancels even an earlier waiting stop without abandoning its initialization task: " + mode);
                            }
                            release.Set(); Pump(() => service.InitializationWork.IsCompleted && (stop == null || stop.IsCompleted), 5000);
                            if (mode == "dispose" || mode == "stop-dispose") {
                                bool cancelled = false; try { service.InitializationWork.GetAwaiter().GetResult(); } catch (OperationCanceledException) { cancelled = true; }
                                check(cancelled && service.Access == null && service.Owner == null && service.HomeAddress == "" && service.ShareOrigin == null,
                                    "disposed initialization settles cancelled without late identity or share publication: " + mode);
                            } else {
                                check(service.InitializationWork.GetAwaiter().GetResult() && !service.InitializationFailed && service.Access != null && service.HomeAddress == "home.example",
                                    "settled initialization publishes the complete coherent saved state: " + mode);
                                using (var held = File.Open(Path.Combine(HomeVpnPrivateFiles.Root, shareName + ".dat"), FileMode.Open, FileAccess.Read, FileShare.None))
                                    check(service.ShareOrigin == "https://profiles.example", "initialized service share getter uses frozen memory even when the real protected source is locked");
                            }
                            if (start != null) { bool cancelled = false; try { start.GetAwaiter().GetResult(); } catch (OperationCanceledException) { cancelled = true; } check(cancelled, "stop before initialized startup prevents late SSH or relay admission"); }
                            if (stop != null) check(stop.GetAwaiter().GetResult() && !service.HasOwnedResources && !relay.IsRunning, "initialization-aware stop completes only after the actual worker settled: " + mode);
                            foreach (var pair in before) check(pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key)), "owned initialization never rewrites its source bytes: " + mode);
                        } finally {
                            release.Set();
                            if (service != null) {
                                service.Dispose(); var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 5000);
                                if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Owned initialization cleanup did not settle");
                                try { service.InitializationWork.GetAwaiter().GetResult(); } catch (OperationCanceledException) { }
                                if (start != null) try { start.GetAwaiter().GetResult(); } catch (OperationCanceledException) { }
                            }
                        }
                    }
                    using (var relay = new Ikev2RelayService())
                    using (var held = File.Open(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"), FileMode.Open, FileAccess.Read, FileShare.None))
                    using (var service = new HomeVpnService(relay, Application.ExecutablePath, UdpPort(), UdpPort(), 17878, true)) {
                        try {
                            Pump(() => service.InitializationWork.IsCompleted, 5000);
                            check(!service.InitializationWork.GetAwaiter().GetResult() && service.InitializationFailed && service.Access == null,
                                "real initialization sharing refusal has an observable failed outcome and no partial saved identity");
                        } finally { service.Dispose(); var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 5000); if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Failed initialization cleanup did not settle"); }
                    }
                    foreach (string optional in new[] { "owner", "home-address", shareName })
                    {
                        Mode("ready");
                        int ike = UdpPort(), nat = UdpPort(); while (nat == ike) nat = UdpPort();
                        using (var relay = new Ikev2RelayService())
                        using (var held = File.Open(Path.Combine(HomeVpnPrivateFiles.Root, optional + ".dat"), FileMode.Open, FileAccess.Read, FileShare.None))
                        using (var service = new HomeVpnService(relay, Application.ExecutablePath, ike, nat, 17878, true))
                        {
                            Task start = null;
                            try {
                                Pump(() => service.InitializationWork.IsCompleted, 5000);
                                check(!service.InitializationWork.GetAwaiter().GetResult() && service.InitializationFailed && service.Access != null,
                                    "unreadable optional metadata is an explicit warning and cannot erase independently valid channel access: " + optional);
                                if (optional == shareName) {
                                    bool refused = false; try { var ignored = service.ShareOrigin; } catch (InvalidOperationException) { refused = true; }
                                    check(refused, "unreadable cached share has an honest metadata error without disk I/O or unconfirmed fallback");
                                }
                                start = service.StartAsync(); Pump(() => start.IsCompleted, 8000); start.GetAwaiter().GetResult();
                                check(relay.IsRunning, "actual owned process and real local relay can start while optional metadata stays unavailable: " + optional);
                            } finally {
                                service.Dispose(); var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 8000);
                                if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Optional metadata fixture cleanup incomplete");
                                if (start != null) try { start.GetAwaiter().GetResult(); } catch (OperationCanceledException) { }
                            }
                        }
                        foreach (var pair in before) check(pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key)), "optional metadata warning/start never repairs or overwrites the protected source: " + optional);
                    }
                } finally {
                    heartbeat.Stop(); form.Close();
                    foreach (var pair in original) { if (pair.Value == null) File.Delete(pair.Key); else File.WriteAllBytes(pair.Key, pair.Value); }
                }
            }
        }
        private static void PublicationReadiness()
        {
            string path = Path.Combine(marker, "publication-probe"); File.Delete(path);
            using (var staged = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim()) {
                var writer = Task.Run(delegate {
                    HomeVpnFixtureFiles.Publish(path, Process.GetCurrentProcess().Id.ToString(), delegate { staged.Set(); release.Wait(); });
                });
                bool unpublished = false;
                try {
                    Pump(() => staged.IsSet || writer.IsCompleted);
                    unpublished = staged.IsSet && !writer.IsCompleted && !File.Exists(path);
                } finally { release.Set(); Pump(() => writer.IsCompleted); }
                writer.GetAwaiter().GetResult();
                check(unpublished, "fixture PID readiness stays unpublished while the staged writer is held open");
                check(Int32.Parse(File.ReadAllText(path)) == Process.GetCurrentProcess().Id
                    && Directory.GetFiles(marker, "publication-probe.*.tmp").Length == 0,
                    "fixture PID readiness publishes complete readable data only after its writer closes");
                staged.Reset(); release.Reset();
                int next = Process.GetCurrentProcess().Id + 1;
                writer = Task.Run(delegate {
                    HomeVpnFixtureFiles.Publish(path, next.ToString(), delegate { staged.Set(); release.Wait(); });
                });
                bool oldComplete = false;
                try {
                    Pump(() => staged.IsSet || writer.IsCompleted);
                    oldComplete = staged.IsSet && !writer.IsCompleted && Int32.Parse(File.ReadAllText(path)) == Process.GetCurrentProcess().Id;
                } finally { release.Set(); Pump(() => writer.IsCompleted); }
                writer.GetAwaiter().GetResult();
                check(oldComplete && Int32.Parse(File.ReadAllText(path)) == next
                    && Directory.GetFiles(marker, "publication-probe.*.tmp").Length == 0,
                    "fixture recovery phases atomically replace a previous complete PID without exposing a staged writer");
            }
            File.Delete(path);
        }
        private static void Mode(string value)
        {
            KillFixture(); File.Delete(Path.Combine(marker, "pid")); File.Delete(Path.Combine(marker, "receiver"));
            File.WriteAllText(Path.Combine(marker, "mode"), value);
        }
        private static int Pid() { return Int32.Parse(File.ReadAllText(Path.Combine(marker, "pid"))); }
        private static bool Gone(int pid) { try { using (var p = Process.GetProcessById(pid)) return p.HasExited; } catch (ArgumentException) { return true; } }
        private static void KillFixture() { if (!File.Exists(Path.Combine(marker, "pid"))) return; try { using (var p = Process.GetProcessById(Pid())) { if (!p.HasExited) { p.Kill(); p.WaitForExit(2000); } } } catch (ArgumentException) { } }
        private static void Pump(Func<bool> ready, int timeout = 16000)
        {
            var watch = Stopwatch.StartNew();
            while (!ready()) { Application.DoEvents(); Thread.Sleep(10); if (watch.ElapsedMilliseconds > timeout) throw new Exception("Home wait fixture timeout"); }
            Application.DoEvents();
        }
        private static object Field(object target, string name) { return target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target); }
        private static void Invoke(object target, string name, params object[] args) { target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args); }
        private static IEnumerable<Control> Controls(Control root) { foreach (Control c in root.Controls) { yield return c; foreach (var child in Controls(c)) yield return child; } }
        private static void Shot(Form form, string work, string name) { using (var image = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(Path.Combine(work, name + ".png")); } }
        private static void Clean(HomeVpnService service, int pid)
        {
            check(Gone(pid) && !service.Relay.IsRunning, "cancelled home startup closes owned process and never publishes a relay");
            check(Field(service, "proxy") == null && Field(service, "sessionDirectory") == null, "confirmed cancellation releases proxy ownership and private temporary session");
            check(HomeVpnPrivateFiles.Load("access") != null && service.Access != null, "cancelled startup retains protected saved access");
        }
        private static void NativeCancel(string token, ClipboardService clipboard, string work, string mode, string route)
        {
            Mode(mode);
            using (var relay = new Ikev2RelayService())
            using (var service = Service(relay))
            using (var form = new HomeVpnWizardForm(service, clipboard))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 }) {
                service.UseToken(token, null); Invoke(form, "ShowStep", 4); form.Show();
                var accessBefore = File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"));
                int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                Controls(form).OfType<Button>().Single(b => b.Text == "Запустить канал").PerformClick();
                Pump(() => File.Exists(Path.Combine(marker, mode == "silent" ? "receiver" : "pid")) && ticks >= 3);
                int pid = Pid(); var cancel = (Button)Field(form, "cancelWait"); var status = (Label)Field(form, "status");
                check(cancel.Visible && cancel.Enabled && !(bool)((Control)Field(form, "body")).Enabled && !(bool)((Button)Field(form, "next")).Enabled,
                    "waiting startup offers scoped cancellation and blocks competing actions: " + mode + "/" + route);
                check(form.AcceptButton == null && form.CancelButton == cancel && status.Text.Contains("10 секунд")
                    && status.AccessibilityObject.Description == status.Text && cancel.AccessibilityObject.Name == "Отменить запуск канала",
                    "waiting startup is accessible, bounded and has no implicit Enter action");
                cancel.Focus(); check(form.ActiveControl == cancel, "cancel remains a reachable keyboard target outside disabled wizard body");
                if (mode == "silent" && route == "button") {
                    Shot(form, work, "home-wait-pending"); form.ClientSize = new Size(684, 581); Application.DoEvents();
                    check(form.RectangleToClient(cancel.RectangleToScreen(cancel.ClientRectangle)).Bottom <= form.ClientSize.Height,
                        "waiting cancellation remains visible at minimum size"); Shot(form, work, "home-wait-minimum");
                }
                var watch = Stopwatch.StartNew();
                if (route == "button") cancel.PerformClick();
                else if (route == "escape") typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { Keys.Escape });
                else if (route == "close") form.Close();
                else form.Dispose();
                Pump(() => !(bool)Field(form, "busy"));
                check(watch.ElapsedMilliseconds < 3500, "native cancellation settles without waiting for the network deadline"); Clean(service, pid);
                check(accessBefore.SequenceEqual(File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"))), "startup cancellation does not rewrite saved credentials");
                if (route == "close" || route == "dispose") check(form.IsDisposed, "close or disposal finishes without late UI access");
                else {
                    check(form.Visible && !cancel.Visible && ((Control)Field(form, "body")).Enabled && status.Text.Contains("отменён") && status.ForeColor == UiTheme.Muted,
                        "button and Escape cancellation keep a retryable wizard with a cancellation outcome");
                    if (mode == "silent" && route == "button") Shot(form, work, "home-wait-cancelled");
                    Mode("ready"); Controls(form).OfType<Button>().Single(b => b.Text == "Запустить канал").PerformClick();
                    Pump(() => !(bool)Field(form, "busy"));
                    check(relay.IsRunning && status.Text.Contains("на телефоне отдельно"), "explicit retry starts a fresh channel without claiming phone internet"); service.Stop(); form.Close();
                }
            }
        }
        private static void ImportCancel(string token, ClipboardService clipboard)
        {
            Mode("silent");
            using (var relay = new Ikev2RelayService())
            using (var service = Service(relay))
            using (var form = new HomeVpnWizardForm(service, clipboard)) {
                form.Show(); Invoke(form, "ShowStep", 1);
                ((TextBox)Field(form, "token")).Text = token;
                ((Button)Field(form, "next")).PerformClick(); Pump(() => File.Exists(Path.Combine(marker, "receiver")));
                ((Button)Field(form, "cancelWait")).PerformClick(); Pump(() => !(bool)Field(form, "busy"));
                check((int)Field(form, "step") == 1 && ((TextBox)Field(form, "token")).Text == token && ((TextBox)Field(form, "token")).UseSystemPasswordChar,
                    "import cancellation preserves the masked draft and does not advance to router setup");
                check(HomeVpnPrivateFiles.Load("access") == token && ((Label)Field(form, "status")).Text.Contains("Сохранённый доступ"),
                    "import cancellation describes already saved access without a rollback claim"); form.Close();
            }
        }
        private static void ServiceChecks(string token)
        {
            Mode("silent");
            using (var relay = new Ikev2RelayService())
            using (var service = Service(relay)) {
                service.UseToken(token, null);
                using (var cts = new CancellationTokenSource()) {
                    cts.Cancel(); var task = service.StartAsync(cts.Token); Pump(() => task.IsCompleted);
                    check(task.IsCanceled && !File.Exists(Path.Combine(marker, "pid")), "pre-cancelled startup creates no process or private session");
                }
                var timeout = service.StartAsync(); Pump(() => timeout.IsCompleted, 15000);
                check(timeout.IsFaulted && !relay.IsRunning && Field(service, "proxy") == null && Field(service, "sessionDirectory") == null,
                    "silent receiver deadline reports failure and cleans startup ownership");
                check(relay.LastError != null, "deadline failure remains distinct from requested cancellation");
                Mode("ready"); var success = service.StartAsync(); Pump(() => success.IsCompleted); success.GetAwaiter().GetResult();
                using (var cts = new CancellationTokenSource()) {
                    cts.Cancel(); var task = service.StartAsync(cts.Token); Pump(() => task.IsCompleted);
                    check(task.IsCanceled && relay.IsRunning, "pre-cancelled request preserves an already running phone channel");
                }
                service.Stop();
            }
        }
        private static void CleanupFailure(string token, ClipboardService clipboard)
        {
            Mode("silent");
            using (var relay = new Ikev2RelayService())
            using (var service = Service(relay))
            using (var form = new HomeVpnWizardForm(service, clipboard)) {
                service.UseToken(token, null); Invoke(form, "ShowStep", 4); form.Show();
                Controls(form).OfType<Button>().Single(b => b.Text == "Запустить канал").PerformClick();
                Pump(() => File.Exists(Path.Combine(marker, "receiver")));
                string session = (string)Field(service, "sessionDirectory");
                using (var held = File.Open(Path.Combine(session, "ssh_config"), FileMode.Open, FileAccess.Read, FileShare.None)) {
                    form.Close(); Pump(() => !(bool)Field(form, "busy"));
                    var status = (Label)Field(form, "status");
                    check(form.Visible && !relay.IsRunning && Gone(Pid()) && status.ForeColor == UiTheme.Error && status.Text.Contains("отмена пока не подтверждена"),
                        "cleanup failure refuses successful cancellation and queued close while explaining the unsettled result");
                    check((string)Field(service, "sessionDirectory") == session && HomeVpnPrivateFiles.Load("access") == token,
                        "cleanup failure retains private session ownership and saved access for recovery");
                    int oldPid = Pid(); Controls(form).OfType<Button>().Single(b => b.Text == "Запустить канал").PerformClick();
                    Pump(() => !(bool)Field(form, "busy"));
                    check((string)Field(service, "sessionDirectory") == session && Pid() == oldPid && !relay.IsRunning && status.Text.Contains("ещё не очищен"),
                        "retry cannot discard an unsettled private session or spawn another process while its file stays locked");
                }
                service.Stop(); check(!Directory.Exists(session), "explicit stop retries private session removal after its lock is released"); form.Close();
            }
        }
        private static void OwnedPreparationStop(string token, bool dispose)
        {
            Mode("ready");
            using (var relay = new Ikev2RelayService())
            using (var service = Service(relay))
            using (var form = new Form())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            {
                service.UseToken(token, null);
                var accessBefore = File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"));
                form.Show(); int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                service.BeforeSessionIo = delegate(string stage) {
                    if (stage == "config") { entered.Set(); if (!release.Wait(5000)) throw new TimeoutException("Owned session preparation barrier timed out"); }
                };
                Task start = null; Task<bool> stop = null;
                try
                {
                    var watch = Stopwatch.StartNew(); start = service.StartAsync();
                    check(watch.ElapsedMilliseconds < 500 && Object.ReferenceEquals(start, service.StartWork),
                        "phone start publishes its tracked worker without synchronous private I/O");
                    Pump(() => entered.IsSet && ticks >= 3, 3000);
                    string session = (string)Field(service, "sessionDirectory");
                    check(!start.IsCompleted && File.Exists(Path.Combine(session, "access"))
                        && !File.Exists(Path.Combine(session, "ssh_config")) && !File.Exists(Path.Combine(marker, "pid")),
                        "held real session preparation retains its private files before any native child");
                    check(Object.ReferenceEquals(start, service.StartAsync()), "duplicate phone start observes the same owned preparation task");
                    watch.Restart();
                    if (dispose) { service.Dispose(); stop = service.StopWork; } else stop = service.StopAsync();
                    check(watch.ElapsedMilliseconds < 500 && Object.ReferenceEquals(stop, service.StopAsync()),
                        "phone Stop or Dispose returns promptly and retains one shared cleanup task: " + dispose);
                    int before = ticks; Pump(() => ticks >= before + 3, 1000);
                    var refused = service.StartAsync(); bool blocked = false;
                    try { refused.GetAwaiter().GetResult(); } catch (ObjectDisposedException) { blocked = dispose; } catch (InvalidOperationException) { blocked = true; }
                    check(blocked && !stop.IsCompleted && !start.IsCompleted && Directory.Exists(session),
                        "pending phone stop preserves session ownership and refuses a competing start: " + dispose);
                    release.Set(); Pump(() => start.IsCompleted && stop.IsCompleted, 8000);
                    check(start.IsCanceled && stop.GetAwaiter().GetResult() && !Directory.Exists(session)
                        && Field(service, "proxy") == null && Field(service, "sessionDirectory") == null && !relay.IsRunning
                        && !File.Exists(Path.Combine(marker, "pid")),
                        "cancelled preparation settles before cleanup without late config or native launch: " + dispose);
                    check(accessBefore.SequenceEqual(File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"))),
                        "async phone cancellation preserves the exact saved DPAPI access: " + dispose);
                    if (!dispose)
                    {
                        service.BeforeSessionIo = null;
                        var retry = service.StartAsync(); Pump(() => retry.IsCompleted, 8000); retry.GetAwaiter().GetResult();
                        check(relay.IsRunning && !Object.ReferenceEquals(retry, start), "phone retry admits one fresh operation only after the cancelled worker settled");
                        int pid = Pid(); stop = service.StopAsync(); Pump(() => stop.IsCompleted, 5000);
                        check(stop.GetAwaiter().GetResult() && Gone(pid), "async phone stop confirms its own native child exit before success");
                    }
                }
                finally
                {
                    release.Set(); service.BeforeSessionIo = null;
                    try { var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 8000); if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Owned phone fixture cleanup incomplete"); }
                    finally { heartbeat.Stop(); form.Close(); }
                }
            }
        }

        private static void OwnedCleanupStop(string token)
        {
            Mode("ready");
            using (var relay = new Ikev2RelayService())
            using (var service = Service(relay))
            using (var form = new Form())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            {
                service.UseToken(token, null); form.Show();
                int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                Task pendingImport = null;
                try
                {
                    var start = service.StartAsync(); Pump(() => start.IsCompleted, 8000); start.GetAwaiter().GetResult();
                    int pid = Pid(); string session = (string)Field(service, "sessionDirectory");
                    service.BeforeSessionIo = delegate(string stage) {
                        if (stage == "cleanup") { entered.Set(); if (!release.Wait(5000)) throw new TimeoutException("Owned session cleanup barrier timed out"); }
                    };
                    var watch = Stopwatch.StartNew(); var stop = service.StopAsync();
                    check(watch.ElapsedMilliseconds < 500, "phone cleanup queues native disposal and directory removal outside the UI thread");
                    Pump(() => entered.IsSet, 3000); int before = ticks; Pump(() => ticks >= before + 3, 1000);
                    check(!stop.IsCompleted && Object.ReferenceEquals(stop, service.StopAsync()) && Gone(pid)
                        && !relay.IsRunning && Directory.Exists(session),
                        "phone cleanup heartbeat continues while its exact private session remains owned after child exit");
                    release.Set(); Pump(() => stop.IsCompleted, 5000);
                    check(stop.GetAwaiter().GetResult() && !Directory.Exists(session), "phone cleanup success follows actual directory removal");
                    service.BeforeSessionIo = null;
                    start = service.StartAsync(); Pump(() => start.IsCompleted, 8000); start.GetAwaiter().GetResult();
                    session = (string)Field(service, "sessionDirectory"); pid = Pid();
                    using (var held = File.Open(Path.Combine(session, "ssh_config"), FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        stop = service.StopAsync(); Pump(() => stop.IsCompleted, 5000);
                        check(!stop.GetAwaiter().GetResult() && Gone(pid) && Directory.Exists(session)
                            && (string)Field(service, "sessionDirectory") == session,
                            "real locked session makes async phone stop fail honestly while retaining its exact cleanup owner");
                        var oldAccess = service.Access; var oldOwner = service.Owner;
                        var accessFile = Path.Combine(HomeVpnPrivateFiles.Root, "access.dat");
                        var ownerFile = Path.Combine(HomeVpnPrivateFiles.Root, "owner.dat");
                        var accessBytes = File.ReadAllBytes(accessFile); var ownerBytes = File.ReadAllBytes(ownerFile);
                        bool refusedImport = false;
                        pendingImport = Task.Run(delegate {
                            try { service.UseToken(token, new HomeVpnOwner { Host = oldAccess.Host, Port = oldAccess.Port, Login = "root" }); }
                            catch (HomeVpnLocalCleanupException) { refusedImport = true; }
                        });
                        Pump(() => pendingImport.IsCompleted, 5000); pendingImport.GetAwaiter().GetResult();
                        check(refusedImport && Object.ReferenceEquals(service.Access, oldAccess) && Object.ReferenceEquals(service.Owner, oldOwner)
                            && accessBytes.SequenceEqual(File.ReadAllBytes(accessFile)) && ownerBytes.SequenceEqual(File.ReadAllBytes(ownerFile))
                            && (string)Field(service, "sessionDirectory") == session && !relay.IsRunning && Gone(pid),
                            "failed real cleanup refuses token import before DPAPI or active identity changes and retains the old session for retry");
                    }
                    stop = service.StopAsync(); Pump(() => stop.IsCompleted, 5000);
                    check(stop.GetAwaiter().GetResult() && !Directory.Exists(session), "explicit phone stop retry removes the previously locked owned session");
                }
                finally
                {
                    release.Set(); service.BeforeSessionIo = null;
                    try {
                        // The lock scope has exited even when the assertion or
                        // primary wait failed. Settle our retained import before
                        // fixture data restoration can race its compatibility I/O.
                        if (pendingImport != null) { Pump(() => pendingImport.IsCompleted, 8000); pendingImport.GetAwaiter().GetResult(); }
                        var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 8000); if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Owned phone fixture cleanup incomplete");
                    }
                    finally { heartbeat.Stop(); form.Close(); }
                }
            }
        }

        private static void WizardOwnedStop(string token, ClipboardService clipboard, string route)
        {
            Mode("ready");
            using (var relay = new Ikev2RelayService())
            using (var service = Service(relay))
            using (var form = new HomeVpnWizardForm(service, clipboard))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            {
                service.UseToken(token, null); Invoke(form, "ShowStep", 4); form.Show();
                int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                try
                {
                    var start = service.StartAsync(); Pump(() => start.IsCompleted, 8000); start.GetAwaiter().GetResult();
                    int pid = Pid(); string session = (string)Field(service, "sessionDirectory");
                    service.BeforeSessionIo = delegate(string stage) {
                        if (stage == "cleanup") { entered.Set(); if (!release.Wait(5000)) throw new TimeoutException("Wizard phone cleanup barrier timed out"); }
                    };
                    var stopButton = Controls(form).OfType<Button>().Single(b => b.Text == "Остановить VPN для телефона");
                    var watch = Stopwatch.StartNew(); stopButton.PerformClick();
                    check(watch.ElapsedMilliseconds < 500, "actual wizard phone Stop click returns while native cleanup remains owned: " + route);
                    Pump(() => entered.IsSet, 3000); int before = ticks; Pump(() => ticks >= before + 3, 1000);
                    var status = (Label)Field(form, "status");
                    check((bool)Field(form, "busy") && !((Control)Field(form, "body")).Enabled && !service.StopWork.IsCompleted
                        && Gone(pid) && Directory.Exists(session) && status.Text.Contains("Дождитесь"),
                        "actual phone Stop keeps responsive UI and honest pending cleanup without duplicate actions: " + route);
                    if (route == "close")
                    {
                        form.Close(); check(form.Visible && !form.IsDisposed && status.Text.Contains("подтверждения остановки"),
                            "phone Stop title close waits for local cleanup rather than claiming completion or cancelling it");
                    }
                    else if (route == "dispose") form.Dispose();
                    release.Set(); Pump(() => service.StopWork.IsCompleted && !(bool)Field(form, "busy"), 8000);
                    check(service.StopWork.GetAwaiter().GetResult() && !Directory.Exists(session) && !service.HasOwnedResources,
                        "wizard close or disposal cannot abandon its actual phone cleanup: " + route);
                    if (route != "stay") check(form.IsDisposed, "queued phone close and forced disposal settle without late UI access: " + route);
                    else
                    {
                        check(form.Visible && ((Control)Field(form, "body")).Enabled && status.Text.Contains("временные файлы очищены"),
                            "completed phone Stop restores explicit retry and reports success only after cleanup");
                        service.BeforeSessionIo = null;
                        start = service.StartAsync(); Pump(() => start.IsCompleted, 8000); start.GetAwaiter().GetResult();
                        session = (string)Field(service, "sessionDirectory");
                        using (var held = File.Open(Path.Combine(session, "ssh_config"), FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            stopButton.PerformClick(); Pump(() => !(bool)Field(form, "busy"), 5000);
                            check(form.Visible && status.ForeColor == UiTheme.Error && status.Text.Contains("остановлен не полностью")
                                && Directory.Exists(session) && ((Control)Field(form, "body")).Enabled,
                                "actual phone Stop shows locked-file failure and restores explicit retry without claiming cleanup");
                        }
                        stopButton.PerformClick(); Pump(() => !(bool)Field(form, "busy"), 5000);
                        check(!Directory.Exists(session) && status.Text.Contains("временные файлы очищены"),
                            "actual wizard retry cleans the same previously locked phone session");
                    }
                }
                finally
                {
                    release.Set(); service.BeforeSessionIo = null;
                    try { var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 8000); if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Wizard phone fixture cleanup incomplete"); }
                    finally { heartbeat.Stop(); form.Dispose(); }
                }
            }
        }

        private static void RelayOwnedLifecycle(bool dispose)
        {
            using (var relay = new Ikev2RelayService())
            using (var form = new Form())
            using (var pulse = new System.Windows.Forms.Timer { Interval = 20 })
            using (var probeEntered = new ManualResetEventSlim())
            using (var stopping = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            {
                var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
                TcpClient accepted = null;
                var peer = Task.Run(delegate {
                    using (var client = listener.AcceptTcpClient()) {
                        Interlocked.Exchange(ref accepted, client); client.ReceiveTimeout = 4000;
                        var stream = client.GetStream(); var hello = Read(stream, 3);
                        if (hello[0] != 5 || hello[1] != 1 || hello[2] != 0) throw new IOException("Unexpected owned probe greeting");
                        probeEntered.Set();
                        if (stream.ReadByte() != -1) throw new IOException("Cancelled probe sent unexpected data");
                    }
                });
                int socksPort = ((IPEndPoint)listener.LocalEndpoint).Port, ikePort = UdpPort(), natPort = UdpPort();
                while (natPort == ikePort) natPort = UdpPort();
                int ticks = 0; pulse.Tick += delegate { ticks++; }; form.Show(); pulse.Start();
                relay.BeforeStopComplete = delegate { stopping.Set(); if (!release.Wait(5000)) throw new TimeoutException("Relay stop finalization barrier"); };
                try
                {
                    var start = relay.StartAsync("127.0.0.1", socksPort, ikePort, natPort, 17878, CancellationToken.None);
                    check(Object.ReferenceEquals(start, relay.StartAsync("127.0.0.1", socksPort, ikePort, natPort, 17878, CancellationToken.None)),
                        "duplicate pending relay startup shares its actual native probe task");
                    Pump(() => probeEntered.IsSet && ticks >= 3, 3000);
                    var watch = Stopwatch.StartNew();
                    if (dispose) relay.Dispose(); else relay.StopAsync();
                    var stop = relay.StopWork;
                    check(watch.ElapsedMilliseconds < 500 && Object.ReferenceEquals(stop, relay.StopAsync()),
                        "relay Stop or Dispose reserves one lifetime without waiting on pending native I/O: " + dispose);
                    Pump(() => stopping.IsSet && peer.IsCompleted, 3000); peer.GetAwaiter().GetResult();
                    int before = ticks; Pump(() => ticks >= before + 3, 1000);
                    check(start.IsCanceled && !relay.IsRunning && !stop.IsCompleted && relay.LastError == null,
                        "real stalled SOCKS probe closes before relay stop reports completion and native UI stays responsive: " + dispose);
                    bool refused = false;
                    try { relay.StartAsync("127.0.0.1", socksPort, ikePort, natPort, 17878, CancellationToken.None).GetAwaiter().GetResult(); }
                    catch (ObjectDisposedException) { refused = dispose; }
                    catch (InvalidOperationException) { refused = true; }
                    check(refused && !stop.IsCompleted, "relay cannot admit a competing start while its stop owner is unfinished: " + dispose);
                    foreach (int port in new[] { ikePort, natPort }) using (var available = new UdpClient(new IPEndPoint(IPAddress.Loopback, port))) { }
                    release.Set(); Pump(() => stop.IsCompleted, 5000); stop.GetAwaiter().GetResult();
                    check(!stop.IsFaulted && relay.PendingStopCount == 0, "relay stop completes only after startup and retiring network tasks settle: " + dispose);
                }
                finally
                {
                    release.Set(); relay.BeforeStopComplete = null;
                    try { var cleanup = relay.StopAsync(); Pump(() => cleanup.IsCompleted, 5000); cleanup.GetAwaiter().GetResult(); }
                    finally {
                        pulse.Stop(); form.Close(); listener.Stop();
                        var client = Interlocked.CompareExchange(ref accepted, null, null); if (client != null) client.Close();
                        Pump(() => peer.IsCompleted, 5000); var observed = peer.Exception;
                    }
                }
            }
        }

        private static void ProtectedOperation(ClipboardService clipboard)
        {
            using (var relay = new Ikev2RelayService())
            using (var service = Service(relay))
            using (var form = new HomeVpnWizardForm(service, clipboard)) {
                var pending = new TaskCompletionSource<int>(); form.Show();
                var task = (Task)typeof(HomeVpnWizardForm).GetMethod("RunStep", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(form, new object[] { new Func<Task>(() => pending.Task) });
                form.Close();
                check(form.Visible && !((Button)Field(form, "cancelWait")).Visible && !((Button)Field(form, "cancelWait")).Enabled
                    && ((Label)Field(form, "status")).Text.Contains("не отменяет") && !task.IsCompleted,
                    "server-changing operation cannot be cancelled or declared rolled back by title close");
                pending.SetResult(0); Pump(() => task.IsCompleted);
                check(form.Visible && !(bool)Field(form, "busy"), "protected operation completion restores controls without a queued close"); form.Close();
            }
        }
    }
}
