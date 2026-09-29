using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The single climb-speed setting, in climber body heights per second -- the pace the player
    /// sees. Everything else follows from it:
    /// - the world climb speed is the pace times the climber's height;
    /// - every level's authored distances (finish, hazard heights, knockback) scale by
    ///   DistanceScaleFor, the same factor as its speed, so each level keeps its authored duration
    ///   and hazard timing at any pace;
    /// - presentation rates (the climber's body swing, camera follow) scale by PresentationRate,
    ///   so the climb looks the same, only faster or slower.
    /// Levels stay authored in world units at their own climbSpeed. A section of GameSettings.
    /// </summary>
    [Serializable]
    public sealed class ClimbPace
    {
        /// <summary>The pace the climber's swing and the camera follow were tuned at.</summary>
        public const float TunedBodyHeightsPerSecond = 0.92f;

        public const float MinBodyHeightsPerSecond = 0.4f;
        public const float MaxBodyHeightsPerSecond = 2f;

        [Tooltip("Climb speed in climber body heights per second.")]
        [Range(MinBodyHeightsPerSecond, MaxBodyHeightsPerSecond)]
        [SerializeField] private float bodyHeightsPerSecond = TunedBodyHeightsPerSecond;

        [Tooltip("The climber's height in world units. Written by the scene builder; 0 (unmeasured) plays every level exactly as authored.")]
        [Min(0f)][SerializeField] private float bodyHeight;

        public float BodyHeightsPerSecond => bodyHeightsPerSecond;

        public float WorldSpeed => bodyHeightsPerSecond * bodyHeight;

        public float PresentationRate => bodyHeightsPerSecond / TunedBodyHeightsPerSecond;

        /// <summary>
        /// Factor from a level's authored units to play units: its authored climbSpeed becomes
        /// WorldSpeed, and scaling every authored distance by the same factor leaves every duration
        /// unchanged. 1 until the scene builder has measured the body height.
        /// </summary>
        public float DistanceScaleFor(LevelDefinition level)
        {
            return bodyHeight > 0f && level != null && level.climbSpeed > 0f ? WorldSpeed / level.climbSpeed : 1f;
        }

        /// <summary>The largest DistanceScaleFor this level can reach within the allowed pace range, for sizing the scene.</summary>
        public static float MaxDistanceScaleFor(LevelDefinition level, float bodyHeight)
        {
            return level != null && level.climbSpeed > 0f ? MaxBodyHeightsPerSecond * bodyHeight / level.climbSpeed : 1f;
        }
    }
}
