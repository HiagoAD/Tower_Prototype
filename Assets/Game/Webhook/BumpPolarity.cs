namespace Game.Webhook
{
    /// <summary>
    /// Which way a /bump pushes the climber. Serialized by index (BumpCatalog.defaultPolarity):
    /// append new members at the end only.
    /// </summary>
    public enum BumpPolarity
    {
        Positive = 0,
        Negative = 1,
    }
}
