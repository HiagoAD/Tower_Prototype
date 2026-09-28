using Game.Core;
using Game.Gameplay;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// Procedurally poses the character's existing rigid limb parts (character-c.fbx has no bones --
    /// arm-left/arm-right/leg-left/leg-right/torso/head are separate rigid meshes, each pivoted at
    /// its own joint) from PlayerMotor/GameSession state. Lives on the "Visual" child, well below
    /// the gameplay root -- LateUpdate only ever writes local rotations/positions on limb and body
    /// transforms under itself, never PlayerMotor's own transform.
    ///
    /// Climb phase advances by ascended distance (deltaHeight / strideLength), not by time, so limbs
    /// only move while the character is actually rising and freeze the instant it stops.
    /// </summary>
    public sealed class ClimberPoseDriver : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private GameSession session;
        [SerializeField] private Transform armLeft;
        [SerializeField] private Transform armRight;
        [SerializeField] private Transform legLeft;
        [SerializeField] private Transform legRight;
        [SerializeField] private Transform bodyRoot;

        [SerializeField] private float strideLength = 1.1f;

        [Header("Grip / climb angles (degrees, local X)")]
        [SerializeField] private float armGripAngle = 150f;
        [SerializeField] private float armSwingAmplitude = 35f;
        [SerializeField] private float legGripAngle = 35f;
        [SerializeField] private float legSwingAmplitude = 25f;
        [SerializeField] private float bodyRiseAmplitude = 0.05f;

        [Header("Hit / recovery")]
        [SerializeField] private float hitPushDistance = 0.12f;
        [SerializeField] private float hitFlailAmplitude = 55f;
        [SerializeField] private float hitFlailFrequency = 18f;

        [Header("Win / lose")]
        [SerializeField] private float winArmAngle = 175f;
        [SerializeField] private float loseArmAngle = -35f;
        [SerializeField] private float loseBodyTiltDegrees = 16f;

        private float _phase;
        private float _lastHeight;
        private bool _hasLastHeight;

        private Vector3 _bodyRestLocalPosition;
        private Quaternion _bodyRestLocalRotation;

        private float _armLeftAngle;
        private float _armRightAngle;
        private float _legLeftAngle;
        private float _legRightAngle;
        private float _bodyRiseOffset;
        private float _bodyPushOffset;
        private float _bodyTiltOffset;

        private SessionState _sessionState = SessionState.Menu;

        private void Awake()
        {
            if (bodyRoot != null)
            {
                _bodyRestLocalPosition = bodyRoot.localPosition;
                _bodyRestLocalRotation = bodyRoot.localRotation;
            }

            // Start already in the held-grip pose (both hands up) rather than snapping to it, since
            // the character is visible on the tower from the very first frame (menu included).
            _armLeftAngle = _armRightAngle = armGripAngle;
            _legLeftAngle = _legRightAngle = legGripAngle;
        }

        private void OnEnable()
        {
            if (session != null)
            {
                session.StateChanged += OnStateChanged;
                _sessionState = session.State;
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(SessionState state)
        {
            _sessionState = state;
        }

        private void LateUpdate()
        {
            if (motor == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            float height = motor.Height;
            float deltaHeight = _hasLastHeight ? height - _lastHeight : 0f;
            _lastHeight = height;
            _hasLastHeight = true;

            bool ascendingNow = deltaHeight > 0.0001f && !motor.IsInKnockback;
            if (ascendingNow && strideLength > 0f)
            {
                // strideLength is the world-space distance of one complete hand-over-hand cycle.
                _phase += (deltaHeight / strideLength) * Mathf.PI * 2f;
            }

            float targetArmL;
            float targetArmR;
            float targetLegL;
            float targetLegR;
            float targetRise = 0f;
            float targetPush = 0f;
            float targetTilt = 0f;
            float smoothTime;

            if (_sessionState == SessionState.Won)
            {
                targetArmL = targetArmR = winArmAngle;
                targetLegL = targetLegR = legGripAngle * 0.3f;
                smoothTime = 0.2f;
            }
            else if (_sessionState == SessionState.Lost)
            {
                targetArmL = targetArmR = loseArmAngle;
                targetLegL = targetLegR = loseArmAngle * 0.5f;
                targetTilt = loseBodyTiltDegrees;
                smoothTime = 0.25f;
            }
            else if (motor.IsInKnockback)
            {
                // Body pushed back and arms flail during the knockback itself -- near-immediate
                // blend so the impact reads as abrupt, not eased into.
                float flail = Mathf.Sin(Time.time * hitFlailFrequency) * hitFlailAmplitude;
                targetArmL = armGripAngle + flail;
                targetArmR = armGripAngle - flail;
                targetLegL = legGripAngle - flail * 0.4f;
                targetLegR = legGripAngle + flail * 0.4f;
                targetPush = hitPushDistance;
                smoothTime = 0.03f;
            }
            else if (motor.IsLockedOut)
            {
                // Knockback displacement finished but climb input is still locked (re-grip): ease
                // back into the held-grip pose.
                targetArmL = targetArmR = armGripAngle;
                targetLegL = targetLegR = legGripAngle;
                smoothTime = 0.15f;
            }
            else if (ascendingNow)
            {
                targetArmL = armGripAngle + armSwingAmplitude * Mathf.Sin(_phase);
                targetArmR = armGripAngle + armSwingAmplitude * Mathf.Sin(_phase + Mathf.PI);
                // Legs bend/push opposite the arms: leg-left tracks arm-right's phase and vice versa.
                targetLegL = legGripAngle + legSwingAmplitude * Mathf.Sin(_phase + Mathf.PI);
                targetLegR = legGripAngle + legSwingAmplitude * Mathf.Sin(_phase);
                targetRise = bodyRiseAmplitude * Mathf.Max(0f, Mathf.Sin(_phase));
                smoothTime = 0.05f;
            }
            else
            {
                // Idle/grip: both hands up on the surface, held. Blends in over ~0.15s per spec.
                targetArmL = targetArmR = armGripAngle;
                targetLegL = targetLegR = legGripAngle;
                smoothTime = 0.15f;
            }

            float blend = smoothTime > 0f ? 1f - Mathf.Exp(-dt / smoothTime) : 1f;
            _armLeftAngle = Mathf.Lerp(_armLeftAngle, targetArmL, blend);
            _armRightAngle = Mathf.Lerp(_armRightAngle, targetArmR, blend);
            _legLeftAngle = Mathf.Lerp(_legLeftAngle, targetLegL, blend);
            _legRightAngle = Mathf.Lerp(_legRightAngle, targetLegR, blend);
            _bodyRiseOffset = Mathf.Lerp(_bodyRiseOffset, targetRise, blend);
            _bodyPushOffset = Mathf.Lerp(_bodyPushOffset, targetPush, blend);
            _bodyTiltOffset = Mathf.Lerp(_bodyTiltOffset, targetTilt, blend);

            ApplyLimb(armLeft, _armLeftAngle);
            ApplyLimb(armRight, _armRightAngle);
            ApplyLimb(legLeft, _legLeftAngle);
            ApplyLimb(legRight, _legRightAngle);

            if (bodyRoot != null)
            {
                bodyRoot.localPosition = _bodyRestLocalPosition + new Vector3(0f, _bodyRiseOffset, -_bodyPushOffset);
                bodyRoot.localRotation = _bodyRestLocalRotation * Quaternion.Euler(_bodyTiltOffset, 0f, 0f);
            }
        }

        private static void ApplyLimb(Transform limb, float angleDegrees)
        {
            if (limb != null)
            {
                limb.localRotation = Quaternion.Euler(angleDegrees, 0f, 0f);
            }
        }
    }
}
