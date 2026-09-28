using Game.Core;
using Game.Webhook;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// Stack of reference-style event cards on one screen edge: every accepted webhook bump of this
    /// view's polarity pushes a card (sender line = the sender's tag, detail line = "Boxing*1", badge
    /// = the type's icon) onto the top slot, older cards shift down, and each fades out after its
    /// lifetime. One instance per side -- positive on the left, negative on the right. Slots are
    /// pre-built in the scene; this only moves text, badge and alpha between them.
    /// </summary>
    public sealed class EventFeedView : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [Tooltip("Only bumps of this polarity get a card here.")]
        [SerializeField] private BumpPolarity polarity;
        [SerializeField] private CanvasGroup[] cards;
        [SerializeField] private Text[] senderTexts;
        [SerializeField] private Text[] detailTexts;
        [SerializeField] private Image[] badgeIcons;
        [SerializeField] private Image[] badgeOutlines;
        [SerializeField] private float lifetimeSeconds = 4f;
        [SerializeField] private float fadeSeconds = 0.5f;
        [SerializeField] private float popSeconds = 0.18f;

        private const float PopStartScale = 1.12f;

        private float[] _ages;
        private Vector3[] _baseScales;

        private void Awake()
        {
            _ages = new float[cards.Length];
            _baseScales = new Vector3[cards.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                _baseScales[i] = cards[i].transform.localScale; // the scene builder's card scale; the pop multiplies into it.
                _ages[i] = float.MaxValue;
                cards[i].alpha = 0f;
            }
        }

        private void OnEnable()
        {
            session.BumpAccepted += OnBumpAccepted;
            session.StateChanged += OnStateChanged;
            session.LevelStarted += ClearCards;
        }

        private void OnDisable()
        {
            session.BumpAccepted -= OnBumpAccepted;
            session.StateChanged -= OnStateChanged;
            session.LevelStarted -= ClearCards;
        }

        private void OnStateChanged(SessionState state)
        {
            if (state == SessionState.Menu)
            {
                ClearCards();
            }
        }

        /// <summary>Drops every card at once, so nothing from the previous run shows on the next.</summary>
        private void ClearCards()
        {
            for (int i = 0; i < _ages.Length; i++)
            {
                _ages[i] = float.MaxValue;
                cards[i].alpha = 0f;
            }
        }

        private void OnBumpAccepted(BumpEvent bump)
        {
            if (bump.Polarity != polarity)
            {
                return;
            }

            for (int i = cards.Length - 1; i > 0; i--)
            {
                senderTexts[i].text = senderTexts[i - 1].text;
                detailTexts[i].text = detailTexts[i - 1].text;
                CopyBadge(i - 1, i);
                _ages[i] = _ages[i - 1];
            }

            SetLine(senderTexts[0], bump.Tag);
            SetLine(detailTexts[0], bump.Type.displayName + "*1");
            SetBadge(0, bump.Type.icon, bump.Type.iconTint);

            _ages[0] = 0f;
        }

        /// <summary>Keeps the text on one line: a value wider than its slot is cut and ends in an ellipsis.</summary>
        private static void SetLine(Text text, string value)
        {
            TextGenerationSettings settings = text.GetGenerationSettings(Vector2.zero);
            float maxWidth = text.rectTransform.rect.width;
            var generator = new TextGenerator();
            string shown = value;
            while (shown.Length > 0 && generator.GetPreferredWidth(shown, settings) / text.pixelsPerUnit > maxWidth)
            {
                value = value.Substring(0, value.Length - 1);
                shown = value.TrimEnd() + "\u2026";
                if (value.Length == 0)
                {
                    shown = string.Empty;
                }
            }

            text.text = shown;
        }

        private void CopyBadge(int from, int to)
        {
            if (badgeIcons != null && to < badgeIcons.Length)
            {
                SetBadge(to, badgeIcons[from].sprite, badgeIcons[from].color);
            }
        }

        private void SetBadge(int slot, Sprite sprite, Color tint)
        {
            if (badgeIcons == null || slot >= badgeIcons.Length)
            {
                return;
            }

            // A type without an icon shows no badge; an Image with no sprite would draw a white box.
            badgeIcons[slot].sprite = sprite;
            badgeIcons[slot].color = tint;
            badgeIcons[slot].enabled = sprite != null;
            if (badgeOutlines != null && slot < badgeOutlines.Length)
            {
                badgeOutlines[slot].sprite = sprite;
                badgeOutlines[slot].enabled = sprite != null;
            }
        }

        private void Update()
        {
            // Cards hold still under the pause panel; on Won they keep fading.
            float dt = session.State == SessionState.Paused ? 0f : Time.unscaledDeltaTime;
            for (int i = 0; i < cards.Length; i++)
            {
                if (_ages[i] == float.MaxValue)
                {
                    continue;
                }

                _ages[i] += dt;
                float remaining = lifetimeSeconds - _ages[i];
                cards[i].alpha = Mathf.Clamp01(remaining / fadeSeconds);

                float pop = Mathf.Clamp01(_ages[i] / popSeconds);
                cards[i].transform.localScale = _baseScales[i] * Mathf.Lerp(PopStartScale, 1f, pop);

                if (remaining <= 0f)
                {
                    _ages[i] = float.MaxValue;
                }
            }
        }
    }
}
