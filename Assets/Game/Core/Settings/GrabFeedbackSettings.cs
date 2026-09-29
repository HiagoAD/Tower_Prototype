using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>Particles at each real hand grab. Sizes and speeds are in body heights so they follow the climber's scale.</summary>
    [Serializable]
    public sealed class GrabFeedbackSettings
    {
        [Tooltip("Master switch for the grab particles.")]
        public bool enabled = true;

        [Tooltip("Particles per grab. Kept small: grabs are the lightest feedback (below hazard hits and bumps).")]
        [Range(0, 16)] public int particlesPerGrab = 5;

        [Tooltip("Particle lifetime in seconds.")]
        [Min(0.05f)] public float lifetimeSeconds = 0.32f;

        [Tooltip("Particle size in body heights.")]
        [Min(0.001f)] public float sizeBodyHeights = 0.055f;

        [Tooltip("Random size variation, 0 = uniform.")]
        [Range(0f, 0.9f)] public float sizeJitter = 0.35f;

        [Tooltip("Outward burst speed in body heights per second.")]
        [Min(0f)] public float speedBodyHeights = 0.45f;

        [Tooltip("How far in front of the hand (toward the camera) the burst starts, in body heights, so it is not hidden by the tower or the hand.")]
        [Min(0f)] public float towardCameraBodyHeights = 0.04f;

        [Tooltip("Hard cap on live particles; the pool never grows past this.")]
        [Range(4, 64)] public int maxParticles = 24;

        [Tooltip("Particle tint, multiplied with the star sprite.")]
        public Color color = new Color(1f, 0.92f, 0.55f, 1f);
    }
}
