namespace Game.Presentation
{
    /// <summary>
    /// Paces a bump's punch volley: the main impact punch, then one punch per glove passing the climber,
    /// no closer than a minimum interval and no more than a maximum per bump.
    /// </summary>
    public sealed class PunchVolley
    {
        private float _lastTime = float.NegativeInfinity;

        /// <summary>Punches played this bump, including the main impact.</summary>
        public int Count { get; private set; }

        public void Reset()
        {
            _lastTime = float.NegativeInfinity;
            Count = 0;
        }

        /// <summary>Records a punch that plays regardless of pacing (the main impact).</summary>
        public void Register(float time)
        {
            _lastTime = time;
            Count++;
        }

        /// <summary>True, and records the punch, when one more may play at time.</summary>
        public bool TryPunch(float time, float minInterval, int maxPunches)
        {
            if (Count >= maxPunches || time - _lastTime < minInterval)
            {
                return false;
            }

            Register(time);
            return true;
        }
    }
}
