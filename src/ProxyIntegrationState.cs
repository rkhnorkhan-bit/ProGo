using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace ProGo
{
    // Receipts describe our actual writes. Fresh comparisons preserve later edits;
    // this is deliberately not a cross-process registry/file transaction.
    internal sealed class ProxyIntegrationState
    {
        private sealed class Change { internal Action Undo; internal bool Settled, Journal; }
        private readonly List<Change> changes = new List<Change>();
        private bool environmentChanged, windowsChanged;
        private int targetPort;
        private const string EnvironmentKey = "Environment";
        private const string WindowsKey = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
        private const int MaximumFileBytes = 512 * 1024;
        internal static ProxyIntegrationState Capture() { return new ProxyIntegrationState(); }
        internal static string ReadPortRecoveryMessage(int configuredPort, CancellationToken token)
        {
            // A process restart loses prepared listeners and in-memory receipts.
            // Existing ownership journals remain authoritative for explicit Off;
            // do not silently start a different port and report recovery success.
            var ports = new List<int>(); var json = new JavaScriptSerializer();
            var environmentBytes = ReadBytes(CliProxyEnvironmentService.BackupPath, token);
            if (environmentBytes != null) {
                var saved = json.Deserialize<Dictionary<string, string>>(Decode(environmentBytes));
                string text; int port = 1881;
                if (saved == null || (saved.TryGetValue("ProGoAppliedPort", out text) &&
                    (!Int32.TryParse(text, out port) || port < 1 || port > 65535)))
                    throw new IOException("Некорректная копия восстановления терминалов.");
                if (port != configuredPort) ports.Add(port);
            }
            token.ThrowIfCancellationRequested();
            var windowsBytes = ReadBytes(SystemProxyService.BackupPath, token);
            if (windowsBytes != null) {
                var saved = json.Deserialize<SystemProxyBackup>(Decode(windowsBytes));
                if (saved == null) throw new IOException("Некорректная копия восстановления Windows.");
                string server = saved.AppliedServer;
                if (saved.OwnedFields != null) {
                    var field = saved.OwnedFields.Find(value => value != null && value.Name == "ProxyServer" && value.Pending);
                    server = field == null || field.Applied == null || !field.Applied.Exists ? null : json.Deserialize<string>(field.Applied.Data);
                }
                if (!String.IsNullOrEmpty(server)) {
                    const string prefix = "http=127.0.0.1:"; int port;
                    int delimiter = server.IndexOf(';');
                    if (!server.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || delimiter <= prefix.Length ||
                        !Int32.TryParse(server.Substring(prefix.Length, delimiter - prefix.Length), out port) || port < 1 || port > 65535 ||
                        !String.Equals(server, prefix + port + ";https=127.0.0.1:" + port, StringComparison.OrdinalIgnoreCase))
                        throw new IOException("Некорректный собственный порт в копии восстановления Windows.");
                    if (port != configuredPort && !ports.Contains(port)) ports.Add(port);
                }
            }
            if (ports.Count == 0) return null;
            return "Обнаружены настройки прежнего переноса HTTP-прокси: в копиях восстановления порт " +
                String.Join(", ", Array.ConvertAll(ports.ToArray(), value => value.ToString(System.Globalization.CultureInfo.InvariantCulture))) +
                ", в ProGo — " + configuredPort.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                ". Нажмите «Отключить прокси на ПК» для проверки и очистки собственных настроек, затем повторите подключение. Копии восстановления сохранены.";
        }
        internal void MoveOwned(int port) { MoveOwned(port, CancellationToken.None); }
        internal void MoveOwned(int port, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            targetPort = port;
            var json = new JavaScriptSerializer();
            byte[] environmentBytes = ReadBytes(CliProxyEnvironmentService.BackupPath, token);
            if (environmentBytes != null) {
                var saved = json.Deserialize<Dictionary<string, string>>(Decode(environmentBytes));
                if (saved == null) throw new IOException("Некорректная копия настроек терминалов.");
                var corrections = CliProxyEnvironmentService.ReadTypedCorrections(saved);
                if (corrections.Count != 0) {
                    SystemProxyService.RetryTypedValues(corrections, SystemProxyService.WriteValue);
                    saved.Remove("ProGoPendingWindowsCorrections");
                    var settled = Encoding.UTF8.GetBytes(json.Serialize(saved));
                    ChangeFile(CliProxyEnvironmentService.BackupPath, environmentBytes, settled, true, token);
                    environmentBytes = settled;
                }
                string text; int before = 1881;
                if (saved.TryGetValue("ProGoAppliedPort", out text) && (!Int32.TryParse(text, out before) || before < 1 || before > 65535))
                    throw new IOException("Некорректный сохранённый порт терминалов.");
                var native = new List<Action>();
                foreach (var name in CliProxyEnvironmentService.Names) {
                    string expected = name == "NO_PROXY" ? "localhost,127.0.0.1,::1" : CliProxyBridgeService.UrlFor(before);
                    string next = name == "NO_PROXY" ? expected : CliProxyBridgeService.UrlFor(port);
                    var current = ReadRegistry(EnvironmentKey, name);
                    if (current.Exists && (current.Kind == RegistryValueKind.String || current.Kind == RegistryValueKind.ExpandString) &&
                        String.Equals(json.Deserialize<string>(current.Data), expected, StringComparison.OrdinalIgnoreCase) && expected != next) {
                        var field = name; var original = current; var applied = WindowsProxyValue.From(next, current.Kind);
                        native.Add(delegate { ChangeRegistry(EnvironmentKey, field, original, applied); environmentChanged = true; });
                    }
                }
                if (native.Count != 0) {
                    saved["ProGoAppliedPort"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    ChangeFile(CliProxyEnvironmentService.BackupPath, environmentBytes, Encoding.UTF8.GetBytes(json.Serialize(saved)), true, token);
                    foreach (var write in native) { token.ThrowIfCancellationRequested(); write(); }
                    Notify(CliProxyEnvironmentService.BroadcastEnvironmentChange);
                }
            }
            token.ThrowIfCancellationRequested();
            byte[] windowsBytes = ReadBytes(SystemProxyService.BackupPath, token);
            if (windowsBytes != null) {
                var backup = json.Deserialize<SystemProxyBackup>(Decode(windowsBytes));
                if (backup == null) throw new IOException("Некорректная копия настроек Windows.");
                var current = ReadRegistry(WindowsKey, "ProxyServer"); WindowsProxyValue owned = null;
                if (backup.OwnedFields != null) {
                    var server = backup.OwnedFields.Find(f => f != null && f.Name == "ProxyServer");
                    if (server != null && server.Pending) owned = server.Applied;
                } else if (!String.IsNullOrEmpty(backup.AppliedServer)) owned = WindowsProxyValue.From(backup.AppliedServer, RegistryValueKind.String);
                if (owned != null && current.Matches(owned)) {
                    string server = "http=" + CliProxyBridgeService.Host + ":" + port + ";https=" + CliProxyBridgeService.Host + ":" + port;
                    var applied = WindowsProxyValue.From(server, RegistryValueKind.String);
                    if (!current.Matches(applied)) {
                        SystemProxyService.UpdateOwnedServer(backup, server);
                        var document = json.Deserialize<Dictionary<string, object>>(Decode(windowsBytes));
                        document["AppliedServer"] = server;
                        var field = ServerField(document);
                        if (field != null) { field["Applied"] = json.DeserializeObject(json.Serialize(applied)); field["Pending"] = true; }
                        ChangeFile(SystemProxyService.BackupPath, windowsBytes, Encoding.UTF8.GetBytes(json.Serialize(document)), true, token);
                        token.ThrowIfCancellationRequested(); ChangeRegistry(WindowsKey, "ProxyServer", current, applied); windowsChanged = true;
                        Notify(SystemProxyService.RefreshSystemProxy);
                    }
                }
            }
            token.ThrowIfCancellationRequested();
            byte[] launcher = ReadBytes(CodexProxyService.LauncherPath, token);
            if (launcher != null && Decode(launcher).Contains("rem ProGo scoped Codex launcher v1"))
                ChangeFile(CodexProxyService.LauncherPath, launcher, Encoding.ASCII.GetBytes(CodexProxyService.LauncherContent(port)), false, token);
        }
        private void ChangeRegistry(string keyPath, string name, WindowsProxyValue before, WindowsProxyValue after)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(keyPath)) {
                if (!SystemProxyService.ReadValue(key, name).Matches(before)) return;
                var receipt = new Change();
                receipt.Undo = delegate {
                    using (var live = Registry.CurrentUser.CreateSubKey(keyPath)) {
                        if (SystemProxyService.ReadValue(live, name).Matches(after)) SystemProxyService.WriteValue(live, name, before);
                    }
                };
                changes.Add(receipt); // SetValue may throw after its native write.
                SystemProxyService.WriteValue(key, name, after);
            }
        }
        private void ChangeFile(string path, byte[] before, byte[] after, bool journal, CancellationToken token)
        {
            if (Equal(before, after)) return;
            token.ThrowIfCancellationRequested();
            if (!Equal(ReadBytes(path, token), before)) throw new IOException("Настройки приложений изменились вне ProGo. Проверьте их и повторите сохранение.");
            var receipt = new Change { Journal = journal };
            receipt.Undo = delegate {
                var current = ReadBytes(path, CancellationToken.None);
                if (Equal(current, after)) { ReplaceBytes(path, before); return; }
                if (Equal(current, before)) return;
                if (journal && before != null && after != null && current != null) {
                    var recovered = RecoverJournal(path, before, after, current);
                    if (!Equal(ReadBytes(path, CancellationToken.None), current)) throw new IOException("Копия восстановления изменилась во время проверки.");
                    ReplaceBytes(path, recovered); return;
                }
                if (journal && after != null) throw new IOException("Копия восстановления изменена вне ProGo. Она сохранена; проверьте её перед повторной очисткой.");
            };
            changes.Add(receipt); ReplaceBytes(path, after);
        }
        internal void Restore()
        {
            var errors = new List<Exception>();
            // Keep the new-endpoint journals while a native/file undo is incomplete.
            for (int i = changes.Count - 1; i >= 0; i--) {
                var change = changes[i]; if (change.Settled || change.Journal) continue;
                try { change.Undo(); change.Settled = true; } catch (Exception ex) { errors.Add(ex); }
            }
            try {
                if (errors.Count == 0 && environmentChanged) Notify(CliProxyEnvironmentService.BroadcastEnvironmentChange);
                if (errors.Count == 0 && windowsChanged) Notify(SystemProxyService.RefreshSystemProxy);
            } catch (Exception ex) { errors.Add(ex); }
            if (errors.Count == 0) for (int i = changes.Count - 1; i >= 0; i--) {
                var change = changes[i]; if (change.Settled || !change.Journal) continue;
                try { change.Undo(); change.Settled = true; } catch (Exception ex) { errors.Add(ex); }
            }
            if (errors.Count != 0) throw new AggregateException(errors);
        }
        private void Notify(Action notification)
        {
            var errors = new List<Exception>();
            byte[] prior = null, prepared = null;
            SystemProxyService.PreserveTypedValues(notification, SystemProxyService.WriteValue, delegate(WindowsProxyFieldBackup correction, Exception failure) {
                var name = correction.Name; var before = correction.Original; var after = correction.Applied;
                changes.Add(new Change { Undo = delegate {
                    using (var key = Registry.CurrentUser.CreateSubKey(WindowsKey))
                        if (SystemProxyService.ReadValue(key, name).Matches(after)) SystemProxyService.WriteValue(key, name, before);
                } });
                errors.Add(failure);
            }, delegate(Dictionary<string, WindowsProxyValue> before) {
                var guards = CliProxyEnvironmentService.PrepareTypedCorrections(before);
                if (guards.Count == 0) return;
                var json = new JavaScriptSerializer(); prior = ReadBytes(CliProxyEnvironmentService.BackupPath, CancellationToken.None);
                var saved = prior == null ? new Dictionary<string, string>() : json.Deserialize<Dictionary<string, string>>(Decode(prior));
                if (prior == null) {
                    foreach (var name in CliProxyEnvironmentService.Names) saved[name] = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
                    saved["ProGoAppliedPort"] = targetPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                saved["ProGoPendingWindowsCorrections"] = json.Serialize(guards);
                prepared = Encoding.UTF8.GetBytes(json.Serialize(saved));
                // This exact same live snapshot supplies both the durable guard
                // and the following native correction, as in F31m.
                ChangeFile(CliProxyEnvironmentService.BackupPath, prior, prepared, true, CancellationToken.None);
            });
            if (prepared != null && errors.Count == 0)
                ChangeFile(CliProxyEnvironmentService.BackupPath, prepared, prior, true, CancellationToken.None);
            if (errors.Count != 0) throw new AggregateException(errors);
        }
        private static Dictionary<string, object> ServerField(Dictionary<string, object> document)
        {
            object rows; if (!document.TryGetValue("OwnedFields", out rows) || rows == null) return null;
            Dictionary<string, object> found = null;
            foreach (object raw in (System.Collections.IEnumerable)rows) {
                var field = raw as Dictionary<string, object>; object name;
                if (field != null && field.TryGetValue("Name", out name) && (string)name == "ProxyServer") {
                    if (found != null) throw new IOException("Некорректная копия восстановления Windows."); found = field;
                }
            }
            return found;
        }
        private static byte[] RecoverJournal(string path, byte[] beforeBytes, byte[] afterBytes, byte[] currentBytes)
        {
            var json = new JavaScriptSerializer();
            var before = json.Deserialize<Dictionary<string, object>>(Decode(beforeBytes));
            var after = json.Deserialize<Dictionary<string, object>>(Decode(afterBytes));
            var current = json.Deserialize<Dictionary<string, object>>(Decode(currentBytes));
            if (path == CliProxyEnvironmentService.BackupPath) {
                foreach (var key in CliProxyEnvironmentService.Names) RequireEqual(json, current, after, key);
                RequireEqual(json, current, after, "ProGoAppliedPort");
                RequireEqual(json, current, after, "ProGoPendingWindowsCorrections");
                CopyField(before, current, "ProGoAppliedPort"); CopyField(before, current, "ProGoPendingWindowsCorrections");
            } else {
                foreach (var property in typeof(SystemProxyBackup).GetProperties())
                    if (property.Name != "OwnedFields" && property.Name != "AppliedServer") RequireEqual(json, current, after, property.Name);
                RequireEqual(json, current, after, "AppliedServer");
                var live = ServerField(current); var applied = ServerField(after); var original = ServerField(before);
                if ((live == null) != (applied == null) || (original == null) != (applied == null)) throw new IOException("Структура копии Windows изменена вне ProGo.");
                if (live != null) {
                    foreach (var key in new[] { "Name", "Original", "Applied", "Pending" }) RequireEqual(json, live, applied, key);
                    CopyField(original, live, "Applied"); CopyField(original, live, "Pending");
                }
                CopyField(before, current, "AppliedServer");
            }
            // Extra external metadata remains in the current document. Only our
            // own still-matching port/guard markers are restored.
            return Encoding.UTF8.GetBytes(json.Serialize(current));
        }
        private static void RequireEqual(JavaScriptSerializer json, Dictionary<string, object> live, Dictionary<string, object> own, string key)
        {
            object a, b; bool hasA = live.TryGetValue(key, out a), hasB = own.TryGetValue(key, out b);
            if (hasA != hasB || (hasA && json.Serialize(a) != json.Serialize(b))) throw new IOException("Содержимое копии восстановления изменено вне ProGo.");
        }
        private static void CopyField(Dictionary<string, object> source, Dictionary<string, object> target, string key)
        { object value; if (source.TryGetValue(key, out value)) target[key] = value; else target.Remove(key); }
        private static WindowsProxyValue ReadRegistry(string path, string name)
        { using (var key = Registry.CurrentUser.OpenSubKey(path)) return SystemProxyService.ReadValue(key, name); }
        private static string Decode(byte[] bytes) { return Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF'); }
        private static byte[] ReadBytes(string path, CancellationToken token)
        {
            try {
                using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    if (input.Length > MaximumFileBytes) throw new IOException("Файл настроек приложений слишком большой.");
                    using (var output = new MemoryStream()) {
                        var buffer = new byte[8192]; int read;
                        while (true) { token.ThrowIfCancellationRequested(); read = input.Read(buffer, 0, buffer.Length); if (read == 0) break; output.Write(buffer, 0, read);
                            if (output.Length > MaximumFileBytes) throw new IOException("Файл настроек приложений изменился при чтении."); }
                        return output.ToArray();
                    }
                }
            } catch (FileNotFoundException) { return null; } catch (DirectoryNotFoundException) { return null; }
        }
        private static bool Equal(byte[] a, byte[] b)
        {
            if (a == null || b == null) return a == b;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
        private static void ReplaceBytes(string path, byte[] bytes)
        {
            if (bytes == null) { if (File.Exists(path)) File.Delete(path); return; }
            string pending = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllBytes(pending, bytes); if (File.Exists(path)) File.Replace(pending, path, null); else File.Move(pending, path); }
            finally { if (File.Exists(pending)) File.Delete(pending); }
        }
    }
}
