using System;
using System.Diagnostics;
using System.Text;

namespace ProGo
{
    internal static class SshInteractiveLogin
    {
        internal static ProcessStartInfo CreateStartInfo(string target)
        {
            return CreateStartInfo(new SshProfileSetting { Target = target });
        }
        internal static ProcessStartInfo CreateStartInfo(SshProfileSetting profile)
        { return CreateStartInfo(profile, OpenSshClient.Executable); }
        internal static ProcessStartInfo CreateStartInfo(SshProfileSetting profile, string executable)
        {
            var args = SshConnection.Arguments(profile);
            // Literal PowerShell strings + EncodedCommand keep file paths out of shell syntax.
            // Keep the console visible after refusals, verify the host, and disable configured forwardings.
            var command = "& '" + executable.Replace("'", "''") + "' -o ClearAllForwardings=yes -o StrictHostKeyChecking=ask -o BatchMode=no -o NumberOfPasswordPrompts=3 ";
            foreach (var arg in args) command += "'" + arg.Replace("'", "''") + "' ";
            return new ProcessStartInfo("powershell.exe", "-NoProfile -NoExit -EncodedCommand " +
                Convert.ToBase64String(Encoding.Unicode.GetBytes(command))) {
                UseShellExecute = true, WindowStyle = ProcessWindowStyle.Normal
            };
        }
        internal static void Open(SshProfileSetting profile) { using (var process = Process.Start(CreateStartInfo(profile))) { } }
    }
}
