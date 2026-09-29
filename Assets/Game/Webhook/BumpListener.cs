using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Game.Core;

[assembly: InternalsVisibleTo("Game.Tests.EditMode")]

namespace Game.Webhook
{
    /// <summary>
    /// Minimal local HTTP listener for POST/GET /bump (optional ?polarity=&type=&tag= query and body, see BumpCommandParser). Runs entirely on background threads;
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

        /// <summary>
        /// Simultaneous connections served; further ones get an immediate 503 busy. Above
        /// QueueCapacity so a full queue still answers 429 rather than the cap hiding it.
        /// </summary>
        internal const int MaxConcurrentClients = QueueCapacity + 4;
        internal const int StopTimeoutMs = 1000;

        private const int OverCapSendTimeoutMs = 250;
        private const int SocketTimeoutMs = 3000;
        private const int AcceptWaitTimeoutMs = 750;

        private int _port;
        private readonly ConcurrentQueue<BumpRequest> _queue = new ConcurrentQueue<BumpRequest>();
        private int _queueCount;

        // Guards _running transitions, _clients and _inflight so Stop can snapshot everything a
        // worker has registered and no worker can register after that snapshot.
        private readonly object _gate = new object();
        private readonly HashSet<TcpClient> _clients = new HashSet<TcpClient>();
        private readonly HashSet<BumpRequest> _inflight = new HashSet<BumpRequest>();

        private TcpListener _listener;
        private Thread _acceptThread;
        private volatile bool _running;
        private int _generation;

        public event Action<string> Faulted;

        private volatile IReadOnlyCollection<string> _knownTypeIds;

        /// <summary>
        /// Bump type ids the caller may name (case-insensitive); anything else is answered 400
        /// unknown_type on the worker thread. Set once on the main thread before Start(); null accepts any id.
        /// </summary>
        public IReadOnlyCollection<string> KnownTypeIds
        {
            get => _knownTypeIds;
            set => _knownTypeIds = value;
        }

        /// <summary>
        /// Read synchronously on a background thread for every request -- must not touch
        /// UnityEngine APIs. Returns whether the session is currently accepting gameplay bumps
        /// and the level instance id to stamp onto the request for later staleness checks. This is
        /// a fast-path rejection only; the main thread's drain of the queue remains authoritative.
        /// </summary>
        public Func<(bool accepting, int levelInstanceId)> StateProvider { get; set; }

        /// <summary>The bound port. Resolves to the OS-assigned port after Start() when constructed with 0.</summary>
        public int Port => _port;

        /// <summary>Connections currently being handled (tracked so Stop can close them).</summary>
        internal int ActiveClientCount
        {
            get
            {
                lock (_gate)
                {
                    return _clients.Count;
                }
            }
        }

        public BumpListener(int port = WebhookSettings.DefaultPort)
        {
            _port = port;
        }

