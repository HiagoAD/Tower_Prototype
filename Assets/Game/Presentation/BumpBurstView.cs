using System.Collections;
using Game.Core;
using Game.Gameplay;
using Game.Webhook;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// Full-screen boxing-glove barrage for the webhook event, modelled on the reference capture: a
    /// staggered fountain of gloves punches straight through the character and out past the far
    /// edge -- streaming up from below and the lower sides for a positive bump, down from above and
    /// the upper sides for a negative one -- while a white impact glow and a spray of
    /// yellow stars go off on the character. Bounded: a new trigger while one is already playing
    /// restarts the single coroutine instead of stacking, so accepted requests during an active burst
    /// still produce visible feedback without extending the lock indefinitely.
    /// </summary>
    public sealed class BumpBurstView : MonoBehaviour
    {
        // The source icon's fist points toward the lower right; rotating by (travel angle + this)
        // makes every glove lead with its fist.
        private const float SpritePunchAngleOffset = 45f;

        [SerializeField] private GameSession session;
        [SerializeField] private RectTransform burstRoot;
        [SerializeField] private Image flashImage;
        [SerializeField] private CameraShake cameraShake;
        [SerializeField] private AudioSource impactAudioSource;
        [SerializeField] private Sprite gloveSprite;
        [SerializeField] private Sprite starSprite;
        [SerializeField] private Sprite glowSprite;
        [SerializeField] private Transform hitTarget;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private int gloveCount = 14;
        [SerializeField] private int starCount = 16;
        [SerializeField] private float durationSeconds = 1.3f;
        [SerializeField] private float gloveFlightSeconds = 0.55f;
        [SerializeField] private float gloveSize = 230f;
        [SerializeField] private float starSize = 90f;

        private static readonly Color DefaultProjectileTint = new Color(0.9f, 0.08f, 0.08f, 1f);

        private RectTransform[] _gloves;
        private Image[] _gloveFills;
        private Image[] _gloveOutlines;
        private float[] _gloveLaunchAngles;
        private Vector2[] _gloveStarts;
        private float[] _gloveDelays;
        private RectTransform[] _stars;
        private Vector2[] _starVelocities;
        private RectTransform _glow;
        private Image _glowImage;
        private float _spanX;
        private float _spanY;
        private Coroutine _routine;

        private void Awake()
        {
            // Use the canvas's own local unit space (root RectTransform), not raw Screen pixels --
            // those only coincide when the reference resolution exactly matches the device width.
            var canvasRect = burstRoot.root as RectTransform;
            _spanX = canvasRect != null ? canvasRect.rect.width : Screen.width;
            _spanY = canvasRect != null ? canvasRect.rect.height : Screen.height;

            BuildGlow();
            BuildGloves();
            BuildStars();
            HideAll();
        }

        private void OnEnable()
        {
            session.BumpAccepted += OnBumpAccepted;
        }

        private void OnDisable()
        {
            session.BumpAccepted -= OnBumpAccepted;
            StopOwnedEffects();
        }

        private void OnBumpAccepted(BumpEvent bump)
        {
            Trigger(bump);
        }

        public void Trigger(BumpEvent bump)
        {
            StopOwnedEffects();
            ConfigureProjectiles(bump);
            _routine = StartCoroutine(BurstRoutine());
        }

        /// <summary>Skins every glove with the bump type's icon and tint and mirrors the launch fan: below for positive, above for negative.</summary>
        private void ConfigureProjectiles(BumpEvent bump)
        {
            Sprite sprite = bump.Type != null && bump.Type.icon != null ? bump.Type.icon : gloveSprite;
            Color tint = bump.Type != null && bump.Type.icon != null ? bump.Type.iconTint : DefaultProjectileTint;
            float sign = bump.Polarity == BumpPolarity.Positive ? 1f : -1f;
            float distance = Mathf.Max(_spanX, _spanY) * 0.62f;

            for (int i = 0; i < _gloves.Length; i++)
            {
                _gloveFills[i].sprite = sprite;
                _gloveFills[i].color = tint;
                _gloveOutlines[i].sprite = sprite;

                // Launch angles are authored for the positive fan below the screen (-160..-20 degrees);
                // flipping the sine puts the negative fan above it, travelling downward.
                float rad = _gloveLaunchAngles[i] * Mathf.Deg2Rad;
                _gloveStarts[i] = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad) * sign) * distance;
            }
        }

        private void StopOwnedEffects()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            HideAll();
        }

        private void HideAll()
        {
            SetAllActive(_gloves, false);
            SetAllActive(_stars, false);
            if (_glow != null)
            {
                _glow.gameObject.SetActive(false);
            }

            SetFlashAlpha(0f);
        }

        private static void SetAllActive(RectTransform[] rects, bool active)
        {
            if (rects == null)
            {
                return;
            }

            foreach (RectTransform rect in rects)
            {
                rect.gameObject.SetActive(active);
            }
        }

        private void SetFlashAlpha(float alpha)
        {
            if (flashImage == null)
            {
                return;
            }

            Color c = flashImage.color;
            c.a = alpha;
            flashImage.color = c;
        }

        /// <summary>
        /// Converts hitTarget's current world position (the character's chest, not its feet/root)
        /// to burstRoot's local space, so the barrage converges on the visible character. Recomputed
        /// every frame the burst plays, since CameraFollow keeps tracking the player while it's active.
        /// </summary>
        private Vector2 ComputeTargetLocalPosition()
        {
            if (hitTarget == null || worldCamera == null)
            {
                return Vector2.zero;
            }

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(hitTarget.position);

            // burstRoot lives on a ScreenSpaceOverlay canvas, so the camera argument must be null --
            // passing a camera there is only correct for ScreenSpaceCamera/WorldSpace canvases.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(burstRoot, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }

        private void BuildGlow()
        {
            _glowImage = CreateImage(burstRoot, "ImpactGlow", glowSprite, _spanX * 0.9f, new Color(1f, 0.97f, 0.85f, 0f));
            _glow = _glowImage.rectTransform;
        }

        private void BuildGloves()
        {
            _gloves = new RectTransform[gloveCount];
            _gloveFills = new Image[gloveCount];
            _gloveOutlines = new Image[gloveCount];
            _gloveLaunchAngles = new float[gloveCount];
            _gloveStarts = new Vector2[gloveCount];
            _gloveDelays = new float[gloveCount];

            for (int i = 0; i < gloveCount; i++)
            {
                var glove = new GameObject("Glove" + i, typeof(RectTransform)).GetComponent<RectTransform>();
                glove.SetParent(burstRoot, false);
                glove.sizeDelta = Vector2.zero;

                _gloveOutlines[i] = CreateImage(glove, "Outline", gloveSprite, gloveSize * 1.12f, new Color(0.12f, 0.02f, 0.02f, 1f));
                _gloveFills[i] = CreateImage(glove, "Glove", gloveSprite, gloveSize, DefaultProjectileTint);

                // Fan the launch points along the bottom edge and up the lower sides, like the
                // reference's glove fountain, so every glove crosses the character on its way out.
                // ConfigureProjectiles mirrors the fan for a negative bump.
                float spread = gloveCount > 1 ? (float)i / (gloveCount - 1) : 0.5f;
                _gloveLaunchAngles[i] = Mathf.Lerp(-160f, -20f, spread) + Random.Range(-8f, 8f);

                // Alternate outer and inner launch slots so the stream reads as continuous.
                _gloveDelays[i] = ((i * 7) % gloveCount) / (float)gloveCount * 0.4f;

                glove.localScale = Vector3.one * Random.Range(0.8f, 1.15f);
                _gloves[i] = glove;
            }
        }

        private void BuildStars()
        {
            _stars = new RectTransform[starCount];
            _starVelocities = new Vector2[starCount];

            for (int i = 0; i < starCount; i++)
            {
                Image star = CreateImage(burstRoot, "Star" + i, starSprite, starSize, Color.white);
                _stars[i] = star.rectTransform;

                float angle = (360f / starCount) * i + Random.Range(-12f, 12f);
                float rad = angle * Mathf.Deg2Rad;
                _starVelocities[i] = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * Random.Range(0.45f, 0.8f) * _spanX;
            }
        }

        private static Image CreateImage(RectTransform parent, string name, Sprite sprite, float size, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private IEnumerator BurstRoutine()
        {
            if (cameraShake != null)
            {
                cameraShake.Shake(durationSeconds);
            }

            // The first glove reaches the character halfway through its flight -- that is the impact.
            float impactTime = _gloveDelays.Length > 0 ? Mathf.Min(_gloveDelays) + gloveFlightSeconds * 0.5f : 0f;
            bool impacted = false;

            float t = 0f;
            while (t < durationSeconds)
            {
                t += Time.deltaTime;
                Vector2 target = ComputeTargetLocalPosition();

                UpdateGloves(t, target);

                if (!impacted && t >= impactTime)
                {
                    impacted = true;
                    if (impactAudioSource != null && impactAudioSource.clip != null)
                    {
                        impactAudioSource.PlayOneShot(impactAudioSource.clip);
                    }
                }

                if (impacted)
                {
                    UpdateImpact(t - impactTime, target);
                }

                yield return null;
            }

            HideAll();
            _routine = null;
        }

        /// <summary>Each glove flies in a straight line from its launch point, through the character at mid-flight, and out the far side.</summary>
        private void UpdateGloves(float t, Vector2 target)
        {
            for (int i = 0; i < _gloves.Length; i++)
            {
                float u = (t - _gloveDelays[i]) / gloveFlightSeconds;
                bool flying = u >= 0f && u <= 1f;
                _gloves[i].gameObject.SetActive(flying);
                if (!flying)
                {
                    continue;
                }

                Vector2 start = target + _gloveStarts[i];
                Vector2 end = target - _gloveStarts[i];
                _gloves[i].anchoredPosition = Vector2.LerpUnclamped(start, end, u);

                Vector2 direction = end - start;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + SpritePunchAngleOffset;
                _gloves[i].localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        /// <summary>White flash + expanding glow on the character, and a spray of spinning stars.</summary>
        private void UpdateImpact(float sinceImpact, Vector2 target)
        {
            const float flashSeconds = 0.3f;
            const float glowSeconds = 0.45f;
            float starSeconds = Mathf.Max(0.05f, durationSeconds - 0.3f);

            SetFlashAlpha(Mathf.Lerp(0.65f, 0f, sinceImpact / flashSeconds));

            float glowP = Mathf.Clamp01(sinceImpact / glowSeconds);
            _glow.gameObject.SetActive(glowP < 1f);
            _glow.anchoredPosition = target;
            _glow.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.2f, 1f - (1f - glowP) * (1f - glowP));
            Color glowColor = _glowImage.color;
            glowColor.a = Mathf.Lerp(0.95f, 0f, glowP);
            _glowImage.color = glowColor;

            float starP = Mathf.Clamp01(sinceImpact / starSeconds);
            float travel = 1f - (1f - starP) * (1f - starP);
            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].gameObject.SetActive(starP < 1f);
                _stars[i].anchoredPosition = target + _starVelocities[i] * travel;
                _stars[i].localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 1f : -1f) * 540f * travel);
                _stars[i].localScale = Vector3.one * Mathf.Lerp(1.2f, 0.4f, starP);
            }
        }
    }
}
