using System;
using System.Globalization;

namespace Game.Core
{
    /// <summary>
    /// One telegraphed obstacle band: sits at a fixed height and cycles safe/active windows.
    /// Assumption, not observed reference behavior -- see Docs/Development/REFERENCE_OBSERVATION_ADDENDUM.md.
    /// </summary>
    [Serializable]
    public struct HazardSpec
    {
        public float height;
        public float periodSeconds;
        public float activeSeconds;
        public float phaseOffsetSeconds;

        /// <summary>
        /// The single rule for a hazard's numbers, shared by level files and any editor tooling. Returns
        /// null when valid, otherwise "field must ... (got value)". Non-finite values always fail.
        /// An always-active band (activeSeconds >= periodSeconds) is rejected: the level would be uncompletable.
        /// </summary>
        /// <param name="finishHeight">The owning level's finish height; the band must sit below it.</param>
        public string Validate(float finishHeight)
        {
            if (!IsFinite(height) || height <= 0f || !(height < finishHeight))
            {
                return "height must be > 0 and < finishHeight (got " + Format(height) + ")";
            }

            if (!IsFinite(periodSeconds) || periodSeconds <= 0f)
            {
                return "periodSeconds must be > 0 (got " + Format(periodSeconds) + ")";
            }

            if (!IsFinite(activeSeconds) || activeSeconds <= 0f || activeSeconds >= periodSeconds)
            {
                return "activeSeconds must be > 0 and < periodSeconds (got " + Format(activeSeconds) + ")";
            }

            if (!IsFinite(phaseOffsetSeconds))
            {
                return "phaseOffsetSeconds must be a finite number (got " + Format(phaseOffsetSeconds) + ")";
            }

            return null;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static string Format(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public bool IsActiveAt(float timeSeconds)
        {
            if (periodSeconds <= 0f)
            {
                return false;
            }

            float t = (timeSeconds - phaseOffsetSeconds) % periodSeconds;
            if (t < 0f)
            {
                t += periodSeconds;
            }

            return t < activeSeconds;
        }
    }
}
