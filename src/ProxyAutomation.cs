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
    internal enum ProxyFeature { Terminal, Windows, Codex }

    // One pending application per enabled option. Manual off cancels that application
    // until the next app launch or until the user explicitly enables automation again.
    internal sealed class AutomationPlan
    {
        private readonly HashSet<ProxyFeature> pending = new HashSet<ProxyFeature>();
        internal void Update(AppSettings before, AppSettings after)
        {
            foreach (ProxyFeature feature in Enum.GetValues(typeof(ProxyFeature)))
            {
                if (!Enabled(after, feature)) pending.Remove(feature);
                else if (before == null || !Enabled(before, feature)) pending.Add(feature);
            }
        }
        internal bool Take(ProxyFeature feature, bool ready) { return ready && pending.Remove(feature); }
        internal void Cancel(ProxyFeature feature) { pending.Remove(feature); }
        internal static bool Enabled(AppSettings s, ProxyFeature feature)
        {
            return feature == ProxyFeature.Terminal ? s.AutoApplyProxy : feature == ProxyFeature.Windows ? s.AutoSystemProxy : s.AutoCodexProxy;
        }
    }

    internal static class CodexProxyService
    {
        private const string Marker = "rem ProGo scoped Codex launcher v1";
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
        {
            if (File.Exists(LauncherPath) && !File.ReadAllText(LauncherPath).Contains(Marker)) throw new IOException("Файл запуска Codex уже существует и создан не ProGo. Он сохранён без изменений.");
            Directory.CreateDirectory(Path.GetDirectoryName(LauncherPath));
            File.WriteAllText(LauncherPath, LauncherContent(port), Encoding.ASCII);
            Directory.CreateDirectory(Path.GetDirectoryName(ShortcutPath));
            object shortcut = null;
            try
            {
                shortcut = new ShellLink();
                var link = (IShellLinkW)shortcut;
                link.SetPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"));
                link.SetArguments("/D /K \"\"" + LauncherPath + "\"\"");
                link.SetWorkingDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                link.SetIconLocation(Application.ExecutablePath, 0);
                link.SetDescription("Codex CLI с прокси ProGo только для этого окна");
                ((IPersistFile)shortcut).Save(ShortcutPath, true);
            }
            finally
            {
                if (shortcut != null) Marshal.FinalReleaseComObject(shortcut);
            }
        }
        [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink { }
        // Use the Unicode Shell interface: WScript's shortcut writer can lose
        // non-ASCII file names on Windows installations with another locale.
        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            void GetPath(IntPtr path, int length, IntPtr findData, uint flags);
            void GetIDList(out IntPtr list);
            void SetIDList(IntPtr list);
            void GetDescription(IntPtr description, int length);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string description);
            void GetWorkingDirectory(IntPtr directory, int length);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string directory);
            void GetArguments(IntPtr arguments, int length);
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
        {
            if (!File.Exists(LauncherPath) || !File.ReadAllText(LauncherPath).Contains(Marker)) return;
            if (File.Exists(ShortcutPath)) File.Delete(ShortcutPath);
            File.Delete(LauncherPath);
        }
        public static void Open(int port)
        {
            // Launch in a scoped child process. Never modify Codex config, PATH or API keys.
            var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"), "/D /K codex")
            {
                UseShellExecute = false,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };
            CliProxyEnvironmentService.ApplyProcessEnvironment(start, port);
            Process.Start(start);
        }
    }
}
