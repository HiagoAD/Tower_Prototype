using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// Fades the climb prompt (laid out over ClimbInputSource's exact touch region -- see
    /// Level1SceneSetup.BuildControlsHint) out once the player has climbed a little. Never
    /// intercepts input itself: the text has raycastTarget = false, set where it's created.
    /// </summary>
    public sealed class ControlsHintView : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private Graphic hintText;
        [Tooltip("Tuning asset; the hud section sets where the hint fades out.")]
        [SerializeField] private GameSettings settings;

        private GameSettings Settings => GameSettings.OrDefaults(settings);

        private float _textBaseAlpha;

        private void Awake()
        {
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
            HudSettings hud = Settings.hud;
            float t = hud.hintFadeDistance > 0f ? Mathf.Clamp01((height - hud.hintFadeStartHeight) / hud.hintFadeDistance) : (height > hud.hintFadeStartHeight ? 1f : 0f);
            float multiplier = 1f - t;

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
