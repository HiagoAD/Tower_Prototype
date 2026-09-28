using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// One kind of /bump the server can name: how it looks and how far it moves the climber. Data
    /// only -- adding a type is a new entry in the BumpCatalog asset, no code.
    /// </summary>
    [Serializable]
    public sealed class BumpType
    {
        [Tooltip("The id the server sends as `type` (matched case-insensitively), e.g. boxing.")]
        public string id;

        [Tooltip("Shown on the event card's detail line as `<name>*1`.")]
        public string displayName;

        [Tooltip("Card badge and burst projectile sprite. Leave empty to fall back to the burst view's glove.")]
        public Sprite icon;

        [Tooltip("Tint of the badge and the burst projectiles.")]
        public Color iconTint = Color.white;

        [Tooltip("Distance a positive bump lifts the climber, in the level's authored units (scaled by the climb pace like every level distance).")]
        [Min(0f)] public float liftDistance;

        [Tooltip("Distance a negative bump knocks the climber down, in the level's authored units (scaled by the climb pace).")]
        [Min(0f)] public float dropDistance;
    }
}
