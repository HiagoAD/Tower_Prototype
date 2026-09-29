using Game.Core;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>Shows/hides menu panels from GameSession.StateChanged. Buttons call GameSession methods directly.</summary>
    public sealed class MenuView : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject hudPanel;
        [Tooltip("Shown instead of the win panel after the last level: campaign complete, Exit only.")]
        [SerializeField] private GameObject finalWinPanel;
        [SerializeField] private GameObject winPanel;
        [Tooltip("Shown while paused, only when the pause menu feature is on.")]
        [SerializeField] private GameObject pausePanel;
        [Tooltip("Shown when the run is lost (lives feature); its Continue retries the level.")]
        [SerializeField] private GameObject losePanel;
        [Tooltip("Seconds after the win before its panel appears, so the climber's slide onto the summit plays first.")]
        [SerializeField] private float winPanelDelaySeconds = 0.8f;

        private float _winDelayRemaining;

        private void OnEnable()
        {
            session.StateChanged += OnStateChanged;
            OnStateChanged(session.State);
        }

        private void OnDisable()
        {
            session.StateChanged -= OnStateChanged;
        }

        private void Update()
        {
            if (_winDelayRemaining <= 0f)
            {
                return;
            }

            _winDelayRemaining -= Time.unscaledDeltaTime;
            if (_winDelayRemaining <= 0f && session.State == SessionState.Won)
            {
                ShowWinPanel();
            }
        }

        private void OnStateChanged(SessionState state)
        {
            _winDelayRemaining = state == SessionState.Won ? winPanelDelaySeconds : 0f;
            SetActive(mainMenuPanel, state == SessionState.Menu);
            SetActive(hudPanel, state == SessionState.Playing || state == SessionState.Paused);
            SetActive(pausePanel, state == SessionState.Paused && session.PauseMenuEnabled);
            SetActive(losePanel, state == SessionState.Lost);
            SetActive(winPanel, false);
            SetActive(finalWinPanel, false);
            if (state == SessionState.Won && winPanelDelaySeconds <= 0f)
            {
                ShowWinPanel();
            }
        }

        private void ShowWinPanel()
        {
            bool campaignComplete = session.IsFinalLevel;
            SetActive(winPanel, !campaignComplete);
            SetActive(finalWinPanel, campaignComplete);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }
    }
}
