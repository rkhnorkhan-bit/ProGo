using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace ProGo
{
    // Owns only the sing-box process started by this ProGo instance. It never
    // terminates a process found by name/PID and never changes unrelated routes.
    internal sealed class SmartRoutingEngine : IDisposable
    {
        private readonly SettingsService settings;
        private readonly ProxyService proxy;
        private Process active;
        private readonly object gate = new object();

        public SmartRoutingEngine(SettingsService settingsService, ProxyService proxyService)
        {
            settings = settingsService;
            proxy = proxyService;
        }

        public bool IsActive
        {
            get
            {
                lock (gate)
                {
                    try { return active != null && !active.HasExited; }
                    catch (InvalidOperationException) { return false; }
                }
            }
        }

        public static string DefaultEnginePath
        {
            get { return Path.Combine(AppPaths.Root, "tools", "sing-box.exe"); }
        }

        public bool Start(out string message)
        {
            message = null;
            if (IsActive) return true;
            var options = settings.Current;
            if (!options.SmartRoutingEnabled)
            {
                message = "Включите «Умная маршрутизация» и сохраните настройки.";
                return false;
            }
            if (options.AutoSystemProxy)
            {
                message = "Нельзя одновременно включать системный HTTP-прокси и режим TUN. Отключите автоматическое применение прокси Windows.";
                return false;
            }

            using (var identity = WindowsIdentity.GetCurrent())
            {
                if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                {
                    message = "TUN требует прав администратора. Запустите ProGo от имени администратора; настройки и правила уже сохранены.";
                    return false;
                }
            }

            var enginePath = String.IsNullOrWhiteSpace(options.SmartRoutingEnginePath)
                ? DefaultEnginePath : options.SmartRoutingEnginePath.Trim();
            if (!Path.IsPathRooted(enginePath) || !File.Exists(enginePath)
                || !String.Equals(Path.GetFileName(enginePath), "sing-box.exe", StringComparison.OrdinalIgnoreCase))
            {
                message = "Не найден sing-box.exe. Выберите официальный исполняемый файл в настройках маршрутизации.";
                return false;
            }

            var hasProxyRules = ApplicationRouting.Normalize(options.ApplicationRoutes)
                .Exists(r => r.Mode == ApplicationRouting.Proxy);
            if (hasProxyRules && !proxy.IsListening())
            {
                message = "SSH/SOCKS-соединение не готово. Запустите подключение ProGo перед включением TUN.";
                return false;
            }

            // A previous manual Windows proxy setting is owned by its application.
            // Only restore values known to have been set by ProGo.
            var current = SystemProxyService.ReadCurrent();
            if (current.ProxyEnable != 0 && SystemProxyService.IsOwned)
            {
                string restoreError;
                if (!SystemProxyService.Restore(out restoreError))
                {
                    message = restoreError;
                    return false;
                }
                current = SystemProxyService.ReadCurrent();
            }
            if (current.ProxyEnable != 0)
            {
                message = "В Windows уже включён сторонний системный прокси. Режим TUN не будет менять его без разрешения.";
                return false;
            }

            var routingDirectory = Path.Combine(AppPaths.Root, "smart-routing");
            Directory.CreateDirectory(routingDirectory);
            var configFile = Path.Combine(routingDirectory, "active.json");
            var pendingFile = configFile + ".tmp";
            try
            {
                // The static config references this local file. It must exist
                // before sing-box check and before any network route changes.
                SmartDomainPolicy.SaveRuleSet(new string[0]);
                File.WriteAllText(pendingFile, ApplicationRouting.GenerateSingBoxConfig(options), new UTF8Encoding(false));
                // Check before replacing the active config.
                string checkMessage;
                if (!CheckConfig(enginePath, pendingFile, out checkMessage))
                {
                    message = "sing-box отклонил конфигурацию: " + checkMessage;
                    return false;
                }
                if (File.Exists(configFile)) File.Replace(pendingFile, configFile, null);
                else File.Move(pendingFile, configFile);

                var start = new ProcessStartInfo(enginePath)
                {
                    Arguments = "run -c \"" + configFile + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = routingDirectory
                };
                var process = new Process { StartInfo = start, EnableRaisingEvents = true };
                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs args)
                {
                    if (args.Data != null) SafeLog.Info("Routing engine: " + args.Data);
                };
                process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs args)
                {
                    if (args.Data != null) SafeLog.Info("Routing engine: " + args.Data);
                };
                if (!process.Start())
                {
                    process.Dispose();
                    message = "Не удалось запустить sing-box.";
                    return false;
                }
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                Thread.Sleep(800);
                if (process.HasExited)
                {
                    message = "sing-box остановился при запуске (код " + process.ExitCode + "). Смотрите журнал ProGo.";
                    process.Dispose();
                    return false;
                }
                lock (gate) active = process;
                SafeLog.Info("Smart routing started. Config: " + configFile);
                return true;
            }
            catch (Exception ex)
            {
                SafeLog.Error("Smart routing start failed.", ex);
                message = "Не удалось запустить маршрутизатор: " + ex.Message;
                return false;
            }
            finally
            {
                try { if (File.Exists(pendingFile)) File.Delete(pendingFile); }
                catch (Exception) { }
            }
        }

        // CLI dry-run is mandatory; untrusted binaries are never downloaded
        // and never invoked by a system shell.
        private static bool CheckConfig(string enginePath, string configPath, out string message)
        {
            message = null;
            var psi = new ProcessStartInfo(enginePath)
            {
                Arguments = "check -c \"" + configPath + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            using (var check = new Process { StartInfo = psi })
            {
                check.Start();
                if (!check.WaitForExit(15000))
                {
                    try { check.Kill(); } catch (Exception) { }
                    message = "превышено время проверки (15 секунд)";
                    return false;
                }
                var stderr = check.StandardError.ReadToEnd();
                var stdout = check.StandardOutput.ReadToEnd();
                if (check.ExitCode == 0) return true;
                message = (stderr + " " + stdout).Trim();
                if (message.Length > 500) message = message.Substring(0, 500);
                return false;
            }
        }

        public void Stop()
        {
            Process process;
            lock (gate) { process = active; active = null; }
            if (process == null) return;
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(); // Only the child started by this instance.
                    if (!process.WaitForExit(5000))
                        SafeLog.Error("Owned routing engine did not terminate.", new TimeoutException());
                }
            }
            catch (Exception ex) { SafeLog.Error("Routing engine stop failed.", ex); }
            finally { process.Dispose(); }
        }

        public void Dispose() { Stop(); }
    }
}
