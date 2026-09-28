using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>Consumes GameSession snapshots/events only -- never writes gameplay state.</summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private Text heightText;
        [SerializeField] private Text hitPointsText;
        [SerializeField] private Text bumpFeedText;

        private float _bumpFeedTimer;
        private int _lastHeight = int.MinValue;
        private int _lastFinishHeight = int.MinValue;

        private void OnEnable()
        {
            session.HeightUpdated += OnHeightUpdated;
            session.HitPointsChanged += OnHitPointsChanged;
            session.BumpAccepted += OnBumpAccepted;
        }

        private void OnDisable()
        {
            session.HeightUpdated -= OnHeightUpdated;
            session.HitPointsChanged -= OnHitPointsChanged;
            session.BumpAccepted -= OnBumpAccepted;
        }

        private void Update()
        {
            if (_bumpFeedTimer <= 0f)
            {
                return;
            }

            _bumpFeedTimer -= Time.deltaTime;
            if (_bumpFeedTimer <= 0f && bumpFeedText != null)
            {
                bumpFeedText.text = string.Empty;
            }
        }

        private void OnHeightUpdated(float height, float finishHeight)
        {
            int flooredHeight = Mathf.FloorToInt(height);
            int flooredFinish = Mathf.FloorToInt(finishHeight);
            if (flooredHeight == _lastHeight && flooredFinish == _lastFinishHeight)
            {
                return;
            }

            _lastHeight = flooredHeight;
            _lastFinishHeight = flooredFinish;

            if (heightText != null)
            {
                heightText.text = flooredHeight + "m / " + flooredFinish + "m";
            }
        }

        private void OnHitPointsChanged(int hitPoints)
        {
            if (hitPointsText != null)
            {
                hitPointsText.text = "HP: " + hitPoints;
            }
        }

        private void OnBumpAccepted(string requestId)
        {
            if (bumpFeedText != null)
            {
                bumpFeedText.text = "BUMP! " + requestId.Substring(0, 8);
            }

            _bumpFeedTimer = 2f;
        }
    }
}
