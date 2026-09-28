using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>Child-transform shake offset. Never touches the follow target's own position.</summary>
    public sealed class CameraShake : MonoBehaviour
    {
        [SerializeField] private float magnitude = 0.35f;

        private float _timer;
        private float _duration;

        public void Shake(float durationSeconds)
        {
            _duration = durationSeconds;
            _timer = durationSeconds;
        }

        private void Update()
        {
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
            transform.localPosition = (Vector3)(Random.insideUnitCircle * magnitude * falloff);
        }
    }
}
