using System.Globalization;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// Reference-style altitude HUD: a vertical bar on the left edge whose yellow fill and marker
    /// (carrying the live height number) rise with the player, the finish height printed above the
    /// bar. Consumes GameSession snapshots/events only -- never
    /// writes gameplay state.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private RectTransform progressFill;
        [SerializeField] private RectTransform progressMarker;
        [SerializeField] private Text heightLabel;
        [SerializeField] private Text finishLabel;
        [SerializeField] private Text levelLabel;
        [Tooltip("Parent of the life icons; shown only when the lives feature is on.")]
        [SerializeField] private GameObject livesRoot;
        [Tooltip("One icon per possible life; icon i shows while i < lives.")]
        [SerializeField] private GameObject[] lifeIcons;
        [Tooltip("Shown only when the pause menu feature is on.")]
        [SerializeField] private GameObject pauseButton;

        [Tooltip("Tuning asset; the hud section scales the altitude readout, which counts the level's authored units so the numbers stay the same at any climb pace.")]
        [SerializeField] private GameSettings settings;

        // ref.png groups thousands with a dot ("6.162", "10.000").
        private static readonly NumberFormatInfo AltitudeFormat = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = ".", NumberGroupSizes = new[] { 3 } };

        private GameSettings Settings => GameSettings.OrDefaults(settings);

        private int _lastHeight = int.MinValue;
        private int _lastFinishHeight = int.MinValue;

        private readonly HeightFeedback _heightFeedback = new HeightFeedback();
        private Color _heightBaseColor;
        private bool _heightBaseCaptured;

        private void OnEnable()
        {
            CaptureHeightBase();
            session.HeightUpdated += OnHeightUpdated;
            session.LevelStarted += OnLevelStarted;
            session.StateChanged += OnStateChanged;
            session.LivesChanged += OnLivesChanged;
            SetActive(livesRoot, session.LivesEnabled);
            SetActive(pauseButton, session.PauseMenuEnabled);
            OnLivesChanged(session.Lives);
        }

        private void OnDisable()
        {
            session.HeightUpdated -= OnHeightUpdated;
            session.LevelStarted -= OnLevelStarted;
            session.StateChanged -= OnStateChanged;
            session.LivesChanged -= OnLivesChanged;
            ResetHeightFeedback();
        }

        private void CaptureHeightBase()
        {
            if (!_heightBaseCaptured && heightLabel != null)
            {
                _heightBaseColor = heightLabel.color;
                _heightBaseCaptured = true;
            }
        }

        private void OnStateChanged(SessionState state)
        {
            // Paused keeps its tint and scale frozen (no HeightUpdated arrives); every other non-playing state clears them.
            if (state != SessionState.Playing && state != SessionState.Paused)
            {
                ResetHeightFeedback();
            }
        }

        private void ResetHeightFeedback()
        {
            _heightFeedback.Reset();
            ApplyHeightFeedback();
        }

        private void ApplyHeightFeedback()
        {
            if (heightLabel == null || !_heightBaseCaptured)
            {
                return;
            }

            HeightFeedbackSettings fx = Settings.heightFx;
            heightLabel.color = _heightFeedback.Evaluate(_heightBaseColor, fx);
            heightLabel.rectTransform.localScale = Vector3.one * _heightFeedback.Scale;
        }

        private void OnLivesChanged(int lives)
        {
            if (lifeIcons == null)
            {
                return;
            }

            for (int i = 0; i < lifeIcons.Length; i++)
            {
                SetActive(lifeIcons[i], i < lives);
            }
        }

        private void OnLevelStarted()
        {
            ResetHeightFeedback();
            if (levelLabel != null)
            {
                levelLabel.text = "LEVEL " + (session.LevelIndex + 1) + "/" + session.LevelCount + "\n" + session.CurrentLevel.displayName.ToUpperInvariant();
            }
        }

        private void OnHeightUpdated(float height, float finishHeight)
        {
            _heightFeedback.Sample(height, session.PlayDeltaTime, Settings.heightFx);
            ApplyHeightFeedback();

            float progress = finishHeight > 0f ? Mathf.Clamp01(height / finishHeight) : 0f;
            SetAnchorTop(progressFill, progress);
            if (progressMarker != null)
            {
                progressMarker.anchorMin = new Vector2(progressMarker.anchorMin.x, progress);
                progressMarker.anchorMax = new Vector2(progressMarker.anchorMax.x, progress);
            }

            float perWorldUnit = Settings.hud.displayUnitsPerWorldUnit / Mathf.Max(session.DistanceScale, 0.0001f);
            int displayHeight = Mathf.RoundToInt(height * perWorldUnit);
            int displayFinish = Mathf.RoundToInt(finishHeight * perWorldUnit);
            if (displayHeight == _lastHeight && displayFinish == _lastFinishHeight)
            {
                return;
            }

            _lastHeight = displayHeight;
            _lastFinishHeight = displayFinish;

            if (heightLabel != null)
            {
                heightLabel.text = displayHeight.ToString("#,0", AltitudeFormat);
            }

            if (finishLabel != null)
            {
                finishLabel.text = displayFinish.ToString("#,0", AltitudeFormat);
            }
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }

        private static void SetAnchorTop(RectTransform rect, float top)
        {
            if (rect != null)
            {
                rect.anchorMax = new Vector2(rect.anchorMax.x, top);
            }
        }
    }
}
