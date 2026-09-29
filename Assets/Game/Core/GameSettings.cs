using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Every tuning value the game reads, in one asset (Assets/Game/Settings/GameSettings.asset).
    /// Components hold a reference to it and read their section; nothing tunable lives on a component
    /// or in a scene, so a scene rebuild never resets a tuned value. The scene builder creates the
    /// asset if it is missing and only ever writes ClimbPace's measured body height.
    ///
    /// Not here, deliberately: measurements the scene builder takes from the imported art (body
    /// height on GameSession, tower and summit geometry, camera offset), the builder's scene
    /// composition constants (Level1Layout), per-level content (the level JSON files) and the
    /// webhook transport's safety limits (BumpListener).
    /// </summary>
    [CreateAssetMenu(menuName = "Tower/Game Settings", fileName = "GameSettings")]
    public sealed class GameSettings : ScriptableObject
    {
        public GameFeatures features = new GameFeatures();
        public ClimbPace pace = new ClimbPace();
        public BumpCatalog bumps = new BumpCatalog();
        public MotorSettings motor = new MotorSettings();
        public InputSettings input = new InputSettings();
        public CameraSettings camera = new CameraSettings();
        public HudSettings hud = new HudSettings();
        public SummitSettings summit = new SummitSettings();
        public BurstSettings burst = new BurstSettings();
        public ClimberPoseSettings pose = new ClimberPoseSettings();
        public WebhookSettings webhook = new WebhookSettings();
        public CameraFeedbackSettings cameraFx = new CameraFeedbackSettings();
        public GrabFeedbackSettings grab = new GrabFeedbackSettings();
        public HeightFeedbackSettings heightFx = new HeightFeedbackSettings();
        public VictorySettings victory = new VictorySettings();
        public HapticsSettings haptics = new HapticsSettings();
        public SoundSettings sound = new SoundSettings();

        private static GameSettings _defaults;

        /// <summary>A shared in-memory instance holding every default, for components left without an asset (EditMode tests).</summary>
        public static GameSettings Defaults
        {
            get
            {
                if (_defaults == null)
                {
                    _defaults = CreateInstance<GameSettings>();
                    _defaults.hideFlags = HideFlags.HideAndDontSave;
                }

                return _defaults;
            }
        }

        /// <summary>settings, or Defaults when it is null.</summary>
        public static GameSettings OrDefaults(GameSettings settings)
        {
            return settings != null ? settings : Defaults;
        }

        private void OnValidate()
        {
            bumps.WarnOnDuplicateIds(this);
        }
    }

    /// <summary>
    /// PlayerMotor's hit and bump response. A hit or bump never teleports the climber -- it eases them
    /// to the new height (over knockbackSeconds for a hit, bumpMoveSeconds for a bump), then holds for
    /// regripSeconds before climb input is accepted again. Together they define PlayerMotor's
    /// ClimbLockoutSeconds / BumpLockoutSeconds, the single source of truth for how long input stays locked.
    /// </summary>
    [Serializable]
    public sealed class MotorSettings
    {
        [Tooltip("Hazard knockback distance in the level's authored units; scaled by DistanceScale.")]
        public float hitDisplacement = 1.5f;

        [Tooltip("Duration of a hazard hit's eased knockback.")]
        public float knockbackSeconds = 0.35f;

        [Tooltip("Duration of a webhook bump's eased move. Longer than a hazard knockback so the climber visibly travels on screen.")]
        public float bumpMoveSeconds = 0.6f;

        [Tooltip("Hold after a knockback or bump move before climb input is accepted again.")]
        public float regripSeconds = 0.25f;

        // Must never exceed the climb lockout: invulnerability exists so a hit can't be re-applied
        // while the player is still being carried through the knockback/re-grip they can't yet
        // respond to, not to grant free passage back through a still-active band. PlayerMotor clamps
        // it against ClimbLockoutSeconds so a misconfigured value can't reopen the G3 pass-through bug.
        [Tooltip("Hazard invulnerability after a hit, clamped to the knockback plus regrip lockout.")]
        public float invulnerabilitySeconds = 0.6f;

        [Tooltip("Seconds from an accepted bump to its glove impact, when the climber's move starts. The burst times its first glove's mid-flight to this.")]
        public float bumpImpactDelaySeconds = 0.275f;

        [Tooltip("How far below a hazard band the knocked-back climber's head must end up, in body heights.")]
        public float hazardClearanceBodyHeights = 0.1f;
    }

    [Serializable]
    public sealed class InputSettings
    {
        // Climbing is full-screen: any touch that starts outside an interactive control holds the climb, so there
        // is nothing to tune yet. The section stays so the asset keeps a home for future input options.
    }

    [Serializable]
    public sealed class CameraSettings
    {
        [Tooltip("Follow rate at the tuned pace; scales with the climb pace so the camera trails by the same share of the climber at any speed.")]
        public float followLerp = 6f;

        [Tooltip("Multiplies the follow rate while a webhook bump move plays, so the climber visibly travels on screen and the camera then catches up. Hazard hits are unaffected.")]
        [Min(0.01f)] public float bumpFollowFactor = 0.25f;

        [Tooltip("Peak camera shake offset on a bump impact, in world units.")]
        public float shakeMagnitude = 1.575f;
    }

    [Serializable]
    public sealed class HudSettings
    {
        // World units are small (a level is tens of units tall); the reference counts altitude in
        // the thousands, so the readout is scaled for display only.
        [Tooltip("Altitude readout units per authored world unit.")]
        public int displayUnitsPerWorldUnit = 100;

        [Tooltip("Climbed height at which the controls hint starts fading out.")]
        public float hintFadeStartHeight = 1.5f;

        [Tooltip("Climbed distance over which the controls hint fades out.")]
        public float hintFadeDistance = 2.5f;

        [Tooltip("Seconds an event card stays up before it fades.")]
        public float cardLifetimeSeconds = 4f;

        [Tooltip("Seconds an event card takes to fade out.")]
        public float cardFadeSeconds = 0.5f;

        [Tooltip("Seconds of an event card's pop-in.")]
        public float cardPopSeconds = 0.18f;
    }

    [Serializable]
    public sealed class SummitSettings
    {
        [Tooltip("Duration of the climber's slide onto the summit. The win panel waits the same time, so the slide plays first.")]
        public float slideSeconds = 0.8f;

        [Tooltip("Rotation about Y applied by the end of the slide (180 turns from facing the tower to facing the camera).")]
        public float endYawDegrees = 180f;

        [Tooltip("Share of the slide spent rising before the inward move starts, so the climber clears the lip first.")]
        [Range(0f, 0.9f)] public float riseFirstFraction = 0.4f;
    }

    [Serializable]
    public sealed class BurstSettings
    {
        public int gloveCount = 14;
        public int starCount = 16;
        [Tooltip("Total length of the full-screen bump burst.")]
        public float durationSeconds = 1.3f;
        public float gloveFlightSeconds = 0.55f;
        [Tooltip("Glove sprite size, in canvas units.")]
        public float gloveSize = 230f;
        [Tooltip("Star sprite size, in canvas units.")]
        public float starSize = 90f;
        [Tooltip("Length of the full-screen impact flash.")]
        public float flashSeconds = 0.3f;
        [Tooltip("Length of the glow at the impact point.")]
        public float glowSeconds = 0.45f;
        [Tooltip("Most punches one bump plays, counting the main impact punch. Each further glove passing the climber adds one, spaced by volleyMinIntervalSeconds. 1 plays the impact punch alone.")]
        [Min(1)] public int volleyMaxPunches = 7;
        [Tooltip("Shortest gap between two punches of a bump's volley, so the gloves read as a flurry rather than a buzz.")]
        public float volleyMinIntervalSeconds = 0.055f;
        [Tooltip("Volume of each volley punch, relative to the main impact punch.")]
        [Range(0f, 1f)] public float volleyVolume = 0.6f;
    }

    /// <summary>ClimberPoseDriver's procedural climbing pose. Swing, yank and reach timings are tuned at ClimbPace.TunedBodyHeightsPerSecond and scale with the pace.</summary>
    [Serializable]
    public sealed class ClimberPoseSettings
    {
        [Header("Grips (arm lengths, relative to the shoulder)")]
        [Tooltip("How far out to the side each hand grips the tower.")]
        public float gripOutward = 0.4f;
        [Tooltip("A new grip is taken this high above the shoulder.")]
        public float gripHigh = 0.95f;
        [Tooltip("A planted hand lets go once its grip is this far above the shoulder (negative = below).")]
        public float gripLow = 0f;
        [Tooltip("Climbed distance, in arm lengths, over which a reaching hand travels to its new grip.")]
        public float reachDistance = 0.4f;
        [Tooltip("A hand caught mid-reach when the climber stops finishes its reach over this many seconds.")]
        public float idleReachSeconds = 0.18f;
        public float reachArcOutward = 0.25f;
        public float reachArcAway = 0.35f;

        [Header("Body swing")]
        [Tooltip("Sideways hang toward the hand holding alone, in arm lengths.")]
        public float swayDistance = 0.2f;
        public float swayFrequency = 3f;
        [Range(0f, 1f)] public float swayDamping = 0.45f;
        public float rollDegreesPerSway = 6f;
        public float twistDegrees = 8f;
        [Tooltip("Upward velocity given to the body on every grab, in arm lengths per second.")]
        public float grabYank = 1.2f;
        public float yankFrequency = 3.5f;
        [Range(0f, 1f)] public float yankDamping = 0.45f;

        [Header("Legs")]
        public Vector3 legHangDirection = new Vector3(0.1f, -1f, -0.05f);
        public float legPendulumFrequency = 1.6f;
        [Range(0f, 1f)] public float legPendulumDamping = 0.3f;

        [Header("Hit / recovery")]
        [Tooltip("Body thrown back from the wall on a hit, in arm lengths.")]
        public float hitPushDistance = 0.3f;
        public float hitTiltDegrees = 14f;
        public float hitFlailFrequency = 18f;
        public float regripSeconds = 0.2f;

        [Header("Override arm directions (x outward, y up, z into the tower)")]
        public Vector3 flailArmDirection = new Vector3(0.7f, 0.75f, -0.2f);
        public Vector3 winArmDirection = new Vector3(0.3f, 1f, 0.1f);
        public Vector3 loseArmDirection = new Vector3(0.5f, -0.8f, -0.4f);
        public float loseBodyTiltDegrees = 16f;
        [Tooltip("Arms of the boosted pose, played during an upward bump move: raised overhead.")]
        public Vector3 boostArmDirection = new Vector3(0.25f, 1f, 0.05f);
        [Tooltip("Legs of the boosted pose: hanging, trailing slightly behind the lift (x outward, y up, z into the tower).")]
        public Vector3 boostLegDirection = new Vector3(0.08f, -1f, -0.18f);
    }

    [Serializable]
    public sealed class WebhookSettings
    {
        /// <summary>Also BumpListener's default, for listeners built without settings (the smoke scene, tests).</summary>
        public const int DefaultPort = 56789;

        [Tooltip("Port the /bump listener binds on the device.")]
        public int port = DefaultPort;
    }
}
