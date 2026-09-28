using Game.Core;
using Game.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Level 1's bump feedback layer on top of the canvas: screen flash, glove/star burst root, impact audio and the BumpBurstView that drives them.</summary>
    internal static class Level1Burst
    {
        public static void Build(Transform canvas, GameSession session, Transform hitTarget, CameraBuildResult camera, Level1Inputs inputs)
        {
            Image flashImage = UiKit.AddImage(canvas, "FlashImage", null, new Color(1f, 1f, 1f, 0f));
            UiKit.Stretch(flashImage.rectTransform);

            GameObject burstRootGo = new GameObject("BumpBurstRoot", typeof(RectTransform));
            burstRootGo.transform.SetParent(canvas, false);
            var burstRect = burstRootGo.GetComponent<RectTransform>();
            burstRect.anchorMin = burstRect.anchorMax = new Vector2(0.5f, 0.5f);
            burstRect.sizeDelta = Vector2.zero;

            var audioGo = new GameObject("ImpactAudioSource", typeof(AudioSource));
            audioGo.transform.SetParent(canvas, false);
            AudioSource audioSource = audioGo.GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = inputs.ImpactClip;

            var gloveBurstGo = new GameObject("BumpBurstView");
            gloveBurstGo.transform.SetParent(canvas, false);
            BumpBurstView gloveBurst = gloveBurstGo.AddComponent<BumpBurstView>();
            SceneBinding.Bind(gloveBurst, "session", session);
            SceneBinding.Bind(gloveBurst, "burstRoot", burstRect);
            SceneBinding.Bind(gloveBurst, "flashImage", flashImage);
            SceneBinding.Bind(gloveBurst, "cameraShake", camera.Shake);
            SceneBinding.Bind(gloveBurst, "impactAudioSource", audioSource);
            SceneBinding.Bind(gloveBurst, "gloveSprite", inputs.GloveSprite);
            SceneBinding.Bind(gloveBurst, "starSprite", inputs.StarSprite);
            SceneBinding.Bind(gloveBurst, "glowSprite", UiKit.KnobSprite);
            SceneBinding.Bind(gloveBurst, "hitTarget", hitTarget);
            SceneBinding.Bind(gloveBurst, "worldCamera", camera.Camera);
        }
    }
}
