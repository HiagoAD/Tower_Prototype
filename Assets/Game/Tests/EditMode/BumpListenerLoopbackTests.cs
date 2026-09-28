using System;
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
    /// <summary>
    /// Real-socket smoke tests: a genuine TcpClient talking to a BumpListener bound to an
    /// OS-assigned free port (port 0), with this test's thread standing in for GameSession.Update
    /// as "the main thread" pumping TryDequeue. Confirms the whole accept/reject/expire handshake
    /// end-to-end, not just its pieces.
    /// </summary>
    public sealed class BumpListenerLoopbackTests
    {
        [Test]
        public void DrainedAndAccepted_Respond200_WellUnderOneSecond()
        {
            var listener = new BumpListener(0);
            listener.StateProvider = () => (true, 0);
            listener.Start();

            try
            {
                using var client = new TcpClient();
                client.Connect(IPAddress.Loopback, listener.Port);
                client.ReceiveTimeout = 5000;
                client.SendTimeout = 5000;

                using NetworkStream stream = client.GetStream();

                byte[] request = Encoding.ASCII.GetBytes(
                    "POST /bump HTTP/1.1\r\nHost: localhost\r\nContent-Length: 4\r\n\r\nping");

                var stopwatch = Stopwatch.StartNew();
                stream.Write(request, 0, request.Length);

                BumpRequest pending = WaitForEnqueue(listener, TimeSpan.FromSeconds(2));
                Assert.IsNotNull(pending, "request never reached the queue");
                Assert.IsTrue(pending.TryResolve(BumpRequestState.Accepted));

                string response = ReadAll(stream);
                stopwatch.Stop();

                StringAssert.StartsWith("HTTP/1.1 200", response);
                Assert.Less(stopwatch.ElapsedMilliseconds, 1000);
            }
            finally
            {
                listener.Dispose();
            }
        }

        [Test]
        public void LeftUndrained_ExpiresTo503_AfterBoundedWait()
        {
            var listener = new BumpListener(0);
            listener.StateProvider = () => (true, 0);
            listener.Start();

            try
            {
                using var client = new TcpClient();
                client.Connect(IPAddress.Loopback, listener.Port);
                client.ReceiveTimeout = 5000;
                client.SendTimeout = 5000;

                using NetworkStream stream = client.GetStream();

                byte[] request = Encoding.ASCII.GetBytes("GET /bump HTTP/1.1\r\nHost: localhost\r\n\r\n");

                var stopwatch = Stopwatch.StartNew();
                stream.Write(request, 0, request.Length);

                // Deliberately never drained -- the worker must give up on its own bounded wait
                // instead of hanging for the old 3-second socket timeout or forever.
                string response = ReadAll(stream);
                stopwatch.Stop();

                StringAssert.StartsWith("HTTP/1.1 503", response);
                Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, 700);
                Assert.Less(stopwatch.ElapsedMilliseconds, 2000);
            }
            finally
            {
                listener.Dispose();
            }
        }

        private static BumpRequest WaitForEnqueue(BumpListener listener, TimeSpan timeout)
        {
            DateTime deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (listener.TryDequeue(out BumpRequest request))
                {
                    return request;
                }

                Thread.Sleep(5);
            }

            return null;
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

                sb.Append(Encoding.ASCII.GetString(buffer, 0, read));
            }

            return sb.ToString();
        }
    }
}
