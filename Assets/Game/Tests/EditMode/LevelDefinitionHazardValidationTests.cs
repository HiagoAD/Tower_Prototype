using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>G2 follow-up: per-hazard validation in LevelDefinition.FromJson / HazardSpec.Validate.</summary>
    public sealed class LevelDefinitionHazardValidationTests
    {
        private static string LevelJson(string hazardsJson)
        {
            return "{\"levelId\":1,\"displayName\":\"A\",\"finishHeight\":30,\"climbSpeed\":2,\"hazards\":[" + hazardsJson + "]}";
        }

        private static string Hazard(string height, string period, string active, string phase)
        {
            return "{\"height\":" + height + ",\"periodSeconds\":" + period + ",\"activeSeconds\":" + active + ",\"phaseOffsetSeconds\":" + phase + "}";
        }

        [Test]
        public void ValidHazards_Pass()
        {
            LevelDefinition level = LevelDefinition.FromJson(LevelJson(Hazard("10", "4", "1.5", "0") + "," + Hazard("20", "5", "1.5", "-2")));

            Assert.AreEqual(2, level.hazards.Length);
        }

        [TestCase("0", "4", "1", "0", "hazards[1].height")]
        [TestCase("-3", "4", "1", "0", "hazards[1].height")]
        [TestCase("30", "4", "1", "0", "hazards[1].height")]
        [TestCase("45", "4", "1", "0", "hazards[1].height")]
        [TestCase("10", "0", "1", "0", "hazards[1].periodSeconds")]
        [TestCase("10", "-1", "1", "0", "hazards[1].periodSeconds")]
        [TestCase("10", "4", "0", "0", "hazards[1].activeSeconds")]
        [TestCase("10", "4", "4", "0", "hazards[1].activeSeconds")]
        [TestCase("10", "4", "5", "0", "hazards[1].activeSeconds")]
        public void BadHazard_IsRejectedNamingIndexAndField(string height, string period, string active, string phase, string expected)
        {
            string json = LevelJson(Hazard("10", "4", "1", "0") + "," + Hazard(height, period, active, phase));

            var ex = Assert.Throws<FormatException>(() => LevelDefinition.FromJson(json));

            StringAssert.Contains(expected, ex.Message);
        }

        [Test]
        public void AlwaysActiveBand_MessageShowsTheValue()
        {
            string json = LevelJson(Hazard("10", "4", "1", "0") + "," + Hazard("10", "4", "5", "0"));

            var ex = Assert.Throws<FormatException>(() => LevelDefinition.FromJson(json));

            StringAssert.Contains("hazards[1].activeSeconds must be > 0 and < periodSeconds (got 5)", ex.Message);
        }

        [Test]
        public void NullHazardEntry_IsRejectedNamingTheIndex()
        {
            string json = LevelJson(Hazard("10", "4", "1", "0") + ",null");

            var ex = Assert.Throws<FormatException>(() => LevelDefinition.FromJson(json));

            StringAssert.Contains("hazards[1]", ex.Message);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonFiniteValues_FailValidation(float bad)
        {
            var good = new HazardSpec { height = 10f, periodSeconds = 4f, activeSeconds = 1f, phaseOffsetSeconds = 0f };
            Assert.IsNull(good.Validate(30f));

            HazardSpec h = good;
            h.height = bad;
            StringAssert.Contains("height", h.Validate(30f));

            h = good;
            h.periodSeconds = bad;
            StringAssert.Contains("periodSeconds", h.Validate(30f));

            h = good;
            h.activeSeconds = bad;
            StringAssert.Contains("activeSeconds", h.Validate(30f));

            h = good;
            h.phaseOffsetSeconds = bad;
            StringAssert.Contains("phaseOffsetSeconds", h.Validate(30f));
        }

        [Test]
        public void NonFiniteFinishHeight_FailsValidation()
        {
            var h = new HazardSpec { height = 10f, periodSeconds = 4f, activeSeconds = 1f };
            Assert.IsNotNull(h.Validate(float.NaN));
        }
    }
}
