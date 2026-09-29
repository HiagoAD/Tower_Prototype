using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>The altitude readout's reaction to climbing and losing height (tint and scale).</summary>
    [Serializable]
    public sealed class HeightFeedbackSettings
    {
        [Tooltip("Height change rate, in world units per second, below which the height counts as stable. Keeps float noise and tiny drift from tinting the label.")]
        [Min(0f)] public float deadbandUnitsPerSecond = 0.05f;

        [Tooltip("Seconds a gain or loss tint is kept after the height stops changing that way, so a one-frame pause between climb steps does not flicker.")]
        [Min(0f)] public float holdSeconds = 0.15f;

        [Tooltip("Label tint while gaining height.")]
        public Color gainColor = new Color(0.30f, 0.92f, 0.32f, 1f);

        [Tooltip("Label tint while losing height (hazard knockback or a negative bump).")]
        public Color lossColor = new Color(1f, 0.26f, 0.22f, 1f);

        [Tooltip("Label scale multiplier while actively climbing.")]
        [Min(1f)] public float gainScale = 1.08f;

        [Tooltip("Label scale multiplier while losing height. Larger than the climb scale so a setback reads as a hit.")]
        [Min(1f)] public float lossScale = 1.25f;

        [Tooltip("Exponential response of tint and scale toward their targets, per second. Higher is snappier.")]
        [Min(0.1f)] public float responseRate = 14f;
    }
}
