using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>Independent shake sources. The offset follows the strongest active one, never their sum, so overlapping requests cannot pile up.</summary>
    public enum ShakeChannel
    {
        Bump = 0,
        Hit = 1,
    }

    /// <summary>
    /// Child-transform shake offset. Never touches the follow target's own position. Each channel holds its
    /// own timer and peak; the offset uses the largest current amplitude, and lands exactly on zero when
    /// every channel is done.
    /// </summary>
    public sealed class CameraShake : MonoBehaviour
    {
        private const int ChannelCount = 2;

        [Tooltip("GameSettings.camera holds the shake magnitude. Without it the built-in defaults apply.")]
        [SerializeField] private GameSettings settings;

        private readonly float[] _timers = new float[ChannelCount];
        private readonly float[] _durations = new float[ChannelCount];
        private readonly float[] _magnitudes = new float[ChannelCount];

        /// <summary>While true the shake holds its timers and current offset. Driven by whoever owns the effect (CameraEffects follows the session pause).</summary>
        public bool Paused { get; set; }

        /// <summary>The largest amplitude among the active channels right now, in world units.</summary>
        public float CurrentAmplitude
        {
            get
            {
                float amplitude = 0f;
                for (int i = 0; i < ChannelCount; i++)
                {
                    amplitude = Mathf.Max(amplitude, AmplitudeOf(i));
                }

                return amplitude;
            }
        }

        public bool IsShaking => CurrentAmplitude > 0f;

        /// <summary>A bump-channel shake at the settings' bump magnitude.</summary>
        public void Shake(float durationSeconds)
        {
            Shake(ShakeChannel.Bump, durationSeconds, GameSettings.OrDefaults(settings).camera.shakeMagnitude);
        }

        /// <summary>
        /// Starts a shake on a channel. A new request replaces that channel's running shake only when it
        /// is at least as strong as what is left of it.
        /// </summary>
        public void Shake(ShakeChannel channel, float durationSeconds, float magnitude)
        {
            int i = (int)channel;
            if (i < 0 || i >= ChannelCount || durationSeconds <= 0f || magnitude <= 0f)
            {
                return;
            }

            if (magnitude < AmplitudeOf(i))
            {
                return;
            }

            _durations[i] = durationSeconds;
            _timers[i] = durationSeconds;
            _magnitudes[i] = magnitude;
        }

        /// <summary>Ends every shake now and puts the camera back on its follow position, un-pausing it.</summary>
        public void Cancel()
        {
            for (int i = 0; i < ChannelCount; i++)
            {
                _timers[i] = 0f;
            }

            Paused = false;
            transform.localPosition = Vector3.zero;
        }

        /// <summary>Ends one channel's shake, leaving the others running.</summary>
        public void Cancel(ShakeChannel channel)
        {
            int i = (int)channel;
            if (i >= 0 && i < ChannelCount)
            {
                _timers[i] = 0f;
            }
        }

        private float AmplitudeOf(int channel)
        {
            if (_timers[channel] <= 0f)
            {
                return 0f;
            }

            return _magnitudes[channel] * Mathf.Clamp01(_timers[channel] / Mathf.Max(_durations[channel], 0.0001f));
        }

        private void Update()
        {
            Step(Time.deltaTime);
        }

        /// <summary>The per-frame update, split out so EditMode tests can drive it with an explicit dt.</summary>
        public void Step(float dt)
        {
            if (Paused)
            {
                return;
            }

            for (int i = 0; i < ChannelCount; i++)
            {
                if (_timers[i] > 0f)
                {
                    _timers[i] = Mathf.Max(0f, _timers[i] - dt);
                }
            }

            float amplitude = CurrentAmplitude;
            if (amplitude <= 0f)
            {
                // Assigned unconditionally: Vector3 != is approximate, so a guard would keep a last-frame offset below its tolerance.
                transform.localPosition = Vector3.zero;
                return;
            }

            transform.localPosition = (Vector3)(Random.insideUnitCircle * amplitude);
        }
    }
}
