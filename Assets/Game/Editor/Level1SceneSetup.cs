using Game.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// One playable level, wired end-to-end to GameSession/PlayerMotor/webhook/glove burst, built
    /// from the licensed Kenney character/tower/sky assets under Assets/Game/Art/Licensed. Run via
    /// -executeMethod Game.Editor.Level1SceneSetup.Build in batch mode.
    ///
    /// A short orchestrator: each scene part is a focused builder (Level1Environment, Level1Tower,
    /// Level1Player, Level1Camera, Level1Ui/Hud/Burst, Level1Wiring) that takes explicit inputs and
    /// returns a small result struct. To add a part (an environment layer, a HUD element, a menu
    /// panel), write a builder next to these and call it here. Bindings go through SceneBinding and
    /// SceneWiringCheck fails the build if any object field is left unassigned.
    /// </summary>
    public static class Level1SceneSetup
    {
        [MenuItem("Tower/Build Level 1 Scene")]
        public static void Build()
        {
            Level1Assets.ConfigureImportSettings();
            Level1Assets.ConfigureMainLightShadows();
            var uiKit = new UiKit(Level1Assets.LoadUiFont());

            Sprite gloveSprite = Level1Assets.LoadGloveSprite();
            Sprite starSprite = Level1Assets.LoadStarSprite();
            AudioClip impactClip = Level1Assets.LoadImpactClip();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            (TextAsset levelJson, LevelDefinition level) = Level1Assets.LoadLevel();
            var inputs = new Level1Inputs(gloveSprite, starSprite, impactClip, levelJson, level);
            HazardVisualAssets hazard = Level1Assets.BuildHazardVisualAssets();

            Level1Environment.BuildLighting();

            // The climb pace scales level distances at runtime (see ClimbPace); build the tower and
            // sky tall enough for the fastest pace allowed, so changing it never needs a rebuild.
            ClimbPace pace = Level1Assets.LoadOrCreatePace();
            float bodyHeight = Level1Layout.CharacterHeightToTowerDiameter * 2f * Level1Layout.TowerRadius();
            float climbTop = level.finishHeight * ClimbPace.MaxDistanceScaleFor(level, bodyHeight);

            TowerMetrics tower = Level1Tower.Build(climbTop);
            Level1Environment.BuildSeaAndPedestal(tower);
            Level1Environment.BuildClouds(climbTop + Level1Layout.TowerHeadroom);
            PlayerBuildResult player = Level1Player.Build(tower);
            float summitLip = Level1Wiring.BindPlayerMeasurements(pace, player);
            BumpCatalog bumpCatalog = Level1Assets.LoadOrCreateBumpCatalog(gloveSprite);
            CameraBuildResult camera = Level1Camera.Build(player.Motor, player.Metrics.ChestHeight, pace);

            GameSession session = Level1Wiring.BuildSession(inputs, hazard, pace, bumpCatalog, tower, player);
            Level1Wiring.WireSessionConsumers(session, pace, level, tower, player, camera, summitLip);

            Level1Ui.Build(uiKit, session, player.HitTarget, camera, inputs);

            // Every object field on a game component must be wired; nothing in this scene is left optional.
            SceneWiringCheck.Verify(scene);

            AssetDatabase.SaveAssets();
            System.IO.Directory.CreateDirectory("Assets/Game/Scenes");
            EditorSceneManager.SaveScene(scene, Level1Paths.Scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Level1Paths.Scene, true) };

            Debug.Log("[Level1SceneSetup] Built and saved " + Level1Paths.Scene);
        }
    }
}
