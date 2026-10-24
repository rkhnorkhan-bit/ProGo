using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProGo
{
    // Compiled from the installed local file by the PowerShell update helper.
    // Transport only: release identity, digest and archive checks stay in the core.
    public static class InstalledUpdateTransport
    {
        public const int MetadataTimeoutMilliseconds = 10000;
        public const int PackageTimeoutMilliseconds = 180000;
        public const int MaxMetadataBytes = 2 * 1024 * 1024;
        public const int MaxPackageBytes = 64 * 1024 * 1024;

        public static string ReadMetadata(string url, CancellationToken cancellation)
        {
            return ReadMetadataAsync(ProductionEndpoint(url), MetadataTimeoutMilliseconds,
                MaxMetadataBytes, cancellation).GetAwaiter().GetResult();
        }
        public static void DownloadPackage(string url, string path, CancellationToken cancellation)
        {
            DownloadPackageAsync(ProductionEndpoint(url), path, PackageTimeoutMilliseconds,
                MaxPackageBytes, cancellation).GetAwaiter().GetResult();
        }
        private static Uri ProductionEndpoint(string url)
        {
            var endpoint = new Uri(url, UriKind.Absolute);
            if (endpoint.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Update downloads require HTTPS.");
            return endpoint;
        }
        internal static async Task<string> ReadMetadataAsync(Uri endpoint, int timeout, int limit,
            CancellationToken cancellation)
        {
            using (var bytes = new MemoryStream())
            {
                await TransferAsync(endpoint, bytes, timeout, limit, true, cancellation).ConfigureAwait(false);
                bytes.Position = 0;
                using (var reader = new StreamReader(bytes, Encoding.UTF8, true))
                    return reader.ReadToEnd();
            }
        }
        internal static async Task DownloadPackageAsync(Uri endpoint, string path, int timeout, int limit,
            CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            bool created = false, completed = false;
            try
            {
                // Never truncate or delete a file belonging to another attempt.
                using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    created = true;
                    await TransferAsync(endpoint, file, timeout, limit, false, cancellation).ConfigureAwait(false);
                    cancellation.ThrowIfCancellationRequested();
                }
                completed = true;
            }
            finally
            {
                if (created && !completed) File.Delete(path);
            }
        }
        private static bool AllowedEndpoint(Uri endpoint)
        {
            return endpoint != null && endpoint.IsAbsoluteUri &&
                (endpoint.Scheme == Uri.UriSchemeHttps ||
                 (endpoint.IsLoopback && endpoint.Scheme == Uri.UriSchemeHttp));
        }
        private static void Abort(HttpWebRequest request) { try { request.Abort(); } catch { } }
        private static void Close(Stream stream) { try { stream.Close(); } catch { } }
        private static async Task TransferAsync(Uri endpoint, Stream destination, int timeout, int limit,
            bool metadata, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            if (!AllowedEndpoint(endpoint)) throw new ArgumentException("Update downloads require HTTPS.");
            if (timeout <= 0 || limit <= 0) throw new ArgumentOutOfRangeException("timeout/limit");
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            {
                deadline.CancelAfter(timeout);
                HttpWebRequest request = null;
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    for (int redirects = 0; ; redirects++)
                    {
                        deadline.Token.ThrowIfCancellationRequested();
                        request = (HttpWebRequest)WebRequest.Create(endpoint);
                        request.UserAgent = "ProGo-Updater";
                        request.Accept = metadata ? "application/vnd.github+json" : "application/octet-stream";
                        request.AllowAutoRedirect = false;
                        // Only internal loopback fixtures bypass the current-user proxy.
                        if (endpoint.IsLoopback) request.Proxy = null;
                        using (deadline.Token.Register(delegate { Abort(request); }))
                        using (var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false))
                        {
                            int status = (int)response.StatusCode;
                            if (status == 301 || status == 302 || status == 303 || status == 307 || status == 308)
                            {
                                if (redirects == 5) throw new InvalidDataException("Слишком много перенаправлений при скачивании обновления.");
                                Uri next;
                                if (!Uri.TryCreate(endpoint, response.Headers["Location"], out next) ||
                                    !AllowedEndpoint(next) ||
                                    (endpoint.Scheme == Uri.UriSchemeHttps && next.Scheme != Uri.UriSchemeHttps) ||
                                    (!endpoint.IsLoopback && next.IsLoopback))
                                    throw new InvalidDataException("Недопустимое перенаправление при скачивании обновления.");
                                endpoint = next;
                                continue;
                            }
                            if (status < 200 || status >= 300)
                                throw new IOException("Не удалось скачать обновление. Повторите попытку.");
                            if (response.ContentLength > limit)
                                throw new InvalidDataException("Размер скачиваемого обновления превышает допустимый предел.");
                            using (var source = response.GetResponseStream())
                            using (deadline.Token.Register(delegate { Close(source); }))
                            {
                                var buffer = new byte[8192];
                                long received = 0;
                                while (true)
                                {
                                    deadline.Token.ThrowIfCancellationRequested();
                                    int count = await source.ReadAsync(buffer, 0, buffer.Length, deadline.Token).ConfigureAwait(false);
                                    if (count == 0) break;
                                    if (received + count > limit)
                                        throw new InvalidDataException("Размер скачиваемого обновления превышает допустимый предел.");
                                    destination.Write(buffer, 0, count);
                                    received += count;
                                }
                                if (response.ContentLength >= 0 && received != response.ContentLength)
                                    throw new IOException("Скачивание обновления не завершено. Повторите попытку.");
                            }
                            deadline.Token.ThrowIfCancellationRequested();
                            return;
                        }
                    }
                }
                catch (Exception error)
                {
                    if (request != null) Abort(request);
                    var webError = error as WebException;
                    if (webError != null && webError.Response != null) webError.Response.Close();
                    cancellation.ThrowIfCancellationRequested();
                    if (deadline.IsCancellationRequested)
                        throw new TimeoutException("Время ожидания скачивания обновления истекло. Повторите попытку.");
                    if (error is WebException)
                        throw new IOException("Не удалось скачать обновление. Проверьте подключение и повторите попытку.");
                    throw;
                }
            }
        }
    }
}
