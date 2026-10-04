using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ProGo
{
    // The lifetime mutex is held by the startup/UI thread, before any settings,
    // backups or proxy services are created. IPC supports window activation and confirmed owned-proxy cleanup.
    internal sealed class ApplicationInstance : IDisposable
    {
        private readonly System.Threading.Mutex mutex;
        private readonly string pipeName;
        private readonly SecurityIdentifier user;
        private readonly object gate = new object();
        private readonly System.Threading.Thread receiver;
        private NamedPipeServerStream pipe;
        private Action activate, shutdownCompleted, shutdownCancelled;
        private Func<bool> prepareShutdown;
        private bool pendingActivation, stopping;
        internal bool IsOwner { get; private set; }

        internal ApplicationInstance()
        {
            user = WindowsIdentity.GetCurrent().User;
            if (user == null) throw new InvalidOperationException("Windows user identity is unavailable.");
            pipeName = "ProGo.Show." + user.Value;
            var security = new MutexSecurity();
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new MutexAccessRule(user, MutexRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), MutexRights.FullControl, AccessControlType.Allow));
            bool created;
            mutex = new System.Threading.Mutex(false, "Global\\ProGo.Instance." + user.Value, out created, security);
            try
            {
                try { IsOwner = mutex.WaitOne(0); }
                catch (System.Threading.AbandonedMutexException) { IsOwner = true; }
                if (!IsOwner) return;
                pipe = CreatePipe();
                receiver = new System.Threading.Thread(Receive) { IsBackground = true, Name = "ProGo activation" };
                receiver.Start();
            }
            catch
            {
                if (pipe != null) pipe.Dispose();
                if (IsOwner) mutex.ReleaseMutex();
                mutex.Dispose();
                throw;
            }
        }

        private NamedPipeServerStream CreatePipe()
        {
            var security = new PipeSecurity();
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new PipeAccessRule(user, PipeAccessRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
            return new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous, 16, 16, security);
        }

        internal void Attach(Action showWindow, Func<bool> shutdown = null, Action finishShutdown = null, Action cancelShutdown = null)
        {
            if (!IsOwner) throw new InvalidOperationException("Only the instance owner can attach its window.");
            bool show;
            lock (gate) { activate = showWindow; prepareShutdown = shutdown; shutdownCompleted = finishShutdown; shutdownCancelled = cancelShutdown; show = pendingActivation; pendingActivation = false; }
            if (show) showWindow();
        }

        internal bool RequestActivation()
        {
            if (IsOwner) throw new InvalidOperationException("The owner does not need a second activation connection.");
            try
            {
                using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    client.Connect(3000); client.WriteByte(1);
                    return ReadCommand(client) == 1;
                }
            }
            catch (IOException) { return false; }
            catch (TimeoutException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (AggregateException) { return false; }
        }

        private static int ReadCommand(Stream stream)
        {
            var buffer = new byte[1];
            var read = stream.ReadAsync(buffer, 0, 1);
            if (!read.Wait(1500)) return -1;
            return read.Result == 1 ? buffer[0] : -1;
        }

        private void Receive()
        {
            try { ReceiveLoop(); }
            catch (Exception ex)
            {
                lock (gate) { if (stopping) return; }
                // A broken activation channel must not bring down the proxy owner.
                try { SafeLog.Error("Window activation channel failed.", ex); } catch { }
            }
        }

        private void ReceiveLoop()
        {
            while (true)
            {
                NamedPipeServerStream current;
                lock (gate) { if (stopping) return; current = pipe; }
                try
                {
                    current.WaitForConnection();
                    int command = ReadCommand(current);
                    if (command == 1)
                    {
                        Action show;
                        lock (gate)
                        {
                            if (stopping) return;
                            show = activate;
                            if (show == null) pendingActivation = true;
                        }
                        if (show != null) show();
                        current.WriteByte(1);
                    }
                    else if (command == 2) {
                        Func<bool> prepare; Action finish, cancel;
                        lock (gate) { prepare = prepareShutdown; finish = shutdownCompleted; cancel = shutdownCancelled; }
                        bool cleaned = false;
                        try { cleaned = prepare != null && finish != null && prepare(); }
                        catch (Exception ex) { SafeLog.Error("Requested proxy cleanup failed.", ex); }
                        try { current.WriteByte(cleaned ? (byte)2 : (byte)0); current.Flush(); }
                        catch { if (cleaned && cancel != null) cancel(); throw; }
                        // Exit only after the requester receives cleanup confirmation.
                        if (cleaned) finish();
                    }
                    else current.WriteByte(0);
                }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
                catch (AggregateException) { }
                finally { current.Dispose(); }
                lock (gate)
                {
                    if (stopping) return;
                    pipe = CreatePipe();
                }
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (stopping) return;
                stopping = true; activate = null; prepareShutdown = null; shutdownCompleted = null; shutdownCancelled = null;
                if (pipe != null) pipe.Dispose();
            }
            if (receiver != null) receiver.Join(2000);
            if (IsOwner) mutex.ReleaseMutex();
            mutex.Dispose();
        }
    }
}
