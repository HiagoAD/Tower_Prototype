using Game.Core;
using Game.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>The cross-component bindings of Level 1 that no single builder owns: the settings' measured body height, the GameSession and everything that references it.</summary>
    internal static class Level1Wiring
    {
        /// <summary>Writes the measured body height into the settings' pace and the summit stand offset into the slide view. Returns the summit lip (how far above the finish the climber's feet stand).</summary>
        public static float BindPlayerMeasurements(GameSettings settings, PlayerBuildResult player)
        {
            SceneBinding.Bind(settings, "pace.bodyHeight", player.Metrics.Height);
            EditorUtility.SetDirty(settings);
            float summitLip = player.Metrics.Height * Level1Layout.SummitLipToBodyHeight;
            SceneBinding.Bind(player.SlideView, "standOffset", new Vector3(0f, summitLip, -player.Motor.transform.position.z));
            return summitLip;
        }

        public static GameSession BuildSession(Level1Inputs inputs, HazardVisualAssets hazard, TowerMetrics tower, PlayerBuildResult player)
        {
            GameObject sessionGo = new GameObject("GameSession");
            GameSession session = sessionGo.AddComponent<GameSession>();
            SceneBinding.Bind(session, "motor", player.Motor);
            SceneBinding.BindArray(session, "levelFiles", inputs.LevelFiles);
            SceneBinding.Bind(session, "settings", inputs.Settings);
            SceneBinding.Bind(session, "hazardVisualPrefab", hazard.Prefab);
            SceneBinding.Bind(session, "hazardActiveMaterial", hazard.Active);
            SceneBinding.Bind(session, "hazardSafeMaterial", hazard.Safe);
            SceneBinding.Bind(session, "hazardVisualDiameter", tower.Radius * 2f * Level1Layout.HazardVisualDiameterMultiplier);
            SceneBinding.Bind(session, "hazardBodyHeight", player.Metrics.Height);
            return session;
        }

        /// <summary>Binds the session into the components built before it existed, and adds the summit and input components that need it.</summary>
        public static void WireSessionConsumers(GameSession session, GameSettings settings, LevelDefinition level, TowerMetrics tower, PlayerBuildResult player, CameraBuildResult camera, float summitLip)
        {
            SceneBinding.Bind(camera.Follow, "session", session); // G2 follow-up: CameraFollow snaps on level start / menu.
            SceneBinding.Bind(player.SlideView, "session", session);

            TowerSummit summit = tower.Root.AddComponent<TowerSummit>();
            SceneBinding.Bind(summit, "session", session);
            SceneBinding.Bind(summit, "motor", player.Motor);
            SceneBinding.Bind(summit, "crown", tower.Crown);
            SceneBinding.Bind(summit, "shaft", tower.Shaft);
            SceneBinding.BindArray(summit, "pieces", tower.Pieces);
            SceneBinding.Bind(summit, "crownPivotAboveBase", tower.CrownPivotAboveBase);
            SceneBinding.Bind(summit, "crownBaseToSurface", tower.CrownBaseToSurface);
            SceneBinding.Bind(summit, "lip", summitLip);
            SceneBinding.Bind(summit, "pieceMargin", tower.PieceMargin);
            float defaultFinish = level.finishHeight * settings.pace.DistanceScaleFor(level);
            SceneBinding.Bind(summit, "defaultFinishHeight", defaultFinish);
            summit.Place(defaultFinish); // so the saved scene already shows Level 1's top.

            SceneBinding.Bind(player.PoseDriver, "session", session);

            var climbInput = player.Motor.gameObject.AddComponent<ClimbInputSource>();
            SceneBinding.Bind(climbInput, "motor", player.Motor);
            SceneBinding.Bind(climbInput, "settings", settings);
        }
    }
}
