using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProGo
{
    internal sealed class AppProxyConsumerSnapshot
    {
        internal readonly bool Known, Windows, Cli, Codex;
        internal readonly string RecoveryMessage;
        internal AppProxyConsumerSnapshot(bool windows, bool cli, bool codex, string recoveryMessage = null)
        { Known = true; Windows = windows; Cli = cli; Codex = codex; RecoveryMessage = recoveryMessage; }
        private AppProxyConsumerSnapshot() { }
        internal static readonly AppProxyConsumerSnapshot Unknown = new AppProxyConsumerSnapshot();
        internal bool Empty { get { return Known && !Windows && !Cli && !Codex && RecoveryMessage == null; } }
    }

    // UI-thread owner of the shared listener. Only the single background observer
    // reads external integrations; getters never wait for its filesystem/registry I/O.
    internal sealed class AppProxyConsumers : IDisposable
    {
        private readonly CliProxyBridgeService bridge;
        private readonly SettingsService settings;
        private readonly List<Process> windows = new List<Process>();
        private readonly Func<AppSettings, int, AppProxyConsumerSnapshot> read;
        private readonly Func<DateTime> now;
        private bool observing, disposed, cliCleanupPending, windowsCleanupPending;
        private long generation, observedBridgeRevision = -1, workGeneration, workBridgeRevision;
        private int workPort;
        private DateTime nextObservation = DateTime.MinValue;
        private DateTime workStartedUtc;
        private AppProxyConsumerSnapshot snapshot;
        private Task<AppProxyConsumerSnapshot> observation;
        private int mutationLeases;
        internal bool MutationPending { get { return System.Threading.Volatile.Read(ref mutationLeases) != 0; } }
        internal IDisposable BeginMutation()
        {
            System.Threading.Interlocked.Increment(ref mutationLeases);
            observing = true; Invalidate();
            return new MutationLease(this);
        }
        private sealed class MutationLease : IDisposable
        {
            private AppProxyConsumers owner;
            internal MutationLease(AppProxyConsumers owner) { this.owner = owner; }
            public void Dispose()
            {
                var value = System.Threading.Interlocked.Exchange(ref owner, null);
                if (value != null) System.Threading.Interlocked.Decrement(ref value.mutationLeases);
            }
        }

        internal bool CliCleanupPending
        {
            get { return cliCleanupPending; }
            set { if (cliCleanupPending != value) { cliCleanupPending = value; Invalidate(); } }
        }
        internal bool WindowsCleanupPending
        {
            get { return windowsCleanupPending; }
            set { if (windowsCleanupPending != value) { windowsCleanupPending = value; Invalidate(); } }
        }
        internal bool ObservationPending { get { return observation != null || snapshot == null; } }
        internal AppProxyConsumers(CliProxyBridgeService bridge, SettingsService settings,
            Func<AppSettings, int, AppProxyConsumerSnapshot> read = null, Func<DateTime> now = null)
        {
            this.bridge = bridge; this.settings = settings;
            this.read = read ?? new Func<AppSettings, int, AppProxyConsumerSnapshot>(ReadConsumers);
            this.now = now ?? (() => DateTime.UtcNow);
        }
        internal void Invalidate()
        {
            generation++; snapshot = null; nextObservation = DateTime.MinValue;
            // An invalidated worker must finish before another starts. Slow I/O
            // must not consume a new thread on every timer tick.
        }
        internal void Observe() { observing = true; Invalidate(); }
        internal void TrackWindow(Process process)
        {
            if (process == null) throw new InvalidOperationException("Не удалось получить окно приложения с прокси.");
            observing = true; windows.Add(process); Invalidate();
        }
        private void PruneWindows()
        {
            bool changed = false;
            for (int i = windows.Count - 1; i >= 0; i--) {
                bool exited;
                try { exited = windows[i].HasExited; }
                catch (InvalidOperationException) { exited = true; }
                catch { continue; }
                if (exited) { windows[i].Dispose(); windows.RemoveAt(i); changed = true; }
            }
            if (changed) Invalidate();
        }
        internal int WindowCount { get { PruneWindows(); return windows.Count; } }
        internal string Summary
        {
            get {
                PruneWindows(); var names = new List<string>();
                var current = snapshot;
                if (MutationPending) names.Add("применяем настройки прокси…");
                if (bridge.CleanupPending) names.Add("перенос портов: требуется завершить очистку");
                if (WindowsCleanupPending || (current != null && current.Windows)) names.Add("Windows");
                if (CliCleanupPending || (current != null && current.Cli)) names.Add("терминалы и Codex");
                if (windows.Count != 0) names.Add("отдельные окна: " + windows.Count);
                if (current != null && current.Codex) names.Add("ярлык Codex");
                if (current != null && current.RecoveryMessage != null) names.Add(current.RecoveryMessage);
                if (current == null || !current.Known)
                    names.Add(current == null ? "проверяем потребителей прокси…" : "потребители прокси: нужна проверка");
                return String.Join(", ", names.ToArray());
            }
        }
        internal void ReleaseIfUnused()
        {
            if (disposed || MutationPending || bridge.CleanupPending) return;
            PruneWindows();
            long revision = bridge.ConsumerRevision;
            if (observedBridgeRevision != revision) { observedBridgeRevision = revision; Invalidate(); }
            if (!observing || !bridge.IsRunning) return;
            if (observation != null && observation.IsCompleted) {
                // Result is consumed only after completion, on this UI thread.
                // Workers never stop a listener or publish into disposed forms.
                var result = observation.GetAwaiter().GetResult(); observation = null;
                if (workGeneration == generation && workBridgeRevision == bridge.ConsumerRevision && workPort == bridge.Port) {
                    var observedAt = now(); var age = observedAt - workStartedUtc;
                    // External edits do not increment our generation. Slow I/O or
                    // delayed UI delivery must not turn old evidence into a stop.
                    if (age < TimeSpan.Zero || age > TimeSpan.FromSeconds(5)) result = AppProxyConsumerSnapshot.Unknown;
                    snapshot = result; nextObservation = observedAt.AddSeconds(10);
                    if (result.Empty && !CliCleanupPending && !WindowsCleanupPending && windows.Count == 0) {
                        bridge.Stop(); Invalidate(); return;
                    }
                }
            }
            if (observation != null || now() < nextObservation) return;
            workGeneration = generation; workBridgeRevision = bridge.ConsumerRevision; workPort = bridge.Port;
            workStartedUtc = now();
            var currentSettings = settings.Current.Clone(); int port = workPort; var probe = read;
            nextObservation = DateTime.MaxValue;
            observation = Task.Run(delegate {
                try { return probe(currentSettings, port) ?? AppProxyConsumerSnapshot.Unknown; }
                catch { return AppProxyConsumerSnapshot.Unknown; }
            });
        }
        private static bool Exists(string path)
        {
            // File.Exists hides access/I/O errors as false. Uncertain ownership
            // must retain the listener rather than appear to be an empty set.
            try { File.GetAttributes(path); return true; }
            catch (FileNotFoundException) { return false; }
            catch (DirectoryNotFoundException) { return false; }
        }
        private const int CodexLauncherByteLimit = 64 * 1024;
        // The observer deliberately does not call IsOwned: the fresh destructive
        // check reads the whole file, which may have been replaced externally.
        // Keep this signature compatible with CodexProxyService.LauncherContent.
        private const string CodexLauncherMarker = "rem ProGo scoped Codex launcher v1";
        internal static bool ReadCodexOwnership(string path)
        {
            if (!CodexPathExists(path)) return false;
            bool owned;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                owned = ReadCodexOwnership(stream);
            // Do not publish a file that disappeared or became a linked path
            // while its shared read was in progress.
            if (!CodexPathExists(path)) throw new IOException();
            return owned;
        }
        private static bool CodexPathExists(string path)
        {
            bool missing = false;
            // Reject junctions in parents as well as a linked launcher itself.
            // Continue past a missing child so a linked parent cannot look empty.
            for (var entry = Path.GetFullPath(path); !String.IsNullOrEmpty(entry); entry = Path.GetDirectoryName(entry)) {
                FileAttributes attributes;
                try { attributes = File.GetAttributes(entry); }
                catch (FileNotFoundException) { missing = true; continue; }
                catch (DirectoryNotFoundException) { missing = true; continue; }
                if ((attributes & FileAttributes.ReparsePoint) != 0) throw new IOException();
            }
            return !missing;
        }
        internal static bool ReadCodexOwnership(Stream stream)
        {
            long length = stream.Length;
            // Inspect size before allocating or reading even the marker. A large
            // replacement remains uncertain and never permits listener release.
            if (length < 0 || length > CodexLauncherByteLimit) throw new IOException();
            var bytes = new byte[(int)length]; int received = 0;
            while (received < bytes.Length) {
                int count = stream.Read(bytes, received, bytes.Length - received);
                if (count == 0) throw new IOException();
                received += count;
            }
            // Shared readers do not block external writers. A size change during
            // this bounded observation must not publish incomplete ownership.
            if (stream.Length != length) throw new IOException();
            using (var reader = new StreamReader(new MemoryStream(bytes, false), Encoding.UTF8, true))
                return reader.ReadToEnd().Contains(CodexLauncherMarker);
        }
        internal static AppProxyConsumerSnapshot ReadConsumers(AppSettings current, int port)
        {
            var windows = SystemProxyService.ReadCurrent();
            string expected = "http=" + CliProxyBridgeService.Host + ":" + port + ";https=" + CliProxyBridgeService.Host + ":" + port;
            bool windowsApplied = windows.HadProxyEnable && windows.ProxyEnable != 0 && windows.HadProxyServer &&
                String.Equals(windows.ProxyServer, expected, StringComparison.OrdinalIgnoreCase);
            bool windowsConsumer = windowsApplied || Exists(SystemProxyService.BackupPath);
            bool cliConsumer = CliProxyEnvironmentService.HasProxyEndpoint(port) || Exists(CliProxyEnvironmentService.BackupPath);
            // This is an observation only. Enable/Disable/MoveOwned retain their
            // own fresh ownership checks and never use this cached decision.
            bool codexConsumer = ReadCodexOwnership(CodexProxyService.LauncherPath);
            return new AppProxyConsumerSnapshot(windowsConsumer, cliConsumer, codexConsumer,
                ProxyIntegrationState.ReadPortRecoveryMessage(current.HttpProxyPort, CancellationToken.None));
        }
        internal void ForgetWindows()
        {
            foreach (var window in windows) window.Dispose();
            windows.Clear(); observing = false; Invalidate();
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; ForgetWindows();
            // A blocked native file read cannot be cancelled reliably. Do not
            // join it; it owns only its captured settings and produces no effects.
        }
    }
}
