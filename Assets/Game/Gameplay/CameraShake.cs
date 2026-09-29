using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>Child-transform shake offset. Never touches the follow target's own position.</summary>
    public sealed class CameraShake : MonoBehaviour
    {
        [Tooltip("GameSettings.camera holds the shake magnitude. Without it the built-in defaults apply.")]
        [SerializeField] private GameSettings settings;

        private float _timer;
        private float _duration;

        /// <summary>While true the shake holds its timer and current offset. Driven by whoever owns the effect (BumpBurstView follows the session pause).</summary>
        public bool Paused { get; set; }

        public void Shake(float durationSeconds)
        {
            _duration = durationSeconds;
            _timer = durationSeconds;
        }

        /// <summary>Ends any shake now and puts the camera back on its follow position, un-pausing it.</summary>
        public void Cancel()
        {
            _timer = 0f;
            Paused = false;
            transform.localPosition = Vector3.zero;
        }

        private void Update()
        {
            if (Paused)
            {
                return;
            }

            if (_timer <= 0f)
            {
                if (transform.localPosition != Vector3.zero)
                {
                    transform.localPosition = Vector3.zero;
                }

                return;
            }

            _timer -= Time.deltaTime;
            float falloff = Mathf.Clamp01(_timer / Mathf.Max(_duration, 0.0001f));
            transform.localPosition = (Vector3)(Random.insideUnitCircle * GameSettings.OrDefaults(settings).camera.shakeMagnitude * falloff);
        }
    }
}
