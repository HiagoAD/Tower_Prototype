using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// The win celebration: a pop-and-wobble on the panel's heading plus a confetti burst. MenuView
    /// calls Celebrate when it shows the win or final panel (after the summit slide), so the heading and
    /// confetti start with the buttons, which are tappable at once: confetti is plain Images that are
    /// never raycast targets and sit behind the heading and buttons. The final TOWER CLEARED screen
    /// gets a bigger heading pop and a larger, longer, two-wave burst. Unscaled time, like the panels;
    /// no pause can happen while Won. Stop (any state but Won, or a level start) removes everything and
    /// restores the heading.
    /// </summary>
    public sealed class VictoryView : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [Tooltip("The win panel's heading text.")]
        [SerializeField] private RectTransform winHeading;
        [Tooltip("The final TOWER CLEARED panel's heading text.")]
        [SerializeField] private RectTransform finalHeading;
        [Tooltip("Confetti sprite (the licensed yellow star); every second piece is a plain tinted square.")]
        [SerializeField] private Sprite confettiSprite;
        [Tooltip("Tuning asset; the victory section sets the heading pop and the confetti.")]
        [SerializeField] private GameSettings settings;

        private struct Piece
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Velocity;
            public float Spin;
            public float Age;
            public float Lifetime;
            public Color Tint;
            public bool Alive;
        }

        private GameSettings Settings => GameSettings.OrDefaults(settings);

        private readonly List<Piece> _pieces = new List<Piece>();
        private RectTransform _root;
        private RectTransform _heading;
        private Vector3 _headingBaseScale;
        private bool _final;
        private bool _active;
        private float _elapsed;
        private bool _secondBurstDone;
        private int _spawned;

        public bool IsActive => _active;

        /// <summary>Celebration time so far, exposed for tests.</summary>
        public float Elapsed => _elapsed;

        public int AliveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _pieces.Count; i++)
                {
                    n += _pieces[i].Alive ? 1 : 0;
                }

                return n;
            }
        }

        private void OnEnable()
        {
            session.StateChanged += OnStateChanged;
            session.LevelStarted += Stop;
        }

        private void OnDisable()
        {
            session.StateChanged -= OnStateChanged;
            session.LevelStarted -= Stop;
            Stop();
        }

        private void OnStateChanged(SessionState state)
        {
            if (state != SessionState.Won)
            {
                Stop();
            }
        }

        /// <summary>Starts the celebration on the panel that was just shown. panel is the parent the confetti sits in (behind its heading and buttons).</summary>
        public void Celebrate(bool final, RectTransform panel)
        {
            Stop();
            _final = final;
            _heading = final ? finalHeading : winHeading;
            _active = true;
            _elapsed = 0f;
            _secondBurstDone = false;
            _spawned = 0;
            if (_heading != null)
            {
                _headingBaseScale = _heading.localScale;
            }

            EnsureRoot();
            _root.SetParent(panel, false);
            _root.SetSiblingIndex(Mathf.Min(1, panel.childCount - 1)); // after the panel's dim, before heading and buttons.
            _root.gameObject.SetActive(true);
            ApplyHeading();
            SpawnBurst(VictoryMath.FirstBurstCount(Settings.victory, final));
        }

        /// <summary>Cancels the celebration: hides all confetti and restores the heading's scale and tilt.</summary>
        public void Stop()
        {
            if (_heading != null)
            {
                _heading.localScale = _headingBaseScale;
                _heading.localRotation = Quaternion.identity;
            }

            _heading = null;
            _active = false;
            for (int i = 0; i < _pieces.Count; i++)
            {
                Piece p = _pieces[i];
                p.Alive = false;
                if (p.Rect != null)
                {
                    p.Rect.gameObject.SetActive(false);
                }

                _pieces[i] = p;
            }

            if (_root != null)
            {
                _root.gameObject.SetActive(false);
                _root.SetParent(transform, false);
            }
        }

        private void Update()
        {
            if (_active)
            {
                Step(Time.unscaledDeltaTime);
            }
        }

        /// <summary>Advances the celebration by dt seconds.</summary>
        public void Step(float dt)
        {
            if (!_active)
            {
                return;
            }

            _elapsed += dt;
            VictorySettings v = Settings.victory;
            ApplyHeading();
            if (_final && !_secondBurstDone && v.finalSecondBurstDelay > 0f && _elapsed >= v.finalSecondBurstDelay)
            {
                _secondBurstDone = true;
                SpawnBurst(VictoryMath.SecondBurstCount(v, true));
            }

            for (int i = 0; i < _pieces.Count; i++)
            {
                Piece p = _pieces[i];
                if (!p.Alive)
                {
                    continue;
                }

                p.Age += dt;
                if (p.Age >= p.Lifetime)
                {
                    p.Alive = false;
                    p.Rect.gameObject.SetActive(false);
                    _pieces[i] = p;
                    continue;
                }

                p.Velocity += Vector2.down * (v.confettiGravity * dt);
                p.Rect.anchoredPosition += p.Velocity * dt;
                p.Rect.localRotation = Quaternion.Euler(0f, 0f, p.Rect.localEulerAngles.z + p.Spin * dt);
                Color c = p.Tint;
                c.a *= VictoryMath.ConfettiAlpha(p.Age, p.Lifetime, v.confettiFadeFraction);
                p.Image.color = c;
                _pieces[i] = p;
            }
        }

        private void ApplyHeading()
        {
            if (_heading == null)
            {
                return;
            }

            VictorySettings v = Settings.victory;
            float seconds = VictoryMath.HeadingSeconds(v, _final);
            float t = seconds > 0f ? Mathf.Clamp01(_elapsed / seconds) : 1f;
            _heading.localScale = _headingBaseScale * VictoryMath.HeadingScale(v, _final, t);
            _heading.localRotation = Quaternion.Euler(0f, 0f, VictoryMath.HeadingTilt(v, _final, t));
        }

        private void EnsureRoot()
        {
            if (_root != null)
            {
                return;
            }

            var go = new GameObject("VictoryConfetti", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _root = go.GetComponent<RectTransform>();
            StretchToParent(_root);
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void SpawnBurst(int count)
        {
            if (count <= 0)
            {
                return;
            }

            VictorySettings v = Settings.victory;
            Vector2 origin = OriginInRoot();
            Color[] colors = v.confettiColors;
            float lifeScale = _final ? v.finalLifetimeMultiplier : 1f;
            for (int n = 0; n < count; n++)
            {
                int index = FreeIndex();
                Piece p = _pieces[index];
                float angle = Random.Range(0f, Mathf.PI * 2f);
                // Upper-biased fan: the gravity pulls it back down, so it reads as a pop from the heading.
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.6f + 0.4f);
                p.Velocity = dir * Random.Range(v.confettiSpeed.x, v.confettiSpeed.y);
                p.Spin = Random.Range(-v.confettiSpinDegrees, v.confettiSpinDegrees);
                p.Age = 0f;
                p.Lifetime = Random.Range(v.confettiLifetime.x, v.confettiLifetime.y) * lifeScale;
                p.Tint = colors != null && colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.white;
                p.Alive = true;
                float size = Random.Range(v.confettiSize.x, v.confettiSize.y);
                p.Rect.sizeDelta = new Vector2(size, size);
                p.Rect.anchoredPosition = origin;
                p.Rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                p.Image.sprite = _spawned % 2 == 0 ? confettiSprite : null;
                p.Image.color = p.Tint;
                p.Rect.gameObject.SetActive(true);
                _pieces[index] = p;
                _spawned++;
            }
        }

        /// <summary>The heading's position in the confetti root's anchored space (root is stretched with a centred pivot).</summary>
        private Vector2 OriginInRoot()
        {
            if (_heading == null)
            {
                return Vector2.zero;
            }

            Vector3 local = _root.InverseTransformPoint(_heading.position);
            return new Vector2(local.x, local.y);
        }

        private int FreeIndex()
        {
            for (int i = 0; i < _pieces.Count; i++)
            {
                if (!_pieces[i].Alive)
                {
                    return i;
                }
            }

            var go = new GameObject("Confetti", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false; // never blocks Next/Exit.
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            go.SetActive(false);
            _pieces.Add(new Piece { Rect = rect, Image = image });
            return _pieces.Count - 1;
        }
    }
}
