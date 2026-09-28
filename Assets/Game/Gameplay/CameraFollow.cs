using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>Fixed-azimuth follow: tracks the player's height only. Shake is applied by a child offset (CameraShake), never here.</summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private PlayerMotor target;
        [Tooltip("Optional. When set, the camera snaps to the climber on every level start and on return to the menu instead of gliding down from the previous run's height.")]
        [SerializeField] private GameSession session;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -12f);
        [Tooltip("Follow rate at the tuned pace; scales with the climb pace so the camera trails by the same share of the climber at any speed.")]
        [SerializeField] private float followLerp = 6f;
        [SerializeField] private ClimbPace pace;
        [Tooltip("Multiplies the follow rate while a webhook bump move plays, so the climber visibly travels on screen and the camera then catches up. Hazard hits are unaffected.")]
        [Min(0.01f)] [SerializeField] private float bumpFollowFactor = 0.25f;

        private void OnEnable()
        {
            if (session != null)
            {
                session.LevelStarted += Snap;
                session.StateChanged += OnStateChanged;
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.LevelStarted -= Snap;
                session.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(SessionState state)
        {
            if (state == SessionState.Menu)
            {
                Snap();
            }
        }

        /// <summary>Jumps straight to the follow position for the target's current height.</summary>
        public void Snap()
        {
            if (target != null)
            {
                transform.position = new Vector3(offset.x, target.Height + offset.y, offset.z);
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = new Vector3(offset.x, target.Height + offset.y, offset.z);
            float rate = followLerp * (pace != null ? pace.PresentationRate : 1f);
            if (target.IsBumpMove)
            {
                rate *= bumpFollowFactor;
            }

            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-rate * Time.deltaTime));
        }
    }
}
