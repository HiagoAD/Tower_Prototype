using System.Collections.Generic;
using Game.Core;
using Game.Webhook;

namespace Game.Presentation
{
    /// <summary>Sound cues GameAudio can play. Append only.</summary>
    public enum SoundCue
    {
        Grab,
        Hit,
        PositiveBump,
        LevelWin,
        FinalWin,
        UiClick,
    }

    /// <summary>
    /// GameAudio's engine-free logic: which cue an event maps to, per-cue cooldown and overlap
    /// limits, and cues held back until a delay (a bump's impact moment) has elapsed on session time.
    /// Pending cues advance only by the delta the caller passes, so a paused session freezes them.
    /// </summary>
    public sealed class SoundCueScheduler
    {
        private readonly struct Pending
        {
            public readonly SoundCue Cue;
            public readonly float Remaining;

            public Pending(SoundCue cue, float remaining)
            {
                Cue = cue;
                Remaining = remaining;
            }
        }

        private const int CueCount = 6;

        private readonly SoundSettings _settings;
        private readonly List<Pending> _pending = new List<Pending>();
        private readonly float[] _lastPlayed = new float[CueCount];
        private readonly List<float>[] _endTimes = new List<float>[CueCount];

        public SoundCueScheduler(SoundSettings settings)
        {
            _settings = settings;
            for (int i = 0; i < CueCount; i++)
            {
                _lastPlayed[i] = float.NegativeInfinity;
                _endTimes[i] = new List<float>();
            }
        }

        public int PendingCount => _pending.Count;

        /// <summary>The win cue for the level that was just cleared.</summary>
        public static SoundCue WinCue(bool isFinalLevel)
        {
            return isFinalLevel ? SoundCue.FinalWin : SoundCue.LevelWin;
        }

        /// <summary>The cue for an accepted bump, or null for a negative bump (BumpBurstView's punch covers it).</summary>
        public static SoundCue? BumpCue(BumpEvent bump)
        {
            return bump.Polarity == BumpPolarity.Positive ? SoundCue.PositiveBump : (SoundCue?)null;
        }

        public float VolumeOf(SoundCue cue)
        {
            switch (cue)
            {
                case SoundCue.Grab: return _settings.grabVolume;
                case SoundCue.Hit: return _settings.hitVolume;
                case SoundCue.PositiveBump: return _settings.bumpVolume;
                case SoundCue.LevelWin: return _settings.winVolume;
                case SoundCue.FinalWin: return _settings.finalWinVolume;
                case SoundCue.UiClick: return _settings.clickVolume;
                default: return 1f;
            }
        }

        private float CooldownOf(SoundCue cue)
        {
            switch (cue)
            {
                case SoundCue.Grab: return _settings.grabCooldownSeconds;
                case SoundCue.Hit: return _settings.hitCooldownSeconds;
                case SoundCue.PositiveBump: return _settings.bumpCooldownSeconds;
                case SoundCue.UiClick: return _settings.clickCooldownSeconds;
                default: return 0f;
            }
        }

        private int MaxOverlapOf(SoundCue cue)
        {
            switch (cue)
            {
                case SoundCue.Grab: return _settings.grabMaxOverlap;
                case SoundCue.Hit: return _settings.hitMaxOverlap;
                case SoundCue.PositiveBump: return _settings.bumpMaxOverlap;
                default: return 1;
            }
        }

        /// <summary>Holds a cue back for delaySeconds of session time (a non-positive delay is due on the next Advance).</summary>
        public void Schedule(SoundCue cue, float delaySeconds)
        {
            _pending.Add(new Pending(cue, delaySeconds));
        }

        /// <summary>Counts every pending cue down by dt and appends the ones now due to due, in scheduling order.</summary>
        public void Advance(float dt, List<SoundCue> due)
        {
            if (_pending.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _pending.Count;)
            {
                Pending p = _pending[i];
                float remaining = p.Remaining - dt;
                if (remaining <= 0f)
                {
                    due.Add(p.Cue);
                    _pending.RemoveAt(i);
                }
                else
                {
                    _pending[i] = new Pending(p.Cue, remaining);
                    i++;
                }
            }
        }

        /// <summary>Drops every pending cue and forgets the voices in flight (level start, menu, win).</summary>
        public void Cancel()
        {
            _pending.Clear();
            for (int i = 0; i < CueCount; i++)
            {
                _endTimes[i].Clear();
                _lastPlayed[i] = float.NegativeInfinity;
            }
        }

        /// <summary>
        /// Whether the cue may sound now: false inside its cooldown, or when maxOverlap voices of it are
        /// still playing. A true result records the play (now and clipLength are in the same clock).
        /// </summary>
        public bool TryPlay(SoundCue cue, float now, float clipLength)
        {
            int index = (int)cue;
            if (index < 0 || index >= CueCount)
            {
                return false;
            }

            if (now - _lastPlayed[index] < CooldownOf(cue))
            {
                return false;
            }

            List<float> ends = _endTimes[index];
            ends.RemoveAll(end => end <= now);
            if (ends.Count >= MaxOverlapOf(cue))
            {
                return false;
            }

            _lastPlayed[index] = now;
            ends.Add(now + clipLength);
            return true;
        }
    }
}
