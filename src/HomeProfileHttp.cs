using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProGo
{
    internal sealed class HomeProfileHttpCancelledException : OperationCanceledException
    {
        internal HomeProfileHttpCancelledException(string message) : base(message) { }
    }
    internal sealed class HomeProfileHttpFailureException : InvalidOperationException
    {
        internal HomeProfileHttpFailureException(string message) : base(message) { }
    }
    internal enum HomeProfileHttpPurpose { Verify, Create, Revoke }

    // One deadline spans every request in the operation, including health + POST.
    // Abort owns only this operation's current request, never a shared client.
    internal sealed class HomeProfileHttp : IDisposable
    {
        internal const int TimeoutMs = 20000;
        private readonly CancellationToken caller;
        private readonly CancellationTokenSource deadline = new CancellationTokenSource();
        private readonly CancellationTokenSource cancellation;
        private readonly Func<Uri, HttpWebRequest> factory;
        private readonly HomeProfileHttpPurpose purpose;
        private readonly Stopwatch elapsed = Stopwatch.StartNew();
        private readonly int timeoutMs;
        private bool mutationStarted;

        internal HomeProfileHttp(HomeProfileHttpPurpose purpose, CancellationToken token, int timeoutMs, Func<Uri, HttpWebRequest> factory)
        {
            if (timeoutMs < 1 || timeoutMs > TimeoutMs) throw new ArgumentOutOfRangeException("timeoutMs");
            this.purpose = purpose; this.caller = token; this.timeoutMs = timeoutMs; this.factory = factory;
            cancellation = CancellationTokenSource.CreateLinkedTokenSource(token, deadline.Token);
            deadline.CancelAfter(timeoutMs);
        }
        private string Unconfirmed
        {
            get { return purpose == HomeProfileHttpPurpose.Create
                ? "Создание QR не подтверждено. VPS мог создать ссылку и заменить прежнюю. ProGo не повторяет запрос автоматически."
                : "Отзыв ссылки не подтверждён. VPS мог выполнить его. ProGo не повторяет запрос автоматически; установленный VPN не изменяется."; }
        }
        internal void Check()
        {
            if (caller.IsCancellationRequested) throw new HomeProfileHttpCancelledException(mutationStarted ? "Ожидание HTTPS прервано. " + Unconfirmed
                : purpose == HomeProfileHttpPurpose.Create ? "Создание QR отменено до отправки запроса выдачи."
                : purpose == HomeProfileHttpPurpose.Revoke ? "Отзыв ссылки отменён до отправки запроса."
                : "Проверка HTTPS отменена. Прежний адрес и доступ сохранены.");
            if (deadline.IsCancellationRequested || elapsed.ElapsedMilliseconds >= timeoutMs)
                throw new HomeProfileHttpFailureException(mutationStarted ? "Время ожидания HTTPS истекло. " + Unconfirmed
                    : purpose == HomeProfileHttpPurpose.Revoke ? "Время ожидания отзыва ссылки истекло до отправки запроса."
                    : "Время проверки HTTPS истекло. Прежний адрес и доступ сохранены; проверьте соединение и домен.");
        }
        internal async Task<string> RequestAsync(string origin, string path, string method, HomeVpnAccess access, string body)
        {
            Check();
            var uri = new Uri(HomeProfileShare.Origin(origin) + path);
            var request = factory == null ? (HttpWebRequest)WebRequest.Create(uri) : factory(uri);
            request.Method = method; request.AllowAutoRedirect = false; request.Proxy = null;
            request.Timeout = request.ReadWriteTimeout = Math.Max(1, timeoutMs - (int)Math.Min(timeoutMs - 1, elapsed.ElapsedMilliseconds));
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            if (access != null) request.Headers[HttpRequestHeader.Authorization] = "Basic "
                + Convert.ToBase64String(Encoding.ASCII.GetBytes(access.User + ":" + access.Password));
            using (cancellation.Token.Register(delegate { request.Abort(); })) {
                try {
                    Check();
                    if (method == "POST") {
                        byte[] bytes = Encoding.UTF8.GetBytes(body);
                        request.ContentType = "application/json"; request.ContentLength = bytes.Length;
                        mutationStarted = true;
                        using (var stream = await request.GetRequestStreamAsync().ConfigureAwait(false)) {
                            Check(); await stream.WriteAsync(bytes, 0, bytes.Length, cancellation.Token).ConfigureAwait(false); Check();
                        }
                    } else if (method == "DELETE") mutationStarted = true;
                    using (var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false)) {
                        Check();
                        if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 300) throw new IOException();
                        using (var stream = response.GetResponseStream())
                        using (var output = new MemoryStream()) {
                            var buffer = new byte[4096];
                            while (true) {
                                Check(); int count = await stream.ReadAsync(buffer, 0, buffer.Length, cancellation.Token).ConfigureAwait(false); Check();
                                if (count == 0) break;
                                if (output.Length + count > 65536) throw new IOException();
                                output.Write(buffer, 0, count);
                            }
                            Check(); return new UTF8Encoding(false, true).GetString(output.ToArray());
                        }
                    }
                } catch (Exception ex) {
                    if (ex is OutOfMemoryException) throw;
                    var web = ex as WebException; var response = web == null ? null : web.Response as HttpWebResponse;
                    int code = response == null ? 0 : (int)response.StatusCode; if (response != null) response.Dispose();
                    Check();
                    if (code == 401) throw new HomeProfileHttpFailureException("Доступ к выдаче профиля отозван. Попросите владельца проверить приглашение.");
                    throw new HomeProfileHttpFailureException(mutationStarted ? Unconfirmed
                        : "HTTPS-выдача недоступна. Проверьте домен, сертификат и порты 80/443 на VPS. После первой настройки сертификат может выпускаться несколько минут.");
                }
            }
        }
        public void Dispose() { cancellation.Dispose(); deadline.Dispose(); }
    }
}
