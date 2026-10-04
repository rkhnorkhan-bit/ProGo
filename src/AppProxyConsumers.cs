using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ProGo
{
    // UI-thread owner of the shared listener. Process handles avoid PID reuse;
    // releasing a handle never closes or kills the user's terminal.
    internal sealed class AppProxyConsumers : IDisposable
    {
        private readonly CliProxyBridgeService bridge;
        private readonly SettingsService settings;
        private readonly List<Process> windows = new List<Process>();
        private bool observing;
        internal AppProxyConsumers(CliProxyBridgeService bridge, SettingsService settings)
        {
            this.bridge = bridge; this.settings = settings;
        }
        internal void Observe() { observing = true; }
        internal void TrackWindow(Process process)
        {
            if (process == null) throw new InvalidOperationException("Не удалось получить окно приложения с прокси.");
            observing = true; windows.Add(process);
        }
        private void PruneWindows()
        {
            for (int i = windows.Count - 1; i >= 0; i--) {
                bool exited;
                try { exited = windows[i].HasExited; }
                catch (InvalidOperationException) { exited = true; }
                catch { continue; } // Uncertain process state must not cut a live consumer.
                if (exited) { windows[i].Dispose(); windows.RemoveAt(i); }
            }
        }
        internal int WindowCount { get { PruneWindows(); return windows.Count; } }
        internal string Summary
        {
            get {
                PruneWindows(); var names = new List<string>();
                // A pending ownership journal also retains the listener after a failed cleanup.
                if (SystemProxyService.IsApplied(settings.Current) || File.Exists(SystemProxyService.BackupPath)) names.Add("Windows");
                if (CliProxyEnvironmentService.HasProxyEndpoint(bridge.Port) || File.Exists(CliProxyEnvironmentService.BackupPath)) names.Add("терминалы и Codex");
                if (windows.Count != 0) names.Add("отдельные окна: " + windows.Count);
                try { if (CodexProxyService.IsOwned) names.Add("ярлык Codex"); }
                catch { names.Add("ярлык Codex: нужна проверка"); }
                return String.Join(", ", names.ToArray());
            }
        }
        internal void ReleaseIfUnused()
        {
            if (observing && bridge.IsRunning && Summary.Length == 0) bridge.Stop();
        }
        internal void ForgetWindows()
        {
            foreach (var window in windows) window.Dispose();
            windows.Clear(); observing = false;
        }
        public void Dispose() { ForgetWindows(); }
    }
}
