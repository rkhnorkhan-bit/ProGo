using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        internal static string LauncherContent()
        {
            var text = new StringBuilder("@echo off\r\n" + Marker + "\r\nsetlocal\r\n");
            foreach (var name in new[] { "HTTP_PROXY", "HTTPS_PROXY", "ALL_PROXY" }) text.AppendLine("set \"" + name + "=" + CliProxyEnvironmentService.ProxyUrl + "\"");
            text.AppendLine("set \"NO_PROXY=localhost,127.0.0.1,::1\"");
            text.AppendLine("echo Codex via ProGo. Keep ProGo connected while using this window.");
            text.AppendLine("where codex >nul 2>nul");
            text.AppendLine("if errorlevel 1 (echo Codex CLI is not installed or not on PATH. & exit /b 1)");
            text.AppendLine("call codex %*");
            text.AppendLine("endlocal");
            return text.ToString();
        }
        public static void Enable()
        {
            if (File.Exists(LauncherPath) && !File.ReadAllText(LauncherPath).Contains(Marker)) throw new IOException("Файл запуска Codex уже существует и создан не ProGo. Он сохранён без изменений.");
            Directory.CreateDirectory(Path.GetDirectoryName(LauncherPath));
            File.WriteAllText(LauncherPath, LauncherContent(), Encoding.ASCII);
            Directory.CreateDirectory(Path.GetDirectoryName(ShortcutPath));
            object shell = null, shortcut = null;
            try
            {
                var type = Type.GetTypeFromProgID("WScript.Shell");
                shell = Activator.CreateInstance(type);
                shortcut = type.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { ShortcutPath });
                Set(shortcut, "TargetPath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"));
                Set(shortcut, "Arguments", "/D /K \"\"" + LauncherPath + "\"\"");
                Set(shortcut, "WorkingDirectory", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                Set(shortcut, "IconLocation", Application.ExecutablePath + ",0");
                Set(shortcut, "Description", "Codex CLI с прокси ProGo только для этого окна");
                shortcut.GetType().InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
            }
            finally
            {
                if (shortcut != null) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
                if (shell != null) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
            }
        }
        private static void Set(object target, string property, object value)
        {
            target.GetType().InvokeMember(property, System.Reflection.BindingFlags.SetProperty, null, target, new[] { value });
        }
        public static void Disable()
        {
            if (!File.Exists(LauncherPath) || !File.ReadAllText(LauncherPath).Contains(Marker)) return;
            if (File.Exists(ShortcutPath)) File.Delete(ShortcutPath);
            File.Delete(LauncherPath);
        }
        public static void Open()
        {
            // Launch in a scoped child process. Never modify Codex config, PATH or API keys.
            var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"), "/D /K codex")
            {
                UseShellExecute = false,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };
            CliProxyEnvironmentService.ApplyProcessEnvironment(start);
            Process.Start(start);
        }
    }
}
