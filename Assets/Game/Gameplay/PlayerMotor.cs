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
        [Tooltip("Knockback distance in the level's authored units; scaled by DistanceScale.")]
        [SerializeField] private float hitDisplacement = 1.5f;

        // A hit or bump never teleports the player -- it eases them to the new height over
        // knockbackSeconds (down for a hit or negative bump, up for a positive one), then holds
        // for regripSeconds before climb input is accepted again. Both together define
        // ClimbLockoutSeconds, the single source of truth for how long input stays locked.
        [SerializeField] private float knockbackSeconds = 0.35f;
        [SerializeField] private float regripSeconds = 0.25f;

        // Must never exceed ClimbLockoutSeconds: invulnerability exists so a hit can't be
        // re-applied while the player is still being carried through the knockback/re-grip they
        // can't yet respond to, not to grant free passage back through a still-active band. If this
        // outlasts the lockout, a player who holds climb through the whole recovery would pass back
        // through a still-red band for free (the G3 pass-through bug) -- StartDisplacement clamps
        // against ClimbLockoutSeconds below so that can't happen even if this is misconfigured.
        //
        // Separately, IsInvulnerable is always true while the eased move is playing, so an upward
        // bump that carries the climber through a hazard band costs no hit point, even on the
        // move's final frame.
        [SerializeField] private float invulnerabilitySeconds = 0.6f;

        private float _invulnTimer;
        private float _lockoutTimer;

        private bool _knockbackActive;
        private float _knockbackElapsed;
        private float _knockbackStartHeight;
        private float _knockbackTargetHeight;

        public float Height { get; private set; }
        public float FinishHeight { get; set; } = 1f;
        public float ClimbSpeed { get; set; } = 4f;

        /// <summary>Authored-to-play distance factor for the current level (see ClimbPace). Scales the knockback like every other level distance.</summary>
        public float DistanceScale { get; set; } = 1f;

        /// <summary>Set every frame by an input source. Movement only applies while CanClimb is true.</summary>
        public bool ClimbHeld { get; set; }

        public bool CanClimb { get; set; }

        /// <summary>Set by GameSession while paused. Freezes every timer below, not just movement.</summary>
        public bool Paused { get; set; }

        public bool IsInvulnerable => _invulnTimer > 0f || _knockbackActive;

        /// <summary>True only while the eased hit/bump displacement is actively playing.</summary>
        public bool IsInKnockback => _knockbackActive;

        /// <summary>True from the moment a hit lands until climb input is accepted again (knockback + re-grip).</summary>
        public bool IsLockedOut => _lockoutTimer > 0f;

        public float KnockbackSeconds => knockbackSeconds;
        public float ClimbLockoutSeconds => knockbackSeconds + regripSeconds;

        public float NormalizedProgress => FinishHeight <= 0f ? 0f : Mathf.Clamp01(Height / FinishHeight);

        /// <summary>Fired with (previousHeight, newHeight) on every upward or downward change, for hazard contact checks.</summary>
        public event Action<float, float> HeightChanged;

        private void Update()
        {
            if (Paused)
            {
                return; // level clock owns pause semantics -- every timer below freezes with it.
            }

            Step(Time.deltaTime);
        }

        /// <summary>
        /// The actual per-frame update logic, split out from Update() so EditMode tests can drive it
        /// with explicit, deterministic dt instead of waiting on real frame time.
        /// </summary>
        public void Step(float dt)
        {
            if (Paused)
            {
                return;
            }

            if (_invulnTimer > 0f)
            {
                _invulnTimer -= dt;
            }

            if (_lockoutTimer > 0f)
            {
                _lockoutTimer -= dt;
            }

            if (_knockbackActive)
            {
                AdvanceKnockback(dt);
                return;
            }

            if (!CanClimb || !ClimbHeld || _lockoutTimer > 0f)
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

        /// <summary>
        /// Bounded, nonlethal knockback with a re-grip. Returns false if currently invulnerable
        /// (no-op). The displacement itself plays out over several Step() calls (see
        /// AdvanceKnockback) rather than applying instantly.
        /// </summary>
        public bool TryApplyHit()
        {
            return BeginHit(Mathf.Max(0f, Height - hitDisplacement * DistanceScale));
        }

        /// <summary>
        /// Knockback for a hazard band at bandHeight: the usual displacement, or further if that would
        /// leave the climber's head (Height + bodyHeight) still touching the band, so they always come to
        /// rest with the head clearance below it (never below height 0). Same easing, lockout and
        /// invulnerability as TryApplyHit.
        /// </summary>
        public bool TryApplyHazardHit(float bandHeight, float bodyHeight, float clearance)
        {
            float usual = Height - hitDisplacement * DistanceScale;
            float clear = bandHeight - bodyHeight - clearance;
            return BeginHit(Mathf.Max(0f, Mathf.Min(Height, Mathf.Min(usual, clear))));
        }

        private bool BeginHit(float targetHeight)
        {
            if (IsInvulnerable)
            {
                return false;
            }

            StartDisplacement(targetHeight);
            return true;
        }

        /// <summary>
        /// Webhook bump: eases the climber by a signed authored distance (positive = up), scaled by
        /// DistanceScale and clamped to [0, FinishHeight]. Unlike TryApplyHit it always applies, even
        /// while invulnerable. A bump during an eased move adds to that move's target, so rapid bumps
        /// stack their full distances. It never
        /// touches hit points; an upward bump that reaches the finish wins through the normal check.
        /// </summary>
        public void ApplyBump(float authoredDistance)
        {
            float from = _knockbackActive ? _knockbackTargetHeight : Height;
            StartDisplacement(Mathf.Clamp(from + authoredDistance * DistanceScale, 0f, FinishHeight));
        }

        private void StartDisplacement(float targetHeight)
        {
            _knockbackStartHeight = Height;
            _knockbackTargetHeight = targetHeight;
            _knockbackElapsed = 0f;
            _knockbackActive = true;

            // Clamp against ClimbLockoutSeconds -- see the field comment above for why this must
            // never be allowed to outlast the lockout.
            _invulnTimer = Mathf.Min(invulnerabilitySeconds, ClimbLockoutSeconds);
            _lockoutTimer = ClimbLockoutSeconds;
        }

        public void ResetState(float startHeight)
        {
            Height = startHeight;
            _invulnTimer = 0f;
            _lockoutTimer = 0f;
            _knockbackActive = false;
            Paused = false;
            ApplyTransform();
        }

        private void AdvanceKnockback(float dt)
        {
            _knockbackElapsed += dt;
            float t = knockbackSeconds > 0f ? Mathf.Clamp01(_knockbackElapsed / knockbackSeconds) : 1f;
            float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic

            float previous = Height;
            // Upward bumps do cross HazardBand heights; they are covered by the invulnerability
            // window, which spans this whole move, so the crossing costs no hit point.
            float next = Mathf.Lerp(_knockbackStartHeight, _knockbackTargetHeight, eased);
            next = Mathf.Clamp(next, 0f, FinishHeight);

            if (!Mathf.Approximately(next, previous))
            {
                Height = next;
                ApplyTransform();
                HeightChanged?.Invoke(previous, next);
            }

            if (t >= 1f)
            {
                _knockbackActive = false;
            }
        }

        private void ApplyTransform()
        {
            Vector3 pos = transform.position;
            pos.y = Height;
            transform.position = pos;
        }
    }
}
