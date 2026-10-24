using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void StartupMigrationWorkflow(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") return;
            var original = settings.Current.Clone();
            try {
                var configured = AppSettings.Defaults(); configured.SshProfile = "fixture";
                configured.AutoStartSocks = configured.AutoCliProxy = configured.AutoSystemProxy = false;
                configured.TrayCloseExplained = true; settings.Save(configured);
                StartupMigrationNativeWorker(settings);
                StartupMigrationShutdown(settings);
                StartupMigrationQueuedShutdown(settings);
                StartupMigrationOwnerClosed(settings, false);
                StartupMigrationOwnerClosed(settings, true);
                StartupMigrationPreservesLinks(settings);
                StartupMigrationPortableGuard(settings);
                StartupMigrationInvalidPath(settings);
            } finally { settings.Save(original); }
        }

        // The blocking hook never replaces the implementation: released workers
        // still create/read real ShellLink files in disposable Programs/Startup.
        private sealed class StartupMigrationFixture : IDisposable
        {
            internal readonly string Root, Install, Programs, Startup, Executable, Primary, Legacy, StartupLink;
            internal readonly UpdateAwareTrayApplicationContext Context;
            private readonly ProxyService proxy;
            private readonly CliProxyBridgeService bridge;
            private readonly Ikev2RelayService relay;
            private readonly HomeVpnService home;
            private readonly ClipboardService clipboard;
            private readonly ConnectionHealthMonitor health;
            internal StartupMigrationFixture(SettingsService settings)
            {
                Root = Path.Combine(Path.GetTempPath(), "ProGo-menu-async-" + Guid.NewGuid().ToString("N"));
                Install = Path.Combine(Root, "app"); Programs = Path.Combine(Root, "Programs"); Startup = Path.Combine(Root, "Startup");
                Executable = Path.Combine(Install, "ProGo.exe"); Primary = Path.Combine(Programs, "ProGo", "ProGo.lnk");
                Legacy = Path.Combine(Programs, "ProGo", "ProGo Status.lnk"); StartupLink = Path.Combine(Startup, "ProGo.lnk");
                Directory.CreateDirectory(Install); File.WriteAllText(Executable, "opaque fixture executable; never launched");
                var shortcuts = new ApplicationShortcuts(Install, Programs, Startup);
                shortcuts.Install(false, false, false); File.Move(Primary, Legacy);
                proxy = new ProxyService(() => settings.Current, value => settings.Save(value), "unused-test-ssh", () => DateTime.UtcNow, false);
                bridge = new CliProxyBridgeService(settings); relay = new Ikev2RelayService(); home = new HomeVpnService(relay);
                clipboard = new ClipboardService(settings); health = new ConnectionHealthMonitor(() => settings.Current);
                Context = new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false, health,
                    windowsRestore: () => new WindowsProxyRestoreResult(), cliRestore: () => { },
                    settingsShutdownTimeoutMilliseconds: 1000, dailyRootProvider: () => Path.Combine(Root, "daily"));
            }
            internal Task Start(Action<CancellationToken> before = null, string currentExecutable = null)
            { return Context.StartShortcutMigrationAsync(currentExecutable ?? Executable, Install, Programs, Startup, before); }
            public void Dispose()
            {
                Context.Dispose();
                var pending = Context.ShortcutMigrationWork;
                if (pending != null && !pending.Wait(30000)) throw new Exception("startup migration worker did not settle after fixture disposal");
                bridge.Dispose(); clipboard.Dispose(); home.Dispose(); relay.Dispose(); health.Dispose(); proxy.Dispose();
                if (Directory.Exists(Root)) Directory.Delete(Root, true);
            }
        }

        private static void StartupMigrationNativeWorker(SettingsService settings)
        {
            int calls = 0, worker = 0, activations = 0, owner = Thread.CurrentThread.ManagedThreadId;
            bool attached = false; ApartmentState apartment = ApartmentState.Unknown;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var instance = new ApplicationInstance())
            using (var fixture = new StartupMigrationFixture(settings))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 15 }) {
                var context = fixture.Context;
                byte[] legacy = File.ReadAllBytes(fixture.Legacy), startup = File.ReadAllBytes(fixture.StartupLink), saved = File.ReadAllBytes(AppPaths.SettingsPath);
                Check(instance.IsOwner && calls == 0 && context.ShortcutMigrationWork.IsCompleted && Field(context, "mainWindow") == null,
                    "native owner construction does not read or migrate its Start menu before startup scheduling");
                instance.Attach(delegate { Interlocked.Increment(ref activations); context.RequestShowStatus(); },
                    context.RequestShutdown, context.CompleteShutdown, context.CancelShutdown);
                var watch = Stopwatch.StartNew();
                var operation = fixture.Start(token => {
                    attached = instance.IsOwner && Field(instance, "activate") != null && Field(instance, "prepareShutdown") != null;
                    worker = Thread.CurrentThread.ManagedThreadId; apartment = Thread.CurrentThread.GetApartmentState();
                    Interlocked.Increment(ref calls); entered.Set(); AsyncSettingsWait(release);
                });
                try {
                    Check(watch.ElapsedMilliseconds < 1000 && !operation.IsCompleted && calls == 0 && !entered.IsSet &&
                        Object.ReferenceEquals(operation, context.ShortcutMigrationWork) && Object.ReferenceEquals(operation, fixture.Start()),
                        "shortcut migration returns one full task before owner dispatch or native shortcut I/O");
                    int ticks = 0; pulse.Tick += delegate { ticks++; }; pulse.Start(); PumpUntil(() => entered.IsSet && ticks >= 3);
                    Check(attached && worker != owner && apartment == ApartmentState.MTA && calls == 1 && !operation.IsCompleted && !File.Exists(fixture.Primary) &&
                        legacy.SequenceEqual(File.ReadAllBytes(fixture.Legacy)),
                        "one attached-owner migration waits off UI while native heartbeat runs and legacy bytes remain intact");
                    var activation = Task.Run(delegate {
                        using (var client = new NamedPipeClientStream(".", "ProGo.Show." + WindowsIdentity.GetCurrent().User.Value, PipeDirection.InOut)) {
                            client.Connect(3000); client.WriteByte(1); return client.ReadByte() == 1;
                        }
                    });
                    PumpUntil(() => activation.IsCompleted && Field(context, "mainWindow") != null);
                    Check(activation.GetAwaiter().GetResult() && activations == 1 && ((MainWindow)Field(context, "mainWindow")).Visible &&
                        !operation.IsCompleted && legacy.SequenceEqual(File.ReadAllBytes(fixture.Legacy)),
                        "real instance IPC activates the native dashboard while Start menu migration remains blocked");
                    release.Set(); WaitAsyncSettings(operation);
                    var link = ReadMigrationLink(fixture.Primary);
                    Check(!operation.IsFaulted && !File.Exists(fixture.Legacy) && link[0] == fixture.Executable && link[1] == "--show" && link[2] == fixture.Install,
                        "background native ShellLink migration verifies a real visible launcher before deleting the old link");
                    Check(startup.SequenceEqual(File.ReadAllBytes(fixture.StartupLink)) && saved.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)),
                        "successful menu migration preserves exact login shortcut and durable connection settings bytes");
                    byte[] primary = File.ReadAllBytes(fixture.Primary); bool same = true;
                    for (int i = 0; i < 100; i++) same &= Object.ReferenceEquals(operation, fixture.Start());
                    Application.DoEvents();
                    Check(same && calls == 1 && primary.SequenceEqual(File.ReadAllBytes(fixture.Primary)),
                        "100 later startup calls retain the settled task without rerunning or rewriting native menu migration");
                } finally { release.Set(); WaitAsyncSettings(operation); }
            }
        }

        private static void StartupMigrationShutdown(SettingsService settings)
        {
            CancellationToken observed = CancellationToken.None;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new StartupMigrationFixture(settings))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 15 }) {
                var context = fixture.Context; byte[] legacy = File.ReadAllBytes(fixture.Legacy), startup = File.ReadAllBytes(fixture.StartupLink);
                var operation = fixture.Start(token => { observed = token; entered.Set(); AsyncSettingsWait(release); });
                try {
                    PumpUntil(() => entered.IsSet); int ticks = 0; pulse.Tick += delegate { ticks++; }; pulse.Start();
                    var watch = Stopwatch.StartNew(); var shutdown = context.RequestShutdownAsync();
                    Check(watch.ElapsedMilliseconds < 1000 && !shutdown.IsCompleted, "shutdown returns without waiting synchronously for a held native migration worker");
                    PumpUntil(() => shutdown.IsCompleted && ticks >= 3);
                    Check(!shutdown.Result && watch.ElapsedMilliseconds < 3500 && observed.IsCancellationRequested && !operation.IsCompleted &&
                        Object.ReferenceEquals(operation, context.ShortcutMigrationWork) && !File.Exists(fixture.Primary) && legacy.SequenceEqual(File.ReadAllBytes(fixture.Legacy)),
                        "bounded shutdown refuses handoff while the cancelled raw migration worker still owns pending file work");
                    release.Set(); WaitAsyncSettings(operation);
                    Check(!operation.IsFaulted && !File.Exists(fixture.Primary) && legacy.SequenceEqual(File.ReadAllBytes(fixture.Legacy)) &&
                        startup.SequenceEqual(File.ReadAllBytes(fixture.StartupLink)),
                        "released cancelled migration settles without late shortcut creation, deletion or login changes");
                    var retry = context.RequestShutdownAsync(); WaitAsyncSettings(retry);
                    Check(retry.Result && Object.ReferenceEquals(operation, fixture.Start()),
                        "explicit shutdown retry succeeds only after the real migration worker has settled");
                } finally { release.Set(); WaitAsyncSettings(operation); }
            }
        }

        private static void StartupMigrationOwnerClosed(SettingsService settings, bool queued)
        {
            int calls = 0; CancellationToken observed = CancellationToken.None;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new StartupMigrationFixture(settings)) {
                byte[] legacy = File.ReadAllBytes(fixture.Legacy), startup = File.ReadAllBytes(fixture.StartupLink);
                var operation = fixture.Start(token => { observed = token; Interlocked.Increment(ref calls); entered.Set(); AsyncSettingsWait(release); });
                try {
                    if (!queued) PumpUntil(() => entered.IsSet);
                    var watch = Stopwatch.StartNew(); fixture.Context.Dispose();
                    Check(watch.ElapsedMilliseconds < 1000 && (queued ? calls == 0 : observed.IsCancellationRequested && !operation.IsCompleted),
                        "owner disposal cancels " + (queued ? "queued" : "in-flight") + " migration without blocking or hiding a still-held worker");
                    release.Set(); WaitAsyncSettings(operation); Application.DoEvents();
                    Check(!operation.IsFaulted && !File.Exists(fixture.Primary) && legacy.SequenceEqual(File.ReadAllBytes(fixture.Legacy)) &&
                        startup.SequenceEqual(File.ReadAllBytes(fixture.StartupLink)) && (queued ? calls == 0 : calls == 1),
                        "disposed " + (queued ? "queued" : "in-flight") + " migration cannot perform late native shortcut writes");
                } finally { release.Set(); WaitAsyncSettings(operation); }
            }
        }

        private static void StartupMigrationQueuedShutdown(SettingsService settings)
        {
            using (var fixture = new StartupMigrationFixture(settings)) {
                int calls = 0; byte[] legacy = File.ReadAllBytes(fixture.Legacy), startup = File.ReadAllBytes(fixture.StartupLink);
                var operation = fixture.Start(token => Interlocked.Increment(ref calls));
                Check(!operation.IsCompleted && calls == 0, "queued shortcut migration reserves its full task before any owner message runs");
                var shutdown = fixture.Context.RequestShutdownAsync(); WaitAsyncSettings(shutdown); WaitAsyncSettings(operation);
                Check(shutdown.Result && calls == 0 && !operation.IsFaulted && Object.ReferenceEquals(operation, fixture.Start()) &&
                    !File.Exists(fixture.Primary) && legacy.SequenceEqual(File.ReadAllBytes(fixture.Legacy)) && startup.SequenceEqual(File.ReadAllBytes(fixture.StartupLink)),
                    "shutdown before first dispatch drains the queued reservation without starting native migration or changing shortcuts");
            }
        }

        private static void StartupMigrationPreservesLinks(SettingsService settings)
        {
            for (int variant = 0; variant < 3; variant++)
            using (var fixture = new StartupMigrationFixture(settings)) {
                if (variant == 0) File.Copy(fixture.StartupLink, fixture.Legacy, true); // Same executable, user-chosen arguments.
                else {
                    string other = Path.Combine(fixture.Root, "other-app"), programs = Path.Combine(fixture.Root, "OtherPrograms");
                    Directory.CreateDirectory(other); File.WriteAllText(Path.Combine(other, "ProGo.exe"), "foreign fixture; never launched");
                    new ApplicationShortcuts(other, programs, Path.Combine(fixture.Root, "OtherStartup")).Install(true, true, false);
                    File.Copy(Path.Combine(programs, "ProGo", "ProGo.lnk"), variant == 1 ? fixture.Legacy : fixture.Primary, true);
                }
                byte[] legacy = File.ReadAllBytes(fixture.Legacy), primary = File.Exists(fixture.Primary) ? File.ReadAllBytes(fixture.Primary) : null;
                var operation = fixture.Start(); WaitAsyncSettings(operation);
                Check(!operation.IsFaulted && legacy.SequenceEqual(File.ReadAllBytes(fixture.Legacy)) &&
                    (primary == null ? !File.Exists(fixture.Primary) : primary.SequenceEqual(File.ReadAllBytes(fixture.Primary))),
                    "native migration safely settles and preserves " + (variant == 0 ? "customized legacy arguments" : variant == 1 ? "foreign legacy target" : "foreign primary and usable legacy") + " byte-for-byte");
            }
        }

        private static void StartupMigrationPortableGuard(SettingsService settings)
        {
            using (var fixture = new StartupMigrationFixture(settings)) {
                int calls = 0; byte[] legacy = File.ReadAllBytes(fixture.Legacy), startup = File.ReadAllBytes(fixture.StartupLink);
                string portable = Path.Combine(fixture.Root, "portable", "ProGo.exe");
                Directory.CreateDirectory(Path.GetDirectoryName(portable)); File.WriteAllText(portable, "portable fixture; never launched");
                var operation = fixture.Start(token => Interlocked.Increment(ref calls), portable); WaitAsyncSettings(operation);
                Check(calls == 0 && !operation.IsFaulted && !File.Exists(fixture.Primary) && legacy.SequenceEqual(File.ReadAllBytes(fixture.Legacy)) &&
                    startup.SequenceEqual(File.ReadAllBytes(fixture.StartupLink)),
                    "portable executable guard skips native menu discovery and preserves installed menu and login bytes");
            }
        }

        private static void StartupMigrationInvalidPath(SettingsService settings)
        {
            using (var fixture = new StartupMigrationFixture(settings)) {
                int calls = 0; byte[] legacy = File.ReadAllBytes(fixture.Legacy), startup = File.ReadAllBytes(fixture.StartupLink);
                var operation = fixture.Start(token => Interlocked.Increment(ref calls), "invalid\0executable"); WaitAsyncSettings(operation);
                Check(calls == 0 && !operation.IsFaulted && Object.ReferenceEquals(operation, fixture.Start()) && !File.Exists(fixture.Primary) &&
                    legacy.SequenceEqual(File.ReadAllBytes(fixture.Legacy)) && startup.SequenceEqual(File.ReadAllBytes(fixture.StartupLink)),
                    "invalid executable path settles the one-shot background failure before native discovery and preserves shortcut bytes");
            }
        }

        // Independent native reader verifies the worker-created .lnk target,
        // arguments and working directory without invoking production Matches.
        private static string[] ReadMigrationLink(string path)
        {
            object native = new MigrationShellLink();
            try {
                ((System.Runtime.InteropServices.ComTypes.IPersistFile)native).Load(path, 0x40);
                var link = (IMigrationShellLink)native;
                var target = new StringBuilder(32768); var arguments = new StringBuilder(32768); var directory = new StringBuilder(32768);
                link.GetPath(target, target.Capacity, IntPtr.Zero, 4); link.GetArguments(arguments, arguments.Capacity);
                link.GetWorkingDirectory(directory, directory.Capacity);
                return new[] { target.ToString(), arguments.ToString(), directory.ToString() };
            } finally { Marshal.FinalReleaseComObject(native); }
        }
        [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
        private class MigrationShellLink { }
        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMigrationShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int length, IntPtr data, uint flags);
            void GetIDList(out IntPtr list); void SetIDList(IntPtr list);
            void GetDescription(IntPtr description, int length); void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string description);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder directory, int length);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string directory);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder arguments, int length);
        }
    }
}
