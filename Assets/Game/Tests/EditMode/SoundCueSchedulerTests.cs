using System.Collections.Generic;
using Game.Core;
using Game.Presentation;
using Game.Webhook;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>GameAudio's engine-free logic: cue selection, cooldowns, overlap limits and delayed cues.</summary>
    public sealed class SoundCueSchedulerTests
    {
        private static SoundCueScheduler New(out SoundSettings sound)
        {
            sound = new SoundSettings();
            return new SoundCueScheduler(sound);
        }

        [Test]
        public void WinCue_IsFinalOnlyOnTheLastLevel()
        {
            Assert.AreEqual(SoundCue.FinalWin, SoundCueScheduler.WinCue(true));
            Assert.AreEqual(SoundCue.LevelWin, SoundCueScheduler.WinCue(false));
        }

        [Test]
        public void BumpCue_IsPositiveOnly()
        {
            var type = new BumpType();
            Assert.AreEqual(SoundCue.PositiveBump, SoundCueScheduler.BumpCue(new BumpEvent("a", BumpPolarity.Positive, type, "t")));
            Assert.IsNull(SoundCueScheduler.BumpCue(new BumpEvent("b", BumpPolarity.Negative, type, "t")));
        }

        [Test]
        public void Grab_IsRateLimitedByCooldown()
        {
            SoundCueScheduler s = New(out SoundSettings sound);
            Assert.IsTrue(s.TryPlay(SoundCue.Grab, 10f, 0.1f));
            Assert.IsFalse(s.TryPlay(SoundCue.Grab, 10f + sound.grabCooldownSeconds * 0.5f, 0.1f));
            Assert.IsTrue(s.TryPlay(SoundCue.Grab, 10f + sound.grabCooldownSeconds + 0.001f, 0.1f));
        }

        [Test]
        public void Overlap_CapsVoicesStillPlaying_AndFreesThemWhenTheyEnd()
        {
            SoundCueScheduler s = New(out SoundSettings sound);
            sound.hitCooldownSeconds = 0f;
            sound.hitMaxOverlap = 2;
            Assert.IsTrue(s.TryPlay(SoundCue.Hit, 0f, 1f));
            Assert.IsTrue(s.TryPlay(SoundCue.Hit, 0.1f, 1f));
            Assert.IsFalse(s.TryPlay(SoundCue.Hit, 0.2f, 1f));
            Assert.IsTrue(s.TryPlay(SoundCue.Hit, 1.05f, 1f));
        }

        [Test]
        public void Cues_LimitIndependently()
        {
            SoundCueScheduler s = New(out _);
            Assert.IsTrue(s.TryPlay(SoundCue.Grab, 1f, 0.1f));
            Assert.IsTrue(s.TryPlay(SoundCue.Hit, 1f, 0.1f));
        }

        [Test]
        public void Scheduled_IsDueOnlyAfterTheDelayOfAdvancedTime()
        {
            SoundCueScheduler s = New(out _);
            var due = new List<SoundCue>();
            s.Schedule(SoundCue.PositiveBump, 0.5f);

            s.Advance(0.3f, due);
            Assert.IsEmpty(due);
            s.Advance(0f, due); // paused session: PlayDeltaTime is 0.
            Assert.IsEmpty(due);
            s.Advance(0.3f, due);
            CollectionAssert.AreEqual(new[] { SoundCue.PositiveBump }, due);
            Assert.AreEqual(0, s.PendingCount);
        }

        [Test]
        public void Cancel_DropsPendingCues_AndClearsLimits()
        {
            SoundCueScheduler s = New(out _);
            var due = new List<SoundCue>();
            s.Schedule(SoundCue.PositiveBump, 0.1f);
            Assert.IsTrue(s.TryPlay(SoundCue.Grab, 1f, 5f));

            s.Cancel();
            s.Advance(1f, due);

            Assert.IsEmpty(due);
            Assert.IsTrue(s.TryPlay(SoundCue.Grab, 1f, 5f));
        }
    }
}
