using Game.Core;
using Game.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>The cross-component bindings of Level 1 that no single builder owns: the pace's measured body height, the GameSession and everything that references it.</summary>
    internal static class Level1Wiring
    {
        /// <summary>Writes the measured body height into the pace and the summit-slide settings into the slide view. Returns the summit lip (how far above the finish the climber's feet stand).</summary>
        public static float BindPlayerMeasurements(ClimbPace pace, PlayerBuildResult player)
        {
            SceneBinding.Bind(pace, "bodyHeight", player.Metrics.Height);
            EditorUtility.SetDirty(pace);
            float summitLip = player.Metrics.Height * Level1Layout.SummitLipToBodyHeight;
            SceneBinding.Bind(player.SlideView, "standOffset", new Vector3(0f, summitLip, -player.Motor.transform.position.z));
            SceneBinding.Bind(player.SlideView, "slideSeconds", Level1Layout.SummitSlideSeconds);
            return summitLip;
        }

        public static GameSession BuildSession(Level1Inputs inputs, HazardVisualAssets hazard, ClimbPace pace, BumpCatalog bumpCatalog, TowerMetrics tower, PlayerBuildResult player)
        {
            GameObject sessionGo = new GameObject("GameSession");
            GameSession session = sessionGo.AddComponent<GameSession>();
            SceneBinding.Bind(session, "motor", player.Motor);
            SceneBinding.Bind(session, "levelJson", inputs.LevelJson);
            SceneBinding.Bind(session, "pace", pace);
            SceneBinding.Bind(session, "bumpCatalog", bumpCatalog);
            SceneBinding.Bind(session, "hazardVisualPrefab", hazard.Prefab);
            SceneBinding.Bind(session, "hazardActiveMaterial", hazard.Active);
            SceneBinding.Bind(session, "hazardSafeMaterial", hazard.Safe);
            SceneBinding.Bind(session, "hazardVisualDiameter", tower.Radius * 2f * Level1Layout.HazardVisualDiameterMultiplier);
            SceneBinding.Bind(session, "hazardBodyHeight", player.Metrics.Height);
            return session;
        }

        /// <summary>Binds the session into the components built before it existed, and adds the summit and input components that need it.</summary>
        public static void WireSessionConsumers(GameSession session, ClimbPace pace, LevelDefinition level, TowerMetrics tower, PlayerBuildResult player, CameraBuildResult camera, float summitLip)
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
            float defaultFinish = level.finishHeight * pace.DistanceScaleFor(level);
            SceneBinding.Bind(summit, "defaultFinishHeight", defaultFinish);
            summit.Place(defaultFinish); // so the saved scene already shows Level 1's top.

            SceneBinding.Bind(player.PoseDriver, "session", session);
            SceneBinding.Bind(player.PoseDriver, "pace", pace);

            var climbInput = player.Motor.gameObject.AddComponent<ClimbInputSource>();
            SceneBinding.Bind(climbInput, "motor", player.Motor);
        }
    }
}
