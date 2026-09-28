using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

[assembly: InternalsVisibleTo("Game.Tests.EditMode")]

namespace Game.Webhook
{
    /// <summary>
    /// Minimal local HTTP listener for POST/GET /bump. Runs entirely on background threads;
    /// makes no UnityEngine calls. Every accepted-for-consideration request is pushed onto a
    /// bounded queue and the worker blocks (bounded) for the main thread's accept/reject decision
    /// -- the HTTP response is never sent before that decision is known. This is transport only --
    /// it does not know about game/session state beyond the StateProvider fast-path below.
    /// </summary>
    public sealed class BumpListener : IDisposable
    {
        public const string BumpPath = "/bump";

        internal const int MaxHeaderBytes = 8 * 1024;
        internal const int MaxBodyBytes = 8 * 1024;
        internal const int QueueCapacity = 16;

        private const int SocketTimeoutMs = 3000;
        private const int AcceptWaitTimeoutMs = 750;

        private int _port;
        private readonly ConcurrentQueue<BumpRequest> _queue = new ConcurrentQueue<BumpRequest>();
        private int _queueCount;

        private TcpListener _listener;
        private Thread _acceptThread;
        private volatile bool _running;

        public event Action<string> Faulted;

        /// <summary>
        /// Read synchronously on a background thread for every request -- must not touch
        /// UnityEngine APIs. Returns whether the session is currently accepting gameplay bumps
        /// and the level instance id to stamp onto the request for later staleness checks. This is
        /// a fast-path rejection only; the main thread's drain of the queue remains authoritative.
        /// </summary>
        public Func<(bool accepting, int levelInstanceId)> StateProvider { get; set; }

        /// <summary>The bound port. Resolves to the OS-assigned port after Start() when constructed with 0.</summary>
        public int Port => _port;

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
            _port = ((IPEndPoint)_listener.LocalEndpoint).Port;
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

            // Release any worker threads still blocked in WaitForResolution so shutdown never hangs.
            while (TryDequeue(out BumpRequest request))
            {
                request.TryResolve(BumpRequestState.Rejected);
            }
        }

        public void Dispose()
        {
            Stop();
        }

        /// <summary>Drain one pending request, if any. Call from the main thread only.</summary>
        public bool TryDequeue(out BumpRequest request)
        {
            if (_queue.TryDequeue(out request))
            {
                Interlocked.Decrement(ref _queueCount);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Atomically reserves one queue slot. Returns false (reserving nothing) if the queue is
        /// already at capacity. Pairs with <see cref="TryDequeue"/>, which releases the slot.
        /// </summary>
        internal bool TryReserveCapacity()
        {
            if (Interlocked.Increment(ref _queueCount) > QueueCapacity)
            {
                Interlocked.Decrement(ref _queueCount);
                return false;
            }

            return true;
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

                    using (Stream stream = client.GetStream())
                    {
                        if (!TryReadRequest(stream, out HttpRequestLine requestLine, out _, out int failureStatusCode))
                        {
                            if (failureStatusCode == 413)
                            {
                                WriteResponse(stream, 413, "Payload Too Large", "{\"error\":\"payload_too_large\"}");
                            }
                            else
                            {
                                WriteResponse(stream, 400, "Bad Request", "{\"error\":\"bad_request\"}");
                            }

                            return;
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

                        if (!TryReserveCapacity())
                        {
                            WriteResponse(stream, 429, "Too Many Requests", "{\"error\":\"too_many_requests\"}");
                            return;
                        }

                        string requestId = Guid.NewGuid().ToString("N");
                        var request = new BumpRequest(requestId, requestLine.Method, DateTime.UtcNow, state.levelInstanceId);
                        _queue.Enqueue(request);

                        bool resolvedInTime = request.WaitForResolution(TimeSpan.FromMilliseconds(AcceptWaitTimeoutMs));
                        if (!resolvedInTime && request.TryResolve(BumpRequestState.Expired))
                        {
                            // We won the Pending -> Expired race; the main thread never accepted this one.
                            WriteResponse(stream, 503, "Service Unavailable", "{\"error\":\"timeout\"}");
                            return;
                        }

                        // Either resolved within the wait, or the main thread's Accept/Reject won the
                        // race against our own expiry attempt above -- read the final state either way.
                        switch (request.State)
                        {
                            case BumpRequestState.Accepted:
                                WriteResponse(stream, 200, "OK", "{\"requestId\":\"" + requestId + "\",\"accepted\":true}");
                                break;
                            case BumpRequestState.Rejected:
                                WriteResponse(stream, 409, "Conflict", "{\"error\":\"not_playing\"}");
                                break;
                            default: // Expired
                                WriteResponse(stream, 503, "Service Unavailable", "{\"error\":\"timeout\"}");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Faulted?.Invoke(ex.Message);
            }
        }

        internal readonly struct HttpRequestLine
        {
            public readonly string Method;
            public readonly string Path;

            public HttpRequestLine(string method, string path)
            {
                Method = method;
                Path = path;
            }
        }

        /// <summary>
        /// Parses one HTTP request (request line, headers, body) off <paramref name="stream"/>.
        /// Bytes already read past the header terminator in the same underlying read are treated as
        /// the start of the body -- a coalesced header+body read (the common case for a small POST)
        /// never re-reads or waits for bytes that already arrived. Internal and Stream-based (not
        /// NetworkStream-specific) so tests can drive it with a MemoryStream or a fragmenting Stream
        /// without opening a socket.
        /// </summary>
        internal static bool TryReadRequest(Stream stream, out HttpRequestLine requestLine, out byte[] body, out int failureStatusCode)
        {
            requestLine = default;
            body = Array.Empty<byte>();
            failureStatusCode = 400;

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
                    return false; // socket error/timeout while reading headers
                }

                if (read <= 0)
                {
                    return false; // connection closed before headers completed
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
                return false; // headers never terminated within MaxHeaderBytes
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

            int contentLength = 0;
            bool hasContentLength = false;
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                int colon = line.IndexOf(':');
                if (colon <= 0)
                {
                    continue;
                }

                string name = line.Substring(0, colon).Trim();
                if (!string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string value = line.Substring(colon + 1).Trim();
                if (!int.TryParse(value, out int parsed) || parsed < 0)
                {
                    return false; // negative or non-numeric Content-Length
                }

                if (hasContentLength && parsed != contentLength)
                {
                    return false; // duplicate, conflicting Content-Length headers
                }

                contentLength = parsed;
                hasContentLength = true;
            }

            if (contentLength > MaxBodyBytes)
            {
                failureStatusCode = 413;
                return false;
            }

            if (contentLength == 0)
            {
                return true;
            }

            body = new byte[contentLength];

            // Bytes already read past the header terminator in the loop above are the start of the
            // body -- carry them over instead of discarding them and re-reading from the socket.
            int alreadyRead = Math.Min(total - headerEnd, contentLength);
            if (alreadyRead > 0)
            {
                Buffer.BlockCopy(buffer, headerEnd, body, 0, alreadyRead);
            }

            int bodyTotal = alreadyRead;
            while (bodyTotal < contentLength)
            {
                int read;
                try
                {
                    read = stream.Read(body, bodyTotal, contentLength - bodyTotal);
                }
                catch (IOException)
                {
                    return false; // timeout/socket error before the full body arrived
                }

                if (read <= 0)
                {
                    return false; // connection closed before the full body arrived
                }

                bodyTotal += read;
            }

            return true;
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

        private static void WriteResponse(Stream stream, int statusCode, string statusText, string jsonBody)
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
