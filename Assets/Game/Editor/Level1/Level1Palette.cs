using UnityEngine;

namespace Game.Editor
{
    /// <summary>Level 1's colours.</summary>
    internal static class Level1Palette
    {
        // Palette sampled from the preview image (ref.png), the art-direction baseline: bright cyan
        // sky paling toward the bottom, a pale white/light-blue tower with blue windows.
        public static readonly Color SkyTop = new Color32(62, 166, 230, 255);
        public static readonly Color SkyMid = new Color32(112, 199, 238, 255);
        public static readonly Color SkyBottom = new Color32(128, 210, 242, 255);
        public static readonly Color TowerStone = new Color32(214, 226, 242, 255);
        public static readonly Color WindowFrame = new Color32(70, 104, 158, 255);
        public static readonly Color WindowPane = new Color32(150, 190, 236, 255);
        public static readonly Color Sea = new Color32(104, 182, 226, 255);

        // A safe band must read as a harmless, deliberate ring against both the pale tower stone and
        // the cyan sky, so it is a saturated mint green rather than another collar; active ones glow red.
        public static readonly Color HazardActive = new Color(0.95f, 0.12f, 0.08f, 1f);
        public static readonly Color HazardSafe = new Color32(52, 201, 110, 255);

        // HUD palette from ref.png: dark altitude track, yellow fill, yellow current altitude and red
        // goal altitude, both heavy and outlined in near-black.
        public static readonly Color BarTrack = new Color32(44, 50, 60, 255);
        public static readonly Color BarFill = new Color32(252, 192, 14, 255);
        public static readonly Color HudYellow = new Color32(255, 214, 20, 255);
        public static readonly Color HudRed = new Color32(236, 24, 24, 255);
        public static readonly Color HudStroke = new Color32(24, 16, 10, 255);
        public static readonly Color HudButton = new Color32(44, 50, 60, 217);
        public static readonly Color HeartFill = new Color32(235, 30, 40, 255);
        public static readonly Color HeartOutline = new Color(0.2f, 0f, 0.02f, 0.9f);
        public static readonly Color CardBanner = new Color32(30, 84, 170, 235);
        public static readonly Color CardBannerGlint = new Color32(84, 156, 236, 200);
        public static readonly Color FlameGlow = new Color32(240, 70, 20, 110);
        public static readonly Color FlameBanner = new Color32(248, 118, 22, 245);
        public static readonly Color FlameCore = new Color32(255, 176, 40, 220);
        public static readonly Color PrimaryButton = new Color32(240, 240, 240, 255);
        public static readonly Color PrimaryButtonText = new Color32(50, 50, 50, 255);
        public static readonly Color SecondaryButton = new Color32(60, 60, 60, 255);
        public static readonly Color MenuDim = new Color(0.02f, 0.04f, 0.1f, 0.6f);
    }
}
