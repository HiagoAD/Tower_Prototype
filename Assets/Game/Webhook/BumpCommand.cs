namespace Game.Webhook
{
    /// <summary>
    /// The raw, pre-default fields of one /bump request as the sender wrote them. A null field was
    /// absent (or empty after cleaning); the main thread fills it from the BumpCatalog defaults.
    /// </summary>
    public readonly struct BumpCommand
    {
        public readonly BumpPolarity? Polarity;
        public readonly string TypeId;
        public readonly string Tag;

        public BumpCommand(BumpPolarity? polarity, string typeId, string tag)
        {
            Polarity = polarity;
            TypeId = typeId;
            Tag = tag;
        }
    }
}
