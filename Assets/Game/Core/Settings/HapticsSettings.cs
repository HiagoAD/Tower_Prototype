using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Device vibration on grabs, hazard hits and wins. Hand grab &lt; hazard hit; the win has its own
    /// pattern. Strengths are 0..1 of the device's amplitude range, scaled again by intensity.
    /// </summary>
    [Serializable]
    public sealed class HapticsSettings
    {
        [Tooltip("Master switch. Off, nothing vibrates.")]
        public bool enabled = true;

        [Tooltip("Overall strength, 0..1. Scales every pulse's amplitude and duration; 0 is off.")]
        [Range(0f, 1f)] public float intensity = 1f;

        [Tooltip("Light pulse on a real hand grab: duration in milliseconds.")]
        [Range(1, 100)] public int grabMilliseconds = 18;

        [Tooltip("Light pulse on a real hand grab: strength, 0..1.")]
        [Range(0f, 1f)] public float grabStrength = 0.3f;

        [Tooltip("Minimum seconds between grab pulses, so a fast pace does not turn into a buzz.")]
        [Min(0f)] public float grabMinIntervalSeconds = 0.18f;

        [Tooltip("Hazard hit pulse: duration in milliseconds.")]
        [Range(1, 500)] public int hitMilliseconds = 70;

        [Tooltip("Hazard hit pulse: strength, 0..1. Above a grab.")]
        [Range(0f, 1f)] public float hitStrength = 0.85f;

        [Tooltip("Win pulse: duration in milliseconds.")]
        [Range(1, 500)] public int winMilliseconds = 110;

        [Tooltip("Win pulse: strength, 0..1.")]
        [Range(0f, 1f)] public float winStrength = 0.7f;

        [Tooltip("Pulses in the final TOWER CLEARED win (a level win is a single pulse).")]
        [Range(1, 6)] public int finalWinPulses = 3;

        [Tooltip("Gap between the final win's pulses in milliseconds.")]
        [Range(20, 400)] public int finalWinGapMilliseconds = 90;

        [Tooltip("Seconds after a hit or win pulse during which grab pulses are skipped, so a light tick never blurs a heavy one.")]
        [Min(0f)] public float grabQuietAfterHeavySeconds = 0.35f;
    }
}
