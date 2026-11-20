using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
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
        private DateTime lastTaskCheck = DateTime.MinValue;
        private bool taskRunning;
        private bool taskOwned;
        private bool taskRefreshInProgress;
        private const string PinnedEngineSha256 = "8f30682ee198855440d2a7fe070d5d3653793b2d01bb8c85eea88f93bbe7c8b9";

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
                    try { if (active != null && !active.HasExited) return true; }
                    catch (InvalidOperationException) { }
                    if (!taskRefreshInProgress && DateTime.UtcNow - lastTaskCheck >= TimeSpan.FromSeconds(8))
                    {
                        taskRefreshInProgress = true;
                        ThreadPool.QueueUserWorkItem(delegate {
                            string status = SmartRoutingTask.State();
                            lock (gate)
                            {
                                taskRunning = SmartRoutingTask.IsRunningState(status);
                                taskOwned = SmartRoutingTask.IsManagedState(status);
                                lastTaskCheck = DateTime.UtcNow;
                                taskRefreshInProgress = false;
                            }
                        });
                    }
                    return taskRunning;
                }
            }
        }

        public static string DefaultEnginePath
        {
            get
            {
                return File.Exists(SmartRoutingTask.ProtectedEnginePath) ? SmartRoutingTask.ProtectedEnginePath :
                    Path.Combine(AppPaths.Root, "tools", "sing-box.exe");
            }
        }

        internal static void PrepareConfig(AppSettings options)
        {
            if (options == null) throw new ArgumentNullException("options");
            var dir = Path.Combine(AppPaths.Root, "smart-routing");
            Directory.CreateDirectory(dir);
            if (!File.Exists(SmartDomainPolicy.RuleSetPath))
                SmartDomainPolicy.SaveRuleSet(new string[0]);
            ApplicationRouting.SaveManualRuleSets(options);
            SmartDomainPolicy.WriteAtomic(Path.Combine(dir, "active.json"), ApplicationRouting.GenerateSingBoxConfig(options));
        }

        public bool Start(out string message)
        {
            message = null;
            var options = settings.Current;
            if (!options.SmartRoutingEnabled)
            {
                message = "Включите «Умная маршрутизация» и сохраните настройки.";
                return false;
            }
            if (IsActive) return true;
            if (options.AutoSystemProxy)
            {
                message = "Нельзя одновременно включать системный HTTP-прокси и режим TUN. Отключите автоматическое применение прокси Windows.";
                return false;
            }

            var taskState = SmartRoutingTask.State();
            if (String.Equals(taskState, "Waiting", StringComparison.OrdinalIgnoreCase))
            {
                message = "Ожидание готовности SSH/SOCKS; задача TUN уже запущена.";
                return false;
            }
            if (String.Equals(taskState, "INVALID", StringComparison.OrdinalIgnoreCase))
            {
                message = "Найдена сторонняя задача ProGoSmartRouting. Она не будет запущена или остановлена ProGo.";
                return false;
            }
            bool managedTask = SmartRoutingTask.IsManagedState(taskState);
            if (!managedTask)
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        message = "Для автоматического TUN один раз установите защищённый компонент ProGoRoutingEngine из официального пакета. Не запускайте весь интерфейс ProGo с повышенными правами.";
                        return false;
                    }
                }
            }

            var enginePath = managedTask ? SmartRoutingTask.ProtectedEnginePath :
                (String.IsNullOrWhiteSpace(options.SmartRoutingEnginePath) ? DefaultEnginePath : options.SmartRoutingEnginePath.Trim());
            if (!Path.IsPathRooted(enginePath) || !File.Exists(enginePath)
                || !String.Equals(Path.GetFileName(enginePath), "sing-box.exe", StringComparison.OrdinalIgnoreCase))
            {
                message = "Не найден sing-box.exe. Выберите официальный исполняемый файл в настройках маршрутизации.";
                return false;
            }

            string hashError;
            if (!VerifyPinnedEngine(enginePath, out hashError))
            {
                message = hashError;
                return false;
            }

            if (!proxy.IsListening())
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
                ApplicationRouting.SaveManualRuleSets(options);
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

                if (managedTask)
                {
                    if (!SmartRoutingTask.IsRunningState(taskState))
                    {
                        string taskMessage;
                        if (!SmartRoutingTask.Run(out taskMessage))
                        {
                            message = "Планировщик отказал в запуске TUN: " + taskMessage;
                            return false;
                        }
                        Thread.Sleep(1300);
                    }
                    var latestState = SmartRoutingTask.State();
                    lock (gate)
                    {
                        taskOwned = true;
                        taskRunning = SmartRoutingTask.IsRunningState(latestState);
                        lastTaskCheck = DateTime.UtcNow;
                    }
                    if (!SmartRoutingTask.IsRunningState(latestState))
                    {
                        message = "Планировщик принял команду, но TUN не запустился: " + latestState;
                        return false;
                    }
                    SafeLog.Info("Smart routing started via protected scheduled task.");
                    return true;
                }

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

        private static bool VerifyPinnedEngine(string path, out string message)
        {
            message = null;
            try
            {
                using (var sha = SHA256.Create())
                using (var file = File.OpenRead(path))
                {
                    var digest = BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
                    if (String.Equals(digest, PinnedEngineSha256, StringComparison.Ordinal)) return true;
                }
                message = "Неверная контрольная сумма sing-box.exe. Нужен официальный sing-box 1.14.4 (amd64). Исполняемый файл не запущен.";
            }
            catch (Exception ex) { message = "Не удалось проверить SHA-256 sing-box.exe: " + ex.Message; }
            return false;
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
            var state = SmartRoutingTask.State();
            if (SmartRoutingTask.IsRunningState(state) ||
                String.Equals(state, "Waiting", StringComparison.OrdinalIgnoreCase))
            {
                string taskMessage;
                if (!SmartRoutingTask.Stop(out taskMessage))
                    SafeLog.Error("Could not stop owned routing task.", new InvalidOperationException(taskMessage));
            }
            lock (gate)
            {
                taskRunning = false;
                lastTaskCheck = DateTime.MinValue;
            }
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
