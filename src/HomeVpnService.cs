using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ProGo
{
    internal sealed class HomeVpnOwner
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Login { get; set; }
        public string KeyFile { get; set; }
        internal void Validate()
        {
            if (!HomeVpnAccess.ValidHost(Host) || Port < 1 || Port > 65535
                || !System.Text.RegularExpressions.Regex.IsMatch(Login ?? "", @"\A[a-z_][a-z0-9_-]{0,31}\z"))
                throw new ArgumentException("Проверьте адрес VPS, SSH-порт и имя пользователя.");
            if (!String.IsNullOrWhiteSpace(KeyFile) && !File.Exists(KeyFile)) throw new ArgumentException("Файл SSH-ключа не найден.");
        }
    }

    internal sealed class HomeVpnInvitation
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Revoked { get; set; }
        public string Created { get; set; }
        internal string Status { get { return Revoked ? "Отозван" : "Активен до отзыва"; } }
        internal string CreatedText {
            get {
                DateTimeOffset value;
                return DateTimeOffset.TryParse(Created, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal, out value)
                    ? value.UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'", System.Globalization.CultureInfo.InvariantCulture)
                    : "Дата не передана сервером";
            }
        }
        public override string ToString() {
            var id = Id ?? "";
            return (Name ?? "Без имени") + " — " + Status + " (" + id.Substring(0, Math.Min(8, id.Length)) + ")";
        }
    }

    internal sealed class HomeVpnService : IDisposable
    {
        internal readonly Ikev2RelayService Relay;
        internal HomeVpnAccess Access { get; private set; }
        internal HomeVpnOwner Owner { get; private set; }
        internal string HomeAddress { get; private set; }
        internal string ShareOrigin
        {
            get { return Access == null ? null : HomeVpnPrivateFiles.Load("share-" + Access.ServerId) ?? Access.ShareUrl; }
        }
        internal void SetShareOrigin(string value)
        {
            if (Access == null) throw new InvalidOperationException("Сначала добавьте доступ к VPS.");
            HomeVpnPrivateFiles.Save("share-" + Access.ServerId, HomeProfileShare.Origin(value));
        }
        private ProxyService proxy;
        private string sessionDirectory;
        private CancellationTokenSource starting;
        private bool disposed;
        private readonly string sshExecutable;
        private readonly int ikePort, natPort, bridgePort;
        internal bool AutoRestart = true;
        internal string RecoveryStatus { get { return proxy == null ? "Не запущен" : proxy.RecoveryStatus; } }

        internal HomeVpnService(Ikev2RelayService relay)
            : this(relay, "ssh.exe", Ikev2RelayService.IkePort, Ikev2RelayService.NatPort, Ikev2RelayService.BridgePort) { }

        // Isolated Windows fixtures use their own executable and unprivileged UDP ports.
        internal HomeVpnService(Ikev2RelayService relay, string sshExecutable, int ikePort, int natPort, int bridgePort)
        {
            Relay = relay; this.sshExecutable = sshExecutable;
            this.ikePort = ikePort; this.natPort = natPort; this.bridgePort = bridgePort;
            try
            {
                var token = HomeVpnPrivateFiles.Load("access");
                if (token != null) Access = HomeVpnAccess.Parse(token);
                var owner = HomeVpnPrivateFiles.Load("owner");
                if (owner != null) Owner = new JavaScriptSerializer().Deserialize<HomeVpnOwner>(owner);
                HomeAddress = HomeVpnPrivateFiles.Load("home-address") ?? "";
            }
            catch { SafeLog.Info("Saved home VPN access needs to be imported again."); }
        }

        internal void UseToken(string token, HomeVpnOwner owner)
        {
            var access = HomeVpnAccess.Parse(token);
            Stop();
            HomeVpnPrivateFiles.Save("access", token.Trim());
            HomeVpnPrivateFiles.Save("owner", new JavaScriptSerializer().Serialize(owner));
            Access = access; Owner = owner;
        }

        internal void SetHomeAddress(string address)
        {
            address = (address ?? "").Trim();
            if (!HomeVpnAccess.ValidHost(address)) throw new ArgumentException("Введите внешний адрес домашнего роутера или DDNS, без https:// и порта.");
            HomeVpnPrivateFiles.Save("home-address", address); HomeAddress = address;
        }

        internal async Task StartAsync()
        {
            await StartAsync(CancellationToken.None);
        }

        internal async Task StartAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (disposed) throw new ObjectDisposedException("HomeVpnService");
            if (Access == null) throw new InvalidOperationException("Сначала добавьте VPS или вставьте токен.");
            if (Relay.IsRunning || starting != null) return;
            Stop();
            if (proxy != null || sessionDirectory != null)
                throw new InvalidOperationException("Предыдущий запуск канала ещё не очищен. Закройте программы, использующие временные файлы, и повторите остановку VPN для телефона.");
            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token); starting = cancellation;
            try
            {
                HomeVpnPrivateFiles.SecureDirectory(HomeVpnPrivateFiles.Root);
                sessionDirectory = Path.Combine(HomeVpnPrivateFiles.Root, "session-" + Guid.NewGuid().ToString("N"));
                HomeVpnPrivateFiles.SecureDirectory(sessionDirectory);
                var key = Path.Combine(sessionDirectory, "access");
                var known = Path.Combine(sessionDirectory, "known_hosts");
                var config = Path.Combine(sessionDirectory, "ssh_config");
                File.WriteAllText(key, Access.PrivateKey, new UTF8Encoding(false));
                File.WriteAllText(known, "progo-home-" + Access.ServerId + " " + Access.HostKey + "\n", new UTF8Encoding(false));
                File.WriteAllText(config, "Host progo-home\n HostName " + Access.Host + "\n Port " + Access.Port + "\n User " + Access.User
                    + "\n IdentityFile " + ConfigPath(key) + "\n IdentitiesOnly yes\n IdentityAgent none\n BatchMode yes\n StrictHostKeyChecking yes"
                    + "\n HostKeyAlias progo-home-" + Access.ServerId + "\n HostKeyAlgorithms ssh-ed25519\n UserKnownHostsFile " + ConfigPath(known)
                    + "\n GlobalKnownHostsFile NUL\n", new UTF8Encoding(false));
                int port;
                var reservation = new TcpListener(IPAddress.Loopback, 0); reservation.Start();
                try { port = ((IPEndPoint)reservation.LocalEndpoint).Port; } finally { reservation.Stop(); }
                var options = AppSettings.Defaults(); options.SocksPort = port; options.SshProfile = "progo-home";
                proxy = new ProxyService(delegate { options.AutoRestartSocks = AutoRestart; return options; }, delegate { }, sshExecutable, delegate { return DateTime.UtcNow; }, true,
                    "-F " + Argument(config) + " ", 10000);
                // Reuse the owned asynchronous SOCKS handshake, deadline and cancellation.
                if (!await proxy.StartTunnelAsync(cancellation.Token)) throw new InvalidOperationException("SOCKS не готов.");
                cancellation.Token.ThrowIfCancellationRequested();
                await Relay.StartAsync("127.0.0.1", port, ikePort, natPort, bridgePort, cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
            }
            catch
            {
                CleanupFailedStart();
                cancellation.Token.ThrowIfCancellationRequested();
                throw new InvalidOperationException("Канал к VPS не готов. Проверьте интернет и токен: владелец мог отозвать доступ или изменить SSH-ключ сервера. Можно повторить запуск.");
            }
            finally { if (starting == cancellation) starting = null; cancellation.Dispose(); }
        }

        private void CleanupFailedStart()
        {
            Relay.Stop();
            if (proxy != null)
            {
                proxy.Dispose();
                if (proxy.HasOwnedProcess) throw new InvalidOperationException("Не удалось подтвердить остановку процесса канала. Повторите остановку VPN для телефона; отмена пока не подтверждена.");
                proxy = null;
            }
            if (sessionDirectory != null)
            {
                try { if (Directory.Exists(sessionDirectory)) Directory.Delete(sessionDirectory, true); }
                catch { throw new InvalidOperationException("Канал остановлен, но временные файлы доступа не удалось удалить. Закройте программы, использующие эти файлы; отмена пока не подтверждена."); }
                sessionDirectory = null;
            }
        }

        internal void Stop()
        {
            if (starting != null) starting.Cancel();
            Relay.Stop();
            if (proxy != null) {
                proxy.Dispose();
                if (proxy.HasOwnedProcess) return; // Retain ownership; never overwrite an unsettled child.
                proxy = null;
            }
            if (sessionDirectory != null)
            {
                try { if (Directory.Exists(sessionDirectory)) Directory.Delete(sessionDirectory, true); sessionDirectory = null; }
                catch { /* Keep ownership so cancellation/retry cannot claim cleanup succeeded. */ }
            }
        }

        private static string ConfigPath(string path)
        {
            if (path.IndexOfAny(new[] { '\r', '\n', '"' }) >= 0) throw new ArgumentException("Недопустимый путь к профилю Windows.");
            return "\"" + path.Replace('\\', '/') + "\"";
        }

        // Windows CreateProcess argument quoting, including trailing backslashes.
        internal static string Argument(string value)
        {
            var result = new StringBuilder("\""); int slashes = 0;
            foreach (char c in value)
            {
                if (c == '\\') { slashes++; continue; }
                result.Append('\\', c == '"' ? slashes * 2 + 1 : slashes); result.Append(c); slashes = 0;
            }
            result.Append('\\', slashes * 2); return result.Append('"').ToString();
        }

        internal static async Task<string> AdminAsync(HomeVpnOwner owner, string action, string label, string identifier, Action<string> progress)
        {
            return await AdminAsync(owner, action, label, identifier, progress,
                (executable, arguments) => HomeVpnPreparationForm.CopyAsync(System.Windows.Forms.Form.ActiveForm, executable, arguments,
                    action == "recover-setup" || (action == "setup" && HomeVpnSetupRecovery.HasPending())), (executable, arguments, output) =>
                    action == "setup" || action == "recover-setup"
                        ? HomeVpnPreparationForm.WaitForCommandAsync(System.Windows.Forms.Form.ActiveForm, executable, arguments, output)
                        : ConsoleAsync(executable, arguments, output));
        }

        // Injected transports keep Windows fixtures isolated from live VPS credentials.
        internal static async Task<string> AdminAsync(HomeVpnOwner owner, string action, string label, string identifier, Action<string> progress,
            Func<string, string, Task> copy, Func<string, string, string, Task> commandTransport)
        {
            owner.Validate();
            bool setup = action == "setup" || action == "recover-setup";
            if (!setup && action != "invite" && action != "list" && action != "revoke" && action != "share" && action != "repair") throw new ArgumentException("Unknown action");
            if (action == "revoke" && !System.Text.RegularExpressions.Regex.IsMatch(identifier ?? "", @"\A[0-9a-f]{24}\z")) throw new ArgumentException("Invalid invitation");
            if ((label ?? "").Length > 80 || (label ?? "").IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0) throw new ArgumentException("Название слишком длинное.");
            if (setup && Array.Exists((label ?? "").ToCharArray(), Char.IsControl)) throw new ArgumentException("Проверьте название доступа: без управляющих знаков.");
            if (action == "share") identifier = new Uri(HomeProfileShare.Origin(identifier)).Host;
            using (var lease = setup ? await HomeVpnSetupRecovery.AcquireAsync() : null)
            {
                var request = setup ? HomeVpnSetupRecovery.Load(owner, label) : null;
                bool checking = request != null;
                if (action == "recover-setup" && !checking) throw new HomeVpnSetupPendingException("Сохранённый запрос настройки отсутствует. Новая команда не запускалась.");
                HomeVpnPrivateFiles.SecureDirectory(HomeVpnPrivateFiles.Root);
                var work = Path.Combine(HomeVpnPrivateFiles.Root, "admin-" + Guid.NewGuid().ToString("N"));
                HomeVpnPrivateFiles.SecureDirectory(work);
                var name = "progo-" + Guid.NewGuid().ToString("N");
                var upload = Path.Combine(work, name); Directory.CreateDirectory(upload);
                var output = Path.Combine(work, "result.txt");
                bool remoteStarted = false;
                try
                {
                    foreach (var file in new[] { "home_vpn_setup.py", "ikev2_relay.py", "install-ikev2-relay.sh", "profile_share_setup.py", "profile_share.py", "qrcodegen.py", "QR_LICENSE.txt" })
                        File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "home-vpn", "server", file), Path.Combine(upload, file));
                    var keyArgs = String.IsNullOrWhiteSpace(owner.KeyFile) ? "" : " -i " + Argument(owner.KeyFile);
                    var target = owner.Login + "@" + owner.Host;
                    progress(checking ? "Подготовка проверки прежней настройки VPS. Предыдущая команда могла завершиться; повторной выдачи доступа не будет."
                        : "Копирование помощника на VPS. Если SSH спросит пароль или подтверждение ключа, ответьте в открывшемся окне.");
                    await copy("scp.exe", "-o ConnectTimeout=15 -P " + owner.Port + keyArgs + " -r " + Argument(upload) + " " + Argument(target + ":/tmp/"));
                    var remote = "/tmp/" + name;
                    if (setup && !checking) request = HomeVpnSetupRecovery.Register(owner, label);
                    var prefix = (owner.Login == "root" ? "" : "sudo -n ") + "python3 -I " + remote + "/home_vpn_setup.py ";
                    var options = " --host " + Shell(owner.Host) + " --port " + owner.Port + " --name " + Shell(label ?? "My iPhone")
                        + (action == "revoke" ? " --id " + identifier : action == "share" ? " --domain " + Shell(identifier) : "")
                        + (setup ? " --request-id " + request.RequestId : "");
                    var ssh = "-o ConnectTimeout=15 -T -p " + owner.Port + keyArgs + " " + Argument(target) + " ";
                    if (checking) {
                        progress("Проверяем прежнюю команду VPS. Новая настройка и новый доступ не запрашиваются.");
                        remoteStarted = true;
                        // Keep the freshly copied public helper for both read-only queries.
                        // The result query owns remote cleanup; status uncertainty preserves
                        // only public helpers in /tmp, without claiming remote rollback.
                        await commandTransport("ssh.exe", ssh + Argument(prefix + "operation-status --request-id " + request.RequestId
                            + " --output " + remote + "/status 1>&2 && cat " + remote + "/status"), output);
                        HomeVpnSetupRecovery.RequireCompleted(ReadSetupOutput(output), request);
                        progress("Прежняя настройка завершена. Получаем исходный доступ, без выдачи нового.");
                        var recovered = Path.Combine(work, "recovered.txt");
                        await commandTransport("ssh.exe", ssh + Argument("trap 'rm -rf -- " + remote + "' EXIT; " + prefix
                            + "operation-result --request-id " + request.RequestId + " --output " + remote + "/result 1>&2 && cat " + remote + "/result"), recovered);
                        return HomeVpnSetupRecovery.RetainResult(owner, request, ReadSetupOutput(recovered));
                    }
                    var command = "trap 'rm -rf -- " + remote + "' EXIT; " + prefix + action + options
                        + " --output " + remote + "/result 1>&2 && cat " + remote + "/result";
                    progress(setup ? "Настройка VPS. ID запроса сохранён до запуска SSH. При потере ответа проверьте прежнюю настройку вместо повторной выдачи доступа."
                        : "Настройка VPS. Окно SSH показывает ход установки; для пользователя без root нужен sudo без запроса пароля.");
                    // A real console remains available for OpenSSH password/host-key prompts.
                    // Only stdout goes to a private local file; the token is never a command argument.
                    remoteStarted = true;
                    await commandTransport("ssh.exe", ssh + Argument(command), output);
                    var result = setup ? ReadSetupOutput(output) : File.ReadAllText(output).Trim();
                    if (result.Length == 0 || result.Length > 32768) throw new InvalidOperationException("VPS не вернул результат.");
                    return setup ? HomeVpnSetupRecovery.RetainResult(owner, request, result) : result;
                }
                catch (HomeVpnPreparationCancelledException) {
                    if (checking) throw new HomeVpnPreparationCancelledException(true);
                    throw;
                }
                catch (HomeVpnSetupPendingException) { throw; }
                catch (Exception ex) {
                    if (ex is OutOfMemoryException || !setup || request == null) throw;
                    throw new HomeVpnSetupPendingException(HomeVpnSetupRecovery.PendingMessage);
                }
                finally {
                    try { Directory.Delete(work, true); }
                    catch {
                        if (!remoteStarted) throw new IOException(checking
                            ? "Подготовка проверки прервана, но локальные файлы не удалось удалить. Прежний запрос сохранён; его результат не подтверждён. Закройте программы, использующие эти файлы."
                            : "Команды настройки VPS не запускались, но локальные файлы подготовки не удалось удалить. Закройте программы, использующие эти файлы; завершение подготовки пока не подтверждено.");
                    }
                }
            }
        }

        private static string ReadSetupOutput(string path)
        {
            if (new FileInfo(path).Length > 32768) throw new HomeVpnSetupPendingException(HomeVpnSetupRecovery.PendingMessage);
            return File.ReadAllText(path, new UTF8Encoding(false, true)).Trim();
        }

        private static string Shell(string value) { return "'" + value.Replace("'", "'\"'\"'") + "'"; }
        private static string PowerShell(string value) { return "'" + value.Replace("'", "''") + "'"; }
        private static Task ConsoleAsync(string executable, string arguments, string output)
        {
            return Task.Run(delegate
            {
                // Start-Process ArgumentList preserves the exact, already quoted command line.
                var script = "$p=Start-Process -FilePath " + PowerShell(executable) + " -ArgumentList " + PowerShell(arguments)
                    + " -NoNewWindow -PassThru -Wait" + (output == null ? "" : " -RedirectStandardOutput " + PowerShell(output))
                    + "; if($p.ExitCode -ne 0){ Write-Host 'SSH operation failed. Check the message above.'; Start-Sleep -Seconds 5 }; exit $p.ExitCode";
                using (var process = Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -EncodedCommand "
                    + Convert.ToBase64String(Encoding.Unicode.GetBytes(script))) { UseShellExecute = true }))
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0) throw new InvalidOperationException("Операция SSH не завершена. Проверьте адрес, права пользователя и сообщение в окне SSH. Результат команды VPS не подтверждён.");
                }
            });
        }

        public void Dispose() { disposed = true; Stop(); }
    }
}
