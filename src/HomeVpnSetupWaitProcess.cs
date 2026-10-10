using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProGo
{
    // Only the own-VPS setup/request queries registered by HomeVpnSetupRecovery.
    // The server result stays uncertain when the local SSH client is interrupted.
    internal static class HomeVpnSetupWaitProcess
    {
        internal const int TimeoutMs = 600000;
        internal const string StopFailure = "Остановка локальных процессов SSH не подтверждена. Запрос VPS сохранён; результат сервера неизвестен. Дождитесь остановки процессов перед проверкой.";
        internal static Task CommandAsync(string executable, string arguments, string output, int timeoutMs, CancellationToken token)
        {
            if (timeoutMs < 1) throw new ArgumentOutOfRangeException("timeoutMs");
            return Task.Run(delegate {
                var script = "$p=Start-Process -FilePath " + Literal(executable) + " -ArgumentList " + Literal(arguments)
                    + " -NoNewWindow -PassThru -Wait -RedirectStandardOutput " + Literal(output)
                    + "; if($p.ExitCode -ne 0){ Write-Host 'SSH result is unconfirmed. Check the message above.'; Start-Sleep -Seconds 5 }; exit $p.ExitCode";
                var shell = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
                try {
                    int code = HomeVpnConsoleProcess.Run(shell, "-NoProfile -EncodedCommand "
                        + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)), timeoutMs, token, "ProGo — ожидание ответа VPS",
                        "Время ожидания SSH истекло. Локальные процессы остановлены; запрос VPS сохранён. Результат сервера нужно проверить через «Проверить прошлую настройку».", StopFailure);
                    if (code != 0) throw new HomeVpnSetupPendingException(HomeVpnSetupRecovery.PendingMessage);
                }
                catch (TimeoutException ex) { throw new HomeVpnSetupPendingException(ex.Message); }
                catch (IOException) { throw new HomeVpnSetupPendingException(StopFailure); }
            });
        }
        private static string Literal(string value) { return "'" + value.Replace("'", "''") + "'"; }
    }
}
