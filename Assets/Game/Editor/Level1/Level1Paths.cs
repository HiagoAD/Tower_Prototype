namespace Game.Editor
{
    /// <summary>Every asset path Level 1's builders read or write.</summary>
    internal static class Level1Paths
    {
        public const string Scene = "Assets/Game/Scenes/Level1.unity";
        // Campaign order; GameSession.levelFiles is bound to these in this order.
        public static readonly string[] LevelFiles =
        {
            "Assets/Game/Levels/Level1.json",
            "Assets/Game/Levels/Level2.json",
            "Assets/Game/Levels/Level3.json",
            "Assets/Game/Levels/Level4.json",
            "Assets/Game/Levels/Level5.json",
        };
        public const string ClimbPace = "Assets/Game/Levels/ClimbPace.asset";
        public const string BumpCatalog = "Assets/Game/Levels/BumpCatalog.asset";
        public const string GameFeatures = "Assets/Game/Levels/GameFeatures.asset";
        public const string GlovePng = "Assets/Game/Art/Licensed/BoxingGlove/boxing-glove-white.png";
        public const string ImpactSfx = "Assets/Game/Art/Licensed/ImpactSounds/impactPunch_heavy_000.ogg";
        public const string HazardVisualPrefab = "Assets/Game/Prefabs/HazardVisual.prefab";
        public const string HazardActiveMaterial = "Assets/Game/Art/Materials/HazardActive.mat";
        public const string HazardSafeMaterial = "Assets/Game/Art/Materials/HazardSafe.mat";

        public const string CharacterFbx = "Assets/Game/Art/Licensed/Character/character-b.fbx";
        public const string CharacterTexture = "Assets/Game/Art/Licensed/Character/texture-b-goku.png";
        public const string CharacterMaterial = "Assets/Game/Art/Materials/Climber.mat";

        public const string TowerBaseFbx = "Assets/Game/Art/Licensed/Tower/tower-base.fbx";
        public const string TowerTopFbx = "Assets/Game/Art/Licensed/Tower/tower-top.fbx";
        public const string TowerMaterial = "Assets/Game/Art/Materials/TowerStone.mat";

        public const string SkyTexture = "Assets/Game/Art/Licensed/Sky/skybox-day.png";
        public const string SkyMaterial = "Assets/Game/Art/Materials/SkyGradient.mat";
        public const string CloudMaterialFormat = "Assets/Game/Art/Materials/Cloud{0}.mat";
        public const string SeaMaterial = "Assets/Game/Art/Materials/Sea.mat";
        public const string WindowFrameMaterial = "Assets/Game/Art/Materials/WindowFrame.mat";
        public const string WindowPaneMaterial = "Assets/Game/Art/Materials/WindowPane.mat";
        public const string StarPng = "Assets/Game/Art/Licensed/UI/star-yellow.png";
        public const string UiFont = "Assets/Game/Art/Licensed/UI/Fonts/KenneyFuture.ttf";
    }
}
