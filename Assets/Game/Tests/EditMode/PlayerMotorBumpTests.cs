using Game.Core;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>Covers PlayerMotor.ApplyBump: signed eased displacement, clamping, stacking, and safe passage through hazard bands.</summary>
    public sealed class PlayerMotorBumpTests
    {
        private const float Dt = 1f / 60f;

        private GameObject _go;
        private PlayerMotor _motor;
        private GameSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<GameSettings>();
            _go = new GameObject("Motor");
            _motor = _go.AddComponent<PlayerMotor>();
            SettingsBinding.Bind(_motor, _settings);
            _motor.FinishHeight = 30f;
            _motor.ClimbSpeed = 2.5f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_settings);
        }

        private void RunFor(float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
            {
                _motor.Step(Dt);
            }
        }

        [Test]
        public void PositiveBump_EasesUpByTheWorldDistance_IgnoringDistanceScale()
        {
            _motor.DistanceScale = 2f;
            _motor.ResetState(10f);

            _motor.ApplyBump(3f);
            Assert.AreEqual(10f, _motor.Height, "no teleport on the bump frame");
            _motor.Step(Dt);
            Assert.Greater(_motor.Height, 10f);
            Assert.Less(_motor.Height, 13f);

            RunFor(_motor.BumpMoveSeconds + Dt);

            Assert.AreEqual(13f, _motor.Height, 0.001f, "a bump's distance is already in world units");
            Assert.IsFalse(_motor.IsInKnockback);
        }

        [Test]
        public void PositiveBump_ClampsAtFinish()
        {
            _motor.ResetState(29.5f);

            _motor.ApplyBump(5f);
            RunFor(_motor.BumpMoveSeconds + Dt);

            Assert.AreEqual(30f, _motor.Height, 0.001f);
            Assert.AreEqual(1f, _motor.NormalizedProgress, 0.0001f);
        }

        [Test]
        public void NegativeBump_ClampsAtZero()
        {
            _motor.ResetState(0.5f);

            _motor.ApplyBump(-5f);
            RunFor(_motor.BumpMoveSeconds + Dt);

            Assert.AreEqual(0f, _motor.Height, 0.001f);
        }

        [Test]
        public void Bump_AppliesWhileInvulnerable_AndStacks()
        {
            _motor.ResetState(10f);
            Assert.IsTrue(_motor.TryApplyHit());
            Assert.IsTrue(_motor.IsInvulnerable);
            RunFor(_motor.KnockbackSeconds + Dt); // settles at 8.5

            _motor.ApplyBump(1.5f);
            Assert.IsTrue(_motor.IsInKnockback, "a bump lands even though the hit's invulnerability may still be running");

            _motor.Step(Dt * 6f);
            _motor.ApplyBump(1.5f); // retargets from the current height, mid-move
            RunFor(_motor.BumpMoveSeconds + Dt);

            Assert.AreEqual(8.5f + 3f, _motor.Height, 0.001f, "the second bump adds to the first bump's target");
        }

        [Test]
        public void RapidPositiveBumps_AccumulateFullDistance()
        {
            _motor.ResetState(10f);

            _motor.ApplyBump(3f);
            _motor.Step(Dt * 3f);
            _motor.ApplyBump(3f);
            RunFor(_motor.BumpMoveSeconds + Dt);

            Assert.AreEqual(10f + 6f, _motor.Height, 0.001f);
        }

        [Test]
        public void UpwardBump_CrossingBandOnItsLastStep_CostsNoHitPoint_EvenWithShortInvulnerability()
        {
            _settings.motor.invulnerabilitySeconds = 0.05f;
            var hits = 0;
            const float bandHeight = 11.9999f; // just under the 12 target: only the final eased steps reach it.
            _motor.HeightChanged += (previous, next) =>
            {
                if (previous < bandHeight && next >= bandHeight && _motor.TryApplyHit())
                {
                    hits++;
                }
            };
            _motor.ResetState(9f);

            _motor.ApplyBump(3f);
            RunFor(_motor.BumpMoveSeconds + Dt);

            Assert.AreEqual(12f, _motor.Height, 0.001f);
            Assert.AreEqual(0, hits);
        }

        [Test]
        public void Bump_InvulnerabilityCoversTheWholeEasedMove()
        {
            _motor.ResetState(5f);

            _motor.ApplyBump(3f);
            RunFor(_motor.BumpMoveSeconds - 2f * Dt);

            Assert.IsTrue(_motor.IsInKnockback);
            Assert.IsTrue(_motor.IsInvulnerable, "a hit must be refused for the entire move");
            Assert.IsFalse(_motor.TryApplyHit());
        }

        [Test]
        public void UpwardBump_AcrossActiveHazardBand_CostsNoHitPoint()
        {
            var hits = 0;
            const float bandHeight = 10f;
            // What HazardBand + GameSession.OnHazardHit do: an upward crossing asks TryApplyHit and only a granted hit costs a point.
            _motor.HeightChanged += (previous, next) =>
            {
                if (previous < bandHeight && next >= bandHeight && _motor.TryApplyHit())
                {
                    hits++;
                }
            };
            _motor.ResetState(9f);

            _motor.ApplyBump(3f);
            RunFor(_motor.BumpMoveSeconds + Dt);

            Assert.Greater(_motor.Height, bandHeight, "the bump carried the climber through the band");
            Assert.AreEqual(0, hits);
        }

        [Test]
        public void DelayedBump_DoesNotMoveBeforeTheImpactDelay()
        {
            _motor.ResetState(10f);

            _motor.ApplyBump(3f, 0.3f);
            RunFor(0.25f);

            Assert.AreEqual(10f, _motor.Height);
            Assert.IsFalse(_motor.IsInKnockback);
            Assert.IsFalse(_motor.IsInvulnerable);
            Assert.IsFalse(_motor.IsLockedOut);
            Assert.AreEqual(1, _motor.PendingBumpCount);

            RunFor(0.1f + Dt);
            Assert.IsTrue(_motor.IsBumpMove);
            Assert.AreEqual(0, _motor.PendingBumpCount);
            RunFor(_motor.BumpMoveSeconds + Dt);
            Assert.AreEqual(13f, _motor.Height, 0.001f);
        }

        [Test]
        public void PendingBump_FreezesWhilePaused()
        {
            _motor.ResetState(10f);
            _motor.ApplyBump(3f, 0.3f);
            RunFor(0.2f);

            _motor.Paused = true;
            RunFor(5f);
            Assert.AreEqual(1, _motor.PendingBumpCount);
            Assert.IsFalse(_motor.IsInKnockback);

            _motor.Paused = false;
            RunFor(0.05f);
            Assert.IsFalse(_motor.IsInKnockback, "only about 0.25 s of the delay has elapsed");
            RunFor(0.1f);
            Assert.IsTrue(_motor.IsBumpMove);
        }

        [Test]
        public void ResetState_CancelsAPendingBump()
        {
            _motor.ResetState(10f);
            _motor.ApplyBump(3f, 0.3f);

            _motor.ResetState(0f);
            RunFor(1f + _motor.BumpMoveSeconds);

            Assert.AreEqual(0, _motor.PendingBumpCount);
            Assert.AreEqual(0f, _motor.Height);
            Assert.IsFalse(_motor.IsInKnockback);
        }

        [Test]
        public void BumpMove_UsesBumpMoveSeconds_AndItsOwnLockout()
        {
            _motor.ResetState(10f);
            _motor.ApplyBump(3f);

            Assert.IsTrue(_motor.IsBumpMove);
            Assert.AreEqual(1, _motor.BumpMoveDirection);
            Assert.Greater(_motor.BumpMoveSeconds, _motor.KnockbackSeconds);
            Assert.AreEqual(_motor.BumpMoveSeconds + (_motor.ClimbLockoutSeconds - _motor.KnockbackSeconds), _motor.BumpLockoutSeconds, 0.0001f);

            RunFor(_motor.KnockbackSeconds + 2f * Dt);
            Assert.IsTrue(_motor.IsInKnockback, "a bump outlasts a hazard knockback");
            Assert.Less(_motor.Height, 13f);

            RunFor(_motor.BumpMoveSeconds - _motor.KnockbackSeconds);
            Assert.IsFalse(_motor.IsInKnockback);
            Assert.AreEqual(13f, _motor.Height, 0.001f);
            Assert.IsTrue(_motor.IsLockedOut, "re-grip follows the move");

            RunFor(_motor.BumpLockoutSeconds - _motor.BumpMoveSeconds + 2f * Dt);
            Assert.IsFalse(_motor.IsLockedOut);
        }

        [Test]
        public void HazardHit_IsNotABumpMove_AndUsesKnockbackSeconds()
        {
            _motor.ResetState(10f);
            Assert.IsTrue(_motor.TryApplyHit());

            Assert.IsFalse(_motor.IsBumpMove);
            Assert.AreEqual(0, _motor.BumpMoveDirection);
            RunFor(_motor.KnockbackSeconds + 2f * Dt);
            Assert.IsFalse(_motor.IsInKnockback);
        }

        [Test]
        public void NegativeBump_ReportsDownwardDirection()
        {
            _motor.ResetState(10f);
            _motor.ApplyBump(-3f);

            Assert.AreEqual(-1, _motor.BumpMoveDirection);
        }

        [Test]
        public void DelayedBumps_StackTheirFullDistances()
        {
            _motor.ResetState(10f);
            _motor.ApplyBump(3f, 0.2f);
            _motor.ApplyBump(3f, 0.3f);

            RunFor(0.3f + _motor.BumpMoveSeconds + 2f * Dt);

            Assert.AreEqual(16f, _motor.Height, 0.001f);
        }
    }
}
