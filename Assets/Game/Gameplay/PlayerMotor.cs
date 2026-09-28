using System;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// Sole owner of the player's gameplay height/position. Presentation never writes this
    /// transform directly -- it only reads Height/NormalizedProgress or listens to events.
    /// </summary>
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private float hitDisplacement = 1.5f;
        [SerializeField] private float invulnerabilitySeconds = 0.6f;

        private float _invulnTimer;

        public float Height { get; private set; }
        public float FinishHeight { get; set; } = 1f;
        public float ClimbSpeed { get; set; } = 4f;

        /// <summary>Set every frame by an input source. Movement only applies while CanClimb is true.</summary>
        public bool ClimbHeld { get; set; }

        public bool CanClimb { get; set; }

        public bool IsInvulnerable => _invulnTimer > 0f;

        public float NormalizedProgress => FinishHeight <= 0f ? 0f : Mathf.Clamp01(Height / FinishHeight);

        /// <summary>Fired with (previousHeight, newHeight) on every upward or downward change, for hazard crossing checks.</summary>
        public event Action<float, float> HeightChanged;

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_invulnTimer > 0f)
            {
                _invulnTimer -= dt;
            }

            if (!CanClimb || !ClimbHeld)
            {
                return;
            }

            float previous = Height;
            float next = Mathf.Min(Height + ClimbSpeed * dt, FinishHeight);
            if (Mathf.Approximately(next, previous))
            {
                return;
            }

            Height = next;
            ApplyTransform();
            HeightChanged?.Invoke(previous, next);
        }

        /// <summary>Bounded, nonlethal downward displacement with a re-grip. Returns false if currently invulnerable (no-op).</summary>
        public bool TryApplyHit()
        {
            if (IsInvulnerable)
            {
                return false;
            }

            float previous = Height;
            Height = Mathf.Max(0f, Height - hitDisplacement);
            ApplyTransform();
            _invulnTimer = invulnerabilitySeconds;
            HeightChanged?.Invoke(previous, Height);
            return true;
        }

        public void ResetState(float startHeight)
        {
            Height = startHeight;
            _invulnTimer = 0f;
            ApplyTransform();
        }

        private void ApplyTransform()
        {
            Vector3 pos = transform.position;
            pos.y = Height;
            transform.position = pos;
        }
    }
}
