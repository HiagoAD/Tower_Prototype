using System;
using System.IO;
using System.Text;
using Game.Webhook;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Exercises BumpListener.TryReadRequest directly against a Stream -- no socket involved --
    /// covering the coalesced-header-and-body bug from the G2 review plus the reject-fast cases.
    /// </summary>
    public sealed class BumpListenerParsingTests
    {
        [Test]
        public void CoalescedHeaderAndBody_ParsesImmediatelyWithoutASecondRead()
        {
            byte[] raw = BuildRequest("POST", "/bump", "hello");
            using var stream = new MemoryStream(raw);

            bool ok = BumpListener.TryReadRequest(stream, out BumpListener.HttpRequestLine requestLine, out byte[] body, out int failureStatusCode);

            Assert.IsTrue(ok);
            Assert.AreEqual("POST", requestLine.Method);
            Assert.AreEqual("/bump", requestLine.Path);
            Assert.AreEqual("hello", Encoding.ASCII.GetString(body));
            // A single MemoryStream backing array means every byte was available on the first Read
            // call the parser made -- if it had discarded the coalesced body and gone looking for
            // more, this would have returned false instead (EOF), not succeeded.
        }

        [Test]
        public void FragmentedDelivery_OneByteAtATime_StillParses()
        {
            byte[] raw = BuildRequest("POST", "/bump", "fragmented-body");
            using var stream = new OneByteAtATimeStream(raw);

            bool ok = BumpListener.TryReadRequest(stream, out BumpListener.HttpRequestLine requestLine, out byte[] body, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual("POST", requestLine.Method);
            Assert.AreEqual("fragmented-body", Encoding.ASCII.GetString(body));
        }

        [Test]
        public void IncompleteBody_EofBeforeContentLength_Fails()
        {
            byte[] headers = Encoding.ASCII.GetBytes(
                "POST /bump HTTP/1.1\r\nHost: x\r\nContent-Length: 10\r\n\r\n");
            byte[] shortBody = Encoding.ASCII.GetBytes("abc"); // only 3 of the promised 10 bytes
            byte[] raw = Concat(headers, shortBody);
            using var stream = new MemoryStream(raw);

            bool ok = BumpListener.TryReadRequest(stream, out _, out _, out int failureStatusCode);

            Assert.IsFalse(ok);
            Assert.AreEqual(400, failureStatusCode);
        }

        [Test]
        public void OversizedContentLength_RejectedWith413_NoBodyRead()
        {
            byte[] headers = Encoding.ASCII.GetBytes(
                "POST /bump HTTP/1.1\r\nHost: x\r\nContent-Length: " + (BumpListener.MaxBodyBytes + 1) + "\r\n\r\n");
            using var stream = new MemoryStream(headers); // no body bytes at all -- must not be read

            bool ok = BumpListener.TryReadRequest(stream, out _, out _, out int failureStatusCode);

            Assert.IsFalse(ok);
            Assert.AreEqual(413, failureStatusCode);
        }

        [Test]
        public void NegativeContentLength_RejectedWith400()
        {
            byte[] raw = Encoding.ASCII.GetBytes("POST /bump HTTP/1.1\r\nHost: x\r\nContent-Length: -5\r\n\r\n");
            using var stream = new MemoryStream(raw);

            bool ok = BumpListener.TryReadRequest(stream, out _, out _, out int failureStatusCode);

            Assert.IsFalse(ok);
            Assert.AreEqual(400, failureStatusCode);
        }

        [Test]
        public void GarbageContentLength_RejectedWith400()
        {
            byte[] raw = Encoding.ASCII.GetBytes("POST /bump HTTP/1.1\r\nHost: x\r\nContent-Length: not-a-number\r\n\r\n");
            using var stream = new MemoryStream(raw);

            bool ok = BumpListener.TryReadRequest(stream, out _, out _, out int failureStatusCode);

            Assert.IsFalse(ok);
            Assert.AreEqual(400, failureStatusCode);
        }

        [Test]
        public void DuplicateConflictingContentLength_RejectedWith400()
        {
            byte[] raw = Encoding.ASCII.GetBytes(
                "POST /bump HTTP/1.1\r\nHost: x\r\nContent-Length: 5\r\nContent-Length: 9\r\n\r\nhello");
            using var stream = new MemoryStream(raw);

            bool ok = BumpListener.TryReadRequest(stream, out _, out _, out int failureStatusCode);

            Assert.IsFalse(ok);
            Assert.AreEqual(400, failureStatusCode);
        }

        [Test]
        public void DuplicateMatchingContentLength_IsNotTreatedAsConflicting()
        {
            byte[] raw = Encoding.ASCII.GetBytes(
                "POST /bump HTTP/1.1\r\nHost: x\r\nContent-Length: 5\r\nContent-Length: 5\r\n\r\nhello");
            using var stream = new MemoryStream(raw);

            bool ok = BumpListener.TryReadRequest(stream, out _, out byte[] body, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual("hello", Encoding.ASCII.GetString(body));
        }

        [Test]
        public void Get_WithNoBody_Succeeds()
        {
            byte[] raw = Encoding.ASCII.GetBytes("GET /bump HTTP/1.1\r\nHost: x\r\n\r\n");
            using var stream = new MemoryStream(raw);

            bool ok = BumpListener.TryReadRequest(stream, out BumpListener.HttpRequestLine requestLine, out byte[] body, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual("GET", requestLine.Method);
            Assert.AreEqual(0, body.Length);
        }

        private static byte[] BuildRequest(string method, string path, string body)
        {
            byte[] bodyBytes = Encoding.ASCII.GetBytes(body);
            byte[] headers = Encoding.ASCII.GetBytes(
                method + " " + path + " HTTP/1.1\r\n" +
                "Host: x\r\n" +
                "Content-Length: " + bodyBytes.Length + "\r\n" +
                "\r\n");
            return Concat(headers, bodyBytes);
        }

        private static byte[] Concat(byte[] a, byte[] b)
        {
            var result = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, result, 0, a.Length);
            Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
            return result;
        }

        /// <summary>Forces TryReadRequest through many small reads instead of one big coalesced one.</summary>
        private sealed class OneByteAtATimeStream : MemoryStream
        {
            public OneByteAtATimeStream(byte[] buffer) : base(buffer)
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return base.Read(buffer, offset, Math.Min(1, count));
            }
        }
    }
}
