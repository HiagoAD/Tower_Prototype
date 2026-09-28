using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Webhook;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Covers the main-thread-authoritative acceptance handshake: BumpRequest's Pending -> terminal
    /// state transition, and BumpListener's atomic queue-capacity reservation. Neither test opens a
    /// socket.
    /// </summary>
    public sealed class BumpAcceptanceTests
    {
        [Test]
        public void MainThreadAccept_ResolvesToAccepted()
        {
            var request = NewRequest();

            bool won = request.TryResolve(BumpRequestState.Accepted);

            Assert.IsTrue(won);
            Assert.AreEqual(BumpRequestState.Accepted, request.State);
        }

        [Test]
        public void MainThreadReject_ResolvesToRejected()
        {
            var request = NewRequest();

            bool won = request.TryResolve(BumpRequestState.Rejected);

            Assert.IsTrue(won);
            Assert.AreEqual(BumpRequestState.Rejected, request.State);
        }

        [Test]
        public void WorkerTimeout_ThenExpire_WinsWhenStillPending()
        {
            var request = NewRequest();

            bool resolvedInTime = request.WaitForResolution(TimeSpan.FromMilliseconds(20));
            Assert.IsFalse(resolvedInTime, "nothing resolved this request yet");

            bool expiredWon = request.TryResolve(BumpRequestState.Expired);

            Assert.IsTrue(expiredWon);
            Assert.AreEqual(BumpRequestState.Expired, request.State);
        }

        [Test]
        public void LateMainThreadAccept_AfterExpiry_LosesAndDoesNotDispatch()
        {
            var request = NewRequest();
            Assert.IsTrue(request.TryResolve(BumpRequestState.Expired));

            // Simulates GameSession.DrainBumpQueue reaching this request after the worker already
            // gave up and replied 503 -- the accept must fail so the caller never dispatches it.
            bool lateAcceptWon = request.TryResolve(BumpRequestState.Accepted);

            Assert.IsFalse(lateAcceptWon);
            Assert.AreEqual(BumpRequestState.Expired, request.State);
        }

        [Test]
        public void SecondResolveAttempt_AfterAWinner_AlwaysLoses()
        {
            var request = NewRequest();
            Assert.IsTrue(request.TryResolve(BumpRequestState.Rejected));

            Assert.IsFalse(request.TryResolve(BumpRequestState.Accepted));
            Assert.AreEqual(BumpRequestState.Rejected, request.State);
        }

        [Test]
        public void ConcurrentCapacityReservation_NeverExceedsCapacity()
        {
            var listener = new BumpListener(); // never Started -- no socket, pure in-memory counter
            int attempts = BumpListener.QueueCapacity * 4;
            int successCount = 0;

            Parallel.For(0, attempts, _ =>
            {
                if (listener.TryReserveCapacity())
                {
                    Interlocked.Increment(ref successCount);
                }
            });

            Assert.AreEqual(BumpListener.QueueCapacity, successCount);
        }

        private static BumpRequest NewRequest()
        {
            return new BumpRequest("req-1", "POST", DateTime.UtcNow, levelInstanceId: 1);
        }
    }
}
