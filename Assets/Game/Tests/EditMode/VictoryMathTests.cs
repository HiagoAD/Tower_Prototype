using Game.Core;
using Game.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class VictoryMathTests
    {
        private static VictorySettings Defaults => new VictorySettings();

        [TestCase(false)]
        [TestCase(true)]
        public void HeadingReturnsToBaselineAtEnd(bool final)
        {
            Assert.AreEqual(1f, VictoryMath.HeadingScale(Defaults, final, 1f), 1e-4f);
            Assert.AreEqual(0f, VictoryMath.HeadingTilt(Defaults, final, 1f), 1e-4f);
            Assert.AreEqual(0f, VictoryMath.HeadingTilt(Defaults, final, 0f), 1e-4f);
        }

        [Test]
        public void HeadingStartsAtStartScaleAndOvershoots()
        {
            VictorySettings v = Defaults;
            Assert.AreEqual(v.headingStartScale, VictoryMath.HeadingScale(v, false, 0f), 1e-4f);
            Assert.AreEqual(v.headingOvershoot, VictoryMath.HeadingScale(v, false, v.headingRiseFraction), 1e-4f);
        }

        [Test]
        public void FinalIsBiggerAndLongerThanIntermediate()
        {
            VictorySettings v = Defaults;
            Assert.Greater(VictoryMath.HeadingSeconds(v, true), VictoryMath.HeadingSeconds(v, false));
            Assert.Greater(VictoryMath.HeadingScale(v, true, v.headingRiseFraction), VictoryMath.HeadingScale(v, false, v.headingRiseFraction));
            Assert.Greater(VictoryMath.FirstBurstCount(v, true), VictoryMath.FirstBurstCount(v, false));
            Assert.AreEqual(0, VictoryMath.SecondBurstCount(v, false));
            Assert.Greater(VictoryMath.SecondBurstCount(v, true), 0);
        }

        [Test]
        public void ConfettiAlphaFadesToZeroAtLifetime()
        {
            Assert.AreEqual(1f, VictoryMath.ConfettiAlpha(0f, 2f, 0.3f), 1e-4f);
            Assert.AreEqual(0f, VictoryMath.ConfettiAlpha(2f, 2f, 0.3f), 1e-4f);
            Assert.AreEqual(0f, VictoryMath.ConfettiAlpha(1f, 0f, 0.3f));
        }

        [Test]
        public void ViewCelebrateStepAndStopRestoreHeadingAndClearConfetti()
        {
            var canvas = new GameObject("c", typeof(RectTransform));
            var panel = new GameObject("p", typeof(RectTransform)).GetComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);
            var heading = new GameObject("h", typeof(RectTransform)).GetComponent<RectTransform>();
            heading.SetParent(panel, false);
            var sessionGo = new GameObject("s");
            var session = sessionGo.AddComponent<GameSession>();
            var viewGo = new GameObject("v");
            viewGo.SetActive(false);
            var view = viewGo.AddComponent<VictoryView>();
            SetField(view, "session", session);
            SetField(view, "winHeading", heading);
            SetField(view, "finalHeading", heading);
            viewGo.SetActive(true);
            try
            {
                view.Celebrate(false, panel);
                Assert.IsTrue(view.IsActive);
                Assert.AreEqual(VictoryMath.FirstBurstCount(Defaults, false), view.AliveCount);
                Assert.AreEqual(0.2f, heading.localScale.x, 1e-3f);
                view.Step(10f);
                Assert.AreEqual(0, view.AliveCount, "every piece expires");
                Assert.AreEqual(1f, heading.localScale.x, 1e-3f);
                view.Celebrate(true, panel);
                Assert.AreEqual(VictoryMath.FirstBurstCount(Defaults, true), view.AliveCount);
                view.Stop();
                Assert.IsFalse(view.IsActive);
                Assert.AreEqual(0, view.AliveCount);
                Assert.AreEqual(1f, heading.localScale.x, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(viewGo);
                Object.DestroyImmediate(sessionGo);
                Object.DestroyImmediate(canvas);
            }
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
