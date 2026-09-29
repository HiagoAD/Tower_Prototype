using Game.Presentation;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class PunchVolleyTests
    {
        private const float Interval = 0.055f;

        [Test]
        public void GloveOnTheImpactFrame_DoesNotDoubleTheImpactPunch()
        {
            var volley = new PunchVolley();
            volley.Register(1f);

            Assert.IsFalse(volley.TryPunch(1f, Interval, 7));
            Assert.AreEqual(1, volley.Count);
        }

        [Test]
        public void GlovesCloserThanTheInterval_AreSkipped_LaterOnesPlay()
        {
            var volley = new PunchVolley();
            volley.Register(0f);

            Assert.IsFalse(volley.TryPunch(0.03f, Interval, 7));
            Assert.IsTrue(volley.TryPunch(0.06f, Interval, 7));
            Assert.IsFalse(volley.TryPunch(0.1f, Interval, 7), "the interval counts from the last punch played, not the last glove");
            Assert.IsTrue(volley.TryPunch(0.12f, Interval, 7));
        }

        [Test]
        public void Volley_StopsAtTheMaximum_CountingTheImpact()
        {
            var volley = new PunchVolley();
            volley.Register(0f);
            int played = 0;
            for (int i = 1; i <= 20; i++)
            {
                if (volley.TryPunch(i * 0.1f, Interval, 7))
                {
                    played++;
                }
            }

            Assert.AreEqual(6, played);
            Assert.AreEqual(7, volley.Count);
        }

        [Test]
        public void Reset_StartsTheNextBumpFresh()
        {
            var volley = new PunchVolley();
            volley.Register(0f);
            for (int i = 1; i < 7; i++)
            {
                volley.TryPunch(i * 0.1f, Interval, 7);
            }

            volley.Reset();

            Assert.AreEqual(0, volley.Count);
            Assert.IsTrue(volley.TryPunch(0f, Interval, 7));
        }

        [Test]
        public void MaxOfOne_PlaysTheImpactPunchAlone()
        {
            var volley = new PunchVolley();
            volley.Register(0f);

            Assert.IsFalse(volley.TryPunch(1f, Interval, 1));
        }
    }
}
