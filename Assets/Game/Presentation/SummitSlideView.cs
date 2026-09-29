using Game.Core;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// The finish: on Won the climber's visual slides up over the crown's lip and onto its top
    /// surface, turning to face the camera, and stays there. Lives on a transform between the
    /// motor's Player and the "Visual" that carries the pose driver, so the driver's own maths is
    /// untouched and the motor's transform is never written. Playing or Menu snap it back.
    /// </summary>
    public sealed class SummitSlideView : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [Tooltip("Local offset from the climbing pose to the standing pose: up over the lip, then inward onto the top.")]
        [SerializeField] private Vector3 standOffset;
        [Tooltip("Tuning asset; the summit section sets the slide's duration, turn and rise-first share.")]
        [SerializeField] private GameSettings settings;

        private GameSettings Settings => GameSettings.OrDefaults(settings);

        private bool _sliding;
        private float _elapsed;

        public bool IsSliding => _sliding;

        private void OnEnable()
        {
            if (session != null)
            {
                session.StateChanged += OnSessionStateChanged;
                OnSessionStateChanged(session.State);
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.StateChanged -= OnSessionStateChanged;
            }
        }

        private void Update()
        {
            Step(Time.deltaTime);
        }

        public void OnSessionStateChanged(SessionState state)
        {
            if (state == SessionState.Won)
            {
                if (!_sliding)
                {
                    _sliding = true;
                    _elapsed = 0f;
                }
            }
            else if (state == SessionState.Playing || state == SessionState.Menu)
            {
                _sliding = false;
                Apply(0f);
            }
        }

        /// <summary>Advances the slide; holds the final pose once it completes.</summary>
        public void Step(float dt)
        {
            if (!_sliding)
            {
                return;
            }

            _elapsed += dt;
            float slideSeconds = Settings.summit.slideSeconds;
            Apply(slideSeconds > 0f ? Mathf.Clamp01(_elapsed / slideSeconds) : 1f);
        }

        private void Apply(float t)
        {
            float rise = Smooth(t);
            SummitSettings summit = Settings.summit;
            float across = Smooth(Mathf.InverseLerp(summit.riseFirstFraction, 1f, t));
            transform.localPosition = new Vector3(standOffset.x * across, standOffset.y * rise, standOffset.z * across);
            transform.localRotation = Quaternion.Euler(0f, summit.endYawDegrees * Smooth(t), 0f);
        }

        private static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
