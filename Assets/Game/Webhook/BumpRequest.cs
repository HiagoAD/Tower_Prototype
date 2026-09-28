using System;

namespace Game.Webhook
{
    /// <summary>Plain data snapshot of an accepted /bump request. Never crosses threads as a Unity object.</summary>
    public readonly struct BumpRequest
    {
        public readonly string RequestId;
        public readonly string Method;
        public readonly DateTime ReceivedAtUtc;
        public readonly int LevelInstanceId;

        public BumpRequest(string requestId, string method, DateTime receivedAtUtc, int levelInstanceId)
        {
            RequestId = requestId;
            Method = method;
            ReceivedAtUtc = receivedAtUtc;
            LevelInstanceId = levelInstanceId;
        }
    }
}
