using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Game.Webhook
{
    /// <summary>
    /// Minimal local HTTP listener for POST/GET /bump. Runs entirely on background threads;
    /// makes no UnityEngine calls. Accepted requests are pushed onto a bounded queue that the
    /// main thread drains. This is transport only -- it does not know about game/session state.
    /// </summary>
    public sealed class BumpListener : IDisposable
    {
        public const string BumpPath = "/bump";

        private const int MaxHeaderBytes = 8 * 1024;
        private const int MaxBodyBytes = 8 * 1024;
        private const int SocketTimeoutMs = 3000;
        private const int QueueCapacity = 16;

        private readonly int _port;
        private readonly ConcurrentQueue<BumpRequest> _queue = new ConcurrentQueue<BumpRequest>();
        private int _queueCount;

        private TcpListener _listener;
        private Thread _acceptThread;
        private volatile bool _running;

        public event Action<string> Faulted;

        /// <summary>
        /// Read synchronously on a background thread for every request -- must not touch
        /// UnityEngine APIs. Returns whether the session is currently accepting gameplay bumps
        /// and the level instance id to stamp onto the request for later staleness checks.
        /// </summary>
        public Func<(bool accepting, int levelInstanceId)> StateProvider { get; set; }

        public BumpListener(int port = 56789)
        {
            _port = port;
        }

        public void Start()
        {
            if (_running)
            {
                return;
            }

            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            _running = true;

            _acceptThread = new Thread(AcceptLoop)
            {
                IsBackground = true,
                Name = "BumpListener.Accept",
            };
            _acceptThread.Start();
        }

        public void Stop()
        {
            _running = false;

            try
            {
                _listener?.Stop();
            }
            catch (Exception)
            {
                // Ignore -- shutting down.
            }

            _acceptThread = null;
            _listener = null;
        }

        public void Dispose()
        {
            Stop();
        }

        /// <summary>Drain one accepted request, if any. Call from the main thread only.</summary>
        public bool TryDequeue(out BumpRequest request)
        {
            if (_queue.TryDequeue(out request))
            {
                Interlocked.Decrement(ref _queueCount);
                return true;
            }

            return false;
        }

        private void AcceptLoop()
        {
            while (_running)
            {
                TcpClient client;
                try
                {
                    client = _listener.AcceptTcpClient();
                }
                catch (Exception)
                {
                    // Listener stopped or socket error during shutdown.
                    break;
                }

                ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
            }
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                using (client)
                {
                    client.ReceiveTimeout = SocketTimeoutMs;
                    client.SendTimeout = SocketTimeoutMs;

                    using (var stream = client.GetStream())
                    {
                        HttpRequestLine requestLine;
                        int contentLength;
                        if (!TryReadHeaders(stream, out requestLine, out contentLength))
                        {
                            WriteResponse(stream, 400, "Bad Request", "{\"error\":\"bad_request\"}");
                            return;
                        }

                        if (contentLength > 0)
                        {
                            // Body is read but not currently inspected -- /bump ignores payload contents.
                            ReadBody(stream, contentLength);
                        }

                        if (!string.Equals(requestLine.Path, BumpPath, StringComparison.Ordinal))
                        {
                            WriteResponse(stream, 404, "Not Found", "{\"error\":\"not_found\"}");
                            return;
                        }

                        bool isGet = string.Equals(requestLine.Method, "GET", StringComparison.Ordinal);
                        bool isPost = string.Equals(requestLine.Method, "POST", StringComparison.Ordinal);
                        if (!isGet && !isPost)
                        {
                            WriteResponse(stream, 405, "Method Not Allowed", "{\"error\":\"method_not_allowed\"}");
                            return;
                        }

                        (bool accepting, int levelInstanceId) state = StateProvider != null
                            ? StateProvider()
                            : (accepting: true, levelInstanceId: 0);

                        if (!state.accepting)
                        {
                            WriteResponse(stream, 409, "Conflict", "{\"error\":\"not_playing\"}");
                            return;
                        }

                        if (Volatile.Read(ref _queueCount) >= QueueCapacity)
                        {
                            WriteResponse(stream, 429, "Too Many Requests", "{\"error\":\"too_many_requests\"}");
                            return;
                        }

                        string requestId = Guid.NewGuid().ToString("N");
                        _queue.Enqueue(new BumpRequest(requestId, requestLine.Method, DateTime.UtcNow, state.levelInstanceId));
                        Interlocked.Increment(ref _queueCount);

                        WriteResponse(stream, 200, "OK", "{\"requestId\":\"" + requestId + "\",\"accepted\":true}");
                    }
                }
            }
            catch (Exception ex)
            {
                Faulted?.Invoke(ex.Message);
            }
        }

        private readonly struct HttpRequestLine
        {
            public readonly string Method;
            public readonly string Path;

            public HttpRequestLine(string method, string path)
            {
                Method = method;
                Path = path;
            }
        }

        private static bool TryReadHeaders(NetworkStream stream, out HttpRequestLine requestLine, out int contentLength)
        {
            requestLine = default;
            contentLength = 0;

            var buffer = new byte[MaxHeaderBytes];
            int total = 0;
            int headerEnd = -1;

            while (total < buffer.Length)
            {
                int read;
                try
                {
                    read = stream.Read(buffer, total, buffer.Length - total);
                }
                catch (IOException)
                {
                    return false;
                }

                if (read <= 0)
                {
                    return false;
                }

                total += read;

                headerEnd = IndexOfHeaderTerminator(buffer, total);
                if (headerEnd >= 0)
                {
                    break;
                }
            }

            if (headerEnd < 0)
            {
                return false;
            }

            string headerText = Encoding.ASCII.GetString(buffer, 0, headerEnd);
            string[] lines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
            if (lines.Length == 0)
            {
                return false;
            }

            string[] requestParts = lines[0].Split(' ');
            if (requestParts.Length < 2)
            {
                return false;
            }

            requestLine = new HttpRequestLine(requestParts[0], requestParts[1]);

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                int colon = line.IndexOf(':');
                if (colon <= 0)
                {
                    continue;
                }

                string name = line.Substring(0, colon).Trim();
                string value = line.Substring(colon + 1).Trim();
                if (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(value, out contentLength);
                }
            }

            contentLength = Math.Min(contentLength, MaxBodyBytes);
            return true;
        }

        private static void ReadBody(NetworkStream stream, int contentLength)
        {
            var body = new byte[contentLength];
            int total = 0;
            while (total < contentLength)
            {
                int read;
                try
                {
                    read = stream.Read(body, total, contentLength - total);
                }
                catch (IOException)
                {
                    return;
                }

                if (read <= 0)
                {
                    return;
                }

                total += read;
            }
        }

        private static int IndexOfHeaderTerminator(byte[] buffer, int length)
        {
            for (int i = 0; i + 3 < length; i++)
            {
                if (buffer[i] == '\r' && buffer[i + 1] == '\n' && buffer[i + 2] == '\r' && buffer[i + 3] == '\n')
                {
                    return i + 4;
                }
            }

            return -1;
        }

        private static void WriteResponse(NetworkStream stream, int statusCode, string statusText, string jsonBody)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
            string headers =
                "HTTP/1.1 " + statusCode + " " + statusText + "\r\n" +
                "Content-Type: application/json\r\n" +
                "Content-Length: " + bodyBytes.Length + "\r\n" +
                "Connection: close\r\n" +
                "\r\n";

            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);

            try
            {
                stream.Write(headerBytes, 0, headerBytes.Length);
                stream.Write(bodyBytes, 0, bodyBytes.Length);
                stream.Flush();
            }
            catch (IOException)
            {
                // Client disconnected before the response was fully sent.
            }
        }
    }
}
