using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ProGo
{
    internal sealed class HomeVpnLocalCleanupException : IOException
    {
        internal HomeVpnLocalCleanupException(string message) : base(message) { }
    }

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

    [Flags]
    internal enum HomeVpnStoredStateWarning { None = 0, Owner = 1, HomeAddress = 2, ShareOrigin = 4 }

    // Read-only preparation for the R09 startup route. No file, ACL or settings
    // writes occur here; service/UI publication remains an owned caller step.
    internal sealed class HomeVpnStoredState
    {
        private readonly HomeVpnAccess access;
        private readonly HomeVpnOwner owner;
        internal readonly string HomeAddress, ShareOrigin;
        internal readonly HomeVpnStoredStateWarning Warnings;
        private HomeVpnStoredState(HomeVpnAccess access, HomeVpnOwner owner, string home, string share, HomeVpnStoredStateWarning warnings)
        { this.access = access; this.owner = owner; HomeAddress = home; ShareOrigin = share; Warnings = warnings; }
        internal HomeVpnAccess CopyAccess()
        {
            if (access == null) return null;
            return new HomeVpnAccess { Version = access.Version, ServerId = access.ServerId, InviteId = access.InviteId,
                Host = access.Host, Port = access.Port, User = access.User, PrivateKey = access.PrivateKey,
                HostKey = access.HostKey, Ca = access.Ca, Password = access.Password, ShareUrl = access.ShareUrl };
        }
        internal HomeVpnOwner CopyOwner()
        {
            return owner == null ? null : new HomeVpnOwner { Host = owner.Host, Port = owner.Port,
                Login = owner.Login, KeyFile = owner.KeyFile };
        }
        internal static Task<HomeVpnStoredState> LoadAsync(CancellationToken cancellation, Action<string> beforeRead = null)
        {
            return Task.Run(() => Load(cancellation, beforeRead), cancellation);
        }
        internal static Task<string> LoadShareAsync(string serverId, string fallback, CancellationToken cancellation, Action<string> beforeRead)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(serverId ?? "", @"\A[0-9a-f]{32}\z"))
                throw new ArgumentException("Сначала проверьте сохранённый доступ к VPS.");
            return Task.Run(delegate {
                byte[] encrypted = null, current = null;
                try {
                    cancellation.ThrowIfCancellationRequested();
                    if (beforeRead != null) beforeRead("read");
                    cancellation.ThrowIfCancellationRequested();
                    encrypted = Read("share-" + serverId); string result = Decode(encrypted) ?? fallback;
                    if (beforeRead != null) beforeRead("verify");
                    cancellation.ThrowIfCancellationRequested(); current = Read("share-" + serverId);
                    if (!Same(encrypted, current)) throw new InvalidOperationException("Сохранённый HTTPS-адрес изменился во время чтения. Повторите проверку.");
                    cancellation.ThrowIfCancellationRequested(); return result;
                } finally {
                    if (encrypted != null) Array.Clear(encrypted, 0, encrypted.Length);
                    if (current != null) Array.Clear(current, 0, current.Length);
                }
            }, cancellation);
        }
        private static byte[] Read(string name)
        {
            string path = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat");
            try { return File.ReadAllBytes(path); }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }
        private static string Decode(byte[] encrypted)
        {
            if (encrypted == null) return null;
            byte[] plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            try { return Encoding.UTF8.GetString(plain); }
            finally { Array.Clear(plain, 0, plain.Length); }
        }
        private static bool Same(byte[] left, byte[] right)
        {
            if (left == null || right == null) return left == right;
            if (left.Length != right.Length) return false;
            for (int index = 0; index < left.Length; index++) if (left[index] != right[index]) return false;
            return true;
        }
        private static HomeVpnStoredState Load(CancellationToken cancellation, Action<string> beforeRead)
        {
            var files = new System.Collections.Generic.Dictionary<string, byte[]>();
            Func<string, byte[]> read = delegate(string name) {
                cancellation.ThrowIfCancellationRequested();
                if (beforeRead != null) beforeRead(name);
                cancellation.ThrowIfCancellationRequested();
                byte[] bytes = Read(name); files.Add(name, bytes);
                cancellation.ThrowIfCancellationRequested(); return bytes;
            };
            try
            {
                string token = Decode(read("access"));
                var access = token == null ? null : HomeVpnAccess.Parse(token);
                var warnings = HomeVpnStoredStateWarning.None;
                HomeVpnOwner owner = null; string home = "", share = null;
                try {
                    string ownerJson = Decode(read("owner"));
                    owner = ownerJson == null ? null : new JavaScriptSerializer().Deserialize<HomeVpnOwner>(ownerJson);
                } catch (OperationCanceledException) { throw; }
                catch (Exception ex) { if (ex is OutOfMemoryException) throw; warnings |= HomeVpnStoredStateWarning.Owner; }
                try { home = Decode(read("home-address")) ?? ""; }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { if (ex is OutOfMemoryException) throw; warnings |= HomeVpnStoredStateWarning.HomeAddress; }
                if (access != null) {
                    try { share = Decode(read("share-" + access.ServerId)) ?? access.ShareUrl; }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) { if (ex is OutOfMemoryException) throw; warnings |= HomeVpnStoredStateWarning.ShareOrigin; }
                }
                cancellation.ThrowIfCancellationRequested();
                if (beforeRead != null) beforeRead("verify");
                foreach (var pair in files)
                {
                    cancellation.ThrowIfCancellationRequested(); byte[] current = Read(pair.Key);
                    try {
                        if (!Same(pair.Value, current)) throw new InvalidOperationException("Сохранённый доступ изменился во время чтения. Повторите проверку; данные не перезаписаны.");
                    } finally { if (current != null) Array.Clear(current, 0, current.Length); }
                }
                cancellation.ThrowIfCancellationRequested();
                return new HomeVpnStoredState(access, owner, home, share, warnings);
            }
            finally { foreach (var value in files.Values) if (value != null) Array.Clear(value, 0, value.Length); }
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
            get {
                if (!backgroundInitialization) return Access == null ? null : HomeVpnPrivateFiles.Load("share-" + Access.ServerId) ?? Access.ShareUrl;
                if ((loadedWarnings & HomeVpnStoredStateWarning.ShareOrigin) != 0)
                    throw new InvalidOperationException("Сохранённый HTTPS-адрес недоступен. Повторите чтение после освобождения файла; прежние данные сохранены.");
                return loadedShareOrigin;
            }
        }
        // Compatibility callbacks still wait; the configure route migrates separately.
        internal void SetShareOrigin(string value)
        { SetShareOriginAsync(Access, value, CancellationToken.None).GetAwaiter().GetResult(); }

        private volatile ProxyService proxy;
        private volatile string sessionDirectory;
        private readonly object lifecycleGate = new object();
        private CancellationTokenSource starting;
        private CancellationTokenSource initializing, shareRefreshing, shareRefreshCancelled;
        private Task<bool> shareRefreshWork = Task.FromResult(true);
        private volatile HomeVpnAddressWrite addressWrite;
        private CancellationTokenSource addressSaving, addressSaveCancelled;
        private Task<bool> privateAddressWork = Task.FromResult(true);
        private string addressSaveValue, addressSaveServerId;
        private HomeVpnAccess addressSaveAccess;
        private bool addressIsShare, addressCommitAccepted;
        internal Task<bool> HomeAddressWork { get { lock (lifecycleGate) return privateAddressWork; } }
        internal Task<bool> PrivateAddressWork { get { lock (lifecycleGate) return privateAddressWork; } }
        internal bool IsSavingHomeAddress { get { lock (lifecycleGate) return !addressIsShare && !privateAddressWork.IsCompleted; } }
        internal bool IsSavingShareOrigin { get { lock (lifecycleGate) return addressIsShare && !privateAddressWork.IsCompleted; } }
        private long shareRevision;
        internal Task<bool> ShareRefreshWork { get { lock (lifecycleGate) return shareRefreshWork; } }
        private Task<bool> initializationWork = Task.FromResult(true);
        private readonly bool backgroundInitialization;
        private volatile bool initializationFailed;
        private volatile string loadedShareOrigin;
        private volatile HomeVpnStoredStateWarning loadedWarnings;
        private Task startWork = Task.FromResult(0);
        private Task<bool> stopWork = Task.FromResult(true);
        private bool disposed, startsPaused;
        // Fixture barriers surround real private I/O; no production replacement sink.
        internal Action<string> BeforeSessionIo;
        internal Task StartWork { get { lock (lifecycleGate) return startWork; } }
        internal Task<bool> StopWork { get { lock (lifecycleGate) return stopWork; } }
        internal Task<bool> InitializationWork { get { lock (lifecycleGate) return initializationWork; } }
        internal bool IsInitializing { get { lock (lifecycleGate) return !initializationWork.IsCompleted; } }
        internal bool InitializationFailed { get { return initializationFailed; } }
        internal bool IsStopping { get { lock (lifecycleGate) return !stopWork.IsCompleted; } }
        internal bool IsStarting { get { lock (lifecycleGate) return !startWork.IsCompleted; } }
        internal bool HasOwnedResources { get { return proxy != null || sessionDirectory != null || addressWrite != null; } }
        internal bool StartsPaused { get { lock (lifecycleGate) return startsPaused; } }
        internal void PauseStarts() { lock (lifecycleGate) startsPaused = true; }
        internal void ResumeStarts() { lock (lifecycleGate) { if (!disposed) startsPaused = false; } }
        private readonly string sshExecutable;
        private readonly int ikePort, natPort, bridgePort;
        internal volatile bool AutoRestart = true;
        internal string RecoveryStatus { get { var current = proxy; return current == null ? "Не запущен" : current.RecoveryStatus; } }

        internal HomeVpnService(Ikev2RelayService relay)
            : this(relay, OpenSshClient.Executable, Ikev2RelayService.IkePort, Ikev2RelayService.NatPort, Ikev2RelayService.BridgePort) { }

        internal HomeVpnService(Ikev2RelayService relay, bool initializeInBackground)
            : this(relay, OpenSshClient.Executable, Ikev2RelayService.IkePort, Ikev2RelayService.NatPort, Ikev2RelayService.BridgePort, initializeInBackground) { }

        // Isolated Windows fixtures use their own executable and unprivileged UDP ports.
        internal HomeVpnService(Ikev2RelayService relay, string sshExecutable, int ikePort, int natPort, int bridgePort)
            : this(relay, sshExecutable, ikePort, natPort, bridgePort, false) { }

        internal HomeVpnService(Ikev2RelayService relay, string sshExecutable, int ikePort, int natPort, int bridgePort,
            bool initializeInBackground, Action<string> beforeStoredRead = null)
        {
            Relay = relay; this.sshExecutable = sshExecutable;
            this.ikePort = ikePort; this.natPort = natPort; this.bridgePort = bridgePort;
            backgroundInitialization = initializeInBackground;
            if (initializeInBackground)
            {
                HomeAddress = "";
                var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var cancellation = new CancellationTokenSource();
                initializationWork = completion.Task; initializing = cancellation;
                Task.Run(() => InitializeOwnedAsync(cancellation, completion, beforeStoredRead));
                return;
            }
            try
            {
                var token = HomeVpnPrivateFiles.Load("access");
                if (token != null) Access = HomeVpnAccess.Parse(token);
                var owner = HomeVpnPrivateFiles.Load("owner");
                if (owner != null) Owner = new JavaScriptSerializer().Deserialize<HomeVpnOwner>(owner);
                HomeAddress = HomeVpnPrivateFiles.Load("home-address") ?? "";
            }
            catch { initializationFailed = true; initializationWork = Task.FromResult(false); SafeLog.Info("Saved home VPN access needs to be imported again."); }
        }

        private async Task InitializeOwnedAsync(CancellationTokenSource cancellation,
            TaskCompletionSource<bool> completion, Action<string> beforeStoredRead)
        {
            Exception failure = null;
            var warnings = HomeVpnStoredStateWarning.None;
            HomeVpnAccess access = null; HomeVpnOwner owner = null; string home = null, share = null;
            try {
                var state = await HomeVpnStoredState.LoadAsync(cancellation.Token, beforeStoredRead).ConfigureAwait(false);
                access = state.CopyAccess(); owner = state.CopyOwner(); home = state.HomeAddress; share = state.ShareOrigin; warnings = state.Warnings;
            } catch (Exception ex) { failure = ex; }
            lock (lifecycleGate)
            {
                if (disposed || cancellation.IsCancellationRequested) failure = new OperationCanceledException();
                if (failure == null) { Access = access; Owner = owner; HomeAddress = home; loadedShareOrigin = share; loadedWarnings = warnings; }
                initializationFailed = failure == null ? warnings != HomeVpnStoredStateWarning.None : !(failure is OperationCanceledException);
                if (initializing == cancellation) initializing = null;
            }
            cancellation.Dispose();
            if (failure is OperationCanceledException) completion.TrySetCanceled();
            else {
                if (failure != null) try { SafeLog.Info("Saved home VPN access needs to be imported again."); } catch { }
                completion.TrySetResult(failure == null && warnings == HomeVpnStoredStateWarning.None);
            }
        }

        internal Task<bool> RefreshShareOriginAsync(CancellationToken token, Action<string> beforeRead = null)
        {
            TaskCompletionSource<bool> completion;
            CancellationTokenSource cancellation;
            HomeVpnAccess access; string serverId, fallback; long revision;
            lock (lifecycleGate)
            {
                if (disposed || startsPaused || token.IsCancellationRequested || !initializationWork.IsCompleted || !stopWork.IsCompleted)
                {
                    var refused = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    if (token.IsCancellationRequested) refused.SetCanceled();
                    else refused.SetException(new InvalidOperationException("Дождитесь чтения или остановки VPN для телефона и повторите проверку адреса."));
                    return refused.Task;
                }
                if (!shareRefreshWork.IsCompleted) return shareRefreshWork;
                access = Access; if (access == null) return Task.FromResult(false);
                serverId = access.ServerId; fallback = access.ShareUrl; revision = shareRevision;
                completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                shareRefreshing = cancellation; shareRefreshWork = completion.Task;
            }
            Task.Run(() => RefreshShareOwnedAsync(access, serverId, fallback, revision, cancellation, completion, beforeRead));
            return completion.Task;
        }

        private async Task RefreshShareOwnedAsync(HomeVpnAccess access, string serverId, string fallback, long revision,
            CancellationTokenSource cancellation, TaskCompletionSource<bool> completion, Action<string> beforeRead)
        {
            Exception failure = null; string origin = null;
            try { origin = await HomeVpnStoredState.LoadShareAsync(serverId, fallback, cancellation.Token, beforeRead).ConfigureAwait(false); }
            catch (Exception ex) { failure = ex; }
            lock (lifecycleGate)
            {
                if (disposed || cancellation.IsCancellationRequested || shareRefreshCancelled == cancellation) failure = new OperationCanceledException();
                else if (!Object.ReferenceEquals(Access, access) || shareRevision != revision)
                    failure = new InvalidOperationException("Адрес или доступ уже изменён. Результат прежнего чтения не применён; повторите проверку.");
                else {
                    if (failure == null) { loadedShareOrigin = origin; loadedWarnings &= ~HomeVpnStoredStateWarning.ShareOrigin; }
                    else loadedWarnings |= HomeVpnStoredStateWarning.ShareOrigin;
                    initializationFailed = loadedWarnings != HomeVpnStoredStateWarning.None;
                }
                if (shareRefreshing == cancellation) shareRefreshing = null;
                if (shareRefreshCancelled == cancellation) shareRefreshCancelled = null;
            }
            cancellation.Dispose();
            if (failure is OperationCanceledException) completion.TrySetCanceled();
            else if (failure is InvalidOperationException) completion.TrySetException(failure);
            else completion.TrySetResult(failure == null);
        }

        internal void UseToken(string token, HomeVpnOwner owner)
        {
            initializationWork.GetAwaiter().GetResult(); // Compatibility importer; real async persistence remains R09.
            var access = HomeVpnAccess.Parse(token);
            if (!StopAsync().GetAwaiter().GetResult())
                throw new HomeVpnLocalCleanupException("Очистка прежнего канала VPN для телефона не завершена. Новый доступ не сохранён. Повторите остановку после освобождения временных файлов.");
            HomeVpnPrivateFiles.Save("access", token.Trim());
            HomeVpnPrivateFiles.Save("owner", new JavaScriptSerializer().Serialize(owner));
            Access = access; Owner = owner;
            if (backgroundInitialization) {
                loadedWarnings &= HomeVpnStoredStateWarning.HomeAddress;
                loadedShareOrigin = null;
                try { loadedShareOrigin = HomeVpnPrivateFiles.Load("share-" + access.ServerId) ?? access.ShareUrl; }
                catch (Exception ex) { if (ex is OutOfMemoryException) throw; loadedWarnings |= HomeVpnStoredStateWarning.ShareOrigin; }
                initializationFailed = loadedWarnings != HomeVpnStoredStateWarning.None;
            }
        }

        // Compatibility fixtures may wait synchronously; the wizard uses the owned async API.
        internal void SetHomeAddress(string address) { SetHomeAddressAsync(address, CancellationToken.None).GetAwaiter().GetResult(); }

        internal Task<bool> SetHomeAddressAsync(string address, CancellationToken token, Action<string> beforeIo = null)
        { return SetPrivateAddressAsync(address, null, false, token, beforeIo); }

        internal Task<bool> SetShareOriginAsync(HomeVpnAccess expectedAccess, string origin, CancellationToken token, Action<string> beforeIo = null)
        { return SetPrivateAddressAsync(origin, expectedAccess, true, token, beforeIo); }

        private Task<bool> SetPrivateAddressAsync(string value, HomeVpnAccess expectedAccess, bool share, CancellationToken token, Action<string> beforeIo)
        {
            HomeVpnAddressWrite work; string serverId = share && expectedAccess != null ? expectedAccess.ServerId : null;
            // Construction is validation and immutable capture only, without private I/O.
            try {
                if (share && expectedAccess == null) throw new InvalidOperationException("Сначала добавьте доступ к VPS.");
                work = share ? HomeVpnAddressWrite.ForShareOrigin(serverId, value) : new HomeVpnAddressWrite(value);
            } catch (Exception ex) { if (ex is OutOfMemoryException) throw; return FailedPrivateWrite(ex); }
            TaskCompletionSource<bool> completion; CancellationTokenSource cancellation; Task initial;
            lock (lifecycleGate)
            {
                if (token.IsCancellationRequested) return FailedPrivateWrite(new OperationCanceledException(token));
                if (disposed) return FailedPrivateWrite(new ObjectDisposedException("HomeVpnService"));
                if (startsPaused || !stopWork.IsCompleted) return FailedPrivateWrite(new InvalidOperationException("Дождитесь остановки или обслуживания перед сохранением адреса."));
                if (share && (!Object.ReferenceEquals(Access, expectedAccess) || !String.Equals(expectedAccess.ServerId, serverId, StringComparison.Ordinal)))
                    return FailedPrivateWrite(new InvalidOperationException("Доступ к VPS уже изменён. Повторите настройку HTTPS-адреса для выбранного сервера."));
                if (!privateAddressWork.IsCompleted) return addressIsShare == share && String.Equals(addressSaveValue, work.Value, StringComparison.Ordinal) &&
                    String.Equals(addressSaveServerId, serverId, StringComparison.Ordinal) && Object.ReferenceEquals(addressSaveAccess, expectedAccess)
                    ? privateAddressWork : FailedPrivateWrite(new InvalidOperationException("Предыдущее сохранение адреса ещё выполняется. Дождитесь результата и повторите изменение."));
                if (addressWrite != null) return FailedPrivateWrite(new HomeVpnLocalCleanupException("Временное сохранение адреса требует очистки. Освободите его файл и повторите остановку VPN для телефона."));
                initial = initializationWork;
                completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                addressWrite = work; addressSaveValue = work.Value; addressSaveServerId = serverId; addressSaveAccess = expectedAccess;
                addressIsShare = share; addressSaving = cancellation; addressCommitAccepted = false;
                privateAddressWork = completion.Task; // One home/share owner published before private filesystem/DPAPI work.
            }
            Task.Run(() => SavePrivateAddressOwnedAsync(work, initial, expectedAccess, serverId, share, cancellation, completion, beforeIo));
            return completion.Task;
        }
        private static Task<bool> FailedPrivateWrite(Exception failure)
        {
            var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (failure is OperationCanceledException) result.SetCanceled(); else result.SetException(failure);
            return result.Task;
        }
        private async Task SavePrivateAddressOwnedAsync(HomeVpnAddressWrite work, Task initial, HomeVpnAccess expectedAccess, string serverId, bool share,
            CancellationTokenSource cancellation, TaskCompletionSource<bool> completion, Action<string> beforeIo)
        {
            Exception failure = null; bool committed = false, cleaned = false; HomeVpnAccess access = null;
            try {
                await initial.ConfigureAwait(false);
                cancellation.Token.ThrowIfCancellationRequested();
                lock (lifecycleGate) {
                    if (disposed || addressSaveCancelled == cancellation) throw new OperationCanceledException();
                    access = Access;
                    if (share && (!Object.ReferenceEquals(access, expectedAccess) || !String.Equals(access.ServerId, serverId, StringComparison.Ordinal)))
                        throw new InvalidOperationException("Доступ к VPS уже изменён. Прежний HTTPS-адрес не заменён.");
                }
                work.Prepare(cancellation.Token, beforeIo);
                work.Commit(delegate {
                    lock (lifecycleGate) {
                        if (disposed || startsPaused || cancellation.IsCancellationRequested || addressSaveCancelled == cancellation ||
                            !Object.ReferenceEquals(addressWrite, work) || !Object.ReferenceEquals(Access, access) ||
                            (share && !String.Equals(access.ServerId, serverId, StringComparison.Ordinal))) return false;
                        addressCommitAccepted = true; return true;
                    }
                }, beforeIo);
                committed = true;
                lock (lifecycleGate) {
                    if (!disposed && Object.ReferenceEquals(addressWrite, work) && Object.ReferenceEquals(Access, access) &&
                        (!share || String.Equals(access.ServerId, serverId, StringComparison.Ordinal))) {
                        if (share) { shareRevision++; loadedShareOrigin = work.Value; }
                        else HomeAddress = work.Value;
                        if (backgroundInitialization) {
                            loadedWarnings &= ~(share ? HomeVpnStoredStateWarning.ShareOrigin : HomeVpnStoredStateWarning.HomeAddress);
                            initializationFailed = loadedWarnings != HomeVpnStoredStateWarning.None;
                        }
                    }
                }
            } catch (Exception ex) { failure = ex; }
            try { work.Dispose(); cleaned = true; }
            catch (Exception ex) {
                failure = new HomeVpnLocalCleanupException("Временный файл адреса остался под управлением ProGo. Освободите файл и повторите остановку VPN для телефона; новый запуск сохранения пока недоступен.");
                try { SafeLog.Info("Private address cleanup remains pending (" + ex.GetType().Name + ")."); } catch { }
            }
            lock (lifecycleGate) {
                if (Object.ReferenceEquals(addressWrite, work)) { if (cleaned) addressWrite = null; addressCommitAccepted = false; }
                if (addressSaving == cancellation) addressSaving = null;
                if (addressSaveCancelled == cancellation) addressSaveCancelled = null;
            }
            cancellation.Dispose();
            if (failure is OperationCanceledException) completion.TrySetCanceled();
            else if (failure != null) completion.TrySetException(failure);
            else completion.TrySetResult(committed);
        }

        internal Task StartAsync() { return StartAsync(CancellationToken.None); }

        internal Task StartAsync(CancellationToken token)
        {
            TaskCompletionSource<int> completion;
            CancellationTokenSource cancellation;
            HomeVpnAccess access;
            Action<string> beforeIo;
            lock (lifecycleGate)
            {
                if (token.IsCancellationRequested) return FailedStart(new OperationCanceledException(token));
                if (disposed) return FailedStart(new ObjectDisposedException("HomeVpnService"));
                if (startsPaused) return FailedStart(new InvalidOperationException("ProGo завершает работу или готовит обслуживание. Запуск VPN для телефона временно недоступен."));
                if (addressWrite != null && privateAddressWork.IsCompleted) return FailedStart(new HomeVpnLocalCleanupException("Очистка временного сохранения адреса не завершена. Повторите остановку VPN для телефона перед запуском."));
                if (Access == null && initializationWork.IsCompleted) return FailedStart(new InvalidOperationException("Сначала добавьте VPS или вставьте токен."));
                if (!stopWork.IsCompleted) return FailedStart(new InvalidOperationException("Остановка VPN для телефона ещё не завершена. Дождитесь очистки и повторите запуск."));
                if (!startWork.IsCompleted) return startWork;
                if (Relay.IsRunning) return Task.FromResult(0);
                var current = Access;
                access = current == null ? null : new HomeVpnAccess { ServerId = current.ServerId, Host = current.Host,
                    Port = current.Port, User = current.User, PrivateKey = current.PrivateKey, HostKey = current.HostKey };
                cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                starting = cancellation;
                startWork = completion.Task; // Publish ownership before any worker, file or child.
                beforeIo = BeforeSessionIo;
            }
            Task.Run(() => StartOwnedAsync(access, cancellation, beforeIo, completion));
            return completion.Task;
        }

        private static Task FailedStart(Exception failure)
        {
            var result = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (failure is OperationCanceledException) result.SetCanceled(); else result.SetException(failure);
            return result.Task;
        }

        private async Task StartOwnedAsync(HomeVpnAccess access, CancellationTokenSource cancellation,
            Action<string> beforeIo, TaskCompletionSource<int> completion)
        {
            Exception failure = null;
            try {
                await initializationWork.ConfigureAwait(false);
                cancellation.Token.ThrowIfCancellationRequested();
                if (access == null) {
                    lock (lifecycleGate) {
                        var current = Access;
                        if (current == null) throw new InvalidOperationException("Сначала добавьте VPS или вставьте токен.");
                        access = new HomeVpnAccess { ServerId = current.ServerId, Host = current.Host, Port = current.Port,
                            User = current.User, PrivateKey = current.PrivateKey, HostKey = current.HostKey };
                    }
                }
                await StartCoreAsync(access, cancellation.Token, beforeIo).ConfigureAwait(false);
            }
            catch (Exception ex) { failure = ex; }
            finally
            {
                lock (lifecycleGate) { if (starting == cancellation) starting = null; }
                cancellation.Dispose();
            }
            // Start completion means the worker and any failed-start cleanup settled.
            if (failure is OperationCanceledException) completion.TrySetCanceled();
            else if (failure != null) completion.TrySetException(failure);
            else completion.TrySetResult(0);
        }

        private async Task StartCoreAsync(HomeVpnAccess access, CancellationToken token, Action<string> beforeIo)
        {
            token.ThrowIfCancellationRequested();
            if (!CleanupOwned(beforeIo))
                throw new InvalidOperationException("Предыдущий запуск канала ещё не очищен. Закройте программы, использующие временные файлы, и повторите остановку VPN для телефона.");
            try
            {
                SessionBoundary(beforeIo, "prepare", token);
                HomeVpnPrivateFiles.SecureDirectory(HomeVpnPrivateFiles.Root);
                token.ThrowIfCancellationRequested();
                sessionDirectory = Path.Combine(HomeVpnPrivateFiles.Root, "session-" + Guid.NewGuid().ToString("N"));
                HomeVpnPrivateFiles.SecureDirectory(sessionDirectory);
                var key = Path.Combine(sessionDirectory, "access");
                var known = Path.Combine(sessionDirectory, "known_hosts");
                var config = Path.Combine(sessionDirectory, "ssh_config");
                SessionBoundary(beforeIo, "key", token);
                File.WriteAllText(key, access.PrivateKey, new UTF8Encoding(false));
                SessionBoundary(beforeIo, "known-hosts", token);
                File.WriteAllText(known, "progo-home-" + access.ServerId + " " + access.HostKey + "\n", new UTF8Encoding(false));
                SessionBoundary(beforeIo, "config", token);
                File.WriteAllText(config, "Host progo-home\n HostName " + access.Host + "\n Port " + access.Port + "\n User " + access.User
                    + "\n IdentityFile " + ConfigPath(key) + "\n IdentitiesOnly yes\n IdentityAgent none\n BatchMode yes\n StrictHostKeyChecking yes"
                    + "\n HostKeyAlias progo-home-" + access.ServerId + "\n HostKeyAlgorithms ssh-ed25519\n UserKnownHostsFile " + ConfigPath(known)
                    + "\n GlobalKnownHostsFile NUL\n", new UTF8Encoding(false));
                token.ThrowIfCancellationRequested();
                int port;
                var reservation = new TcpListener(IPAddress.Loopback, 0); reservation.Start();
                try { port = ((IPEndPoint)reservation.LocalEndpoint).Port; } finally { reservation.Stop(); }
                var options = AppSettings.Defaults(); options.SocksPort = port; options.SshProfile = "progo-home";
                proxy = new ProxyService(delegate { options.AutoRestartSocks = AutoRestart; return options; }, delegate { }, sshExecutable, delegate { return DateTime.UtcNow; }, true,
                    "-F " + Argument(config) + " ", 10000);
                token.ThrowIfCancellationRequested();
                if (!await proxy.StartTunnelAsync(token).ConfigureAwait(false)) throw new InvalidOperationException("SOCKS не готов.");
                token.ThrowIfCancellationRequested();
                await Relay.StartAsync("127.0.0.1", port, ikePort, natPort, bridgePort, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
            }
            catch
            {
                if (!CleanupOwned(beforeIo))
                    throw new InvalidOperationException("Канал остановлен не полностью: процесс или временные файлы доступа ещё принадлежат ProGo. Повторите остановку VPN для телефона; отмена пока не подтверждена.");
                token.ThrowIfCancellationRequested();
                throw new InvalidOperationException("Канал к VPS не готов. Проверьте интернет и токен: владелец мог отозвать доступ или изменить SSH-ключ сервера. Можно повторить запуск.");
            }
        }

        private static void SessionBoundary(Action<string> beforeIo, string stage, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (beforeIo != null) beforeIo(stage);
            token.ThrowIfCancellationRequested();
        }

        // Only the retained start worker, or the stop worker after awaiting it,
        // may mutate this session/proxy. No process/filesystem work holds lifecycleGate.
        private bool CleanupOwned(Action<string> beforeIo, bool includePrivateWrite = false)
        {
            Relay.StopAsync().GetAwaiter().GetResult(); // This method belongs to an owned worker.
            if (proxy != null)
            {
                proxy.Dispose();
                if (proxy.HasOwnedProcess) return false;
                proxy = null;
            }
            if (sessionDirectory != null)
            {
                if (beforeIo != null) beforeIo("cleanup");
                try { if (Directory.Exists(sessionDirectory)) Directory.Delete(sessionDirectory, true); }
                catch { return false; } // Keep the exact directory for explicit retry.
                sessionDirectory = null;
            }
            if (includePrivateWrite) {
                var pendingAddress = addressWrite;
                if (pendingAddress != null) {
                    try { pendingAddress.Dispose(); } catch { return false; }
                    lock (lifecycleGate) { if (Object.ReferenceEquals(addressWrite, pendingAddress)) addressWrite = null; }
                }
            }
            return true;
        }

        internal Task<bool> StopAsync()
        {
            Task start, initial, shareRead, addressWork;
            CancellationTokenSource cancellation, shareCancellation, addressCancellation;
            TaskCompletionSource<bool> completion;
            Action<string> beforeIo;
            lock (lifecycleGate)
            {
                if (!stopWork.IsCompleted) return stopWork;
                start = startWork; cancellation = starting; beforeIo = BeforeSessionIo;
                initial = initializationWork; shareRead = shareRefreshWork; shareCancellation = shareRefreshing;
                shareRefreshCancelled = shareCancellation; // Admission is cancelled before the worker can publish; native cancellation stays off UI.
                addressWork = privateAddressWork; addressCancellation = addressCommitAccepted ? null : addressSaving;
                addressSaveCancelled = addressCancellation; // A previously accepted atomic save remains owned until completion.
                completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                stopWork = completion.Task; // Duplicate Stop and Start see the reservation immediately.
            }
            Task.Run(async delegate {
                try
                {
                    // Cancellation callbacks and native disposal also belong to a worker.
                    if (cancellation != null) try { cancellation.Cancel(); } catch (ObjectDisposedException) { }
                    if (shareCancellation != null) try { shareCancellation.Cancel(); } catch (ObjectDisposedException) { }
                    if (addressCancellation != null) try { addressCancellation.Cancel(); } catch (ObjectDisposedException) { }
                    try { await start.ConfigureAwait(false); } catch { /* Start already reports its own failure. */ }
                    try { await initial.ConfigureAwait(false); } catch { /* Read outcome remains observable through InitializationWork. */ }
                    try { await shareRead.ConfigureAwait(false); } catch { /* Read failure remains on ShareRefreshWork; cleanup still owns the actual worker. */ }
                    try { await addressWork.ConfigureAwait(false); } catch { /* Actual write cleanup remains inspectable and is retried below. */ }
                    completion.TrySetResult(CleanupOwned(beforeIo, true));
                }
                catch (Exception ex) { completion.TrySetException(ex); }
            });
            return completion.Task;
        }

        // Synchronous compatibility; the token-import caller is an open R09 UI follow-up.
        internal void Stop() { StopAsync().GetAwaiter().GetResult(); }

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
                        action == "recover-setup" || action == "recover-invite" || action == "recover-share" || (action == "setup" && HomeVpnSetupRecovery.HasPending())), null, HomeVpnPreparationProcess.TimeoutMs);
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
                if (action == "share" || action == "recover-share")
                    return HomeVpnPreparationForm.WaitForShareAsync(System.Windows.Forms.Form.ActiveForm, executable, arguments, output, timeoutMs);
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
            bool sharing = action == "share" || action == "recover-share";
            if (!setup && !issuance && !sharing && action != "list" && action != "revoke" && action != "repair") throw new ArgumentException("Unknown action");
            if (action == "recover-invite" && (label != null || identifier != null)) throw new ArgumentException("Проверка использует только параметры сохранённой выдачи.");
            if (sharing && (label != null || (action == "recover-share" && identifier != null))) throw new ArgumentException("Проверка HTTPS использует только параметры сохранённого запроса.");
            if (action == "revoke" && !System.Text.RegularExpressions.Regex.IsMatch(identifier ?? "", @"\A[0-9a-f]{24}\z")) throw new ArgumentException("Invalid invitation");
            if ((label ?? "").Length > 80 || (label ?? "").IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0) throw new ArgumentException("Название слишком длинное.");
            if (setup && Array.Exists((label ?? "").ToCharArray(), Char.IsControl)) throw new ArgumentException("Проверьте название доступа: без управляющих знаков.");
            if (action == "share") identifier = new Uri(HomeProfileShare.Origin(identifier)).Host;
            using (var lease = setup ? await HomeVpnSetupRecovery.AcquireAsync() : issuance ? await HomeVpnAdminRecovery.AcquireAsync() : sharing ? await HomeVpnShareRecovery.AcquireAsync() : null)
            {
                var request = setup ? HomeVpnSetupRecovery.Load(owner, label) : null;
                var adminRequest = action == "recover-invite" ? HomeVpnAdminRecovery.Load(owner) : null;
                var shareRequest = action == "recover-share" ? HomeVpnShareRecovery.Load(owner) : null;
                bool checking = request != null || adminRequest != null || shareRequest != null;
                if (action == "recover-setup" && !checking) throw new HomeVpnSetupPendingException("Сохранённый запрос настройки отсутствует. Новая команда не запускалась.");
                if (action == "recover-invite" && adminRequest == null) throw new HomeVpnAdminPendingException("Сохранённый запрос выдачи отсутствует. Новая выдача не запускалась.");
                if (action == "invite" && HomeVpnAdminRecovery.HasPending()) throw new HomeVpnAdminPendingException(HomeVpnAdminRecovery.PendingMessage);
                if (action == "recover-share" && shareRequest == null) throw new HomeVpnSharePendingException("Сохранённый HTTPS-запрос отсутствует. Новая настройка не запускалась.");
                if (action == "share" && HomeVpnShareRecovery.HasPending()) throw new HomeVpnSharePendingException(HomeVpnShareRecovery.PendingMessage);
                HomeVpnPrivateFiles.SecureDirectory(HomeVpnPrivateFiles.Root);
                var work = Path.Combine(HomeVpnPrivateFiles.Root, "admin-" + Guid.NewGuid().ToString("N"));
                HomeVpnPrivateFiles.SecureDirectory(work);
                var name = "progo-" + Guid.NewGuid().ToString("N");
                var upload = Path.Combine(work, name); Directory.CreateDirectory(upload);
                var output = Path.Combine(work, "result.txt");
                bool remoteStarted = false;
                return await CompleteAdminWorkAsync(work, action, checking, () => remoteStarted, async delegate {
                try
                {
                    foreach (var file in new[] { "home_vpn_setup.py", "ikev2_relay.py", "install-ikev2-relay.sh", "profile_share_setup.py", "profile_share.py", "qrcodegen.py", "QR_LICENSE.txt" })
                        File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "home-vpn", "server", file), Path.Combine(upload, file));
                    var keyArgs = String.IsNullOrWhiteSpace(owner.KeyFile) ? "" : " -i " + Argument(owner.KeyFile);
                    var target = owner.Login + "@" + owner.Host;
                    progress(action == "list" ? "Подготовка получения списка друзей. Существующий доступ и текущий список сохраняются; новые токены не создаются."
                        : sharing && checking ? "Подготовка проверки прежней настройки HTTPS. Запрос сохранён; новая настройка не запускается."
                        : issuance && checking ? "Подготовка проверки прежней выдачи доступа. Запрос сохранён; новое приглашение и повторный отзыв не запускаются."
                        : checking ? "Подготовка проверки прежней настройки VPS. Предыдущая команда могла завершиться; повторной выдачи доступа не будет."
                        : "Копирование помощника на VPS. Если SSH спросит пароль или подтверждение ключа, ответьте в открывшемся окне.");
                    await copy("scp.exe", "-o ConnectTimeout=15 -P " + owner.Port + keyArgs + " -r " + Argument(upload) + " " + Argument(target + ":/tmp/"));
                    var remote = "/tmp/" + name;
                    if (setup && !checking) request = HomeVpnSetupRecovery.Register(owner, label);
                    if (issuance && !checking) adminRequest = HomeVpnAdminRecovery.Register(owner, label, identifier);
                    if (sharing && !checking) shareRequest = HomeVpnShareRecovery.Register(owner, identifier);
                    var prefix = (owner.Login == "root" ? "" : "sudo -n ") + "python3 -I " + remote + "/home_vpn_setup.py ";
                    var options = " --host " + Shell(owner.Host) + " --port " + owner.Port + " --name " + Shell(label ?? "My iPhone")
                        + (action == "revoke" ? " --id " + identifier : action == "share" ? " --domain " + Shell(identifier) : "")
                        + (setup ? " --request-id " + request.RequestId : "");
                    var ssh = "-o ConnectTimeout=15 -T -p " + owner.Port + keyArgs + " " + Argument(target) + " ";
                    if (sharing && checking) {
                        progress("Проверяем сохранённый HTTPS-запрос. Новая настройка домена не запускается."); remoteStarted = true;
                        await commandTransport("ssh.exe", ssh + Argument("trap 'rm -rf -- " + remote + "' EXIT; " + prefix + "operation-status --request-id " + shareRequest.RequestId
                            + " --output " + remote + "/status 1>&2 && cat " + remote + "/status"), output);
                        HomeVpnShareRecovery.RequireCompleted(ReadShareOutput(output, false), shareRequest);
                        progress("Прежняя настройка HTTPS завершена. Получаем её исходный адрес без новой настройки.");
                        await copy("scp.exe", "-o ConnectTimeout=15 -P " + owner.Port + keyArgs + " -r " + Argument(upload) + " " + Argument(target + ":/tmp/"));
                        var recovered = Path.Combine(work, "recovered.txt");
                        await commandTransport("ssh.exe", ssh + Argument("trap 'rm -rf -- " + remote + "' EXIT; " + prefix
                            + "operation-result --request-id " + shareRequest.RequestId + " --output " + remote + "/result 1>&2 && cat " + remote + "/result"), recovered);
                        return HomeVpnShareRecovery.RequireResult(owner, shareRequest, ReadShareOutput(recovered, false));
                    }
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
                    if (sharing) {
                        var id = shareRequest.RequestId;
                        command = "trap 'rm -rf -- " + remote + "' EXIT; " + prefix + "share" + options
                            + " --request-id " + id + " --output " + remote + "/result 1>&2 && " + prefix + "operation-status --request-id " + id
                            + " --output " + remote + "/status 1>&2 && " + prefix + "operation-result --request-id " + id
                            + " --output " + remote + "/verified 1>&2 && cat " + remote + "/status && printf '\\n' && cat " + remote + "/verified";
                    }
                    progress(action == "list" ? "Получение списка друзей с VPS. Ожидание — до 5 минут; его можно отменить. Доступ друзей не изменяется."
                        : sharing ? "Настройка HTTPS. Запрос сохранён до SSH. Можно прервать ожидание; затем проверяйте прежнюю настройку без новой команды."
                        : issuance ? "Выдача доступа другу. Запрос сохранён до SSH. Можно прервать ожидание; затем проверяйте прежнюю выдачу без нового приглашения."
                        : setup ? "Настройка VPS. ID запроса сохранён до запуска SSH. При потере ответа проверьте прежнюю настройку вместо повторной выдачи доступа."
                        : (action == "revoke" ? "Отзыв выбранного доступа" : action == "repair" ? "Восстановление правил выхода VPN" : "Настройка HTTPS-выдачи профилей")
                            + ". Ожидание — до 5 минут; его можно прервать. Команда VPS могла применить изменения или продолжать работу. ProGo не повторяет её автоматически; проверьте VPS перед повтором.");
                    // A real console remains available for OpenSSH password/host-key prompts.
                    // Only stdout goes to a private local file; the token is never a command argument.
                    remoteStarted = true;
                    await commandTransport("ssh.exe", ssh + Argument(command), output);
                    if (sharing) {
                        string framed = ReadShareOutput(output, true); int split = framed.IndexOf('\n');
                        if (split < 1) throw new HomeVpnSharePendingException(HomeVpnShareRecovery.PendingMessage);
                        HomeVpnShareRecovery.RequireCompleted(framed.Substring(0, split).Trim(), shareRequest);
                        return HomeVpnShareRecovery.RequireResult(owner, shareRequest, framed.Substring(split + 1).Trim());
                    }
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
                catch (HomeVpnSharePendingException) { throw; }
                catch (HomeVpnOwnerUnconfirmedException) { throw; }
                catch (Exception ex) {
                    if (!(ex is OutOfMemoryException) && sharing && shareRequest != null) throw new HomeVpnSharePendingException(HomeVpnShareRecovery.PendingMessage);
                    if (!(ex is OutOfMemoryException) && issuance && adminRequest != null) throw new HomeVpnAdminPendingException(HomeVpnAdminRecovery.PendingMessage);
                    if (!(ex is OutOfMemoryException) && !setup && !issuance && action != "list" && remoteStarted) throw new HomeVpnOwnerUnconfirmedException();
                    if (ex is OutOfMemoryException || !setup || request == null) throw;
                    throw new HomeVpnSetupPendingException(HomeVpnSetupRecovery.PendingMessage);
                }
                });
            }
        }

        private static async Task<string> CompleteAdminWorkAsync(string work, string action, bool checking, Func<bool> remoteStarted, Func<Task<string>> operation)
        {
            string result = null; ExceptionDispatchInfo failure = null;
            try { result = await operation(); }
            catch (Exception error) { failure = ExceptionDispatchInfo.Capture(error); }
            // C# 5 cannot await inside finally. Keep the recovery lease above
            // until this worker settles, then preserve the original outcome.
            bool started = remoteStarted();
            await Task.Run(() => DeleteAdminWork(work, action, checking, started));
            if (failure != null) failure.Throw();
            return result;
        }

        private static void DeleteAdminWork(string work, string action, bool checking, bool remoteStarted)
        {
            var watch = Stopwatch.StartNew();
            while (true) {
                try { Directory.Delete(work, true); return; }
                catch (DirectoryNotFoundException) { if (!Directory.Exists(work)) return; }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                long remaining = 1000 - watch.ElapsedMilliseconds;
                if (remaining <= 0) break;
                Thread.Sleep((int)Math.Min(50, remaining));
            }
            const string cleanup = "Локальные файлы команды VPS не удалось удалить. Завершение локальной очистки пока не подтверждено. Закройте программы, использующие эти файлы. ";
            if (action == "list") throw new HomeVpnLocalCleanupException("Локальные файлы получения списка не удалось удалить. Существующий доступ, текущий список и введённые данные сохранены; завершение очистки пока не подтверждено. Закройте программы, использующие эти файлы, и повторите получение списка.");
            if (remoteStarted) {
                if (action == "setup" || action == "recover-setup") throw new HomeVpnSetupPendingException(cleanup + HomeVpnSetupRecovery.PendingMessage);
                if (action == "invite" || action == "recover-invite") throw new HomeVpnAdminPendingException(cleanup + HomeVpnAdminRecovery.PendingMessage);
                if (action == "share" || action == "recover-share") throw new HomeVpnSharePendingException(cleanup + HomeVpnShareRecovery.PendingMessage);
                throw new HomeVpnOwnerUnconfirmedException(cleanup + HomeVpnOwnerUnconfirmedException.UnconfirmedMessage);
            }
            throw new HomeVpnLocalCleanupException(checking
                ? "Подготовка проверки прервана, но локальные файлы не удалось удалить. Прежний запрос сохранён; его результат не подтверждён. Закройте программы, использующие эти файлы."
                : "Команда VPS не запускалась, но локальные файлы подготовки не удалось удалить. Закройте программы, использующие эти файлы; завершение подготовки пока не подтверждено.");
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
        private static string ReadShareOutput(string path, bool framed)
        {
            if (new FileInfo(path).Length > (framed ? 65536 : 32768)) throw new HomeVpnSharePendingException(HomeVpnShareRecovery.PendingMessage);
            return File.ReadAllText(path, new UTF8Encoding(false, true)).Trim();
        }

        private static string Shell(string value) { return "'" + value.Replace("'", "'\"'\"'") + "'"; }

        public void Dispose()
        {
            Task<bool> work;
            CancellationTokenSource initialCancellation, shareCancellation, addressCancellation;
            lock (lifecycleGate)
            {
                if (disposed) return;
                disposed = true;
                initialCancellation = initializing; shareCancellation = shareRefreshing;
                addressCancellation = addressCommitAccepted ? null : addressSaving;
                if (addressCancellation != null) addressSaveCancelled = addressCancellation;
                work = StopAsync(); // Reentrant metadata gate; publish before Dispose returns.
            }
            // This private source has only owned read scheduling/checkpoints,
            // never user/native cancellation callbacks. Cancel even if an
            // earlier Stop already awaits the read; actual I/O remains owned.
            if (initialCancellation != null) try { initialCancellation.Cancel(); } catch (ObjectDisposedException) { }
            if (shareCancellation != null) try { shareCancellation.Cancel(); } catch (ObjectDisposedException) { }
            if (addressCancellation != null) try { addressCancellation.Cancel(); } catch (ObjectDisposedException) { }
            // Do not wait for an uninterruptible native I/O call on the UI thread.
            // The task and exact session remain owned and inspectable through StopWork.
            work.ContinueWith(delegate(Task<bool> failed) { var ignored = failed.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
