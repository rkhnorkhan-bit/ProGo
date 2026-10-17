using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        // Public, synthetic test input only. Never accepted from app settings or a user.
        private const string ConsoleFixturePhrase = "progo-disposable-console-fixture";
        private static string ConsoleFixtureArgs(string mode, string marker)
        { return "--ssh-console-fixture " + mode + " " + SshConnection.Quote(marker); }
        private static bool ConsoleProcessGone(int pid)
        {
            try { using (var process = Process.GetProcessById(pid)) return process.HasExited; }
            catch (ArgumentException) { return true; }
        }
        private static void ConsoleWaitMarker(string marker)
        {
            if (!SpinWait.SpinUntil(() => {
                try { int pid; return File.Exists(marker) && Int32.TryParse(File.ReadAllText(marker), out pid) && pid > 0; }
                catch (IOException) { return false; }
            }, 5000)) throw new Exception("Owned console fixture did not initialize");
        }
        private static int SshConsoleFixture(string[] args)
        {
            try {
                if (args[1] == "hold") { File.WriteAllText(args[2], Process.GetCurrentProcess().Id.ToString()); Thread.Sleep(30000); return 0; }
                if (args[1] == "spawn" || args[1] == "exit-spawn") {
                    using (var child = Process.Start(new ProcessStartInfo(Application.ExecutablePath, ConsoleFixtureArgs("hold", args[2] + ".child")) {
                        UseShellExecute = false, CreateNoWindow = true })) {
                        File.WriteAllText(args[2], child.Id.ToString());
                        if (args[1] == "spawn") Thread.Sleep(30000);
                    }
                    return 0;
                }
                using (var input = ConsoleNative.Open("CONIN$")) using (var output = ConsoleNative.Open("CONOUT$")) {
                    uint mode, outMode;
                    if (!ConsoleNative.GetConsoleMode(input, out mode) || !ConsoleNative.GetConsoleMode(output, out outMode) ||
                        Console.IsInputRedirected || Console.IsOutputRedirected || Console.IsErrorRedirected) return 11;
                    if (Environment.GetEnvironmentVariable("SSH_ASKPASS") != null || Environment.GetEnvironmentVariable("DISPLAY") != null ||
                        Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null || Environment.GetEnvironmentVariable("SSH_ASKPASS_REQUIRE") != "never") return 12;
                    if (args[1] == "inspect") { File.WriteAllText(args[2], "OWN_CONSOLE_NO_REDIRECT_NO_ASKPASS"); return 0; }
                    if (!ConsoleNative.SetConsoleMode(input, mode & ~4U)) return 13;
                    Console.Write("Enter passphrase for fixture: ");
                    string value = Console.ReadLine();
                    File.WriteAllText(args[2], value == ConsoleFixturePhrase ? "INPUT_OK" : "INPUT_FAILED");
                    return value == ConsoleFixturePhrase ? 0 : 14;
                }
            } catch { return 19; }
        }
        // Only the test helper attaches to a test-owned console and injects fixed
        // dummy keystrokes. Production never attaches, reads or writes console IO.
        private static int SshConsoleFeed(string[] args)
        {
            try {
                int pid = Int32.Parse(args[1]);
                if (!ConsoleNative.AttachConsole((uint)pid)) return 21;
                try {
                    using (var input = ConsoleNative.Open("CONIN$")) using (var output = ConsoleNative.Open("CONOUT$")) {
                        string[] prompts = args[2] == "keygen" ? new[] { "Enter passphrase", "Enter same passphrase again" } : new[] { "Enter passphrase" };
                        foreach (string prompt in prompts) {
                            var watch = Stopwatch.StartNew();
                            while (true) {
                                uint mode; ConsoleNative.Screen info;
                                if (!ConsoleNative.GetConsoleMode(input, out mode) || !ConsoleNative.GetConsoleScreenBufferInfo(output, out info)) return 22;
                                var text = new StringBuilder(Math.Min(65536, info.Size.X * info.Size.Y)); uint read;
                                if (!ConsoleNative.ReadConsoleOutputCharacter(output, text, (uint)text.Capacity, new ConsoleNative.Coord(), out read)) return 23;
                                if ((mode & 4) == 0 && text.ToString().Contains(prompt)) break;
                                if (watch.ElapsedMilliseconds >= 8000) return 24;
                                Thread.Sleep(20);
                            }
                            foreach (char c in ConsoleFixturePhrase + "\r") {
                                var key = new ConsoleNative.Input { Type = 1, Down = 1, Repeat = 1, Character = c, VirtualKey = c == '\r' ? (ushort)13 : (ushort)0 };
                                uint written;
                                if (!ConsoleNative.WriteConsoleInput(input, new[] { key }, 1, out written) || written != 1) return 25;
                            }
                        }
                        File.WriteAllText(args[3], "ECHO_OFF_PROMPTS=" + prompts.Length); return 0;
                    }
                } finally { ConsoleNative.FreeConsole(); }
            } catch { return 29; }
        }
        private static int ConsoleRunFed(string executable, string arguments, string endpoint, string mode, string marker)
        {
            Task<DiagnosticProcessResult> feeder = null;
            int code;
            try {
                code = SshInteractiveProcess.Run(executable, arguments, endpoint, 15000, CancellationToken.None, pid => {
                    feeder = Task.Factory.StartNew(() => DiagnosticProcess.Run(Application.ExecutablePath,
                        "--ssh-console-feed " + pid + " " + mode + " " + SshConnection.Quote(marker), 12000, CancellationToken.None),
                        CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                });
            } finally {
                if (feeder != null) {
                    var result = feeder.GetAwaiter().GetResult();
                    if (result.ExitCode != 0 || result.Truncated || result.Output.Length != 0 || result.Error.Length != 0)
                        throw new Exception("Console input fixture failed; exit=" + result.ExitCode);
                }
            }
            return code;
        }
        private static SshAgentKeyAddRequest KeyAddRequest(string root)
        {
            var profile = KeyMatchProfile(root, "selected key");
            profile.PubkeyAuthentication = "yes"; profile.PreferredAuthentications = "publickey,password";
            SshAgentKeyAddState state; return SshAgentKeyAddRequest.Prepare(profile, "selected key", out state);
        }
        private static void SshInteractiveKeyAddWorkflow()
        {
            int before = passed; Console.WriteLine("SSH interactive key addition checks BEGIN");
            string root = Path.Combine(work, "interactive key " + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            try {
                string fake = Path.Combine(root, "fake"); Directory.CreateDirectory(fake); File.WriteAllText(Path.Combine(fake, "ssh-add.exe"), "");
                KeyAddBoundaries(fake); KeyAddLease(fake); InteractiveConsoleNative(root); InteractiveKeyNative(root);
            } finally { Directory.Delete(root, true); }
            Console.WriteLine("SSH interactive key addition checks PASS: " + (passed - before));
        }
        private static void KeyAddBoundaries(string root)
        {
            var profile = KeyMatchProfile(root, "selected key");
            SshAgentKeyAddState state;
            Check(SshAgentKeyAddRequest.Prepare(null, "selected key", out state) == null && state == SshAgentKeyAddState.NotChecked, "key addition refuses an unresolved profile");
            Check(SshAgentKeyAddRequest.Prepare(profile, "selected key", out state) == null && state == SshAgentKeyAddState.AuthenticationPolicyUnconfirmed, "unknown publickey policy cannot silently authorize a key mutation");
            profile.PubkeyAuthentication = "no"; profile.PreferredAuthentications = "publickey";
            Check(SshAgentKeyAddRequest.Prepare(profile, "selected key", out state) == null && state == SshAgentKeyAddState.NotUsedByAuthentication, "disabled publickey policy prevents key addition");
            profile.PubkeyAuthentication = "yes"; profile.PreferredAuthentications = "password";
            Check(SshAgentKeyAddRequest.Prepare(profile, "selected key", out state) == null && state == SshAgentKeyAddState.NotUsedByAuthentication, "excluded publickey method prevents key addition");
            profile.PreferredAuthentications = "publickey"; profile.IdentityAgent = "none";
            Check(SshAgentKeyAddRequest.Prepare(profile, "selected key", out state) == null && state == SshAgentKeyAddState.DisabledByProfile, "IdentityAgent none cannot mutate the default agent");
            profile.IdentityAgent = "custom-endpoint";
            Check(SshAgentKeyAddRequest.Prepare(profile, "selected key", out state) == null && state == SshAgentKeyAddState.UnsupportedEndpoint, "unverified custom IdentityAgent cannot be replaced with the default");
            profile.IdentityAgent = null;
            string selectedExecutable = profile.ExecutablePath; profile.ExecutablePath = Path.Combine(root, "missing", "ssh.exe");
            Check(SshAgentKeyAddRequest.Prepare(profile, "selected key", out state) == null && state == SshAgentKeyAddState.MissingExecutable, "missing sibling ssh-add never falls back to PATH");
            profile.ExecutablePath = selectedExecutable;
            Check(SshAgentKeyAddRequest.Prepare(profile, "unselected", out state) == null && state == SshAgentKeyAddState.IdentityNotSelected, "key addition requires an exact selected effective IdentityFile");
            foreach (string path in new[] { "relative", "", "bad\0path", "bad\rpath", "bad\npath", @"C:relative", @"\root-relative" }) {
                profile.IdentityPaths["selected key"] = path;
                Check(SshAgentKeyAddRequest.Prepare(profile, "selected key", out state) == null && state == SshAgentKeyAddState.PathUnresolved, "unconfirmed key path is rejected: case " + (String.IsNullOrEmpty(path) ? "empty" : path.Length.ToString()));
            }
            profile.IdentityPaths["selected key"] = Path.Combine(root, "selected key");
            string old = Environment.GetEnvironmentVariable("SSH_AUTH_SOCK");
            try {
                Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", "frozen-fixture-endpoint");
                var request = SshAgentKeyAddRequest.Prepare(profile, "selected key", out state);
                profile.IdentityPaths.Clear(); profile.IdentityFiles.Clear(); profile.ExecutablePath = "changed.exe";
                Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", "changed-fixture-endpoint");
                Check(request != null && state == SshAgentKeyAddState.Prepared && request.AgentEndpoint == "frozen-fixture-endpoint" &&
                    request.Executable == Path.Combine(root, "ssh-add.exe") && request.Arguments == "-k -- " + SshConnection.Quote(Path.Combine(root, "selected key")), "key addition freezes sibling executable, one quoted path and agent endpoint without a shell or certificate auto-add");
                foreach (int code in new[] { 0, 1, 2, 255 }) {
                    var result = SshAgentKeyAdd.Run(request, CancellationToken.None, (e, a, ep, ms, t) => code);
                    var expected = code == 0 ? SshAgentKeyAddState.CommandCompleted : code == 2 ? SshAgentKeyAddState.AgentUnavailable : SshAgentKeyAddState.CommandFailed;
                    Check(result.State == expected && result.ExitCode == code && !result.ToDisplay().Contains("fixture") &&
                        !result.ToDisplay().Contains("SHA256:"), "typed key addition exit result without raw paths or output: " + code);
                }
                Check(new SshAgentKeyAddResult(SshAgentKeyAddState.CommandCompleted).ToDisplay().Contains("вход на сервер ещё не проверены"), "successful ssh-add command is never declared authenticated");
                var native = SshAgentKeyAdd.Run(request, CancellationToken.None, (e, a, ep, ms, t) => { throw new Win32Exception(5, "PRIVATE_DIAGNOSTIC"); });
                Check(native.State == SshAgentKeyAddState.Failed && native.NativeError == 5 && !native.ToDisplay().Contains("PRIVATE_DIAGNOSTIC"), "native key addition error preserves numeric reason without exception text");
                Check(SshAgentKeyAdd.Run(request, CancellationToken.None, (e, a, ep, ms, t) => { throw new TimeoutException(); }).State == SshAgentKeyAddState.TimedOut, "interactive timeout has its own result");
                using (var cancelled = new CancellationTokenSource()) {
                    cancelled.Cancel(); int calls = 0;
                    var result = SshAgentKeyAdd.Run(request, cancelled.Token, (e, a, ep, ms, t) => { calls++; return 0; });
                    Check(result.State == SshAgentKeyAddState.Cancelled && calls == 0, "cancelled key addition cannot launch a child");
                }
                foreach (bool nativeFailure in new[] { true, false }) using (var cancelled = new CancellationTokenSource()) {
                    var result = SshAgentKeyAdd.Run(request, cancelled.Token, (e, a, ep, ms, t) => {
                        cancelled.Cancel(); if (nativeFailure) throw new Win32Exception(5); throw new IOException("fixture cleanup failure");
                    });
                    Check(result.State == SshAgentKeyAddState.Cancelled && (!nativeFailure || result.NativeError == 5) && !result.ToDisplay().Contains("окно OpenSSH закрыто"),
                        "cancellation wins over a late failure after runner cleanup: " + nativeFailure);
                }
                foreach (int budget in new[] { 0, 300001 }) {
                    bool rejected = false; try { SshAgentKeyAdd.RunAsync(request, CancellationToken.None, budget); } catch (ArgumentOutOfRangeException) { rejected = true; }
                    Check(rejected, "invalid interactive lifetime is rejected before scheduling: " + budget);
                }
            } finally { Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", old); }
        }
        private static void KeyAddLease(string root)
        {
            var request = KeyAddRequest(root); int calls = 0;
            using (var held = SshReadOnlyLease.TryAcquire(CancellationToken.None)) {
                Check(SshAgentKeyAdd.Run(request, CancellationToken.None, (e, a, ep, ms, t) => { calls++; return 0; }).State == SshAgentKeyAddState.Busy && calls == 0,
                    "read-only key owner prevents an interactive mutation from starting");
            }
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) using (var cancel = new CancellationTokenSource()) {
                var owner = Task.Factory.StartNew(() => SshAgentKeyAdd.Run(request, cancel.Token, (e, a, ep, ms, t) => { entered.Set(); release.Wait(); return 0; }),
                    CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                try {
                    Check(entered.Wait(3000), "interactive mutation enters its owned worker");
                    cancel.Cancel();
                    Check(!owner.IsCompleted && SshAgentInventory.Read("unused.exe", CancellationToken.None).State == SshAgentInventoryState.Busy &&
                        SshProfileKeyMatching.Check(KeyMatchProfile(root, "selected key"), CancellationToken.None).State == SshProfileKeyMatchState.Busy &&
                        SshAgentKeyAdd.Run(request, CancellationToken.None).State == SshAgentKeyAddState.Busy,
                        "cancelled addition retains the shared lease against inventory, fingerprints and duplicate addition until actual cleanup");
                } finally { release.Set(); if (!owner.Wait(5000)) throw new Exception("Interactive owner failed to settle"); }
                Check(owner.Result.State == SshAgentKeyAddState.Cancelled, "late command success cannot replace cancellation");
            }
            var async = SshAgentKeyAdd.RunAsync(request, new CancellationToken(true));
            Check(async.Wait(3000) && async.Result.State == SshAgentKeyAddState.Cancelled, "asynchronous key addition settles cancellation off the caller thread");
            Check(SshAgentInventory.Read("unused.exe", CancellationToken.None, (e, a, ms, t) => KeyMatchCapture(AgentFingerprintA)).State == SshAgentInventoryState.Available, "key operation lease is reusable after addition cleanup");
        }
        private static void InteractiveConsoleNative(string root)
        {
            string marker = Path.Combine(root, "console-state");
            string oldAsk = Environment.GetEnvironmentVariable("SSH_ASKPASS"), oldRequire = Environment.GetEnvironmentVariable("SSH_ASKPASS_REQUIRE"), oldDisplay = Environment.GetEnvironmentVariable("DISPLAY");
            try {
                Environment.SetEnvironmentVariable("SSH_ASKPASS", "untrusted-fixture-helper"); Environment.SetEnvironmentVariable("SSH_ASKPASS_REQUIRE", "force"); Environment.SetEnvironmentVariable("DISPLAY", "fixture");
                int code = SshInteractiveProcess.Run(Application.ExecutablePath, ConsoleFixtureArgs("inspect", marker), null, 5000, CancellationToken.None);
                Check(code == 0 && File.ReadAllText(marker) == "OWN_CONSOLE_NO_REDIRECT_NO_ASKPASS", "native child has a separate real console with all three standard handles and no inherited askpass");
                Check(Environment.GetEnvironmentVariable("SSH_ASKPASS_REQUIRE") == "force" && Environment.GetEnvironmentVariable("SSH_ASKPASS") == "untrusted-fixture-helper", "child environment sanitation does not change the parent environment");
                code = ConsoleRunFed(Application.ExecutablePath, ConsoleFixtureArgs("input", marker), null, "one", marker + ".feed");
                Check(code == 0 && File.ReadAllText(marker) == "INPUT_OK" && File.ReadAllText(marker + ".feed") == "ECHO_OFF_PROMPTS=1", "native console receives one dummy phrase with echo disabled without parent stdin or output capture");
            } finally {
                Environment.SetEnvironmentVariable("SSH_ASKPASS", oldAsk); Environment.SetEnvironmentVariable("SSH_ASKPASS_REQUIRE", oldRequire); Environment.SetEnvironmentVariable("DISPLAY", oldDisplay);
            }
            File.Delete(marker);
            int pid = 0; bool callbackFailed = false;
            try { SshInteractiveProcess.Run(Application.ExecutablePath, ConsoleFixtureArgs("hold", marker), null, 5000, CancellationToken.None, p => { pid = p; throw new IOException("fixture callback"); }); }
            catch (IOException) { callbackFailed = true; }
            Check(callbackFailed && pid > 0 && ConsoleProcessGone(pid), "owner callback failure cleans its own interactive child");
            File.Delete(marker);
            using (var cancel = new CancellationTokenSource()) {
                var owner = Task.Factory.StartNew(() => {
                    try { SshInteractiveProcess.Run(Application.ExecutablePath, ConsoleFixtureArgs("spawn", marker), null, 15000, cancel.Token); return false; }
                    catch (OperationCanceledException) { return true; }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                try { ConsoleWaitMarker(marker); cancel.Cancel(); Check(owner.Wait(5000) && owner.Result && ConsoleProcessGone(Int32.Parse(File.ReadAllText(marker))), "native cancellation settles the owned console and its descendant"); }
                finally { cancel.Cancel(); if (!owner.Wait(5000)) throw new Exception("Cancelled console did not settle"); }
            }
            File.Delete(marker);
            using (var foreign = Process.Start(new ProcessStartInfo(Application.ExecutablePath, ConsoleFixtureArgs("hold", marker + ".foreign")) { UseShellExecute = false, CreateNoWindow = true })) {
                try {
                    bool timeout = false;
                    try { SshInteractiveProcess.Run(Application.ExecutablePath, ConsoleFixtureArgs("spawn", marker), null, 3000, CancellationToken.None); } catch (TimeoutException) { timeout = true; }
                    Check(timeout && File.Exists(marker) && ConsoleProcessGone(Int32.Parse(File.ReadAllText(marker))) && !foreign.HasExited, "native deadline cleans only its own descendant and leaves a separate process alive");
                } finally { if (!foreign.HasExited) foreign.Kill(); if (!foreign.WaitForExit(5000)) throw new Exception("Foreign fixture cleanup failed"); }
            }
            File.Delete(marker);
            int exit = SshInteractiveProcess.Run(Application.ExecutablePath, ConsoleFixtureArgs("exit-spawn", marker), null, 5000, CancellationToken.None);
            Check(exit == 0 && ConsoleProcessGone(Int32.Parse(File.ReadAllText(marker))), "normal interactive parent exit also cleans an owned descendant");
        }
        private sealed class AgentAddPipe : IDisposable
        {
            internal readonly string Name = "progo-add-" + Guid.NewGuid().ToString("N");
            private readonly NamedPipeServerStream pipe;
            internal readonly Task Work;
            internal int Requests;
            internal AgentAddPipe()
            {
                pipe = new NamedPipeServerStream(Name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                Work = Task.Factory.StartNew(() => {
                    pipe.WaitForConnection(); byte[] header = Read(4);
                    int size = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
                    if (size < 1 || size > 65536) throw new IOException("Invalid disposable add request length");
                    byte[] body = Read(size);
                    try {
                        if (body.Length < 113 || body[0] != 17 || body[1] != 0 || body[2] != 0 || body[3] != 0 || body[4] != 11 ||
                            Encoding.ASCII.GetString(body, 5, 11) != "ssh-ed25519") throw new IOException("Fixture accepts only disposable Ed25519 ADD_IDENTITY");
                        Interlocked.Increment(ref Requests);
                        byte[] success = { 0, 0, 0, 1, 6 }; pipe.Write(success, 0, success.Length); pipe.Flush();
                    } finally { Array.Clear(body, 0, body.Length); }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            private byte[] Read(int count)
            {
                byte[] data = new byte[count]; int offset = 0;
                while (offset < count) { int n = pipe.Read(data, offset, count - offset); if (n == 0) throw new EndOfStreamException(); offset += n; }
                return data;
            }
            public void Dispose()
            {
                pipe.Dispose();
                try { if (!Work.Wait(5000)) throw new Exception("Disposable add endpoint did not settle"); }
                catch (AggregateException ex) { ex.Handle(e => e is IOException || e is ObjectDisposedException); }
            }
        }
        private static void InteractiveKeyNative(string root)
        {
            string ssh = OpenSshClient.Executable, keygen = OpenSshClient.KeygenExecutable(ssh), add = OpenSshClient.AgentExecutable(ssh);
            Check(!String.IsNullOrEmpty(keygen) && !String.IsNullOrEmpty(add), "interactive native oracle uses selected OpenSSH siblings");
            string key = Path.Combine(root, "encrypted disposable key"), feed = Path.Combine(root, "keygen.feed");
            int generated = ConsoleRunFed(keygen, "-q -t ed25519 -C disposable-progo-fixture -f " + SshConnection.Quote(key), null, "keygen", feed);
            Check(generated == 0 && File.Exists(key) && File.Exists(key + ".pub") && File.ReadAllText(feed) == "ECHO_OFF_PROMPTS=2", "real ssh-keygen creates a disposable encrypted key through two echo-off console entries without passphrase argv or environment");
            string old = Environment.GetEnvironmentVariable("SSH_AUTH_SOCK");
            try {
                using (var agent = new AgentAddPipe()) {
                    Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", @"\\.\pipe\" + agent.Name);
                    var profile = KeyMatchProfile(root, "disposable"); profile.ExecutablePath = ssh;
                    profile.PubkeyAuthentication = "yes"; profile.PreferredAuthentications = "publickey"; profile.IdentityPaths["disposable"] = key;
                    SshAgentKeyAddState state; var request = SshAgentKeyAddRequest.Prepare(profile, "disposable", out state);
                    var result = SshAgentKeyAdd.Run(request, CancellationToken.None, (e, a, ep, ms, t) => ConsoleRunFed(e, a, ep, "one", feed + ".add"));
                    Check(result.State == SshAgentKeyAddState.CommandCompleted && result.ExitCode == 0 && agent.Work.Wait(3000) && agent.Requests == 1 &&
                        File.ReadAllText(feed + ".add") == "ECHO_OFF_PROMPTS=1", "real ssh-add unlocks one encrypted selected key with one console entry and sends ADD_IDENTITY only to the isolated endpoint");
                    Check(!result.ToDisplay().Contains(ConsoleFixturePhrase) && !request.Arguments.Contains(ConsoleFixturePhrase), "actual key-add result and argv contain no disposable input or private material");
                }
            } finally { Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", old); }
        }
        private static class ConsoleNative
        {
            internal static SafeFileHandle Open(string name)
            {
                var handle = CreateFile(name, 0xC0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
                if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error()); return handle;
            }
            [StructLayout(LayoutKind.Sequential)] internal struct Coord { internal short X, Y; }
            [StructLayout(LayoutKind.Sequential)] internal struct Rect { internal short Left, Top, Right, Bottom; }
            [StructLayout(LayoutKind.Sequential)] internal struct Screen { internal Coord Size, Cursor; internal ushort Attributes; internal Rect Window; internal Coord Maximum; }
            [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode, Size = 20)] internal struct Input {
                [FieldOffset(0)] internal ushort Type; [FieldOffset(4)] internal int Down; [FieldOffset(8)] internal ushort Repeat;
                [FieldOffset(10)] internal ushort VirtualKey; [FieldOffset(12)] internal ushort Scan; [FieldOffset(14)] internal char Character; [FieldOffset(16)] internal uint Control;
            }
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
            [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool AttachConsole(uint pid);
            [DllImport("kernel32.dll")] internal static extern bool FreeConsole();
            [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool GetConsoleMode(SafeFileHandle handle, out uint mode);
            [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool SetConsoleMode(SafeFileHandle handle, uint mode);
            [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool GetConsoleScreenBufferInfo(SafeFileHandle handle, out Screen info);
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern bool ReadConsoleOutputCharacter(SafeFileHandle handle, StringBuilder text, uint count, Coord at, out uint read);
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern bool WriteConsoleInput(SafeFileHandle handle, Input[] records, uint count, out uint written);
        }
    }
}
