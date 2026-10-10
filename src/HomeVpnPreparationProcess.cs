using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProGo
{
    // Only the preparatory SCP console, before any server configuration command.
    // User terminals, persistent tunnels and remote mutations must not use this owner.
    internal static class HomeVpnPreparationProcess
    {
        internal const int TimeoutMs = 300000;
        internal static Task CopyAsync(string executable, string arguments, int timeoutMs, CancellationToken token)
        {
            return Task.Run(delegate {
                var script = "$p=Start-Process -FilePath " + Literal(executable) + " -ArgumentList " + Literal(arguments)
                    + " -NoNewWindow -PassThru -Wait; if($p.ExitCode -ne 0){ Write-Host 'SCP failed. Check SSH access above.'; Start-Sleep -Seconds 5 }; exit $p.ExitCode";
                var shell = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
                int code = Run(shell, "-NoProfile -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)), timeoutMs, token);
                if (code != 0) throw new IOException("Копирование не завершено. Команды настройки VPS не запускались. Проверьте SSH, пароль и подтверждение ключа в его окне, затем повторите подготовку.");
            });
        }
        private static string Literal(string value) { return "'" + value.Replace("'", "''") + "'"; }
        internal static int Run(string executable, string arguments, int timeoutMs, CancellationToken token)
        {
            return HomeVpnConsoleProcess.Run(executable, arguments, timeoutMs, token, "ProGo — подготовка VPS",
                "Время копирования истекло. Команды настройки VPS не запускались. Проверьте SSH и повторите подготовку.",
                "Остановка процессов копирования не подтверждена. Команды настройки VPS не запускались; отмена пока не подтверждена.");
        }
    }
}
