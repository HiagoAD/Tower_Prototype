using Game.Gameplay;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>EffectPulse: rises to the peak, eases to exactly zero, and blends a newer request from the current value.</summary>
    public sealed class EffectPulseTests
    {
        private const float Dt = 1f / 60f;

        private static void RunFor(EffectPulse pulse, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
            {
                pulse.Step(Dt);
            }
        }

        [Test]
        public void Trigger_ReachesThePeak_ThenReturnsToExactlyZero()
        {
            var pulse = new EffectPulse();
            pulse.Trigger(-0.12f, 0.1f, 0.5f);
            RunFor(pulse, 0.1f + Dt);
            Assert.AreEqual(-0.12f, pulse.Value, 0.01f);

            RunFor(pulse, 0.6f);

            Assert.AreEqual(0f, pulse.Value);
            Assert.IsFalse(pulse.IsActive);
        }

        [Test]
        public void ANewTrigger_StartsFromTheCurrentValue_WithoutAJump()
        {
            var pulse = new EffectPulse();
            pulse.Trigger(0.1f, 0.05f, 0.5f);
            RunFor(pulse, 0.2f);
            float mid = pulse.Value;
            Assert.Greater(mid, 0f);

            pulse.Trigger(-0.12f, 0.05f, 0.5f);
            pulse.Step(Dt);

            Assert.LessOrEqual(System.Math.Abs(pulse.Value - mid), System.Math.Abs(-0.12f - mid) * (Dt / 0.05f) + 1e-4f, "first frame of the new pulse moves only a step toward the new peak");
        }

        [Test]
        public void Reset_ZeroesAtOnce()
        {
            var pulse = new EffectPulse();
            pulse.Trigger(1f, 0.05f, 0.5f);
            RunFor(pulse, 0.1f);

            pulse.Reset();

            Assert.AreEqual(0f, pulse.Value);
            Assert.IsFalse(pulse.IsActive);
        }
    }
}
