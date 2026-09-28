using System;
using System.Threading;

namespace Game.Webhook
{
    /// <summary>
    /// Lifecycle of one accepted-for-consideration /bump request. Never serialized -- purely an
    /// in-memory cross-thread handshake token. Append-only if extended; the numeric values are not
    /// persisted anywhere, but keep the ordering stable for readability.
    /// </summary>
    public enum BumpRequestState
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2,
        Expired = 3,
    }

    /// <summary>
    /// One accepted-for-consideration /bump request, shared between the network worker thread that
    /// created it and the main thread that decides its fate. The worker enqueues this and blocks
    /// (bounded) on <see cref="WaitForResolution"/>; the main thread is the sole authority that
    /// resolves it via <see cref="TryResolve"/>. Exactly one <see cref="TryResolve"/> call can win
    /// the Pending -> terminal transition; every other caller (including the worker's own timeout
    /// path) loses and must not treat the request as theirs. No UnityEngine calls anywhere here.
    /// </summary>
    public sealed class BumpRequest
    {
        public readonly string RequestId;
        public readonly string Method;
        public readonly DateTime ReceivedAtUtc;
        public readonly int LevelInstanceId;

        private readonly ManualResetEventSlim _completed = new ManualResetEventSlim(false);
        private int _state = (int)BumpRequestState.Pending;

        public BumpRequest(string requestId, string method, DateTime receivedAtUtc, int levelInstanceId)
        {
            RequestId = requestId;
            Method = method;
            ReceivedAtUtc = receivedAtUtc;
            LevelInstanceId = levelInstanceId;
        }

        public BumpRequestState State => (BumpRequestState)Volatile.Read(ref _state);

        /// <summary>Blocks the calling thread (the network worker) until resolved or the timeout elapses.</summary>
        public bool WaitForResolution(TimeSpan timeout)
        {
            return _completed.Wait(timeout);
        }

        /// <summary>
        /// Attempts to move Pending -> <paramref name="target"/>. Returns true only if this call won
        /// the transition (state was still Pending); a losing call must not dispatch/act on the
        /// request, since some other thread already decided its fate.
        /// </summary>
        public bool TryResolve(BumpRequestState target)
        {
            int previous = Interlocked.CompareExchange(ref _state, (int)target, (int)BumpRequestState.Pending);
            bool won = previous == (int)BumpRequestState.Pending;
            if (won)
            {
                _completed.Set();
            }

            return won;
        }
    }
}
