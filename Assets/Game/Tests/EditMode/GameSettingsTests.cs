using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// A fresh GameSettings carries exactly the defaults the components' own fields had before the
    /// tuning moved into one asset (values taken from the pre-refactor component sources), so a
    /// component left without an asset behaves as it always did.
    /// </summary>
    public sealed class GameSettingsTests
    {
        private GameSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<GameSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void Motor_DefaultsMatchThePreviousPlayerMotorFields()
        {
            Assert.AreEqual(1.5f, _settings.motor.hitDisplacement);
            Assert.AreEqual(0.35f, _settings.motor.knockbackSeconds);
            Assert.AreEqual(0.6f, _settings.motor.bumpMoveSeconds);
            Assert.AreEqual(0.25f, _settings.motor.regripSeconds);
            Assert.AreEqual(0.6f, _settings.motor.invulnerabilitySeconds);
        }

        [Test]
        public void Camera_DefaultsMatchThePreviousCameraFollowAndShakeFields()
        {
            Assert.AreEqual(6f, _settings.camera.followLerp);
            Assert.AreEqual(0.25f, _settings.camera.bumpFollowFactor);
            Assert.AreEqual(0.35f * 4.5f, _settings.camera.shakeMagnitude, 1e-4f, "the old builder bound 0.35 x WorldScale 4.5");
            Assert.AreEqual(1.575f, _settings.camera.shakeMagnitude, 1e-4f);
        }

        [Test]
        public void Hud_DefaultsMatchThePreviousViewFields()
        {
            Assert.AreEqual(100, _settings.hud.displayUnitsPerWorldUnit, "HudView");
            Assert.AreEqual(1.5f, _settings.hud.hintFadeStartHeight, "ControlsHintView");
            Assert.AreEqual(2.5f, _settings.hud.hintFadeDistance, "ControlsHintView");
            Assert.AreEqual(4f, _settings.hud.cardLifetimeSeconds, "EventFeedView");
            Assert.AreEqual(0.5f, _settings.hud.cardFadeSeconds, "EventFeedView");
            Assert.AreEqual(0.18f, _settings.hud.cardPopSeconds, "EventFeedView");
        }

        [Test]
        public void Input_TouchRegionMatchesThePreviousConstant()
        {
            Assert.AreEqual(0.35f, _settings.input.touchRegionNormalizedHeight);
        }

        [Test]
        public void Summit_DefaultsMatchThePreviousSlideViewFields()
        {
            Assert.AreEqual(0.8f, _settings.summit.slideSeconds);
            Assert.AreEqual(180f, _settings.summit.endYawDegrees);
            Assert.AreEqual(0.4f, _settings.summit.riseFirstFraction);
        }

        [Test]
        public void Burst_DefaultsMatchThePreviousBumpBurstViewFields()
        {
            Assert.AreEqual(14, _settings.burst.gloveCount);
            Assert.AreEqual(16, _settings.burst.starCount);
            Assert.AreEqual(1.3f, _settings.burst.durationSeconds);
            Assert.AreEqual(0.55f, _settings.burst.gloveFlightSeconds);
            Assert.AreEqual(230f, _settings.burst.gloveSize);
            Assert.AreEqual(90f, _settings.burst.starSize);
            Assert.AreEqual(0.3f, _settings.burst.flashSeconds, "was a local const in BumpBurstView");
            Assert.AreEqual(0.45f, _settings.burst.glowSeconds, "was a local const in BumpBurstView");
        }

        [Test]
        public void Pose_DefaultsMatchThePreviousClimberPoseDriverFields()
        {
            Assert.AreEqual(0.4f, _settings.pose.gripOutward);
            Assert.AreEqual(0.95f, _settings.pose.gripHigh);
            Assert.AreEqual(0.18f, _settings.pose.idleReachSeconds);
            Assert.AreEqual(3f, _settings.pose.swayFrequency);
            Assert.AreEqual(1.2f, _settings.pose.grabYank);
            Assert.AreEqual(0.3f, _settings.pose.hitPushDistance);
            Assert.AreEqual(0.2f, _settings.pose.regripSeconds);
            Assert.AreEqual(new Vector3(0.1f, -1f, -0.05f), _settings.pose.legHangDirection);
            Assert.AreEqual(new Vector3(0.7f, 0.75f, -0.2f), _settings.pose.flailArmDirection);
            Assert.AreEqual(new Vector3(0.25f, 1f, 0.05f), _settings.pose.boostArmDirection);
            Assert.AreEqual(new Vector3(0.08f, -1f, -0.18f), _settings.pose.boostLegDirection);
        }

        [Test]
        public void Features_DefaultToOff_AndPaceToTheTunedRate()
        {
            Assert.IsFalse(_settings.features.LivesEnabled);
            Assert.IsFalse(_settings.features.PauseMenuEnabled);
            Assert.AreEqual(3, _settings.features.StartingLives);
            Assert.AreEqual(ClimbPace.TunedBodyHeightsPerSecond, _settings.pace.BodyHeightsPerSecond);
        }

        [Test]
        public void Webhook_DefaultPortIs56789()
        {
            Assert.AreEqual(56789, _settings.webhook.port);
            Assert.AreEqual(WebhookSettings.DefaultPort, _settings.webhook.port);
        }

        [Test]
        public void Bumps_DefaultToOneBoxingType()
        {
            Assert.AreEqual(1, _settings.bumps.types.Length);
            Assert.AreEqual("boxing", _settings.bumps.types[0].id);
            Assert.AreEqual("boxing", _settings.bumps.defaultTypeId);
            Assert.IsTrue(_settings.bumps.TryGet("boxing", out BumpType boxing));
            Assert.AreEqual(1f, boxing.liftBodyHeights);
            Assert.AreEqual(1f, boxing.dropBodyHeights);
        }

        [Test]
        public void OrDefaults_Null_ReturnsTheSharedDefaults()
        {
            Assert.AreSame(GameSettings.Defaults, GameSettings.OrDefaults(null));
            Assert.AreSame(GameSettings.Defaults, GameSettings.OrDefaults(null), "the same instance every time");
        }

        [Test]
        public void OrDefaults_NonNull_ReturnsThatInstance()
        {
            Assert.AreSame(_settings, GameSettings.OrDefaults(_settings));
        }
    }
}
