using UnityEngine;

namespace Game.Editor
{
    /// <summary>Level 1's composition and proportion constants, and the sizes derived from the camera framing.</summary>
    internal static class Level1Layout
    {
        // Windows inside skybox-day.png (UV scale.xy, offset.zw) that the CloudCutout shader keys a
        // cloud out of: two broad soft cumulus masses, then four small puffs. Aspect = the window's
        // pixel aspect.
        public static readonly Vector4[] CloudWindows =
        {
            new Vector4(0.4600f, 0.2830f, 0.1270f, 0.5800f),
            new Vector4(0.3320f, 0.2640f, 0.5960f, 0.5800f),
            new Vector4(0.0830f, 0.0733f, 0.2441f, 0.5654f),
            new Vector4(0.1172f, 0.0732f, 0.0049f, 0.5850f),
            new Vector4(0.0635f, 0.0342f, 0.7617f, 0.5459f),
            new Vector4(0.0757f, 0.0439f, 0.9229f, 0.5801f),
        };
        public static readonly float[] CloudAspects = { 3.25f, 2.5f, 2.27f, 3.2f, 3.7f, 3.4f };

        // Composition follows ref.png, adapted to portrait rather than copied pixel-for-pixel. In the
        // landscape image the shaft spans only ~0.10 of the frame width, which would leave a sliver on
        // a phone; the relationships kept instead are: the climber (hair to feet, hands raised) is
        // about as tall as the shaft is wide, and sits well below the frame centre with the tower
        // running on above it. The shaft spans TargetTowerWidthFraction of the portrait width.
        //
        // At CameraDistance world units away, a CameraVerticalFovDeg lens on NominalDeviceAspect
        // gives the world width across the screen; the tower radius and character scale derive from
        // that. CharacterHeightToTowerDiameter applies to the character's arms-down bounds -- the
        // raised-arm grip pose adds roughly a fifth to its on-screen height.
        public const float TargetTowerWidthFraction = 0.26f;
        public const float CharacterHeightToTowerDiameter = 0.82f;
        public const float CharacterScreenHeightFraction = 0.34f; // climber's chest, measured up from the bottom of the frame.
        // World units per unit of the original framing. Level data (heights, speed, hazard spacing)
        // stays in world units; scaling the camera -- and with it the tower and climber, which derive
        // from the camera's view width -- slows the on-screen climb to a pace where hands can stay
        // planted on the tower between grabs (see ClimberPoseDriver): about 0.9 body heights/s at
        // speed 2.5, roughly 3 grabs a second.
        public const float WorldScale = 4.5f;
        public const float CameraDistance = 8f * WorldScale;
        public const float CameraVerticalFovDeg = 45f;
        public const float NominalDeviceAspect = 1080f / 2520f;
        public const float TowerHeadroom = 14f; // world units of tower visible above the finish height.

        // Tower rhythm from ref.png, as fractions of the shaft diameter: thin collars (a little wider
        // than the shaft) separating alternating short and tall storeys, each with rows of small
        // blue windows in two columns whose spacing alternates storey to storey.
        public static readonly float[] StoreyHeightsToDiameter = { 0.7f, 1.2f };
        public static readonly int[] StoreyWindowRows = { 2, 3 };
        public static readonly float[] StoreyWindowAnglesDeg = { 40f, 27f };
        public const float CollarHeightToDiameter = 0.12f;
        public const float WindowWidthToDiameter = 0.075f;
        public const float WindowHeightToDiameter = 0.1f;

        public const float CollarRadiusToShaftRadius = 1.11f;

        // The summit is placed at runtime (TowerSummit). The climber's feet reach the level's finish
        // height with its raised hands, about the body's height up, at the crown's lip; the standing
        // surface is that lip above the finish. The tower-top model is a crenellated crown, so its
        // floor is taken as this share of its measured height rather than its rim.
        public const float SummitLipToBodyHeight = 0.85f;
        public const float CrownStandSurfaceFraction = 0.8f;
        // Win panel button, in menu-button slots below the screen centre: the standing climber
        // lands around 0.3-0.5 of the screen height up from the bottom, so the button sits below it.
        public const int WinButtonSlot = 4;

        public const float PedestalRadiusMultiplier = 1.9f;
        public const float PedestalHeightMultiplier = 0.8f;
        public const float SeaHalfExtent = 14f * WorldScale;
        // 30 clouds over Level 1's original span; taller climbs get proportionally more, up to the cap.
        public const int BaseCloudCount = 30;
        public const int MaxCloudCount = 150;
        public const float CloudMargin = 10f * WorldScale; // cloud field extends this far above and below the climb, so the frame is never empty.
        public const float MainLightShadowDistance = 14f * WorldScale;

        // Fraction of the character's total (feet-to-head) bounds height used both for HitTarget
        // placement (glove burst aim) and the camera's chest-height framing.
        public const float CharacterChestHeightFraction = 0.55f;

        public const float HazardVisualDiameterMultiplier = 1.15f; // slightly wider than the tower so the band visibly wraps around it.

        /// <summary>Cloud count that keeps Level 1's cloud density per unit of height on a taller (or shorter) cloud field.</summary>
        public static int CloudCountFor(float span, float referenceSpan)
        {
            return Mathf.Clamp(Mathf.RoundToInt(BaseCloudCount * span / referenceSpan), BaseCloudCount, MaxCloudCount);
        }

        public static float TowerRadius()
        {
            return TargetTowerWidthFraction * WorldHalfWidthAtTower();
        }

        /// <summary>Half the world-space width the camera sees at the tower's depth, on the verification device's aspect.</summary>
        private static float WorldHalfWidthAtTower()
        {
            float verticalFovRad = CameraVerticalFovDeg * Mathf.Deg2Rad;
            float horizontalFovRad = 2f * Mathf.Atan(Mathf.Tan(verticalFovRad * 0.5f) * NominalDeviceAspect);
            return CameraDistance * Mathf.Tan(horizontalFovRad * 0.5f);
        }
    }
}
