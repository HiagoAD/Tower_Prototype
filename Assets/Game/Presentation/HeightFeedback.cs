using Game.Core;
using UnityEngine;

namespace Game.Presentation
{
    public enum HeightTrend
    {
        Stable,
        Gaining,
        Losing,
    }

    /// <summary>
    /// The altitude readout's tint and scale, derived from the actual height change each frame. A rate
    /// deadband plus a short hold stops flicker; scale and tint ease toward per-trend targets, so nothing accumulates
    /// and Reset returns exactly to baseline. Pure logic (no engine objects) so it is testable.
    /// </summary>
    public sealed class HeightFeedback
    {
        private bool _hasLast;
        private float _lastHeight;
        private float _holdRemaining;

        public HeightTrend Trend { get; private set; }

        /// <summary>Current label scale multiplier, 1 at baseline.</summary>
        public float Scale { get; private set; } = 1f;

        /// <summary>0 = base colour, 1 = the full gain or loss colour (which one follows Trend, or the last one while fading out).</summary>
        public float TintAmount { get; private set; }

        private HeightTrend _tintTrend = HeightTrend.Stable;

        public void Reset()
        {
            _hasLast = false;
            _holdRemaining = 0f;
            Trend = HeightTrend.Stable;
            _tintTrend = HeightTrend.Stable;
            Scale = 1f;
            TintAmount = 0f;
        }

        /// <summary>Feed the current height and the play-time delta (0 while paused: nothing changes).</summary>
        public void Sample(float height, float dt, HeightFeedbackSettings s)
        {
            if (!_hasLast)
            {
                _hasLast = true;
                _lastHeight = height;
                return;
            }

            if (dt <= 0f)
            {
                _lastHeight = height;
                return;
            }

            float rate = (height - _lastHeight) / dt;
            _lastHeight = height;

            HeightTrend observed = rate > s.deadbandUnitsPerSecond ? HeightTrend.Gaining : rate < -s.deadbandUnitsPerSecond ? HeightTrend.Losing : HeightTrend.Stable;
            if (observed != HeightTrend.Stable)
            {
                Trend = observed;
                _holdRemaining = s.holdSeconds;
            }
            else if (_holdRemaining > 0f)
            {
                _holdRemaining -= dt;
            }
            else
            {
                Trend = HeightTrend.Stable;
            }

            float k = 1f - Mathf.Exp(-s.responseRate * dt);
            float targetScale = Trend == HeightTrend.Gaining ? s.gainScale : Trend == HeightTrend.Losing ? s.lossScale : 1f;
            Scale = Mathf.Lerp(Scale, targetScale, k);

            if (Trend != HeightTrend.Stable)
            {
                _tintTrend = Trend; // a gain-to-loss switch changes hue at once and keeps the amount.
                TintAmount = Mathf.Lerp(TintAmount, 1f, k);
            }
            else
            {
                TintAmount = Mathf.Lerp(TintAmount, 0f, k);
                if (TintAmount < 0.001f)
                {
                    TintAmount = 0f;
                }
            }

            if (Trend == HeightTrend.Stable && Mathf.Abs(Scale - 1f) < 0.0005f)
            {
                Scale = 1f;
            }
        }

        public Color Evaluate(Color baseColor, HeightFeedbackSettings s)
        {
            Color target = _tintTrend == HeightTrend.Losing ? s.lossColor : s.gainColor;
            target.a = baseColor.a;
            return Color.Lerp(baseColor, target, TintAmount);
        }
    }
}
