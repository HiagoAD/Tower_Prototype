using Game.Core;
using Game.Gameplay;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>The camera rig's parts: the camera itself, the shake and effect-offset transforms CameraEffects drives, and the follow the session is bound to afterwards.</summary>
    internal readonly struct CameraBuildResult
    {
        public readonly Camera Camera;
        public readonly CameraShake Shake;
        public readonly CameraFollow Follow;
        public readonly Transform Offset;

        public CameraBuildResult(Camera camera, CameraShake shake, CameraFollow follow, Transform offset)
        {
            Camera = camera;
            Shake = shake;
            Follow = follow;
            Offset = offset;
        }
    }

    /// <summary>Level 1's camera rig: follow, shake offset, effect offset (win move) and the main camera.</summary>
    internal static class Level1Camera
    {
        public static CameraBuildResult Build(PlayerMotor motor, float characterChestHeight, GameSettings settings)
        {
            // A level camera raised above the climber, so their chest sits Level1Layout.CharacterScreenHeightFraction
            // up from the bottom of the frame with a long run of tower overhead, as in ref.png. Raising
            // rather than tilting keeps the shaft vertical on screen.
            float playerDistance = Level1Layout.CameraDistance + motor.transform.position.z;
            float visibleHeight = 2f * playerDistance * Mathf.Tan(Level1Layout.CameraVerticalFovDeg * 0.5f * Mathf.Deg2Rad);
            float raise = (0.5f - Level1Layout.CharacterScreenHeightFraction) * visibleHeight;
            var cameraOffset = new Vector3(0f, characterChestHeight + raise, -Level1Layout.CameraDistance);

            var rigGo = new GameObject("CameraRig");
            rigGo.transform.position = cameraOffset; // avoid starting inside the tower and lerping out on frame 1
            var follow = rigGo.AddComponent<CameraFollow>();
            SceneBinding.Bind(follow, "target", motor);
            SceneBinding.Bind(follow, "offset", cameraOffset);
            SceneBinding.Bind(follow, "settings", settings);

            var shakeGo = new GameObject("CameraShakeOffset");
            shakeGo.transform.SetParent(rigGo.transform, false);
            CameraShake shake = shakeGo.AddComponent<CameraShake>();
            SceneBinding.Bind(shake, "settings", settings);

            // Carries the win move's drop and arc, separate from the shake so neither overwrites the other.
            var offsetGo = new GameObject("CameraEffectOffset");
            offsetGo.transform.SetParent(shakeGo.transform, false);

            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(offsetGo.transform, false);
            Camera camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = Level1Layout.CameraVerticalFovDeg;

            return new CameraBuildResult(camera, shake, follow, offsetGo.transform);
        }
    }
}
