using Game.Core;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// Feeds the game's real events to the haptics: a light tick per planted hand, a heavier pulse on a hazard
    /// hit and a pattern on the win. Nothing starts while the app is backgrounded or the session is not Playing.
    /// </summary>
    public sealed class HapticsView : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private ClimberPoseDriver poseDriver;
        [Tooltip("Tuning asset (haptics section).")]
        [SerializeField] private GameSettings settings;

        private HapticsController _controller;

        private HapticsController Controller => _controller ??= new HapticsController(HapticsDevices.Create(), () => GameSettings.OrDefaults(settings).haptics);

        private void OnEnable()
        {
            session.HazardHit += OnHazardHit;
            session.StateChanged += OnStateChanged;
            session.LevelStarted += OnLevelStarted;
            poseDriver.HandPlanted += OnHandPlanted;
            Controller.Suspended = !Application.isFocused;
        }

        private void OnDisable()
        {
            session.HazardHit -= OnHazardHit;
            session.StateChanged -= OnStateChanged;
            session.LevelStarted -= OnLevelStarted;
            poseDriver.HandPlanted -= OnHandPlanted;
            _controller?.Cancel();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            Controller.Suspended = !hasFocus;
            if (!hasFocus)
            {
                Controller.Cancel();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            Controller.Suspended = paused || !Application.isFocused;
            if (paused)
            {
                Controller.Cancel();
            }
        }

        private void OnHandPlanted(Vector3 _)
        {
            Controller.Grab(session.State, Time.unscaledTime);
        }

        private void OnHazardHit()
        {
            Controller.HazardHit(session.State, Time.unscaledTime);
        }

        private void OnStateChanged(SessionState state)
        {
            if (state == SessionState.Won)
            {
                Controller.Won(state, session.IsFinalLevel, Time.unscaledTime);
            }
            else if (state == SessionState.Menu || state == SessionState.Paused)
            {
                Controller.Cancel(); // nothing left buzzing. Not on Lost: the hit that ended the run keeps its pulse.
            }
        }

        private void OnLevelStarted()
        {
            Controller.Cancel();
        }
    }
}
