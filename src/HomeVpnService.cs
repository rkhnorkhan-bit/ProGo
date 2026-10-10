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
            : this(relay, OpenSshClient.Executable, Ikev2RelayService.IkePort, Ikev2RelayService.NatPort, Ikev2RelayService.BridgePort) { }

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
                (executable, arguments) => action == "list"
                    ? HomeVpnPreparationForm.CopyForListAsync(System.Windows.Forms.Form.ActiveForm, executable, arguments)
                    : HomeVpnPreparationForm.CopyAsync(System.Windows.Forms.Form.ActiveForm, executable, arguments,
                        action == "recover-setup" || action == "recover-invite" || (action == "setup" && HomeVpnSetupRecovery.HasPending())), null, HomeVpnPreparationProcess.TimeoutMs);
        }

        // Tests replace only the executable/copy, retaining the production dispatch
        // to its real owned waiting window instead of injecting a command transport.
        internal static Task<string> AdminAsync(HomeVpnOwner owner, string action, string label, string identifier, Action<string> progress,
            Func<string, string, Task> copy, string commandExecutable, int timeoutMs)
        {
            return AdminAsync(owner, action, label, identifier, progress, copy, (executable, arguments, output) => {
                executable = commandExecutable ?? executable;
                if (action == "setup" || action == "recover-setup")
                    return HomeVpnPreparationForm.WaitForCommandAsync(System.Windows.Forms.Form.ActiveForm, executable, arguments, output);
                if (action == "list")
                    return HomeVpnPreparationForm.WaitForListAsync(System.Windows.Forms.Form.ActiveForm, executable, arguments, output, timeoutMs);
                if (action == "invite" || action == "recover-invite")
                    return HomeVpnPreparationForm.WaitForAdminAsync(System.Windows.Forms.Form.ActiveForm, executable, arguments, output, timeoutMs);
                return HomeVpnPreparationForm.WaitForOwnerAsync(System.Windows.Forms.Form.ActiveForm, executable, arguments, output, timeoutMs);
            });
        }

        // Injected transports keep Windows fixtures isolated from live VPS credentials.
        internal static async Task<string> AdminAsync(HomeVpnOwner owner, string action, string label, string identifier, Action<string> progress,
            Func<string, string, Task> copy, Func<string, string, string, Task> commandTransport)
        {
            // Freeze the endpoint and authentication path before the first await;
            // a caller's mutable settings object cannot redirect this operation.
            if (owner == null) throw new ArgumentException("Проверьте исходные SSH-данные владельца VPS.");
            owner = new HomeVpnOwner { Host = owner.Host, Port = owner.Port, Login = owner.Login, KeyFile = owner.KeyFile };
            owner.Validate();
            bool setup = action == "setup" || action == "recover-setup";
            bool issuance = action == "invite" || action == "recover-invite";
            if (!setup && !issuance && action != "list" && action != "revoke" && action != "share" && action != "repair") throw new ArgumentException("Unknown action");
            if (action == "recover-invite" && (label != null || identifier != null)) throw new ArgumentException("Проверка использует только параметры сохранённой выдачи.");
            if (action == "revoke" && !System.Text.RegularExpressions.Regex.IsMatch(identifier ?? "", @"\A[0-9a-f]{24}\z")) throw new ArgumentException("Invalid invitation");
            if ((label ?? "").Length > 80 || (label ?? "").IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0) throw new ArgumentException("Название слишком длинное.");
            if (setup && Array.Exists((label ?? "").ToCharArray(), Char.IsControl)) throw new ArgumentException("Проверьте название доступа: без управляющих знаков.");
            if (action == "share") identifier = new Uri(HomeProfileShare.Origin(identifier)).Host;
            using (var lease = setup ? await HomeVpnSetupRecovery.AcquireAsync() : issuance ? await HomeVpnAdminRecovery.AcquireAsync() : null)
            {
                var request = setup ? HomeVpnSetupRecovery.Load(owner, label) : null;
                var adminRequest = action == "recover-invite" ? HomeVpnAdminRecovery.Load(owner) : null;
                bool checking = request != null || adminRequest != null;
                if (action == "recover-setup" && !checking) throw new HomeVpnSetupPendingException("Сохранённый запрос настройки отсутствует. Новая команда не запускалась.");
                if (action == "recover-invite" && adminRequest == null) throw new HomeVpnAdminPendingException("Сохранённый запрос выдачи отсутствует. Новая выдача не запускалась.");
                if (action == "invite" && HomeVpnAdminRecovery.HasPending()) throw new HomeVpnAdminPendingException(HomeVpnAdminRecovery.PendingMessage);
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
                    progress(action == "list" ? "Подготовка получения списка друзей. Существующий доступ и текущий список сохраняются; новые токены не создаются."
                        : issuance && checking ? "Подготовка проверки прежней выдачи доступа. Запрос сохранён; новое приглашение и повторный отзыв не запускаются."
                        : checking ? "Подготовка проверки прежней настройки VPS. Предыдущая команда могла завершиться; повторной выдачи доступа не будет."
                        : "Копирование помощника на VPS. Если SSH спросит пароль или подтверждение ключа, ответьте в открывшемся окне.");
                    await copy("scp.exe", "-o ConnectTimeout=15 -P " + owner.Port + keyArgs + " -r " + Argument(upload) + " " + Argument(target + ":/tmp/"));
                    var remote = "/tmp/" + name;
                    if (setup && !checking) request = HomeVpnSetupRecovery.Register(owner, label);
                    if (issuance && !checking) adminRequest = HomeVpnAdminRecovery.Register(owner, label, identifier);
                    var prefix = (owner.Login == "root" ? "" : "sudo -n ") + "python3 -I " + remote + "/home_vpn_setup.py ";
                    var options = " --host " + Shell(owner.Host) + " --port " + owner.Port + " --name " + Shell(label ?? "My iPhone")
                        + (action == "revoke" ? " --id " + identifier : action == "share" ? " --domain " + Shell(identifier) : "")
                        + (setup ? " --request-id " + request.RequestId : "");
                    var ssh = "-o ConnectTimeout=15 -T -p " + owner.Port + keyArgs + " " + Argument(target) + " ";
                    if (issuance && checking) {
                        progress("Проверяем прежнюю выдачу. Новое приглашение и повторный отзыв не запрашиваются."); remoteStarted = true;
                        await commandTransport("ssh.exe", ssh + Argument("trap 'rm -rf -- " + remote + "' EXIT; " + prefix + "operation-status --request-id " + adminRequest.RequestId
                            + " --output " + remote + "/status 1>&2 && cat " + remote + "/status"), output);
                        HomeVpnAdminRecovery.RequireCompleted(ReadAdminOutput(output, false), adminRequest);
                        progress("Прежняя выдача завершена; получаем тот же токен. Отозванный доступ не восстанавливается.");
                        // Each read-only SSH owns cleanup even when status is
                        // uncertain. Recopy only the public helper for the result.
                        await copy("scp.exe", "-o ConnectTimeout=15 -P " + owner.Port + keyArgs + " -r " + Argument(upload) + " " + Argument(target + ":/tmp/"));
                        var recovered = Path.Combine(work, "recovered.txt");
                        await commandTransport("ssh.exe", ssh + Argument("trap 'rm -rf -- " + remote + "' EXIT; " + prefix
                            + "operation-result --request-id " + adminRequest.RequestId + " --output " + remote + "/result 1>&2 && cat " + remote + "/result"), recovered);
                        return HomeVpnAdminRecovery.RetainResult(owner, adminRequest, ReadAdminOutput(recovered, false));
                    }
                    if (setup && checking) {
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
                    if (issuance) {
                        // The existing receipt protocol rechecks availability before
                        // private stdout handoff. The shell always cleans its result.
                        var id = adminRequest.RequestId;
                        command = "trap 'rm -rf -- " + remote + "' EXIT; " + prefix + "invite" + options
                            + " --request-id " + id + (adminRequest.SourceInviteId == null ? "" : " --id " + adminRequest.SourceInviteId)
                            + " --output " + remote + "/result 1>&2 && " + prefix + "operation-status --request-id " + id
                            + " --output " + remote + "/status 1>&2 && " + prefix + "operation-result --request-id " + id
                            + " --output " + remote + "/verified 1>&2 && cat " + remote + "/status && printf '\\n' && cat " + remote + "/verified";
                    }
                    progress(action == "list" ? "Получение списка друзей с VPS. Ожидание — до 5 минут; его можно отменить. Доступ друзей не изменяется."
                        : issuance ? "Выдача доступа другу. Запрос сохранён до SSH. Можно прервать ожидание; затем проверяйте прежнюю выдачу без нового приглашения."
                        : setup ? "Настройка VPS. ID запроса сохранён до запуска SSH. При потере ответа проверьте прежнюю настройку вместо повторной выдачи доступа."
                        : (action == "revoke" ? "Отзыв выбранного доступа" : action == "repair" ? "Восстановление правил выхода VPN" : "Настройка HTTPS-выдачи профилей")
                            + ". Ожидание — до 5 минут; его можно прервать. Команда VPS могла применить изменения или продолжать работу. ProGo не повторяет её автоматически; проверьте VPS перед повтором.");
                    // A real console remains available for OpenSSH password/host-key prompts.
                    // Only stdout goes to a private local file; the token is never a command argument.
                    remoteStarted = true;
                    await commandTransport("ssh.exe", ssh + Argument(command), output);
                    if (issuance) {
                        string framed = ReadAdminOutput(output, true); int split = framed.IndexOf('\n');
                        if (split < 1) throw new HomeVpnAdminPendingException(HomeVpnAdminRecovery.PendingMessage);
                        HomeVpnAdminRecovery.RequireCompleted(framed.Substring(0, split).Trim(), adminRequest);
                        return HomeVpnAdminRecovery.RetainResult(owner, adminRequest, framed.Substring(split + 1).Trim());
                    }
                    var result = setup ? ReadSetupOutput(output) : ReadCommandOutput(output);
                    if (result.Length == 0 || result.Length > 32768) throw new InvalidOperationException("VPS не вернул результат.");
                    return setup ? HomeVpnSetupRecovery.RetainResult(owner, request, result) : result;
                }
                catch (HomeVpnPreparationCancelledException) {
                    if (checking) throw new HomeVpnPreparationCancelledException(true);
                    throw;
                }
                catch (HomeVpnSetupPendingException) { throw; }
                catch (HomeVpnAdminPendingException) { throw; }
                catch (HomeVpnOwnerUnconfirmedException) { throw; }
                catch (Exception ex) {
                    if (!(ex is OutOfMemoryException) && issuance && adminRequest != null) throw new HomeVpnAdminPendingException(HomeVpnAdminRecovery.PendingMessage);
                    if (!(ex is OutOfMemoryException) && !setup && !issuance && action != "list" && remoteStarted) throw new HomeVpnOwnerUnconfirmedException();
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
        private static string ReadAdminOutput(string path, bool framed)
        {
            if (new FileInfo(path).Length > (framed ? 65536 : 32768)) throw new HomeVpnAdminPendingException(HomeVpnAdminRecovery.PendingMessage);
            return File.ReadAllText(path, new UTF8Encoding(false, true)).Trim();
        }
        private static string ReadCommandOutput(string path)
        {
            if (new FileInfo(path).Length > 32768) throw new InvalidOperationException("VPS вернул слишком большой результат.");
            return File.ReadAllText(path, new UTF8Encoding(false, true)).Trim();
        }

        private static string Shell(string value) { return "'" + value.Replace("'", "'\"'\"'") + "'"; }

        public void Dispose() { disposed = true; Stop(); }
    }
}
