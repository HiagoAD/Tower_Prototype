using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// Fades the climb-touch-region hint (a faint bottom band + a short prompt, both built over
    /// ClimbInputSource's exact touch region -- see Level1SceneSetup.BuildControlsHint) out once the
    /// player has climbed a little. Never intercepts input itself: both graphics have
    /// raycastTarget = false, set where they're created.
    /// </summary>
    public sealed class ControlsHintView : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private Graphic hintBand;
        [SerializeField] private Graphic hintText;
        [SerializeField] private float fadeStartHeight = 1.5f;
        [SerializeField] private float fadeDistance = 2.5f;

        private float _bandBaseAlpha;
        private float _textBaseAlpha;

        private void Awake()
        {
            _bandBaseAlpha = hintBand != null ? hintBand.color.a : 0f;
            _textBaseAlpha = hintText != null ? hintText.color.a : 0f;
        }

        private void OnEnable()
        {
            if (session != null)
            {
                session.HeightUpdated += OnHeightUpdated;
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.HeightUpdated -= OnHeightUpdated;
            }
        }

        private void OnHeightUpdated(float height, float finishHeight)
        {
            float t = fadeDistance > 0f ? Mathf.Clamp01((height - fadeStartHeight) / fadeDistance) : (height > fadeStartHeight ? 1f : 0f);
            float multiplier = 1f - t;

            SetAlpha(hintBand, _bandBaseAlpha * multiplier);
            SetAlpha(hintText, _textBaseAlpha * multiplier);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null)
            {
                return;
            }

            Color c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }
    }
}
