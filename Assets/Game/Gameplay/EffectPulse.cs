using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// One continuous pulsed value (a signed FOV fraction, a volume weight): rises from wherever it is to a
    /// peak over the attack, then eases back to zero over the release. A new trigger while one is running
    /// starts from the current value, so overlapping requests never pop or add up -- the newest peak wins.
    /// </summary>
    public sealed class EffectPulse
    {
        private enum Phase
        {
            Idle,
            Attack,
            Release,
        }

        private Phase _phase;
        private float _from;
        private float _peak;
        private float _attack;
        private float _release;
        private float _elapsed;

        public float Value { get; private set; }

        public bool IsActive => _phase != Phase.Idle;

        public void Trigger(float peak, float attackSeconds, float releaseSeconds)
        {
            _from = Value;
            _peak = peak;
            _attack = Mathf.Max(attackSeconds, 0.0001f);
            _release = Mathf.Max(releaseSeconds, 0.0001f);
            _elapsed = 0f;
            _phase = Phase.Attack;
        }

        public void Step(float dt)
        {
            if (_phase == Phase.Idle)
            {
                return;
            }

            _elapsed += dt;
            if (_phase == Phase.Attack)
            {
                float t = Mathf.Clamp01(_elapsed / _attack);
                Value = Mathf.Lerp(_from, _peak, t);
                if (t < 1f)
                {
                    return;
                }

                _phase = Phase.Release;
                _elapsed = 0f;
                return;
            }

            float u = Mathf.Clamp01(_elapsed / _release);
            float eased = 1f - (1f - u) * (1f - u); // ease-out: leaves the peak fast, settles gently.
            Value = Mathf.Lerp(_peak, 0f, eased);
            if (u >= 1f)
            {
                Reset();
            }
        }

        /// <summary>Back to exactly zero, at once.</summary>
        public void Reset()
        {
            _phase = Phase.Idle;
            Value = 0f;
            _elapsed = 0f;
        }
    }
}
