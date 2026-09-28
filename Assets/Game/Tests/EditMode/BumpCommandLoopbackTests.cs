using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Game.Webhook;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Real-socket tests of the typed /bump contract: query and body fields reach the request,
    /// resolved values come back in the 200 body, and malformed input is refused on the worker
    /// thread before anything is enqueued. The test thread stands in for the main-thread drain.
    /// </summary>
    public sealed class BumpCommandLoopbackTests
    {
        private BumpListener _listener;

        [SetUp]
        public void SetUp()
        {
            _listener = new BumpListener(0);
            _listener.StateProvider = () => (true, 0);
            _listener.KnownTypeIds = new[] { "boxing" };
            _listener.Start();
        }

        [TearDown]
        public void TearDown()
        {
            _listener.Dispose();
        }

        [Test]
        public void GetWithQuery_Returns200_WithResolvedFields_AndCarriesTheCommand()
        {
            string response = Send("GET /bump?polarity=positive&type=BOXING&tag=A HTTP/1.1\r\nHost: localhost\r\n\r\n", (BumpPolarity.Positive, "boxing"), out BumpRequest request);

            StringAssert.StartsWith("HTTP/1.1 200", response);
            StringAssert.Contains("\"accepted\":true", response);
            StringAssert.Contains("\"polarity\":\"positive\"", response);
            StringAssert.Contains("\"type\":\"boxing\"", response);
            Assert.AreEqual(BumpPolarity.Positive, request.Command.Polarity);
            Assert.AreEqual("BOXING", request.Command.TypeId);
            Assert.AreEqual("A", request.Command.Tag);
        }

        [Test]
        public void JsonPost_Returns200_AndCarriesTheCommand()
        {
            const string body = "{\"polarity\":\"negative\",\"type\":\"boxing\",\"tag\":\"Jay\"}";

            string response = Send(Post(body), (BumpPolarity.Negative, "boxing"), out BumpRequest request);

            StringAssert.StartsWith("HTTP/1.1 200", response);
            StringAssert.Contains("\"polarity\":\"negative\"", response);
            Assert.AreEqual(BumpPolarity.Negative, request.Command.Polarity);
            Assert.AreEqual("Jay", request.Command.Tag);
        }

        [Test]
        public void BarePost_Returns200_WithDefaultsFromTheResolver_AndAnEmptyCommand()
        {
            string response = Send("POST /bump HTTP/1.1\r\nHost: localhost\r\nContent-Length: 0\r\n\r\n", (BumpPolarity.Negative, "boxing"), out BumpRequest request);

            StringAssert.StartsWith("HTTP/1.1 200", response);
            StringAssert.Contains("\"polarity\":\"negative\"", response);
            StringAssert.Contains("\"type\":\"boxing\"", response);
            Assert.IsNull(request.Command.Polarity);
            Assert.IsNull(request.Command.TypeId);
            Assert.IsNull(request.Command.Tag);
        }

        [Test]
        public void InvalidPolarity_Returns400_WithoutEnqueueing()
        {
            AssertRejectedBeforeQueue("GET /bump?polarity=sideways HTTP/1.1\r\nHost: localhost\r\n\r\n", "invalid_polarity");
        }

        [Test]
        public void UnknownType_Returns400_WithoutEnqueueing()
        {
            AssertRejectedBeforeQueue("GET /bump?type=laser HTTP/1.1\r\nHost: localhost\r\n\r\n", "unknown_type");
        }

        [Test]
        public void BadJson_Returns400_WithoutEnqueueing()
        {
            AssertRejectedBeforeQueue(Post("{\"tag\":"), "bad_body");
        }

        private void AssertRejectedBeforeQueue(string rawRequest, string errorCode)
        {
            string response = Exchange(rawRequest);

            StringAssert.StartsWith("HTTP/1.1 400", response);
            StringAssert.Contains("\"error\":\"" + errorCode + "\"", response);
            Assert.IsFalse(_listener.TryDequeue(out _), "a malformed request must never reach the main thread");
        }

        private static string Post(string body)
        {
            return "POST /bump HTTP/1.1\r\nHost: localhost\r\nContent-Type: application/json\r\nContent-Length: " + Encoding.UTF8.GetByteCount(body) + "\r\n\r\n" + body;
        }

        /// <summary>Sends the request, plays the main thread (record the resolved values, accept) and returns the raw response.</summary>
        private string Send(string rawRequest, (BumpPolarity polarity, string typeId) acceptWith, out BumpRequest request)
        {
            using var client = new TcpClient();
            client.Connect(IPAddress.Loopback, _listener.Port);
            client.ReceiveTimeout = 5000;
            using NetworkStream stream = client.GetStream();
            byte[] bytes = Encoding.UTF8.GetBytes(rawRequest);
            stream.Write(bytes, 0, bytes.Length);

            request = null;
            DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (request == null && DateTime.UtcNow < deadline)
            {
                if (!_listener.TryDequeue(out request))
                {
                    Thread.Sleep(5);
                }
            }

            Assert.IsNotNull(request, "request never reached the queue");
            request.SetResolvedValues(acceptWith.polarity, acceptWith.typeId);
            Assert.IsTrue(request.TryResolve(BumpRequestState.Accepted));
            return ReadAll(stream);
        }

        private string Exchange(string rawRequest)
        {
            using var client = new TcpClient();
            client.Connect(IPAddress.Loopback, _listener.Port);
            client.ReceiveTimeout = 5000;
            using NetworkStream stream = client.GetStream();
            byte[] bytes = Encoding.UTF8.GetBytes(rawRequest);
            stream.Write(bytes, 0, bytes.Length);
            return ReadAll(stream);
        }

        private static string ReadAll(NetworkStream stream)
        {
            var buffer = new byte[4096];
            var sb = new StringBuilder();
            while (true)
            {
                int read;
                try
                {
                    read = stream.Read(buffer, 0, buffer.Length);
                }
                catch (IOException)
                {
                    break;
                }

                if (read <= 0)
                {
                    break;
                }

                sb.Append(Encoding.UTF8.GetString(buffer, 0, read));
            }

            return sb.ToString();
        }
    }
}
