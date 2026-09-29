using System.Collections.Generic;
using Game.Core;
using Game.Webhook;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// The one owner of every camera and screen effect: bump impact (zoom pulse, shake, aberration and
    /// bloom), the small hazard-hit shake, and the win camera move (push-in, small arc, pulse). Nothing
    /// else writes the camera's field of view, the offset transform or the post-processing weights, so
    /// effects cannot fight or accumulate: each effect is one continuous channel where a newer request
    /// blends from the current value, shake takes the strongest active channel (CameraShake), and every
    /// exit path (menu, level start, win, loss) puts the camera back on its follow position.
    ///
    /// The bump effects start when the gloves land (GameSession.BumpImpactDelaySeconds after the request
    /// is accepted), the same moment the climber's move starts and the impact sound plays. The clock
    /// stops with the pause and pending impacts are dropped on every reset.
    /// </summary>
    public sealed class CameraEffects : MonoBehaviour
    {
        private const float MinFov = 10f;
        private const float MaxFov = 100f;

        [SerializeField] private GameSession session;
        [SerializeField] private Camera worldCamera;
        [Tooltip("Parent of the camera under the shake offset; carries the win move's drop and arc.")]
        [SerializeField] private Transform cameraOffset;
        [Tooltip("The climber the win arc swings around.")]
        [SerializeField] private Transform focusTarget;
        [SerializeField] private CameraShake shake;
        [Tooltip("Applies the aberration and bloom weights to the URP volumes.")]
        [SerializeField] private ScreenFxDriver screenFx;
        [Tooltip("Tuning asset; the cameraFx section sets every effect. Without it the built-in defaults apply.")]
        [SerializeField] private GameSettings settings;

        private struct PendingImpact
        {
            public float Remaining;
            public BumpPolarity Polarity;
        }

        private readonly List<PendingImpact> _pending = new List<PendingImpact>();
        private readonly EffectPulse _fov = new EffectPulse();
        private readonly EffectPulse _aberration = new EffectPulse();
        private readonly EffectPulse _bloom = new EffectPulse();

        private float _baseFov = 45f;
        private bool _hasBaseFov;
        private bool _victoryActive;
        private bool _victoryPulsed;
        private float _victoryElapsed;
        private float _victoryPivotDistance;
        private bool _applied;

        private CameraFeedbackSettings Fx => GameSettings.OrDefaults(settings).cameraFx;

        /// <summary>Impacts still waiting for their glove to land.</summary>
        public int PendingImpactCount => _pending.Count;

        /// <summary>True while the win camera move is running or holding.</summary>
        public bool VictoryActive => _victoryActive;

        /// <summary>Current signed FOV fraction of the bump pulse (positive = wider).</summary>
        public float PulseFovFraction => _fov.Value;

        /// <summary>Current aberration / bloom pulse weights, 0..1.</summary>
        public float AberrationWeight => _aberration.Value;
        public float BloomWeight => _bloom.Value;

        private void Awake()
        {
            CaptureBaseFov();
        }

        private void OnEnable()
        {
            CaptureBaseFov();
            session.BumpAccepted += OnBumpAccepted;
            session.HazardHit += OnHazardHit;
            session.StateChanged += OnStateChanged;
            session.LevelStarted += ResetAll;
            SetShakePaused(session.State == SessionState.Paused);
        }

        private void OnDisable()
        {
            session.BumpAccepted -= OnBumpAccepted;
            session.HazardHit -= OnHazardHit;
            session.StateChanged -= OnStateChanged;
            session.LevelStarted -= ResetAll;
            ResetAll();
        }

        private void CaptureBaseFov()
        {
            if (!_hasBaseFov && worldCamera != null)
            {
                _baseFov = worldCamera.fieldOfView;
                _hasBaseFov = true;
            }
        }

        private void OnStateChanged(SessionState state)
        {
            switch (state)
            {
                case SessionState.Menu:
                    ResetAll();
                    break;
                case SessionState.Won:
                    ResetAll();
                    BeginVictory();
                    break;
                case SessionState.Lost:
                    // Bump effects end with the run; a hazard-hit shake that caused the loss may finish (it fades in a fraction of a second).
                    _pending.Clear();
                    _fov.Reset();
                    _aberration.Reset();
                    _bloom.Reset();
                    if (shake != null)
                    {
                        shake.Cancel(ShakeChannel.Bump);
                    }

                    SetShakePaused(false);
                    Apply();
                    break;
                case SessionState.Paused:
                    SetShakePaused(true);
                    break;
                default:
                    SetShakePaused(false);
                    break;
            }
        }

        private void SetShakePaused(bool paused)
        {
            if (shake != null)
            {
                shake.Paused = paused;
            }
        }

        /// <summary>Drops every pending impact and running effect and puts the camera back on its follow position, base FOV and no post-processing.</summary>
        public void ResetAll()
        {
            _pending.Clear();
            _fov.Reset();
            _aberration.Reset();
            _bloom.Reset();
            _victoryActive = false;
            _victoryPulsed = false;
            _victoryElapsed = 0f;
            if (shake != null)
            {
                shake.Cancel();
            }

            Apply();
        }

        private void OnBumpAccepted(BumpEvent bump)
        {
            float delay = session.BumpImpactDelaySeconds;
            if (delay <= 0f)
            {
                Impact(bump.Polarity);
                return;
            }

            _pending.Add(new PendingImpact { Remaining = delay, Polarity = bump.Polarity });
        }

        private void OnHazardHit()
        {
            CameraFeedbackSettings fx = Fx;
            if (shake != null)
            {
                shake.Shake(ShakeChannel.Hit, fx.hitShakeSeconds, fx.hitShakeMagnitude);
            }
        }

        /// <summary>The glove lands: zoom pulse, shake and post-processing pulses, all tinted by polarity.</summary>
        private void Impact(BumpPolarity polarity)
        {
            CameraFeedbackSettings fx = Fx;
            bool positive = polarity == BumpPolarity.Positive;

            float fovPercent = positive ? fx.positiveFovPercent : fx.negativeFovPercent;
            if (!Mathf.Approximately(fovPercent, 0f))
            {
                _fov.Trigger(fovPercent * 0.01f, fx.pulseAttackSeconds, fx.pulseReleaseSeconds);
            }

            float shakeScale = positive ? fx.positiveShakeScale : fx.negativeShakeScale;
            if (shake != null)
            {
                shake.Shake(ShakeChannel.Bump, fx.bumpShakeSeconds, GameSettings.OrDefaults(settings).camera.shakeMagnitude * shakeScale);
            }

            if (fx.postProcessingEnabled)
            {
                float aberration = fx.aberrationIntensity > 0f ? (positive ? fx.positiveAberrationWeight : fx.negativeAberrationWeight) : 0f;
                float bloom = fx.bloomIntensity > 0f ? (positive ? fx.positiveBloomWeight : fx.negativeBloomWeight) : 0f;
                TriggerPost(aberration, bloom, fx.pulseAttackSeconds, fx.pulseReleaseSeconds);
            }
        }

        private void TriggerPost(float aberration, float bloom, float attack, float release)
        {
            if (aberration > 0f)
            {
                _aberration.Trigger(aberration, attack, release);
            }

            if (bloom > 0f)
            {
                _bloom.Trigger(bloom, attack, release);
            }
        }

        private void BeginVictory()
        {
            if (!Fx.victoryCameraEnabled)
            {
                return;
            }

            _victoryActive = true;
            _victoryElapsed = 0f;
            _victoryPulsed = false;
            float pivotZ = focusTarget != null ? focusTarget.position.z : 0f;
            float cameraZ = worldCamera != null ? worldCamera.transform.position.z : pivotZ - 1f;
            _victoryPivotDistance = Mathf.Max(1f, Mathf.Abs(cameraZ - pivotZ));
        }

        private void Update()
        {
            if (session.State == SessionState.Paused)
            {
                return; // every effect holds; the shake holds itself via its Paused flag.
            }

            Step(Time.deltaTime);
        }

        /// <summary>The per-frame update, split out so EditMode tests can drive it with an explicit dt.</summary>
        public void Step(float dt)
        {
            if (session.State == SessionState.Playing)
            {
                AdvanceImpacts(dt);
            }

            _fov.Step(dt);
            _aberration.Step(dt);
            _bloom.Step(dt);
            if (_victoryActive)
            {
                AdvanceVictory(dt);
            }

            Apply();
        }

        private void AdvanceImpacts(float dt)
        {
            for (int i = 0; i < _pending.Count;)
            {
                PendingImpact pending = _pending[i];
                pending.Remaining -= dt;
                if (pending.Remaining > 0f)
                {
                    _pending[i] = pending;
                    i++;
                    continue;
                }

                _pending.RemoveAt(i);
                Impact(pending.Polarity);
            }
        }

        private void AdvanceVictory(float dt)
        {
            CameraFeedbackSettings fx = Fx;
            _victoryElapsed += dt;
            if (!_victoryPulsed && _victoryElapsed >= fx.victoryStartDelaySeconds)
            {
                _victoryPulsed = true;
                if (fx.postProcessingEnabled)
                {
                    float aberration = fx.aberrationIntensity > 0f ? fx.victoryAberrationWeight : 0f;
                    float bloom = fx.bloomIntensity > 0f ? fx.victoryBloomWeight : 0f;
                    TriggerPost(aberration, bloom, fx.pulseAttackSeconds, fx.victoryPulseReleaseSeconds);
                }
            }
        }

        /// <summary>Eased 0..1 progress of the win move.</summary>
        private float VictoryProgress()
        {
            CameraFeedbackSettings fx = Fx;
            float t = Mathf.Clamp01((_victoryElapsed - fx.victoryStartDelaySeconds) / Mathf.Max(fx.victoryMoveSeconds, 0.0001f));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Writes FOV, the offset transform and the post weights from the channels' current values (neutral when nothing is active).</summary>
        private void Apply()
        {
            CameraFeedbackSettings fx = Fx;
            float victoryFov = 0f;
            Vector3 offset = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            if (_victoryActive)
            {
                float p = VictoryProgress();
                victoryFov = fx.victoryPushInFovPercent * 0.01f * p;
                offset.y = -fx.victoryCameraDropWorldUnits * p;
                if (fx.victoryArcEnabled && fx.victoryArcDegrees > 0f)
                {
                    float theta = fx.victoryArcDegrees * p * Mathf.Deg2Rad;
                    offset.x += _victoryPivotDistance * Mathf.Sin(theta);
                    offset.z += _victoryPivotDistance * (1f - Mathf.Cos(theta));
                    rotation = Quaternion.Euler(0f, -theta * Mathf.Rad2Deg, 0f);
                }
            }

            bool active = _victoryActive || _fov.IsActive || _aberration.IsActive || _bloom.IsActive;
            if (!active && !_applied)
            {
                return;
            }

            if (worldCamera != null)
            {
                worldCamera.fieldOfView = active ? Mathf.Clamp(_baseFov * (1f + _fov.Value + victoryFov), MinFov, MaxFov) : _baseFov;
            }

            if (cameraOffset != null)
            {
                cameraOffset.localPosition = offset;
                cameraOffset.localRotation = rotation;
            }

            if (screenFx != null)
            {
                screenFx.Apply(_aberration.Value, _bloom.Value);
            }

            _applied = active;
        }
    }
}
