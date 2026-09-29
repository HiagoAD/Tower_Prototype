using Game.Core;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>Pure maths for the win heading pop and confetti, kept out of the view so it is testable.</summary>
    public static class VictoryMath
    {
        /// <summary>Pop duration for an intermediate or final win.</summary>
        public static float HeadingSeconds(VictorySettings s, bool final)
        {
            return Mathf.Max(0f, s.headingSeconds * (final ? s.finalHeadingSecondsMultiplier : 1f));
        }

        /// <summary>Heading scale multiplier at normalised time t: start -> overshoot -> exactly 1.</summary>
        public static float HeadingScale(VictorySettings s, bool final, float t)
        {
            t = Mathf.Clamp01(t);
            float peak = 1f + (s.headingOvershoot - 1f) * (final ? s.finalHeadingMultiplier : 1f);
            float rise = Mathf.Clamp(s.headingRiseFraction, 0.1f, 0.9f);
            if (t < rise)
            {
                float u = t / rise;
                float eased = 1f - (1f - u) * (1f - u) * (1f - u);
                return Mathf.LerpUnclamped(s.headingStartScale, peak, eased);
            }

            float v = (t - rise) / (1f - rise);
            return Mathf.LerpUnclamped(peak, 1f, v * v * (3f - 2f * v));
        }

        /// <summary>Heading tilt in degrees at normalised time t; 0 at both ends.</summary>
        public static float HeadingTilt(VictorySettings s, bool final, float t)
        {
            t = Mathf.Clamp01(t);
            float degrees = s.headingWobbleDegrees * (final ? s.finalHeadingMultiplier : 1f);
            return degrees * Mathf.Sin(2f * Mathf.PI * s.headingWobbleCycles * t) * (1f - t);
        }

        /// <summary>Pieces in the first burst.</summary>
        public static int FirstBurstCount(VictorySettings s, bool final)
        {
            return Mathf.Max(0, Mathf.RoundToInt(s.confettiCount * (final ? s.finalConfettiMultiplier : 1f)));
        }

        /// <summary>Pieces in the second burst; 0 for an intermediate win or when the delay is off.</summary>
        public static int SecondBurstCount(VictorySettings s, bool final)
        {
            return final && s.finalSecondBurstDelay > 0f ? FirstBurstCount(s, true) / 2 : 0;
        }

        /// <summary>Alpha at age/lifetime: 1 until the fade share begins, then linear to 0.</summary>
        public static float ConfettiAlpha(float age, float lifetime, float fadeFraction)
        {
            if (lifetime <= 0f)
            {
                return 0f;
            }

            float remaining = 1f - Mathf.Clamp01(age / lifetime);
            return fadeFraction <= 0f ? (remaining > 0f ? 1f : 0f) : Mathf.Clamp01(remaining / fadeFraction);
        }
    }
}
