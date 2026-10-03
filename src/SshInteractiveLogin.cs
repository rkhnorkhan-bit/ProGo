using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ProGo
{
    internal static class SshInteractiveLogin
    {
        internal static ProcessStartInfo CreateStartInfo(string target)
        {
            if (target != null && target.IndexOfAny(new[] { '\r', '\n' }) >= 0) throw new ArgumentException("Адрес SSH должен быть в одной строке.");
            target = (target ?? "").Trim();
            if (target.Length == 0 || target[0] == '-' || !Regex.IsMatch(target, @"^[A-Za-z0-9_.@:\[\]-]+$"))
                throw new ArgumentException("Выберите имя SSH-подключения или адрес user@vpn.example.org без параметров командной строки.");
            // This explicit action opens a visible SSH console. Never bypass host-key validation
            // or store a password; forwardings from the user's SSH config are disabled here.
            // Keep the console open after a refusal so the user can read SSH's explanation.
            return new ProcessStartInfo("powershell.exe", "-NoProfile -NoExit -Command \"& ssh.exe -o ClearAllForwardings=yes -o StrictHostKeyChecking=ask -o BatchMode=no -o NumberOfPasswordPrompts=3 '" + target + "'\"") {
                UseShellExecute = true, WindowStyle = ProcessWindowStyle.Normal
            };
        }
        internal static void Open(string target) { using (var process = Process.Start(CreateStartInfo(target))) { } }
    }
}
