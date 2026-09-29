using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The win celebration: heading pop tween and confetti. Runs on unscaled time after the summit slide
    /// (the win panel's own delay), and the final TOWER CLEARED screen scales the same knobs up.
    /// </summary>
    [Serializable]
    public sealed class VictorySettings
    {
        [Header("Heading pop")]
        [Tooltip("Seconds for the win heading to pop in (scale up past 1, settle back, wobble out).")]
        public float headingSeconds = 0.6f;
        [Tooltip("Heading scale at the start of the pop.")]
        public float headingStartScale = 0.2f;
        [Tooltip("Peak heading scale reached before settling to 1.")]
        public float headingOvershoot = 1.25f;
        [Tooltip("Fraction of the pop spent rising to the overshoot; the rest settles back to 1.")]
        [Range(0.1f, 0.9f)] public float headingRiseFraction = 0.6f;
        [Tooltip("Peak heading tilt in degrees; fades to 0 by the end of the pop.")]
        public float headingWobbleDegrees = 6f;
        [Tooltip("Full wobble swings during the pop.")]
        public float headingWobbleCycles = 2f;

        [Header("Confetti")]
        [Tooltip("Pieces in the win burst.")]
        public int confettiCount = 60;
        [Tooltip("Piece colours, cycled at random. Empty falls back to white.")]
        public Color[] confettiColors =
        {
            new Color(1f, 0.85f, 0.1f, 1f),
            new Color(1f, 0.3f, 0.3f, 1f),
            new Color(0.3f, 0.75f, 1f, 1f),
            new Color(0.45f, 0.9f, 0.4f, 1f),
            new Color(1f, 0.55f, 0.85f, 1f),
        };
        [Tooltip("Piece lifetime range in seconds.")]
        public Vector2 confettiLifetime = new Vector2(1.6f, 2.6f);
        [Tooltip("Launch speed range in canvas units per second (canvas is 1080 wide).")]
        public Vector2 confettiSpeed = new Vector2(500f, 1100f);
        [Tooltip("Downward pull in canvas units per second squared.")]
        public float confettiGravity = 1400f;
        [Tooltip("Piece size range in canvas units.")]
        public Vector2 confettiSize = new Vector2(22f, 44f);
        [Tooltip("Peak spin in degrees per second (random sign).")]
        public float confettiSpinDegrees = 360f;
        [Tooltip("Share of the lifetime spent fading out at the end.")]
        [Range(0f, 1f)] public float confettiFadeFraction = 0.3f;

        [Header("Final win (TOWER CLEARED)")]
        [Tooltip("Multiplier on the heading overshoot's excess over 1 and on the wobble.")]
        public float finalHeadingMultiplier = 1.5f;
        [Tooltip("Multiplier on the heading pop duration.")]
        public float finalHeadingSecondsMultiplier = 1.5f;
        [Tooltip("Multiplier on the confetti count.")]
        public float finalConfettiMultiplier = 2f;
        [Tooltip("Multiplier on the confetti lifetime.")]
        public float finalLifetimeMultiplier = 1.5f;
        [Tooltip("Seconds after the first burst before the final win's second burst (0 = no second burst).")]
        public float finalSecondBurstDelay = 0.7f;
    }
}
