using Game.Core;
using Game.Gameplay;
using Game.PostFx;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Camera and screen effects: the CameraEffects owner (bump zoom pulse, hazard-hit shake, win move)
    /// and the URP volume driver for the aberration and bloom pulses. Both sit on the camera rig.
    /// </summary>
    internal static class Level1CameraFx
    {
        public static void Build(GameSession session, GameSettings settings, PlayerBuildResult player, CameraBuildResult camera)
        {
            Transform rig = camera.Follow.transform;

            var driverGo = new GameObject("ScreenFx");
            driverGo.transform.SetParent(rig, false);
            var driver = driverGo.AddComponent<ScreenFxVolumeDriver>();
            SceneBinding.Bind(driver, "worldCamera", camera.Camera);
            SceneBinding.Bind(driver, "settings", settings);

            var effectsGo = new GameObject("CameraEffects");
            effectsGo.transform.SetParent(rig, false);
            CameraEffects effects = effectsGo.AddComponent<CameraEffects>();
            SceneBinding.Bind(effects, "session", session);
            SceneBinding.Bind(effects, "worldCamera", camera.Camera);
            SceneBinding.Bind(effects, "cameraOffset", camera.Offset);
            SceneBinding.Bind(effects, "focusTarget", player.HitTarget);
            SceneBinding.Bind(effects, "shake", camera.Shake);
            SceneBinding.Bind(effects, "screenFx", driver);
            SceneBinding.Bind(effects, "settings", settings);
        }
    }
}
