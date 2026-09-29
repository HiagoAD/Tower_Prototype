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
        public const string SettingsFolder = "Assets/Game/Settings";
        public const string GameSettings = SettingsFolder + "/GameSettings.asset";
        public const string GlovePng = "Assets/Game/Art/Licensed/BoxingGlove/boxing-glove-white.png";
        public const string ImpactSfx = "Assets/Game/Art/Licensed/ImpactSounds/impactPunch_heavy_000.ogg";

        /// <summary>The bump's punch volley, one per glove passing the climber after the main impact.</summary>
        public static readonly string[] PunchVolleySfx =
        {
            "Assets/Game/Art/Licensed/ImpactSounds/impactPunch_heavy_001.ogg",
            "Assets/Game/Art/Licensed/ImpactSounds/impactPunch_heavy_002.ogg",
            "Assets/Game/Art/Licensed/ImpactSounds/impactPunch_heavy_003.ogg",
            "Assets/Game/Art/Licensed/ImpactSounds/impactPunch_heavy_004.ogg",
            "Assets/Game/Art/Licensed/ImpactSounds/impactPunch_medium_000.ogg",
            "Assets/Game/Art/Licensed/ImpactSounds/impactPunch_medium_001.ogg",
            "Assets/Game/Art/Licensed/ImpactSounds/impactPunch_medium_002.ogg",
            "Assets/Game/Art/Licensed/ImpactSounds/impactPunch_medium_003.ogg",
            "Assets/Game/Art/Licensed/ImpactSounds/impactPunch_medium_004.ogg",
        };
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
        public const string UiFont = "Assets/Game/Art/Licensed/UI/Fonts/LilitaOne-Regular.ttf";
    }
}
