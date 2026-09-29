using Game.Core;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>Default social-bump strengths: independent per polarity, stronger than a hazard hit, and clamped to the tower.</summary>
    public sealed class BumpStrengthTests
    {
        private const float BodyHeight = 2.7f;
        private const float Dt = 1f / 60f;

        private GameSettings _settings;
        private GameObject _go;
        private PlayerMotor _motor;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<GameSettings>();
            _go = new GameObject("Motor");
            _motor = _go.AddComponent<PlayerMotor>();
            SettingsBinding.Bind(_motor, _settings);
            _motor.FinishHeight = 30f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_settings);
        }

        private void Settle()
        {
            for (float t = 0f; t < _motor.BumpMoveSeconds + Dt * 2f; t += Dt)
            {
                _motor.Step(Dt);
            }
        }

        [Test]
        public void DefaultBoxing_BothPolaritiesBeatAHazardKnockback_AndAreIndependentlyTunable()
        {
            Assert.IsTrue(_settings.bumps.TryGet("boxing", out BumpType boxing));
            float hazardBodyHeights = _settings.motor.hitDisplacement / BodyHeight;

            Assert.Greater(boxing.liftBodyHeights, 1.5f, "beyond the old one-body-height baseline");
            Assert.Greater(boxing.dropBodyHeights, 1.5f);
            Assert.Greater(boxing.liftBodyHeights, hazardBodyHeights * 2f);
            Assert.Greater(boxing.dropBodyHeights, hazardBodyHeights * 2f);

            boxing.liftBodyHeights = 3f;
            boxing.dropBodyHeights = 0.5f;
            Assert.AreEqual(3f, boxing.liftBodyHeights);
            Assert.AreEqual(0.5f, boxing.dropBodyHeights, "changing one polarity leaves the other alone");
        }

        [Test]
        public void PositiveBump_NearTheSummit_ClampsAtTheFinish()
        {
            Assert.IsTrue(_settings.bumps.TryGet("boxing", out BumpType boxing));
            _motor.ResetState(28f);

            _motor.ApplyBump(boxing.liftBodyHeights * BodyHeight);
            Settle();

            Assert.AreEqual(30f, _motor.Height, 1e-3f);
        }

        [Test]
        public void NegativeBump_NearTheBottom_ClampsAtZero_AndTheClimberCanRegrip()
        {
            Assert.IsTrue(_settings.bumps.TryGet("boxing", out BumpType boxing));
            _motor.ResetState(1f);

            _motor.ApplyBump(-boxing.dropBodyHeights * BodyHeight);
            Settle();

            Assert.AreEqual(0f, _motor.Height, 1e-3f);
            for (float t = 0f; t < _motor.BumpLockoutSeconds; t += Dt)
            {
                _motor.Step(Dt);
            }

            Assert.IsFalse(_motor.IsLockedOut, "input is accepted again after the bump's lockout");
        }
    }
}
