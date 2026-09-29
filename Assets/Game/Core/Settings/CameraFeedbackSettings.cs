using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Camera and screen effects for bumps, hazard hits and the win (zoom pulse, shake, post-processing,
    /// victory camera move). Intensity hierarchy: hand grab &lt; hazard hit &lt; community bump. A
    /// value of 0 turns the matching effect off; the enable flags switch a whole group.
    /// </summary>
    [Serializable]
    public sealed class CameraFeedbackSettings
    {
        [Header("Bump timing")]
        [Tooltip("Seconds the field of view takes to reach a bump's peak, starting at the glove impact.")]
        [Min(0.001f)] public float pulseAttackSeconds = 0.07f;

        [Tooltip("Seconds the zoom and post-processing pulses take to ease back to nothing after the peak.")]
        [Min(0.01f)] public float pulseReleaseSeconds = 0.55f;

        [Header("Bump zoom (field of view, percent of the base FOV; positive = wider/pull-out, negative = narrower/punch-in)")]
        [Tooltip("Pull-out on a positive (lift) bump. 0 = no zoom.")]
        [Range(-40f, 40f)] public float positiveFovPercent = 10f;

        [Tooltip("Punch-in on a negative (knock-down) bump. 0 = no zoom.")]
        [Range(-40f, 40f)] public float negativeFovPercent = -12f;

        [Header("Shake")]
        [Tooltip("Seconds a bump's shake lasts from the glove impact, fading linearly.")]
        [Min(0f)] public float bumpShakeSeconds = 0.9f;

        [Tooltip("Share of CameraSettings.shakeMagnitude used by a positive bump.")]
        [Min(0f)] public float positiveShakeScale = 0.8f;

        [Tooltip("Share of CameraSettings.shakeMagnitude used by a negative bump.")]
        [Min(0f)] public float negativeShakeScale = 1f;

        [Tooltip("Peak offset of the small shake on a confirmed hazard hit, in world units. Keep it well under CameraSettings.shakeMagnitude. 0 = no hit shake.")]
        [Min(0f)] public float hitShakeMagnitude = 0.55f;

        [Tooltip("Seconds the hazard-hit shake lasts, fading linearly.")]
        [Min(0f)] public float hitShakeSeconds = 0.25f;

        [Header("Bump flash tint (multiplies the full-screen impact flash)")]
        public Color positiveFlashTint = new Color(0.72f, 0.88f, 1f, 1f);
        public Color negativeFlashTint = new Color(1f, 0.7f, 0.55f, 1f);

        [Header("Post-processing (URP volume weights; only rendered while a pulse is active)")]
        [Tooltip("Master switch for the chromatic aberration and bloom pulses. Off costs nothing at run time.")]
        public bool postProcessingEnabled = true;

        [Tooltip("Chromatic aberration intensity at full weight (0..1). 0 = off.")]
        [Range(0f, 1f)] public float aberrationIntensity = 0.5f;

        [Tooltip("Bloom intensity at full weight. 0 = off.")]
        [Min(0f)] public float bloomIntensity = 0.9f;

        [Tooltip("Bloom brightness threshold (linear).")]
        [Min(0f)] public float bloomThreshold = 0.9f;

        [Tooltip("Bloom spread (0..1).")]
        [Range(0f, 1f)] public float bloomScatter = 0.6f;

        [Tooltip("Aberration weight of a positive bump (share of the full intensity).")]
        [Range(0f, 1f)] public float positiveAberrationWeight = 0.35f;

        [Tooltip("Bloom weight of a positive bump: a warm glow.")]
        [Range(0f, 1f)] public float positiveBloomWeight = 1f;

        [Tooltip("Aberration weight of a negative bump: the sting.")]
        [Range(0f, 1f)] public float negativeAberrationWeight = 1f;

        [Tooltip("Bloom weight of a negative bump.")]
        [Range(0f, 1f)] public float negativeBloomWeight = 0.35f;

        [Header("Victory camera move")]
        [Tooltip("Switches the whole win camera move (push-in, arc and pulse).")]
        public bool victoryCameraEnabled = true;

        [Tooltip("Seconds after the win before the camera starts to move. The summit slide starts at once, so a small value lets it lead.")]
        [Min(0f)] public float victoryStartDelaySeconds = 0.1f;

        [Tooltip("Seconds the push-in and arc take, eased; the camera then holds until the next level, retry or menu.")]
        [Min(0.05f)] public float victoryMoveSeconds = 1.6f;

        [Tooltip("Field-of-view change of the push-in, percent of the base FOV (negative = closer). 0 = no push-in.")]
        [Range(-40f, 0f)] public float victoryPushInFovPercent = -16f;

        [Tooltip("World units the camera drops during the move, so the zoom stays centred on the climber standing on the summit. 0 = none.")]
        public float victoryCameraDropWorldUnits = 2f;

        [Tooltip("Front-facing arc: swing the camera around the climber by this many degrees to the side (0 = no arc). Kept small because the tower is only modelled on the camera side.")]
        [Range(0f, 45f)] public float victoryArcDegrees = 10f;

        [Tooltip("Switches the arc alone; the push-in stays.")]
        public bool victoryArcEnabled = true;

        [Tooltip("Aberration weight of the pulse at the start of the win move.")]
        [Range(0f, 1f)] public float victoryAberrationWeight = 0.45f;

        [Tooltip("Bloom weight of the pulse at the start of the win move.")]
        [Range(0f, 1f)] public float victoryBloomWeight = 0.6f;

        [Tooltip("Seconds the win pulse takes to fade after its peak.")]
        [Min(0.01f)] public float victoryPulseReleaseSeconds = 0.9f;
    }
}
