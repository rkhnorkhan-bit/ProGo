using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows.Forms;

namespace ProGo
{
    internal enum ProxyFeature { Cli, Windows }
    internal enum AutomationResult { None, Applied, RetryScheduled, Paused }

    // Keep an enabled option pending until it is applied successfully. The timer
    // never retries a cancelled option or spins forever on a persistent failure.
    internal sealed class AutomationPlan
    {
        private sealed class PendingAction
        {
            internal int Failures;
            internal DateTime RetryAt;
            internal bool Paused, Applying;
        }
        private static readonly int[] RetrySeconds = { 5, 15, 45 };
        private readonly Dictionary<ProxyFeature, PendingAction> pending = new Dictionary<ProxyFeature, PendingAction>();
        private readonly Func<DateTime> now;
        internal AutomationPlan() : this(delegate { return DateTime.UtcNow; }) { }
        internal AutomationPlan(Func<DateTime> clock) { now = clock; }
        internal void Update(AppSettings before, AppSettings after)
        {
            foreach (ProxyFeature feature in Enum.GetValues(typeof(ProxyFeature)))
            {
                if (!Enabled(after, feature)) pending.Remove(feature);
                else if (before == null || !Enabled(before, feature)) pending[feature] = new PendingAction();
            }
        }
        internal AutomationResult TryApply(ProxyFeature feature, bool ready, Action<ProxyFeature> apply, out Exception error)
        {
            var attempt = BeginApply(feature, ready);
            if (attempt == null) { error = null; return AutomationResult.None; }
            Exception failure = null;
            try { apply(feature); } catch (Exception ex) { failure = ex; }
            return CompleteApply(feature, attempt, failure, out error);
        }
        // Both halves run on the UI owner; the native mutation between them may
        // await a worker without retaining a modal SynchronizationContext.
        internal object BeginApply(ProxyFeature feature, bool ready)
        {
            PendingAction state;
            if (!ready || !pending.TryGetValue(feature, out state) || state.Paused || state.Applying || now() < state.RetryAt)
                return null;
            state.Applying = true;
            return state;
        }
        internal AutomationResult CompleteApply(ProxyFeature feature, object attempt, Exception failure, out Exception error)
        {
            error = null;
            var state = (PendingAction)attempt;
            try
            {
                if (!IsCurrent(feature, state)) return AutomationResult.None;
                if (failure == null) { pending.Remove(feature); return AutomationResult.Applied; }
                error = failure;
                state.Failures++;
                if (PermanentFailure(failure) || state.Failures > RetrySeconds.Length)
                {
                    state.Paused = true;
                    return AutomationResult.Paused;
                }
                state.RetryAt = now().AddSeconds(RetrySeconds[state.Failures - 1]);
                return AutomationResult.RetryScheduled;
            }
            finally { state.Applying = false; }
        }
        private bool IsCurrent(ProxyFeature feature, PendingAction state)
        {
            PendingAction current;
            return pending.TryGetValue(feature, out current) && Object.ReferenceEquals(current, state);
        }
        private static bool PermanentFailure(Exception error)
        {
            for (var ex = error; ex != null; ex = ex.InnerException)
                if (ex is UnauthorizedAccessException || ex is System.Security.SecurityException || ex is ArgumentException) return true;
            return false;
        }
        internal int FailureCount(ProxyFeature feature)
        {
            PendingAction state;
            return pending.TryGetValue(feature, out state) ? state.Failures : 0;
        }
        internal string GetStatusText(bool ready)
        {
            foreach (var pair in pending)
                if (pair.Value.Paused) return "Автонастройка " + FeatureName(pair.Key) + ": ошибка. Проверьте настройки.";
            if (pending.Count == 0) return "";
            if (!ready) return "Автонастройка: ожидает подключения к серверу.";
            DateTime retryAt = DateTime.MaxValue;
            foreach (var state in pending.Values)
                if (state.Failures > 0 && state.RetryAt < retryAt) retryAt = state.RetryAt;
            if (retryAt == DateTime.MaxValue) return "Автонастройка: настройки ожидают применения.";
            return "Автонастройка: повтор через " + Math.Max(0, (int)Math.Ceiling((retryAt - now()).TotalSeconds)) + " с.";
        }
        internal static string FeatureName(ProxyFeature feature)
        {
            return feature == ProxyFeature.Cli ? "терминалов и Codex" : "Windows";
        }
        internal void Cancel(ProxyFeature feature) { pending.Remove(feature); }
        internal static bool Enabled(AppSettings s, ProxyFeature feature)
        {
            return feature == ProxyFeature.Cli ? s.AutoCliProxy : s.AutoSystemProxy;
        }
    }

