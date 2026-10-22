using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ProGo
{
    // Relays opaque IKE/ESP packets. strongSwan on the VPS authenticates the phone.
    // This is deliberately separate from the local HTTP proxy and never opens SOCKS to the LAN.
    internal sealed class Ikev2RelayService : IDisposable
    {
        public const int IkePort = 15000;
        public const int NatPort = 14500;
        public const int BridgePort = 17878;
        internal const int MaxDatagram = 65507;
        private const int MaxFlows = 32;
        private const int MaxFlowsPerAddress = 8;
        private const int MaxQueuedBytes = 262144;
        private static readonly byte[] Magic = { 80, 71, 73, 75, 1 };
        private readonly object sync = new object();
        private Run current;
        private readonly List<Task> retiringRuns = new List<Task>();
        internal int PendingStopCount { get { lock (sync) { int count = 0; foreach (var work in retiringRuns) if (!work.IsCompleted) count++; return count; } } }
        private bool disposed;
        private Task startupWork = Task.FromResult(0);
        private Task stopWork = Task.FromResult(0);
        internal Task StopWork { get { lock (sync) return stopWork; } }
        internal Action BeforeStopComplete;
        private CancellationTokenSource startup;
        private string lastError;
        public bool IsRunning { get { lock (sync) return current != null; } }
        public string LastError { get { lock (sync) return lastError; } }
        public long Received { get { lock (sync) return current == null ? 0 : Interlocked.Read(ref current.Received); } }
        public long Sent { get { lock (sync) return current == null ? 0 : Interlocked.Read(ref current.Sent); } }
        public long Returned { get { lock (sync) return current == null ? 0 : Interlocked.Read(ref current.Returned); } }
        public long Dropped { get { lock (sync) return current == null ? 0 : Interlocked.Read(ref current.Dropped); } }

        public async Task StartAsync(string socksHost, int socksPort)
        {
            await StartAsync(socksHost, socksPort, IkePort, NatPort, BridgePort).ConfigureAwait(false);
        }

        // Port injection also allows real socket integration tests without privileged ports.
        internal async Task StartAsync(string socksHost, int socksPort, int ikePort, int natPort, int bridgePort)
        {
            await StartAsync(socksHost, socksPort, ikePort, natPort, bridgePort, CancellationToken.None).ConfigureAwait(false);
        }

        internal Task StartAsync(string socksHost, int socksPort, int ikePort, int natPort, int bridgePort, CancellationToken token)
        {
            if (token.IsCancellationRequested) return FailedStart(new OperationCanceledException(token));
            IPAddress address;
            if (String.Equals(socksHost, "localhost", StringComparison.OrdinalIgnoreCase)) address = IPAddress.Loopback;
            else if (!IPAddress.TryParse(socksHost, out address) || !IPAddress.IsLoopback(address))
                return FailedStart(new InvalidOperationException("Для этого режима нужен локальный SSH/SOCKS-туннель ProGo."));
            CancellationTokenSource source;
            TaskCompletionSource<int> completion;
            lock (sync)
            {
                if (disposed) return FailedStart(new ObjectDisposedException("Ikev2RelayService"));
                if (!stopWork.IsCompleted) return FailedStart(new InvalidOperationException("Остановка пересылки ещё не завершена. Дождитесь очистки и повторите запуск."));
                foreach (var retired in retiringRuns)
                    if (retired.Status != TaskStatus.RanToCompletion)
                        return FailedStart(new InvalidOperationException("Предыдущая пересылка ещё не очищена. Повторите остановку VPN для телефона перед новым запуском."));
                if (!startupWork.IsCompleted) return startupWork;
                if (current != null) return Task.FromResult(0);
                source = CancellationTokenSource.CreateLinkedTokenSource(token);
                completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                startup = source; startupWork = completion.Task; lastError = null;
            }
            Task.Run(() => StartOwnedAsync(address, socksPort, ikePort, natPort, bridgePort, source, completion));
            return completion.Task;
        }

        private static Task FailedStart(Exception failure)
        {
            var result = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (failure is OperationCanceledException) result.SetCanceled(); else result.SetException(failure);
            return result.Task;
        }

        private void CheckStartupAdmission(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            lock (sync) if (disposed || !stopWork.IsCompleted) throw new OperationCanceledException(token);
        }

        private async Task StartOwnedAsync(IPAddress address, int socksPort, int ikePort, int natPort, int bridgePort,
            CancellationTokenSource source, TaskCompletionSource<int> completion)
        {
            Run run = null; Exception failure = null;
            try
            {
                try
                {
                    CheckStartupAdmission(source.Token);
                    using (var probe = await ConnectAsync(address, socksPort, bridgePort, 0, source.Token).ConfigureAwait(false)) { }
                    CheckStartupAdmission(source.Token);
                    run = new Run(this, address, socksPort, bridgePort, ikePort, natPort);
                    lock (sync)
                    {
                        CheckStartupAdmission(source.Token);
                        current = run;
                    }
                    run.Start(); // Native socket/timer work does not hold the service metadata gate.
                    lock (sync)
                    {
                        CheckStartupAdmission(source.Token);
                        if (current != run) throw new IOException("Relay receiver stopped during startup.");
                    }
                }
                catch (Exception ex) { failure = ex; }
                // .NET Framework's C# 5 compiler cannot await inside catch.
                // Keep failure cleanup inside the enclosing CTS lifetime.
                if (failure != null)
                {
                    bool cleanupFailed = false;
                    try { if (run != null) await RetireRun(run).ConfigureAwait(false); }
                    catch (Exception cleanup) { failure = cleanup; cleanupFailed = true; }
                    bool cancelled = source.IsCancellationRequested || failure is OperationCanceledException;
                    if (cancelled && !cleanupFailed) failure = new OperationCanceledException(source.Token);
                    lock (sync)
                    {
                        if (current == run) current = null;
                        lastError = cleanupFailed ? "Не удалось подтвердить очистку пересылки. Повторите остановку VPN для телефона."
                            : cancelled ? null : "Не удалось запустить пересылку. Проверьте SOCKS, приёмник на VPS и свободные UDP-порты ПК.";
                    }
                }
            }
            finally
            {
                lock (sync) { if (startup == source) startup = null; }
                source.Dispose();
            }
            if (failure is OperationCanceledException) completion.TrySetCanceled();
            else if (failure != null) completion.TrySetException(failure);
            else completion.TrySetResult(0);
        }

        internal Task StopAsync()
        {
            CancellationTokenSource source;
            Task startupSettlement;
            TaskCompletionSource<bool> completion;
            Action beforeComplete;
            lock (sync)
            {
                if (!stopWork.IsCompleted) return stopWork;
                source = startup; startupSettlement = startupWork; beforeComplete = BeforeStopComplete;
                completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                stopWork = completion.Task; // Admission is sealed before native cancellation starts.
            }
            Task.Run(async delegate {
                try {
                    if (source != null) try { source.Cancel(); } catch (ObjectDisposedException) { }
                    try { await startupSettlement.ConfigureAwait(false); } catch { /* Startup owns its result. */ }
                    Task[] pending;
                    lock (sync) {
                        if (current != null) { RetireRunLocked(current); current = null; }
                        pending = retiringRuns.ToArray();
                    }
                    await Task.WhenAll(pending).ConfigureAwait(false);
                    if (beforeComplete != null) beforeComplete();
                    completion.TrySetResult(true);
                }
                catch (Exception ex) { completion.TrySetException(ex); }
            });
            return completion.Task;
        }

        public void Stop() { StopAsync().GetAwaiter().GetResult(); }

        public void Dispose()
        {
            Task work;
            lock (sync) { if (disposed) return; disposed = true; work = StopAsync(); }
            work.ContinueWith(delegate(Task failed) { var ignored = failed.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private Task RetireRun(Run run) { lock (sync) return RetireRunLocked(run); }
        private Task RetireRunLocked(Run run)
        {
            var work = run.StopAsync();
            if (!retiringRuns.Contains(work))
            {
                retiringRuns.Add(work);
                work.ContinueWith(delegate(Task done) {
                    var ignored = done.Exception;
                    if (done.Status == TaskStatus.RanToCompletion) lock (sync) retiringRuns.Remove(done);
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            }
            return work;
        }

        private void StopIfCurrent(Run run)
        {
            // Register retirement before dropping current, so a concurrent
            // Stop cannot miss a self-stopping receiver's remaining work.
            lock (sync) { RetireRunLocked(run); if (current == run) current = null; }
        }

        private void Report(Run run)
        {
            lock (sync) if (current == run)
                lastError = "Прервалась связь с приёмником на VPS. Проверьте SSH/SOCKS и переподключите VPN на телефоне.";
        }

        private static async Task<TcpClient> ConnectAsync(IPAddress socks, int port, int bridgePort, byte channel, CancellationToken token)
        {
            var client = new TcpClient(socks.AddressFamily);
            try
            {
                client.NoDelay = true;
                using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token))
                using (deadline.Token.Register(delegate { client.Close(); }))
                {
                    deadline.CancelAfter(10000);
                    await client.ConnectAsync(socks, port).ConfigureAwait(false);
                    var stream = client.GetStream();
                    await stream.WriteAsync(new byte[] { 5, 1, 0 }, 0, 3, deadline.Token).ConfigureAwait(false);
                    var greeting = await ReadExactAsync(stream, 2, deadline.Token).ConfigureAwait(false);
                    if (greeting[0] != 5 || greeting[1] != 0) throw new IOException("SOCKS handshake failed.");
                    // Loopback is interpreted on the VPS by SSH, never as a direct Windows fallback.
                    var request = new byte[] { 5, 1, 0, 1, 127, 0, 0, 1, (byte)(bridgePort >> 8), (byte)bridgePort };
                    await stream.WriteAsync(request, 0, request.Length, deadline.Token).ConfigureAwait(false);
                    var reply = await ReadExactAsync(stream, 4, deadline.Token).ConfigureAwait(false);
                    if (reply[0] != 5 || reply[1] != 0 || reply[2] != 0) throw new IOException("SOCKS connect failed.");
                    int size;
                    if (reply[3] == 1) size = 4;
                    else if (reply[3] == 4) size = 16;
                    else if (reply[3] == 3) size = (await ReadExactAsync(stream, 1, deadline.Token).ConfigureAwait(false))[0];
                    else throw new IOException("Invalid SOCKS address.");
                    if (size == 0) throw new IOException("Invalid SOCKS address.");
                    await ReadExactAsync(stream, size + 2, deadline.Token).ConfigureAwait(false);
                    var hello = new byte[6];
                    Buffer.BlockCopy(Magic, 0, hello, 0, Magic.Length); hello[5] = channel;
                    await stream.WriteAsync(hello, 0, hello.Length, deadline.Token).ConfigureAwait(false);
                    var response = await ReadExactAsync(stream, hello.Length, deadline.Token).ConfigureAwait(false);
                    for (var i = 0; i < hello.Length; i++) if (hello[i] != response[i]) throw new IOException("Relay handshake failed.");
                    deadline.Token.ThrowIfCancellationRequested();
                }
                return client;
            }
            catch { client.Close(); throw; }
        }

        internal static async Task<byte[]> ReadExactAsync(Stream stream, int count, CancellationToken token)
        {
            var data = new byte[count]; var offset = 0;
            while (offset < count)
            {
                var read = await stream.ReadAsync(data, offset, count - offset, token).ConfigureAwait(false);
                if (read == 0) throw new EndOfStreamException();
                offset += read;
            }
            return data;
        }

        internal static bool IsIke(byte[] data, int offset)
        {
            if (data.Length - offset < 28 || data[offset + 17] != 0x20) return false;
            var length = ((uint)data[offset + 24] << 24) | ((uint)data[offset + 25] << 16)
                | ((uint)data[offset + 26] << 8) | data[offset + 27];
            return length == data.Length - offset;
        }

        private static bool IsIkeForChannel(byte[] data, byte channel)
        {
            return channel == 1 ? IsIke(data, 0) : data.Length >= 32 &&
                data[0] == 0 && data[1] == 0 && data[2] == 0 && data[3] == 0 && IsIke(data, 4);
        }

        private sealed class Run
        {
            internal long Received, Sent, Returned, Dropped;
            internal readonly CancellationTokenSource Cancel = new CancellationTokenSource();
            internal readonly IPAddress Socks;
            internal readonly int SocksPort, BackendPort;
            private readonly Ikev2RelayService owner;
            private readonly UdpClient ike, nat;
            private readonly object gate = new object();
            private readonly Dictionary<string, Flow> flows = new Dictionary<string, Flow>();
            private readonly List<Task> receivers = new List<Task>();
            private readonly HashSet<Task> flowWorkers = new HashSet<Task>();
            private readonly TaskCompletionSource<bool> stopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private int stopQueued;
            private System.Threading.Timer expiry;
            private DateTime nextWindow = DateTime.UtcNow;
            private int newFlows;

            internal Run(Ikev2RelayService owner, IPAddress socks, int socksPort, int backendPort, int ikePort, int natPort)
            {
                this.owner = owner; Socks = socks; SocksPort = socksPort; BackendPort = backendPort;
                ike = Bind(ikePort);
                try { nat = Bind(natPort); } catch { ike.Close(); throw; }
            }

            private static UdpClient Bind(int port)
            {
                var udp = new UdpClient(AddressFamily.InterNetwork);
                try
                {
                    udp.ExclusiveAddressUse = true;
                    udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        udp.Client.IOControl(unchecked((int)0x9800000C), new byte[4], null);
                    return udp;
                }
                catch { udp.Close(); throw; }
            }

            internal void Start()
            {
                lock (gate)
                {
                    if (Volatile.Read(ref stopQueued) != 0) return;
                    receivers.Add(ReceiveAsync(ike, 1)); receivers.Add(ReceiveAsync(nat, 2));
                    expiry = new System.Threading.Timer(delegate { Expire(); }, null, 1000, 1000);
                }
            }

            private void TrackFlow(Task work)
            {
                flowWorkers.Add(work); // Caller holds gate before any stop snapshot.
                work.ContinueWith(delegate(Task done) {
                    var ignored = done.Exception;
                    if (done.Status == TaskStatus.RanToCompletion) lock (gate) flowWorkers.Remove(done);
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            }

            internal Task StopAsync()
            {
                if (Interlocked.Exchange(ref stopQueued, 1) == 0)
                    Task.Run(async delegate {
                        try {
                            StopOwned();
                            Task[] pending;
                            lock (gate) {
                                var all = new List<Task>(receivers); all.AddRange(flowWorkers);
                                pending = all.ToArray();
                            }
                            await Task.WhenAll(pending).ConfigureAwait(false);
                            stopped.TrySetResult(true);
                        }
                        catch (Exception ex) { stopped.TrySetException(ex); }
                    });
                return stopped.Task;
            }

            private async Task ReceiveAsync(UdpClient socket, byte channel)
            {
                try
                {
                    while (!Cancel.IsCancellationRequested)
                    {
                        var packet = await socket.ReceiveAsync().ConfigureAwait(false);
                        Interlocked.Increment(ref Received);
                        var data = packet.Buffer;
                        if (data.Length == 0 || data.Length > MaxDatagram) { Interlocked.Increment(ref Dropped); continue; }
                        var key = channel + "/" + packet.RemoteEndPoint;
                        Flow flow;
                        lock (gate)
                        {
                            if (Cancel.IsCancellationRequested || Volatile.Read(ref stopQueued) != 0) return;
                            if (!flows.TryGetValue(key, out flow))
                            {
                                // Stray ESP/keepalives cannot allocate TCP connections. IKE starts a flow.
                                if (!IsIkeForChannel(data, channel) || !CanAdd(packet.RemoteEndPoint.Address))
                                { Interlocked.Increment(ref Dropped); continue; }
                                flow = new Flow(this, key, socket, packet.RemoteEndPoint, channel);
                                flows.Add(key, flow); newFlows++;
                                TrackFlow(flow.PumpAsync());
                            }
                        }
                        flow.Enqueue(data);
                    }
                }
                catch { if (!Cancel.IsCancellationRequested) { owner.Report(this); owner.StopIfCurrent(this); } }
            }

            private bool CanAdd(IPAddress address)
            {
                if (DateTime.UtcNow >= nextWindow) { nextWindow = DateTime.UtcNow.AddSeconds(1); newFlows = 0; }
                if (flows.Count >= MaxFlows || newFlows >= 4) return false;
                var count = 0;
                foreach (var flow in flows.Values) if (flow.Peer.Address.Equals(address)) count++;
                return count < MaxFlowsPerAddress;
            }

            private void Expire()
            {
                List<Flow> stale = new List<Flow>();
                lock (gate) foreach (var flow in flows.Values)
                    if (flow.Idle) stale.Add(flow);
                foreach (var flow in stale) flow.Close();
            }

            internal void Remove(Flow flow, string key)
            {
                lock (gate) flows.Remove(key);
            }

            private void StopOwned()
            {
                List<Flow> pending;
                lock (gate)
                {
                    if (Cancel.IsCancellationRequested) return;
                    Cancel.Cancel();
                    pending = new List<Flow>(flows.Values);
                }
                if (expiry != null) expiry.Dispose();
                ike.Close(); nat.Close();
                foreach (var flow in pending) flow.Close();
            }

            internal void Failure() { if (!Cancel.IsCancellationRequested) owner.Report(this); }
        }

        private sealed class Flow
        {
            internal readonly IPEndPoint Peer;
            private readonly Run run;
            private readonly string key;
            private readonly UdpClient socket;
            private readonly byte channel;
            private readonly object gate = new object();
            private readonly Queue<byte[]> queue = new Queue<byte[]>();
            private readonly SemaphoreSlim ready = new SemaphoreSlim(0);
            private readonly CancellationTokenSource cancel = new CancellationTokenSource();
            private TcpClient client;
            private int queuedBytes;
            private long lastPacket = DateTime.UtcNow.Ticks;
            internal bool Idle { get { return DateTime.UtcNow.Ticks - Interlocked.Read(ref lastPacket) > TimeSpan.FromSeconds(channel == 1 ? 60 : 300).Ticks; } }

            internal Flow(Run run, string key, UdpClient socket, IPEndPoint peer, byte channel)
            { this.run = run; this.key = key; this.socket = socket; Peer = peer; this.channel = channel; }

            internal void Enqueue(byte[] data)
            {
                lock (gate)
                {
                    if (cancel.IsCancellationRequested) return;
                    if (queuedBytes + data.Length > MaxQueuedBytes || queue.Count >= 128)
                    { Interlocked.Increment(ref run.Dropped); return; }
                    queue.Enqueue(data); queuedBytes += data.Length;
                    Interlocked.Exchange(ref lastPacket, DateTime.UtcNow.Ticks);
                    ready.Release();
                }
            }

            internal async Task PumpAsync()
            {
                Task send = null, receive = null;
                try
                {
                    var connected = await ConnectAsync(run.Socks, run.SocksPort, run.BackendPort, channel, cancel.Token).ConfigureAwait(false);
                    lock (gate)
                    {
                        if (cancel.IsCancellationRequested) { connected.Close(); return; }
                        client = connected;
                    }
                    send = SendAsync(); receive = ReceiveAsync();
                    var finished = await Task.WhenAny(send, receive).ConfigureAwait(false);
                    await finished.ConfigureAwait(false);
                }
                catch { if (!cancel.IsCancellationRequested) run.Failure(); }
                finally
                {
                    Close();
                }
                if (send != null) try { await send.ConfigureAwait(false); } catch { }
                if (receive != null) try { await receive.ConfigureAwait(false); } catch { }
            }

            private async Task SendAsync()
            {
                var stream = client.GetStream();
                while (true)
                {
                    await ready.WaitAsync(cancel.Token).ConfigureAwait(false);
                    byte[] data;
                    lock (gate) { data = queue.Dequeue(); queuedBytes -= data.Length; }
                    var frame = new byte[data.Length + 2];
                    frame[0] = (byte)(data.Length >> 8); frame[1] = (byte)data.Length;
                    Buffer.BlockCopy(data, 0, frame, 2, data.Length);
                    // .NET Framework stream cancellation does not always interrupt a pending write.
                    using (var deadline = new CancellationTokenSource(30000))
                    using (deadline.Token.Register(delegate { Close(); }))
                        await stream.WriteAsync(frame, 0, frame.Length, cancel.Token).ConfigureAwait(false);
                    Interlocked.Increment(ref run.Sent);
                }
            }

            private async Task ReceiveAsync()
            {
                var stream = client.GetStream();
                while (true)
                {
                    var header = await ReadExactAsync(stream, 2, cancel.Token).ConfigureAwait(false);
                    var size = (header[0] << 8) | header[1];
                    if (size == 0 || size > MaxDatagram) throw new IOException("Invalid relay frame.");
                    var data = await ReadExactAsync(stream, size, cancel.Token).ConfigureAwait(false);
                    await socket.SendAsync(data, data.Length, Peer).ConfigureAwait(false);
                    Interlocked.Increment(ref run.Returned);
                }
            }

            internal void Close()
            {
                lock (gate)
                {
                    if (cancel.IsCancellationRequested) return;
                    cancel.Cancel();
                    if (client != null) client.Close();
                }
                run.Remove(this, key);
            }
        }
    }
}
