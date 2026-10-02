using System;
using System.Collections.Generic;
using System.IO;

namespace ProGo
{
    // Short transaction around a port change. Never replace the user's original backups.
    internal sealed class ProxyIntegrationState
    {
        private readonly Dictionary<string, string> environment = new Dictionary<string, string>();
        private readonly Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
        private SystemProxyBackup windows;
        private bool windowsOwned;

        internal static ProxyIntegrationState Capture()
        {
            var state = new ProxyIntegrationState();
            foreach (var name in CliProxyEnvironmentService.Names)
                state.environment[name] = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
            foreach (var path in new[] { CliProxyEnvironmentService.BackupPath, SystemProxyService.BackupPath, CodexProxyService.LauncherPath })
                state.files[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            state.windows = SystemProxyService.ReadCurrent();
            state.windowsOwned = SystemProxyService.IsOwned;
            return state;
        }

        internal void MoveOwned(int port)
        {
            CliProxyEnvironmentService.MoveOwned(port);
            if (windowsOwned)
            {
                // Preserve all registry flags, including a manual Windows proxy disable.
                windows.AppliedServer = "http=" + CliProxyBridgeService.Host + ":" + port + ";https=" + CliProxyBridgeService.Host + ":" + port;
                var target = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<SystemProxyBackup>(File.ReadAllText(SystemProxyService.BackupPath));
                target.AppliedServer = windows.AppliedServer;
                var current = SystemProxyService.ReadCurrent();
                current.ProxyServer = windows.AppliedServer; current.HadProxyServer = true;
                SystemProxyService.RestoreSnapshot(current);
                File.WriteAllText(SystemProxyService.BackupPath, new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(target));
            }
            CodexProxyService.MoveOwned(port);
        }

        internal void Restore()
        {
            var errors = new List<Exception>();
            foreach (var pair in environment)
                try { Environment.SetEnvironmentVariable(pair.Key, pair.Value, EnvironmentVariableTarget.User); } catch (Exception ex) { errors.Add(ex); }
            if (windowsOwned) try { SystemProxyService.RestoreSnapshot(windows); } catch (Exception ex) { errors.Add(ex); }
            foreach (var pair in files)
                try { if (pair.Value == null) { if (File.Exists(pair.Key)) File.Delete(pair.Key); } else File.WriteAllBytes(pair.Key, pair.Value); } catch (Exception ex) { errors.Add(ex); }
            CliProxyEnvironmentService.BroadcastEnvironmentChange();
            if (errors.Count != 0) throw new AggregateException(errors);
        }
    }
}
