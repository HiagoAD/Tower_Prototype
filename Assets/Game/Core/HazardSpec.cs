using System;

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

        public bool IsActiveAt(float timeSeconds)
        {
            float t = (timeSeconds - phaseOffsetSeconds) % periodSeconds;
            if (t < 0f)
            {
                t += periodSeconds;
            }

            return t < activeSeconds;
        }
    }
}
