using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Game.Webhook;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>G2 follow-up: concurrency cap, tracked clients, and bounded Stop on the real loopback listener.</summary>
    public sealed class BumpListenerShutdownTests
    {
        private const int GenerousMs = 4000;

        private static BumpListener StartListener(int port = 0)
        {
            var listener = new BumpListener(port);
            listener.StateProvider = () => (true, 0);
            listener.Start();
            return listener;
        }

        private static TcpClient Connect(BumpListener listener)
        {
            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, listener.Port);
            client.ReceiveTimeout = GenerousMs;
            client.SendTimeout = GenerousMs;
            return client;
        }

        private static void WaitFor(Func<bool> condition, string what)
        {
            var sw = Stopwatch.StartNew();
            while (!condition())
            {
                if (sw.ElapsedMilliseconds > GenerousMs)
                {
                    Assert.Fail("timed out waiting for " + what);
                }

                Thread.Sleep(5);
            }
        }

        /// <summary>Reads until the peer closes or errors; returns everything received.</summary>
        private static string ReadToEnd(TcpClient client)
        {
            var sb = new StringBuilder();
            var buffer = new byte[1024];
            NetworkStream stream = client.GetStream();
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

                sb.Append(Encoding.ASCII.GetString(buffer, 0, read));
            }

            return sb.ToString();
        }

        [Test]
        public void Stop_WithClientStalledMidHeaders_ReturnsPromptlyAndClosesIt()
        {
            var listener = StartListener();
            try
            {
                using TcpClient client = Connect(listener);
                byte[] partial = Encoding.ASCII.GetBytes("POST /bump HTTP/1.1\r\nHost: loc");
                client.GetStream().Write(partial, 0, partial.Length);
                WaitFor(() => listener.ActiveClientCount == 1, "worker to register");

                var sw = Stopwatch.StartNew();
                listener.Stop();
                sw.Stop();

                Assert.Less(sw.ElapsedMilliseconds, 2500, "Stop must be bounded");
                Assert.AreEqual(0, listener.ActiveClientCount);
                Assert.AreEqual(string.Empty, ReadToEnd(client), "stalled client should just see the connection close");
            }
            finally
            {
                listener.Dispose();
            }
        }

        [Test]
        public void Stop_WhileWorkerAwaitsMainThreadDecision_RejectsRequestAndClosesClient()
        {
            var listener = StartListener();
            try
            {
                using TcpClient client = Connect(listener);
                byte[] request = Encoding.ASCII.GetBytes("GET /bump HTTP/1.1\r\nHost: localhost\r\n\r\n");
                client.GetStream().Write(request, 0, request.Length);

                BumpRequest pending = null;
                WaitFor(() => listener.TryDequeue(out pending), "request to reach the queue");

                // The main thread has the request but never decides; Stop must release the worker.
                var sw = Stopwatch.StartNew();
                listener.Stop();
                sw.Stop();

                Assert.Less(sw.ElapsedMilliseconds, 2500);
                Assert.AreEqual(BumpRequestState.Rejected, pending.State);
                Assert.AreEqual(0, listener.ActiveClientCount);

                string response = ReadToEnd(client);
                StringAssert.DoesNotContain("200 OK", response);
            }
            finally
            {
                listener.Dispose();
            }
        }

        [Test]
        public void ExceedingConcurrencyCap_GetsImmediate503()
        {
            var listener = StartListener();
            var stalled = new List<TcpClient>();
            try
            {
                for (int i = 0; i < BumpListener.MaxConcurrentClients; i++)
                {
                    stalled.Add(Connect(listener));
                }

                WaitFor(() => listener.ActiveClientCount == BumpListener.MaxConcurrentClients, "cap to fill");

                using TcpClient extra = Connect(listener);
                var sw = Stopwatch.StartNew();
                string response = ReadToEnd(extra);
                sw.Stop();

                StringAssert.StartsWith("HTTP/1.1 503", response);
                StringAssert.Contains("busy", response);
                Assert.Less(sw.ElapsedMilliseconds, 2000, "over-cap connection must not wait on the header read timeout");
            }
            finally
            {
                listener.Dispose();
                foreach (TcpClient c in stalled)
                {
                    c.Dispose();
                }
            }
        }

        [Test]
        public void StartStopStart_OnTheSameFixedPort_Works()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            var listener = StartListener(port);
            try
            {
                Assert.AreEqual(port, listener.Port);
                RoundTripUnknownPath(listener);

                listener.Stop();
                listener.Start();

                Assert.AreEqual(port, listener.Port);
                RoundTripUnknownPath(listener);
            }
            finally
            {
                listener.Dispose();
            }
        }

        [Test]
        public void DoubleStopAndDispose_AreSafe()
        {
            var listener = StartListener();

            Assert.DoesNotThrow(() =>
            {
                listener.Stop();
                listener.Stop();
                listener.Dispose();
                listener.Dispose();
            });

            var neverStarted = new BumpListener(0);
            Assert.DoesNotThrow(() =>
            {
                neverStarted.Stop();
                neverStarted.Dispose();
            });
        }

        private static void RoundTripUnknownPath(BumpListener listener)
        {
            using TcpClient client = Connect(listener);
            byte[] request = Encoding.ASCII.GetBytes("GET /nope HTTP/1.1\r\nHost: localhost\r\n\r\n");
            client.GetStream().Write(request, 0, request.Length);

            StringAssert.StartsWith("HTTP/1.1 404", ReadToEnd(client));
        }
    }
}
