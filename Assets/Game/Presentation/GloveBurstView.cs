using System.Collections;
using Game.Core;
using Game.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// Full-screen six-glove hit burst for the webhook event. Bounded: a new trigger while one is
    /// already playing restarts the single coroutine instead of stacking, so accepted requests
    /// during an active burst still produce visible feedback without extending the lock indefinitely.
    /// </summary>
    public sealed class GloveBurstView : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private RectTransform burstRoot;
        [SerializeField] private Image flashImage;
        [SerializeField] private CameraShake cameraShake;
        [SerializeField] private AudioSource impactAudioSource;
        [SerializeField] private Sprite gloveSprite;
        [SerializeField] private PlayerMotor player;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private int gloveCount = 6;
        [SerializeField] private float durationSeconds = 1f;
        [SerializeField] private float gloveSize = 180f;

        private RectTransform[] _gloveGroups;
        private Vector2[] _edgeStarts;
        private Coroutine _routine;
        private Coroutine _flashRoutine;

        private void Awake()
        {
            BuildGloves();
        }

        private void OnEnable()
        {
            session.BumpAccepted += OnBumpAccepted;
        }

        private void OnDisable()
        {
            session.BumpAccepted -= OnBumpAccepted;

            // Unity already stops every coroutine owned by a disabled component; clear our handles
            // and the flash opacity so a later re-enable does not inherit a stuck full-opacity flash.
            _routine = null;
            _flashRoutine = null;
            ResetFlashOpacity();
        }

        private void OnBumpAccepted(string requestId)
        {
            Trigger();
        }

        public void Trigger()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            // A retrigger must not let a still-running flash from the previous burst fight the new
            // one over opacity.
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            ResetFlashOpacity();

            _routine = StartCoroutine(BurstRoutine());
        }

        private void ResetFlashOpacity()
        {
            if (flashImage == null)
            {
                return;
            }

            Color c = flashImage.color;
            c.a = 0f;
            flashImage.color = c;
        }

        /// <summary>
        /// Converts the player's current world position to burstRoot's local space, so the glove
        /// burst converges on the character rather than the canvas origin. Recomputed every frame
        /// the burst plays, since CameraFollow keeps tracking the player while it's active.
        /// </summary>
        private Vector2 ComputeTargetLocalPosition()
        {
            if (player == null || worldCamera == null)
            {
                return Vector2.zero;
            }

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(player.transform.position);

            // burstRoot lives on a ScreenSpaceOverlay canvas, so the camera argument must be null --
            // passing a camera there is only correct for ScreenSpaceCamera/WorldSpace canvases.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(burstRoot, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }

        private void BuildGloves()
        {
            _gloveGroups = new RectTransform[gloveCount];
            _edgeStarts = new Vector2[gloveCount];

            // Use the canvas's own local unit space (root RectTransform), not raw Screen pixels --
            // those only coincide when the reference resolution exactly matches the device width.
            var canvasRect = burstRoot.root as RectTransform;
            float spanX = canvasRect != null ? canvasRect.rect.width : Screen.width;
            float spanY = canvasRect != null ? canvasRect.rect.height : Screen.height;
            float radius = Mathf.Max(spanX, spanY) * 0.75f;

            for (int i = 0; i < gloveCount; i++)
            {
                var group = new GameObject("Glove" + i, typeof(RectTransform)).GetComponent<RectTransform>();
                group.SetParent(burstRoot, false);
                group.anchorMin = group.anchorMax = new Vector2(0.5f, 0.5f);
                group.pivot = new Vector2(0.5f, 0.5f);
                group.sizeDelta = Vector2.zero;

                CreateGloveImage(group, "Outline", gloveSize * 1.15f, Color.black);
                CreateGloveImage(group, "Glove", gloveSize, new Color(0.85f, 0.1f, 0.1f, 1f));

                float angle = (360f / gloveCount) * i + Random.Range(-20f, 20f);
                float rad = angle * Mathf.Deg2Rad;
                _edgeStarts[i] = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;

                group.anchoredPosition = _edgeStarts[i];
                group.gameObject.SetActive(false);
                _gloveGroups[i] = group;
            }
        }

        private void CreateGloveImage(RectTransform parent, string name, float size, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = gloveSprite;
            image.color = color;
            image.raycastTarget = false;
        }

        private IEnumerator BurstRoutine()
        {
            foreach (RectTransform group in _gloveGroups)
            {
                group.gameObject.SetActive(true);
            }

            if (cameraShake != null)
            {
                cameraShake.Shake(durationSeconds);
            }

            if (impactAudioSource != null && impactAudioSource.clip != null)
            {
                impactAudioSource.PlayOneShot(impactAudioSource.clip);
            }

            _flashRoutine = StartCoroutine(FlashRoutine());

            float inPhase = durationSeconds * 0.55f;
            float outPhase = Mathf.Max(0.05f, durationSeconds - inPhase);

            float t = 0f;
            while (t < inPhase)
            {
                t += Time.deltaTime;
                float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / inPhase), 3f);
                Vector2 target = ComputeTargetLocalPosition();
                for (int i = 0; i < _gloveGroups.Length; i++)
                {
                    _gloveGroups[i].anchoredPosition = Vector2.Lerp(_edgeStarts[i], target, eased);
                }

                yield return null;
            }

            t = 0f;
            while (t < outPhase)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / outPhase);
                Vector2 target = ComputeTargetLocalPosition();
                for (int i = 0; i < _gloveGroups.Length; i++)
                {
                    _gloveGroups[i].anchoredPosition = Vector2.Lerp(target, _edgeStarts[i], p);
                }

                yield return null;
            }

            foreach (RectTransform group in _gloveGroups)
            {
                group.gameObject.SetActive(false);
            }

            _routine = null;
        }

        private IEnumerator FlashRoutine()
        {
            if (flashImage == null)
            {
                _flashRoutine = null;
                yield break;
            }

            const float flashDuration = 0.25f;
            float t = 0f;
            while (t < flashDuration)
            {
                t += Time.deltaTime;
                Color c = flashImage.color;
                c.a = Mathf.Lerp(0.7f, 0f, t / flashDuration);
                flashImage.color = c;
                yield return null;
            }

            ResetFlashOpacity();
            _flashRoutine = null;
        }
    }
}
