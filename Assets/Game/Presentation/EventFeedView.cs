using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// Stack of reference-style event cards on the left edge: every accepted webhook bump pushes a
    /// card (sender line = request ID prefix, detail line = "Boxing*1") onto the top slot, older
    /// cards shift down, and each fades out after its lifetime. Slots are pre-built in the scene;
    /// this only moves text and alpha between them.
    /// </summary>
    public sealed class EventFeedView : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private CanvasGroup[] cards;
        [SerializeField] private Text[] senderTexts;
        [SerializeField] private Text[] detailTexts;
        [SerializeField] private float lifetimeSeconds = 4f;
        [SerializeField] private float fadeSeconds = 0.5f;
        [SerializeField] private float popSeconds = 0.18f;

        private float[] _ages;

        private void Awake()
        {
            _ages = new float[cards.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                _ages[i] = float.MaxValue;
                cards[i].alpha = 0f;
            }
        }

        private void OnEnable()
        {
            session.BumpAccepted += OnBumpAccepted;
            session.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            session.BumpAccepted -= OnBumpAccepted;
            session.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(SessionState state)
        {
            if (state == SessionState.Menu)
            {
                for (int i = 0; i < _ages.Length; i++)
                {
                    _ages[i] = float.MaxValue;
                    cards[i].alpha = 0f;
                }
            }
        }

        private void OnBumpAccepted(string requestId)
        {
            for (int i = cards.Length - 1; i > 0; i--)
            {
                senderTexts[i].text = senderTexts[i - 1].text;
                detailTexts[i].text = detailTexts[i - 1].text;
                _ages[i] = _ages[i - 1];
            }

            senderTexts[0].text = requestId.Length > 8 ? requestId.Substring(0, 8) : requestId;
            detailTexts[0].text = "Boxing*1";
            _ages[0] = 0f;
        }

        private void Update()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (_ages[i] == float.MaxValue)
                {
                    continue;
                }

                _ages[i] += Time.unscaledDeltaTime;
                float remaining = lifetimeSeconds - _ages[i];
                cards[i].alpha = Mathf.Clamp01(remaining / fadeSeconds);

                float pop = Mathf.Clamp01(_ages[i] / popSeconds);
                cards[i].transform.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, pop);

                if (remaining <= 0f)
                {
                    _ages[i] = float.MaxValue;
                }
            }
        }
    }
}
