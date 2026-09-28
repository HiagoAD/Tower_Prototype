using Game.Webhook;

namespace Game.Core
{
    /// <summary>An accepted bump with the catalog defaults already applied; what the presentation reacts to.</summary>
    public readonly struct BumpEvent
    {
        public readonly string RequestId;
        public readonly BumpPolarity Polarity;
        public readonly BumpType Type;
        public readonly string Tag;

        public BumpEvent(string requestId, BumpPolarity polarity, BumpType type, string tag)
        {
            RequestId = requestId;
            Polarity = polarity;
            Type = type;
            Tag = tag;
        }
    }
}