        public void Start()
        {
            lock (_gate)
            {
                if (_running)
                {
                    return;
                }

                var listener = new TcpListener(IPAddress.Loopback, _port);
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Lets a Stop -> Start cycle rebind the same port while old connections sit in TIME_WAIT.
                    listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                }

                listener.Start();
                _port = ((IPEndPoint)listener.LocalEndpoint).Port;
                _listener = listener;
                int generation = ++_generation;
                _running = true;

                _acceptThread = new Thread(() => AcceptLoop(listener, generation))
                {
                    IsBackground = true,
                    Name = "BumpListener.Accept",
                };
                _acceptThread.Start();
            }
        }

        /// <summary>
        /// Stops accepting, closes every active client socket, rejects all queued and in-flight
        /// requests, then waits at most <see cref="StopTimeoutMs"/> in total for the accept thread and
        /// workers to finish -- it can never hang the caller (Unity's main thread). Idempotent; a
        /// closed client sees its connection dropped without a response.
        /// </summary>
        public void Stop()
        {
            TcpListener listener;
            Thread acceptThread;
            TcpClient[] clients;
            BumpRequest[] inflight;

            lock (_gate)
            {
                _running = false;
                listener = _listener;
                acceptThread = _acceptThread;
                _listener = null;
                _acceptThread = null;
                clients = new TcpClient[_clients.Count];
                _clients.CopyTo(clients);
                inflight = new BumpRequest[_inflight.Count];
                _inflight.CopyTo(inflight);
            }

            var deadline = DateTime.UtcNow.AddMilliseconds(StopTimeoutMs);

            try
            {
                listener?.Stop();
            }
            catch (Exception)
            {
                // Ignore -- shutting down.
            }

            // Close sockets first so a released worker finds its connection gone rather than racing a response out.
            foreach (TcpClient client in clients)
            {
                try
                {
                    client.Close();
                }
                catch (Exception)
                {
                    // Ignore -- shutting down.
                }
            }

            // Release any worker still blocked in WaitForResolution so shutdown never hangs.
            foreach (BumpRequest request in inflight)
            {
                request.TryResolve(BumpRequestState.Rejected);
            }

            while (TryDequeue(out BumpRequest request))
            {
                request.TryResolve(BumpRequestState.Rejected);
            }

            if (acceptThread != null && acceptThread != Thread.CurrentThread)
            {
                acceptThread.Join(RemainingMs(deadline));
            }

            lock (_gate)
            {
                while (_clients.Count > 0)
                {
                    int remaining = RemainingMs(deadline);
                    if (remaining <= 0 || !Monitor.Wait(_gate, remaining))
                    {
                        break;
                    }
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private static int RemainingMs(DateTime deadlineUtc)
        {
            return Math.Max(0, (int)(deadlineUtc - DateTime.UtcNow).TotalMilliseconds);
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

        private void AcceptLoop(TcpListener listener, int generation)
        {
            while (_running && generation == _generation)
            {
                TcpClient client;
                try
                {
                    client = listener.AcceptTcpClient();
                }
                catch (Exception)
                {
                    // Listener stopped or socket error during shutdown.
                    break;
                }

                bool admitted;
                lock (_gate)
                {
                    admitted = _running && generation == _generation && _clients.Count < MaxConcurrentClients;
                    if (admitted)
                    {
                        _clients.Add(client);
                    }
                }

                if (!admitted)
                {
                    RejectOverCap(client);
                    continue;
                }

                ThreadPool.QueueUserWorkItem(_ => HandleClient(client, generation));
            }
        }

        /// <summary>Answers a connection over the concurrency cap with a fast 503 without reading anything from it.</summary>
        private static void RejectOverCap(TcpClient client)
        {
            try
            {
                using (client)
                {
                    client.SendTimeout = OverCapSendTimeoutMs;
                    WriteResponse(client.GetStream(), 503, "Service Unavailable", "{\"error\":\"busy\"}");
                }
            }
            catch (Exception)
            {
                // Ignore -- the client is being turned away anyway.
            }
        }

        private void HandleClient(TcpClient client, int generation)
        {
            try
            {
                using (client)
                {
                    client.ReceiveTimeout = SocketTimeoutMs;
                    client.SendTimeout = SocketTimeoutMs;

                    using (Stream stream = client.GetStream())
                    {
                        ServeRequest(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                // Stop closes sockets under a worker on purpose; only surface genuine faults.
                if (_running && generation == _generation && !(ex is ObjectDisposedException))
                {
                    Faulted?.Invoke(ex.Message);
                }
            }
            finally
            {
                lock (_gate)
                {
                    _clients.Remove(client);
                    Monitor.PulseAll(_gate);
                }
            }
        }

        private void ServeRequest(Stream stream)
        {
            if (!TryReadRequest(stream, out HttpRequestLine requestLine, out byte[] body, out int failureStatusCode))
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

            if (!BumpCommandParser.TryParse(requestLine.Query, body.Length > 0 ? Encoding.UTF8.GetString(body) : null, out BumpCommand command, out string parseError))
            {
                WriteResponse(stream, 400, "Bad Request", "{\"error\":\"" + parseError + "\"}");
                return;
            }

            if (command.TypeId != null && !IsKnownType(command.TypeId))
            {
                WriteResponse(stream, 400, "Bad Request", "{\"error\":\"unknown_type\"}");
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
            var request = new BumpRequest(requestId, requestLine.Method, DateTime.UtcNow, state.levelInstanceId, command);

            // Register under the gate so Stop's snapshot either includes this request or we see _running false here.
            bool registered;
            lock (_gate)
            {
                registered = _running;
                if (registered)
                {
                    _inflight.Add(request);
                    _queue.Enqueue(request);
                }
            }

            if (!registered)
            {
                Interlocked.Decrement(ref _queueCount);
                return; // shutting down: drop the connection without a response
            }

            try
            {
                bool resolvedInTime = request.WaitForResolution(TimeSpan.FromMilliseconds(AcceptWaitTimeoutMs));
                if (!resolvedInTime && request.TryResolve(BumpRequestState.Expired))
                {
                    // We won the Pending -> Expired race; the main thread never accepted this one.
                    WriteResponse(stream, 503, "Service Unavailable", "{\"error\":\"timeout\"}");
                    return;
                }
            }
            finally
            {
                lock (_gate)
                {
                    _inflight.Remove(request);
                }
            }

            // Either resolved within the wait, or the main thread's Accept/Reject won the
            // race against our own expiry attempt above -- read the final state either way.
            switch (request.State)
            {
                case BumpRequestState.Accepted:
                    WriteResponse(stream, 200, "OK", BuildAcceptedBody(request));
                    break;
                case BumpRequestState.Rejected:
                    WriteResponse(stream, 409, "Conflict", "{\"error\":\"not_playing\"}");
                    break;
                default: // Expired
                    WriteResponse(stream, 503, "Service Unavailable", "{\"error\":\"timeout\"}");
                    break;
            }
        }

        private bool IsKnownType(string typeId)
        {
            IReadOnlyCollection<string> known = _knownTypeIds;
            if (known == null)
            {
                return true;
            }

            foreach (string id in known)
            {
                if (string.Equals(id, typeId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildAcceptedBody(BumpRequest request)
        {
            var sb = new StringBuilder("{\"requestId\":\"").Append(request.RequestId).Append("\",\"accepted\":true");
            if (request.TryGetResolvedValues(out BumpPolarity polarity, out string typeId))
            {
                sb.Append(",\"polarity\":\"").Append(polarity == BumpPolarity.Positive ? "positive" : "negative").Append('"');
                sb.Append(",\"type\":\"").Append(EscapeJson(typeId.ToLowerInvariant())).Append('"');
            }

            return sb.Append('}').ToString();
        }

        private static string EscapeJson(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        internal readonly struct HttpRequestLine
        {
            public readonly string Method;
            /// <summary>The request target up to (excluding) any '?'.</summary>
            public readonly string Path;
            /// <summary>The raw query string after '?', without the '?'; empty when absent.</summary>
            public readonly string Query;

            public HttpRequestLine(string method, string path, string query = "")
            {
                Method = method;
                Path = path;
                Query = query;
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

            string target = requestParts[1];
            int queryStart = target.IndexOf('?');
            requestLine = queryStart < 0
                ? new HttpRequestLine(requestParts[0], target)
                : new HttpRequestLine(requestParts[0], target.Substring(0, queryStart), target.Substring(queryStart + 1));

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
            catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
            {
                // Client disconnected (or Stop closed it) before the response was fully sent.
            }
        }
    }
}