    internal static class CodexProxyService
    {
        private const string Marker = "rem ProGo scoped Codex launcher v1";
        private const int MaxShortcutBytes = 1024 * 1024;
        private const string ShortcutReadFailure = "Не удалось прочитать ярлык Codex для проверки владения. Файлы сохранены без изменений.";
        internal static string LauncherPath { get { return Path.Combine(AppPaths.Root, "scripts", "Codex-ProGo.cmd"); } }
        private static string ShortcutPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "ProGo", "Codex через ProGo.lnk"); } }
        public static bool IsConfigured { get { return File.Exists(LauncherPath) && File.Exists(ShortcutPath); } }
        internal static string LauncherContent(int port)
        {
            var text = new StringBuilder("@echo off\r\n" + Marker + "\r\nsetlocal\r\n");
            foreach (var name in new[] { "HTTP_PROXY", "HTTPS_PROXY", "ALL_PROXY" }) text.AppendLine("set \"" + name + "=" + CliProxyBridgeService.UrlFor(port) + "\"");
            text.AppendLine("set \"NO_PROXY=localhost,127.0.0.1,::1\"");
            text.AppendLine("echo Codex via ProGo. Keep ProGo connected while using this window.");
            text.AppendLine("where codex >nul 2>nul");
            text.AppendLine("if errorlevel 1 (echo Codex CLI is not installed or not on PATH. & exit /b 1)");
            text.AppendLine("call codex %*");
            text.AppendLine("endlocal");
            return text.ToString();
        }
        public static void Enable(int port)
        { Enable(port, LauncherPath, ShortcutPath); }
        internal static void Enable(int port, string launcherPath, string shortcutPath, Action beforeMutation = null)
        {
            launcherPath = Path.GetFullPath(launcherPath); shortcutPath = Path.GetFullPath(shortcutPath);
            // Acquire both owners before changing either file. The shortcut is
            // kept read/share-read locked while the launcher is updated.
            using (var launcher = OpenOwnedLauncher(launcherPath, true))
            using (var existing = OpenOwnedShortcut(shortcutPath, launcherPath)) {
                if (beforeMutation != null) beforeMutation();
                Directory.CreateDirectory(Path.GetDirectoryName(launcherPath));
                Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath));
                string temporary = Path.Combine(Path.GetDirectoryName(shortcutPath), "progo-codex-" + Guid.NewGuid().ToString("N") + ".lnk");
                string temporaryLauncher = Path.Combine(Path.GetDirectoryName(launcherPath), "progo-codex-" + Guid.NewGuid().ToString("N") + ".tmp");
                CodexFile created = null;
                try {
                    if (existing == null) {
                        WriteShortcut(temporary, launcherPath);
                        // Move never replaces a concurrently created shortcut.
                        File.Move(temporary, shortcutPath);
                        created = OpenOwnedShortcut(shortcutPath, launcherPath);
                        if (created == null) throw new IOException("Новый ярлык Codex исчез до подтверждения владения. Файл запуска не изменён.");
                    }
                    byte[] content = Encoding.ASCII.GetBytes(LauncherContent(port));
                    if (launcher == null) {
                        // A launcher which appeared after the read is preserved.
                        using (var output = new FileStream(temporaryLauncher, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) {
                            output.Write(content, 0, content.Length); output.Flush(true);
                        }
                        File.Move(temporaryLauncher, launcherPath);
                    } else launcher.Write(content);
                } catch {
                    // Only the exact link created and still held by this call
                    // may be removed. A concurrent replacement is never deleted.
                    if (created != null) created.Delete();
                    throw;
                } finally {
                    if (created != null) created.Dispose();
                    if (File.Exists(temporary)) File.Delete(temporary);
                    if (File.Exists(temporaryLauncher)) File.Delete(temporaryLauncher);
                }
            }
        }
        private static string ShortcutArguments(string launcherPath)
        { return "/D /K \"\"" + launcherPath + "\"\""; }
        private static void WriteShortcut(string path, string launcherPath)
        {
            object shortcut = null;
            try
            {
                shortcut = new ShellLink();
                var link = (IShellLinkW)shortcut;
                link.SetPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"));
                link.SetArguments(ShortcutArguments(launcherPath));
                link.SetWorkingDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                link.SetIconLocation(Application.ExecutablePath, 0);
                link.SetDescription("Codex CLI с прокси ProGo только для этого окна");
                ((IPersistFile)shortcut).Save(path, true);
            }
            finally
            {
                if (shortcut != null) Marshal.FinalReleaseComObject(shortcut);
            }
        }
        private static CodexFile OpenOwnedLauncher(string path, bool writable)
        {
            var file = CodexFile.Open(path, writable);
            if (file == null) return null;
            try {
                if (!file.ReadText().Contains(Marker)) {
                    if (!writable) { file.Dispose(); return null; }
                    throw new IOException("Файл запуска Codex уже существует и создан не ProGo. Файлы сохранены без изменений.");
                }
                return file;
            } catch { file.Dispose(); throw; }
        }
        private static CodexFile OpenOwnedShortcut(string path, string launcherPath)
        {
            var file = CodexFile.Open(path, false);
            if (file == null) return null;
            object native = null;
            IStream memory = null;
            try {
                try {
                    // ShellLink's file opener does not share DELETE with our
                    // pinned owner. Parse bytes from that same held stream;
                    // never reopen the pathname or release its protection.
                    byte[] snapshot = file.ReadSnapshot(MaxShortcutBytes);
                    memory = SHCreateMemStream(snapshot, (uint)snapshot.Length);
                    if (memory == null) throw new IOException(ShortcutReadFailure);
                    memory.Seek(0, 0, IntPtr.Zero);
                    native = new ShellLink();
                    ((IPersistStream)native).Load(memory); // stream load; never Resolve
                    var link = (IShellLinkW)native;
                    var target = new StringBuilder(32768); var arguments = new StringBuilder(32768);
                    link.GetPath(target, target.Capacity, IntPtr.Zero, 4); // raw path, no external lookup
                    link.GetArguments(arguments, arguments.Capacity);
                    string expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
                    if (target.Length == 0 || !String.Equals(Path.GetFullPath(target.ToString()), expected, StringComparison.OrdinalIgnoreCase) ||
                        !String.Equals(arguments.ToString(), ShortcutArguments(launcherPath), StringComparison.Ordinal))
                        throw new IOException("Ярлык Codex изменён или создан не ProGo. Файлы сохранены без изменений.");
                } finally {
                    try { if (native != null) Marshal.FinalReleaseComObject(native); }
                    finally { if (memory != null) Marshal.FinalReleaseComObject(memory); }
                }
                return file;
            } catch { file.Dispose(); throw; }
        }
        [DllImport("shlwapi.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Interface)]
        private static extern IStream SHCreateMemStream([In] byte[] initial, uint size);
        [ComImport, Guid("00000109-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPersistStream
        {
            void GetClassID(out Guid classId);
            [PreserveSig] int IsDirty();
            void Load(IStream stream);
            void Save(IStream stream, [MarshalAs(UnmanagedType.Bool)] bool clearDirty);
            void GetSizeMax(out long size);
        }
        // Keep the exact checked object pinned against writes/replacement. The
        // delete disposition uses that same handle rather than a later path read.
        private sealed class CodexFile : IDisposable
        {
            private readonly FileStream stream;
            private CodexFile(Microsoft.Win32.SafeHandles.SafeFileHandle handle, bool writable)
            { stream = new FileStream(handle, writable ? FileAccess.ReadWrite : FileAccess.Read); }
            internal static CodexFile Open(string path, bool writable)
            {
                // Missing leaf directories do not hide a linked ancestor which
                // CreateDirectory would otherwise follow on first enable.
                for (string parent = Path.GetDirectoryName(path); !String.IsNullOrEmpty(parent); parent = Path.GetDirectoryName(parent)) {
                    try {
                        var parentAttributes = File.GetAttributes(parent);
                        if ((parentAttributes & FileAttributes.ReparsePoint) != 0 || (parentAttributes & FileAttributes.Directory) == 0)
                            throw new IOException("Связанные или заменённые каталоги Codex не изменяются.");
                    } catch (DirectoryNotFoundException) { } catch (FileNotFoundException) { }
                }
                FileAttributes attributes;
                try { attributes = File.GetAttributes(path); }
                catch (FileNotFoundException) { return null; } catch (DirectoryNotFoundException) { return null; }
                if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.Directory | FileAttributes.ReadOnly)) != 0)
                    throw new IOException("Связанные, недоступные или заменённые файлы Codex не изменяются.");
                var handle = CreateFile(path, 0x80010000u | (writable ? 0x40000000u : 0), 1, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero);
                if (handle.IsInvalid) {
                    int error = Marshal.GetLastWin32Error(); handle.Dispose();
                    throw new IOException("Не удалось подтвердить владение файлами Codex. Файлы сохранены без изменений.", new System.ComponentModel.Win32Exception(error));
                }
                try {
                    if ((File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory | FileAttributes.ReadOnly)) != 0)
                        throw new IOException("Связанные, недоступные или заменённые файлы Codex не изменяются.");
                    return new CodexFile(handle, writable);
                } catch { handle.Dispose(); throw; }
            }
            internal void Write(byte[] content)
            { stream.Position = 0; stream.Write(content, 0, content.Length); stream.SetLength(content.Length); stream.Flush(true); }
            internal string ReadText()
            {
                stream.Position = 0;
                using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true)) return reader.ReadToEnd();
            }
            internal byte[] ReadSnapshot(int limit)
            {
                long length = stream.Length;
                if (length < 1 || length > limit) throw new IOException(ShortcutReadFailure);
                var bytes = new byte[(int)length]; stream.Position = 0;
                for (int offset = 0; offset < bytes.Length; ) {
                    int count = stream.Read(bytes, offset, bytes.Length - offset);
                    if (count == 0) throw new IOException(ShortcutReadFailure);
                    offset += count;
                }
                if (stream.Length != length) throw new IOException(ShortcutReadFailure);
                return bytes;
            }
            internal void Delete() { SetDelete(true); }
            internal void Keep() { SetDelete(false); }
            private void SetDelete(bool delete)
            {
                var info = new FileDisposition { DeleteFile = delete };
                if (!SetFileInformationByHandle(stream.SafeFileHandle, 4, ref info, 1))
                    throw new IOException("Не удалось завершить изменение файлов Codex.", new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()));
            }
            public void Dispose() { stream.Dispose(); }
            [StructLayout(LayoutKind.Sequential)] private struct FileDisposition { [MarshalAs(UnmanagedType.U1)] internal bool DeleteFile; }
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
            [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool SetFileInformationByHandle(Microsoft.Win32.SafeHandles.SafeFileHandle file, int type, ref FileDisposition information, uint size);
        }
        [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink { }
        // Use the Unicode Shell interface: WScript's shortcut writer can lose
        // non-ASCII file names on Windows installations with another locale.
        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int length, IntPtr findData, uint flags);
            void GetIDList(out IntPtr list);
            void SetIDList(IntPtr list);
            void GetDescription(IntPtr description, int length);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string description);
            void GetWorkingDirectory(IntPtr directory, int length);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string directory);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder arguments, int length);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string arguments);
            void GetHotkey(out short hotkey);
            void SetHotkey(short hotkey);
            void GetShowCmd(out int command);
            void SetShowCmd(int command);
            void GetIconLocation(IntPtr path, int length, out int index);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string path, int index);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path, uint reserved);
            void Resolve(IntPtr window, uint flags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
        }
        internal static bool IsOwned { get { return File.Exists(LauncherPath) && File.ReadAllText(LauncherPath).Contains(Marker); } }
        internal static void MoveOwned(int port)
        {
            if (IsOwned) File.WriteAllText(LauncherPath, LauncherContent(port), Encoding.ASCII);
        }
        public static void Disable()
        { Disable(LauncherPath, ShortcutPath); }
        internal static void Disable(string launcherPath, string shortcutPath, Action beforeMutation = null)
        {
            launcherPath = Path.GetFullPath(launcherPath); shortcutPath = Path.GetFullPath(shortcutPath);
            using (var launcher = OpenOwnedLauncher(launcherPath, false)) {
                if (launcher == null) return;
                using (var shortcut = OpenOwnedShortcut(shortcutPath, launcherPath)) {
                    if (beforeMutation != null) beforeMutation();
                    if (shortcut != null) shortcut.Delete();
                    try { launcher.Delete(); }
                    catch { if (shortcut != null) shortcut.Keep(); throw; }
                }
            }
        }
        public static Process Open(int port)
        {
            // Launch in a scoped child process. Never modify Codex config, PATH or API keys.
            var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"), "/D /K codex")
            {
                UseShellExecute = false,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };
            CliProxyEnvironmentService.ApplyProcessEnvironment(start, port);
            return Process.Start(start);
        }
    }
}
