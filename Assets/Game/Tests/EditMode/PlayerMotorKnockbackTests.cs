using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Covers the knockback recovery rule: invulnerability must end no later than the climb lockout
    /// (knockback + re-grip), and strictly before a continuously-held climb can carry the player
    /// back up to the band height that hit them -- otherwise holding climb lets a player pass back
    /// through a still-active band for free (see PlayerMotor's field comments for the full rule).
    /// Drives PlayerMotor.Step(dt) directly with a fixed timestep instead of yielding real frames,
    /// since EditMode tests don't tick Update().
    /// </summary>
    public sealed class PlayerMotorKnockbackTests
    {
        private const float Dt = 1f / 60f;

        [Test]
        public void Knockback_InvulnerabilityEndsBeforeHeldReclimbReachesBandHeight()
        {
            var go = new GameObject("Motor");
            try
            {
                PlayerMotor motor = go.AddComponent<PlayerMotor>();
                motor.FinishHeight = 30f;
                motor.ClimbSpeed = 2.5f;

                const float bandHeight = 10f;
                motor.ResetState(bandHeight);
                motor.CanClimb = true;
                motor.ClimbHeld = true; // held throughout, exactly the "holding climb" scenario the bug needs.

                Assert.IsTrue(motor.TryApplyHit(), "first hit while not invulnerable must apply");
                Assert.IsTrue(motor.IsInvulnerable, "invulnerability must start immediately on hit");
                Assert.AreEqual(bandHeight, motor.Height, "smooth knockback must not teleport on the hit frame");

                motor.Step(Dt);
                Assert.Less(motor.Height, bandHeight, "knockback target must be below the band that hit the player");

                float elapsed = Dt;
                while (elapsed < motor.ClimbLockoutSeconds)
                {
                    motor.Step(Dt);
                    elapsed += Dt;
                }

                // Climb was locked out for the entire knockback + re-grip window, so no upward
                // movement should have happened yet -- this alone would be true even with the old,
                // buggy 1.2s invulnerability.
                Assert.LessOrEqual(motor.Height, bandHeight, "no upward movement should occur during lockout");

                // The actual fix: by the moment lockout ends (climb input becomes usable again),
                // invulnerability must already be gone.
                Assert.IsFalse(motor.IsInvulnerable,
                    "invulnerability must have expired by the time the climb lockout ends, " +
                    "otherwise a held climb passes back through the band for free");

                // Keep holding climb until it actually carries the player back up to the band height,
                // and confirm invulnerability is (still) not covering that moment either.
                int safety = 100000;
                while (motor.Height < bandHeight && safety-- > 0)
                {
                    motor.Step(Dt);
                }

                Assert.GreaterOrEqual(motor.Height, bandHeight, "test setup: held climb should reach the band height");
                Assert.IsFalse(motor.IsInvulnerable, "invulnerability must not still cover the moment the player re-reaches the band height");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Step_WhilePaused_FreezesKnockbackAndTimers()
        {
            var go = new GameObject("Motor");
            try
            {
                PlayerMotor motor = go.AddComponent<PlayerMotor>();
                motor.FinishHeight = 30f;
                motor.ResetState(10f);
                motor.TryApplyHit();
                motor.Paused = true;

                float heightBeforePause = motor.Height;
                motor.Step(10f);

                Assert.AreEqual(heightBeforePause, motor.Height, "paused Step calls must not advance knockback");
                Assert.IsTrue(motor.IsInKnockback, "paused Step calls must not finish knockback");
                Assert.IsTrue(motor.IsInvulnerable, "paused Step calls must not consume recovery timers");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Knockback_NeverProducesUpwardHeightChangedDuringDisplacement()
        {
            var go = new GameObject("Motor");
            try
            {
                PlayerMotor motor = go.AddComponent<PlayerMotor>();
                motor.FinishHeight = 30f;
                motor.ClimbSpeed = 2.5f;
                motor.ResetState(10f);
                motor.CanClimb = true;
                motor.ClimbHeld = false; // isolate the knockback's own displacement from climb movement.

                bool sawUpwardStep = false;
                motor.TryApplyHit();

                float elapsed = 0f;
                float lastHeight = motor.Height;
                while (elapsed < motor.KnockbackSeconds)
                {
                    motor.Step(Dt);
                    elapsed += Dt;
                    if (motor.Height > lastHeight)
                    {
                        sawUpwardStep = true;
                    }

                    lastHeight = motor.Height;
                }

                Assert.IsFalse(sawUpwardStep, "knockback displacement must be monotonically non-increasing");
                Assert.GreaterOrEqual(motor.Height, 0f, "height must stay clamped to >= 0");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [TestCase(10f, 8.5f)]
        [TestCase(0.5f, 0f)]
        public void Knockback_SettlesAtBoundedTarget(float startHeight, float expectedHeight)
        {
            var go = new GameObject("Motor");
            try
            {
                PlayerMotor motor = go.AddComponent<PlayerMotor>();
                motor.FinishHeight = 30f;
                motor.ResetState(startHeight);

                Assert.IsTrue(motor.TryApplyHit());
                Assert.AreEqual(startHeight, motor.Height, "hit frame must begin smooth knockback without teleporting");

                motor.Step(motor.KnockbackSeconds);

                Assert.IsFalse(motor.IsInKnockback, "knockback must settle after its configured duration");
                Assert.AreEqual(expectedHeight, motor.Height, 0.0001f,
                    "settled knockback must apply the configured displacement and clamp at ground height");
                Assert.That(motor.Height, Is.InRange(0f, motor.FinishHeight));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HeldClimb_ClampsAtFinishHeight()
        {
            var go = new GameObject("Motor");
            try
            {
                PlayerMotor motor = go.AddComponent<PlayerMotor>();
                motor.FinishHeight = 3f;
                motor.ClimbSpeed = 10f;
                motor.ResetState(2.5f);
                motor.CanClimb = true;
                motor.ClimbHeld = true;

                motor.Step(1f);
                motor.Step(1f);

                Assert.AreEqual(motor.FinishHeight, motor.Height, 0.0001f,
                    "held climb must stop at the authored finish height without overshoot");
                Assert.AreEqual(1f, motor.NormalizedProgress, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
