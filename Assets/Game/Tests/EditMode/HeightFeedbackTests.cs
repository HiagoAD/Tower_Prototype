using Game.Core;
using Game.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class HeightFeedbackTests
    {
        private const float Dt = 1f / 60f;
        private HeightFeedbackSettings _fx;
        private HeightFeedback _feedback;
        private float _height;

        [SetUp]
        public void SetUp()
        {
            _fx = new HeightFeedbackSettings();
            _feedback = new HeightFeedback();
            _height = 0f;
            _feedback.Sample(_height, Dt, _fx); // baseline sample
        }

        private void Run(float unitsPerSecond, float seconds)
        {
            int frames = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < frames; i++)
            {
                _height += unitsPerSecond * Dt;
                _feedback.Sample(_height, Dt, _fx);
            }
        }

        [Test]
        public void Climbing_TurnsGainAndScalesUp()
        {
            Run(2.5f, 0.5f);
            Assert.AreEqual(HeightTrend.Gaining, _feedback.Trend);
            Assert.Greater(_feedback.Scale, 1.05f);
            Assert.AreEqual(_fx.gainColor.g, _feedback.Evaluate(Color.yellow, _fx).g, 0.15f);
        }

        [Test]
        public void Losing_TurnsRedAndScalesMoreThanGain()
        {
            Run(-5f, 0.5f);
            Assert.AreEqual(HeightTrend.Losing, _feedback.Trend);
            Assert.Greater(_feedback.Scale, _fx.gainScale);
            Color c = _feedback.Evaluate(Color.yellow, _fx);
            Assert.Greater(c.r, c.g);
        }

        [Test]
        public void Stopping_ReturnsExactlyToBaseline()
        {
            Run(2.5f, 0.5f);
            Run(0f, 1.5f);
            Assert.AreEqual(HeightTrend.Stable, _feedback.Trend);
            Assert.AreEqual(1f, _feedback.Scale);
            Assert.AreEqual(0f, _feedback.TintAmount);
            Assert.AreEqual(Color.yellow, _feedback.Evaluate(Color.yellow, _fx));
        }

        [Test]
        public void BriefPauseInClimb_DoesNotFlicker()
        {
            Run(2.5f, 0.3f);
            Run(0f, _fx.holdSeconds * 0.5f);
            Assert.AreEqual(HeightTrend.Gaining, _feedback.Trend);
        }

        [Test]
        public void DriftInsideTheDeadband_StaysStable()
        {
            Run(_fx.deadbandUnitsPerSecond * 0.5f, 1f);
            Assert.AreEqual(HeightTrend.Stable, _feedback.Trend);
            Assert.AreEqual(1f, _feedback.Scale);
        }

        [Test]
        public void Scale_NeverExceedsTheConfiguredMaximum()
        {
            Run(-5f, 3f);
            Assert.LessOrEqual(_feedback.Scale, _fx.lossScale + 1e-4f);
        }

        [Test]
        public void ZeroDelta_FreezesState()
        {
            Run(2.5f, 0.3f);
            float scale = _feedback.Scale;
            for (int i = 0; i < 100; i++)
            {
                _feedback.Sample(_height, 0f, _fx);
            }

            Assert.AreEqual(scale, _feedback.Scale);
            Assert.AreEqual(HeightTrend.Gaining, _feedback.Trend);
        }

        [Test]
        public void Reset_ClearsEverything_AndNextSampleOnlyBaselines()
        {
            Run(-5f, 0.5f);
            _feedback.Reset();
            Assert.AreEqual(HeightTrend.Stable, _feedback.Trend);
            Assert.AreEqual(1f, _feedback.Scale);
            _feedback.Sample(500f, Dt, _fx); // e.g. new level's height: a jump must not read as a gain
            Assert.AreEqual(HeightTrend.Stable, _feedback.Trend);
        }
    }
}
