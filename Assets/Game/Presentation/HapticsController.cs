using Game.Core;

namespace Game.Presentation
{
    /// <summary>
    /// Decides which pulse plays when: which events count, the grab rate limit, the pause/state gate and
    /// the intensity mapping. Plain C# over an <see cref="IHapticsDevice"/>, so it runs in EditMode tests;
    /// <see cref="HapticsView"/> feeds it the game's events.
    /// </summary>
    public sealed class HapticsController
    {
        private readonly IHapticsDevice _device;
        private readonly System.Func<HapticsSettings> _settings;
        private float _nextGrabTime = float.NegativeInfinity;

        public HapticsController(IHapticsDevice device, System.Func<HapticsSettings> settings)
        {
            _device = device;
            _settings = settings;
        }

        /// <summary>True while the app is backgrounded/unfocused: nothing is started.</summary>
        public bool Suspended { get; set; }

        /// <summary>Light tick on a real hand grab; only while Playing, and at most once per grabMinIntervalSeconds.</summary>
        public bool Grab(SessionState state, float now)
        {
            HapticsSettings s = _settings();
            if (!CanPlay(s) || state != SessionState.Playing || now < _nextGrabTime)
            {
                return false;
            }

            _nextGrabTime = now + s.grabMinIntervalSeconds;
            _device.Pulse(Milliseconds(s.grabMilliseconds, s), Amplitude(s.grabStrength, s));
            return true;
        }

        /// <summary>Stronger pulse on a hazard hit, which the session raises while Playing.</summary>
        public bool HazardHit(SessionState state, float now)
        {
            HapticsSettings s = _settings();
            if (!CanPlay(s) || state != SessionState.Playing)
            {
                return false;
            }

            QuietGrabs(s, now);
            _device.Pulse(Milliseconds(s.hitMilliseconds, s), Amplitude(s.hitStrength, s));
            return true;
        }

        /// <summary>The win: one pulse, or a short burst of them for the final TOWER CLEARED.</summary>
        public bool Won(SessionState state, bool finalLevel, float now)
        {
            HapticsSettings s = _settings();
            if (!CanPlay(s) || state != SessionState.Won)
            {
                return false;
            }

            QuietGrabs(s, now);
            int ms = Milliseconds(s.winMilliseconds, s);
            int amplitude = Amplitude(s.winStrength, s);
            if (!finalLevel || s.finalWinPulses <= 1)
            {
                _device.Pulse(ms, amplitude);
                return true;
            }

            int count = s.finalWinPulses;
            var timings = new long[count * 2];
            var amplitudes = new int[count * 2];
            for (int i = 0; i < count; i++)
            {
                timings[i * 2] = i == 0 ? 0 : s.finalWinGapMilliseconds;
                timings[i * 2 + 1] = ms;
                amplitudes[i * 2] = 0;
                amplitudes[i * 2 + 1] = amplitude;
            }

            _device.Pattern(timings, amplitudes);
            return true;
        }

        /// <summary>Stops a pulse still running, on level start or menu.</summary>
        public void Cancel()
        {
            _device.Cancel();
            _nextGrabTime = float.NegativeInfinity;
        }

        /// <summary>Strength 0..1 and intensity 0..1 to the platform's 1..255 amplitude.</summary>
        public static int Amplitude(float strength, HapticsSettings s)
        {
            return UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(255f * Clamp01(strength) * Clamp01(s.intensity)), 1, 255);
        }

        /// <summary>Duration scaled by intensity, for devices that cannot vary amplitude; never below 5 ms.</summary>
        public static int Milliseconds(int baseMilliseconds, HapticsSettings s)
        {
            return UnityEngine.Mathf.Max(5, UnityEngine.Mathf.RoundToInt(baseMilliseconds * Clamp01(s.intensity)));
        }

        private bool CanPlay(HapticsSettings s)
        {
            return !Suspended && s.enabled && s.intensity > 0f;
        }

        private void QuietGrabs(HapticsSettings s, float now)
        {
            _nextGrabTime = System.Math.Max(_nextGrabTime, now + s.grabQuietAfterHeavySeconds);
        }

        private static float Clamp01(float v)
        {
            return UnityEngine.Mathf.Clamp01(v);
        }
    }
}
