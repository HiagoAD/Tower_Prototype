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
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;

        private void OnEnable()
        {
            session.StateChanged += OnStateChanged;
            OnStateChanged(session.State);
        }

        private void OnDisable()
        {
            session.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(SessionState state)
        {
            SetActive(mainMenuPanel, state == SessionState.Menu);
            SetActive(hudPanel, state == SessionState.Playing || state == SessionState.Paused);
            SetActive(pausePanel, state == SessionState.Paused);
            SetActive(winPanel, state == SessionState.Won);
            SetActive(losePanel, state == SessionState.Lost);
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
