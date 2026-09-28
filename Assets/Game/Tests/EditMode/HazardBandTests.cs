using Game.Core;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>Body-span contact for hazard bands and the hazard knockback, driven through PlayerMotor.Step / HazardBand.Tick with explicit values.</summary>
    public sealed class HazardBandTests
    {
        private const float BandHeight = 10f;
        private const float BodyHeight = 2.7f;
        private const float Clearance = 0.27f;

        private GameObject _motorGo;
        private GameObject _bandGo;
        private PlayerMotor _motor;
        private HazardBand _band;
        private float _clock;
        private int _hits;

        [SetUp]
        public void SetUp()
        {
            _motorGo = new GameObject("Motor");
            _motor = _motorGo.AddComponent<PlayerMotor>();
            _motor.FinishHeight = 100f;
            _motor.ClimbSpeed = 2.5f;
            _motor.CanClimb = true;
            _clock = 0f;
            _hits = 0;
        }

        [TearDown]
        public void TearDown()
        {
            if (_bandGo != null)
            {
                Object.DestroyImmediate(_bandGo);
            }

            Object.DestroyImmediate(_motorGo);
        }

        private void CreateBand(float activeSeconds)
        {
            _bandGo = new GameObject("Band");
            _band = _bandGo.AddComponent<HazardBand>();
            var spec = new HazardSpec { height = BandHeight, periodSeconds = 10f, activeSeconds = activeSeconds, phaseOffsetSeconds = 0f };
            _band.Initialize(spec, _motor, () => _hits++, null, null, null, () => _clock, 2.6f, BodyHeight);
        }

        [Test]
        public void ActiveBand_InMiddleOfBody_Hits()
        {
            CreateBand(activeSeconds: 10f);
            _motor.ResetState(BandHeight - BodyHeight * 0.5f);

            _band.Tick();

            Assert.AreEqual(1, _hits);
        }

        [Test]
        public void ActiveBand_AboveHead_DoesNotHit()
        {
            CreateBand(activeSeconds: 10f);
            _motor.ResetState(BandHeight - BodyHeight - 0.05f);

            _band.Tick();

            Assert.AreEqual(0, _hits);
        }

        [Test]
        public void ActiveBand_BelowFeet_DoesNotHit()
        {
            CreateBand(activeSeconds: 10f);
            _motor.ResetState(BandHeight + 0.05f);

            _band.Tick();

            Assert.AreEqual(0, _hits);
        }

        [Test]
        public void ClimbingHeadIntoActiveBand_Hits()
        {
            CreateBand(activeSeconds: 10f);
            _motor.ResetState(BandHeight - BodyHeight - 0.1f);
            _motor.ClimbHeld = true;

            _motor.Step(0.1f); // 0.25 up: head passes the band.

            Assert.AreEqual(1, _hits);
        }

        [Test]
        public void FastFrame_SweepingPastActiveBand_StillHits()
        {
            CreateBand(activeSeconds: 10f);
            _motor.ResetState(2f);
            _motor.ClimbSpeed = 200f;
            _motor.ClimbHeld = true;

            _motor.Step(0.1f); // 2 -> 22: the whole body ends far above the band.

            Assert.Greater(_motor.Height, BandHeight + BodyHeight);
            Assert.AreEqual(1, _hits);
        }

        [Test]
        public void SafeBand_DoesNotHit()
        {
            CreateBand(activeSeconds: 0f);
            _motor.ResetState(2f);
            _motor.ClimbSpeed = 200f;
            _motor.ClimbHeld = true;

            _motor.Step(0.1f);
            _band.Tick();

            Assert.AreEqual(0, _hits);
        }

        [Test]
        public void BandActivatingWhileOverlappingStationaryClimber_Hits()
        {
            CreateBand(activeSeconds: 1f);
            _clock = 5f; // safe window
            _motor.ResetState(BandHeight - 1f);

            _band.Tick();
            Assert.AreEqual(0, _hits);

            _clock = 10.5f; // wraps into the active window
            _band.Tick();
            Assert.AreEqual(1, _hits);
        }

        [Test]
        public void HazardKnockback_LeavesHeadBelowBand()
        {
            _motor.ResetState(BandHeight - 0.5f); // usual 1.5 knockback would land at 8.5: head at 11.2, still through the band.

            Assert.IsTrue(_motor.TryApplyHazardHit(BandHeight, BodyHeight, Clearance));
            _motor.Step(_motor.KnockbackSeconds + 0.01f);

            Assert.IsFalse(_motor.IsInKnockback);
            Assert.Less(_motor.Height + BodyHeight, BandHeight);
            Assert.AreEqual(BandHeight - BodyHeight - Clearance, _motor.Height, 0.001f);
        }

        [Test]
        public void HazardKnockback_NeverGoesBelowZero()
        {
            _motor.ResetState(1f);

            Assert.IsTrue(_motor.TryApplyHazardHit(BandHeight, BodyHeight, Clearance)); // clear target 7.03 > Height: only the usual drop applies.
            _motor.Step(_motor.KnockbackSeconds + 0.01f);

            Assert.AreEqual(0f, _motor.Height, 0.001f);

            _motor.ResetState(0.5f);
            Assert.IsTrue(_motor.TryApplyHazardHit(1f, BodyHeight, Clearance)); // band lower than the body: clamps at 0.
            _motor.Step(_motor.KnockbackSeconds + 0.01f);

            Assert.AreEqual(0f, _motor.Height, 0.001f);
        }

        [Test]
        public void HazardKnockback_KeepsUsualDistanceWhenAlreadyClear()
        {
            _motor.ResetState(9f); // clear target 7.03 is further than the usual 7.5.

            Assert.IsTrue(_motor.TryApplyHazardHit(BandHeight, BodyHeight, Clearance));
            _motor.Step(_motor.KnockbackSeconds + 0.01f);

            Assert.AreEqual(BandHeight - BodyHeight - Clearance, _motor.Height, 0.001f);
        }

        [Test]
        public void HazardKnockback_RespectsInvulnerability()
        {
            _motor.ResetState(9f);
            Assert.IsTrue(_motor.TryApplyHazardHit(BandHeight, BodyHeight, Clearance));

            Assert.IsFalse(_motor.TryApplyHazardHit(BandHeight, BodyHeight, Clearance));
        }

        [Test]
        public void BumpKnockback_DistanceIsUnchanged()
        {
            _motor.ResetState(10f);

            Assert.IsTrue(_motor.TryApplyHit());
            _motor.Step(_motor.KnockbackSeconds + 0.01f);

            Assert.AreEqual(8.5f, _motor.Height, 0.001f); // hitDisplacement 1.5 at DistanceScale 1.
        }
    }
}
