using Game.Core;
using Game.Gameplay;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// Procedurally poses the character's existing rigid limb parts (the Kenney blocky character has no bones --
    /// arm-left/arm-right/leg-left/leg-right/torso/head are separate rigid meshes, each pivoted at
    /// its own joint) from PlayerMotor/GameSession state. Lives on the "Visual" child, well below
    /// the gameplay root -- LateUpdate only ever writes local rotations/positions on limb and body
    /// transforms under itself, never PlayerMotor's own transform.
    ///
    /// Climbing is hand-over-hand, as in the reference video: each hand in turn reaches straight up,
    /// grabs, and pulls down to shoulder height while the other reaches, so one hand is always on the
    /// tower. The body swings and twists toward the pulling hand and the legs hang, swinging behind the
    /// body like a pendulum, with a small knee lift opposite the reaching arm. Released, the climber
    /// holds on with both hands in a wide grip.
    ///
    /// Limbs are aimed rather than rotated by fixed angles: each pose gives every limb a direction in
    /// this transform's space (x = the character's right, y = up, z = into the tower), and the limb
    /// is turned from its measured rest direction to point that way. Arms therefore stay reaching for
    /// the tower while the torso rolls beneath them.
    ///
    /// The cycle advances by ascended distance (deltaHeight / strideLength), not by time, so limbs
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

        [Tooltip("World height climbed per full cycle: one reach and pull with each hand.")]
        [SerializeField] private float strideLength = 1.2f;

        [Tooltip("Share of each hand's cycle spent reaching; for the rest it holds and pulls.")]
        [Range(0.15f, 0.5f)][SerializeField] private float reachFraction = 0.3f;

        [Header("Arm directions (x = outward from that side, y = up, z = into the tower)")]
        [SerializeField] private Vector3 armReachDirection = new Vector3(0.05f, 1f, 0.2f);
        [SerializeField] private Vector3 armPulledDirection = new Vector3(1f, 0.15f, 0.3f);
        [SerializeField] private Vector3 armHoldDirection = new Vector3(0.85f, 0.6f, 0.25f);

        [Header("Leg directions (x = outward from that side)")]
        [SerializeField] private Vector3 legHangDirection = new Vector3(0.1f, -1f, -0.05f);
        [SerializeField] private Vector3 legLiftDirection = new Vector3(0.35f, -0.65f, -0.65f);
        [Tooltip("Sideways swing of both legs behind the body, as a direction offset.")]
        [SerializeField] private float legSwing = 0.3f;

        [Header("Body swing (world units / degrees)")]
        [SerializeField] private float bodySway = 0.035f;
        [SerializeField] private float bodyRollDegrees = 8f;
        [SerializeField] private float bodyTwistDegrees = 12f;
        [SerializeField] private float bodyPullRise = 0.02f;

        [Header("Hit / recovery")]
        [SerializeField] private float hitPushDistance = 0.12f;
        [SerializeField] private float hitTiltDegrees = 14f;
        [SerializeField] private float hitFlailFrequency = 18f;

        [Header("Win / lose")]
        [SerializeField] private Vector3 winArmDirection = new Vector3(0.3f, 1f, 0.1f);
        [SerializeField] private Vector3 loseArmDirection = new Vector3(0.5f, -0.8f, -0.4f);
        [SerializeField] private float loseBodyTiltDegrees = 16f;

        private struct Limb
        {
            public Transform Transform;
            public Quaternion RestLocalRotation;
            public Vector3 RestDirection; // in the parent's space
            public float Side;            // -1 left, +1 right
        }

        private struct Pose
        {
            public Vector3 ArmLeft;
            public Vector3 ArmRight;
            public Vector3 LegLeft;
            public Vector3 LegRight;
            public Vector3 BodyOffset;
            public Vector3 BodyEuler;
        }

        private Limb[] _limbs; // armLeft, armRight, legLeft, legRight
        private Vector3 _bodyRestPosition;    // in this transform's space
        private Quaternion _bodyRestRotation; // relative to this transform
        private bool _restCaptured;

        private Pose _pose;
        private float _phase;
        private float _lastHeight;
        private bool _hasLastHeight;
        private SessionState _sessionState = SessionState.Menu;

        private void Awake()
        {
            CaptureRest();
            // Start already holding on (the character is visible from the menu onwards).
            _pose = HoldPose();
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

        /// <summary>Snaps straight to the climbing pose at a cycle phase (0..1), or to the released hold. For editor previews, which never run LateUpdate.</summary>
        public void PreviewPose(float climbPhase, bool climbing)
        {
            CaptureRest();
            _pose = climbing ? ClimbPose(climbPhase) : HoldPose();
            Apply(_pose);
        }

        private void LateUpdate()
        {
            if (motor == null || motor.Paused)
            {
                return; // paused: hold the frame exactly, flail included.
            }

            float height = motor.Height;
            float deltaHeight = _hasLastHeight ? height - _lastHeight : 0f;
            _lastHeight = height;
            _hasLastHeight = true;

            bool ascendingNow = deltaHeight > 0.0001f && !motor.IsInKnockback;
            if (ascendingNow && strideLength > 0f)
            {
                _phase = Mathf.Repeat(_phase + deltaHeight / strideLength, 1f);
            }

            Pose target;
            float smoothTime;
            if (_sessionState == SessionState.Won)
            {
                target = HoldPose();
                target.ArmLeft = target.ArmRight = winArmDirection;
                smoothTime = 0.2f;
            }
            else if (_sessionState == SessionState.Lost)
            {
                target = HoldPose();
                target.ArmLeft = target.ArmRight = loseArmDirection;
                target.BodyEuler = new Vector3(loseBodyTiltDegrees, 0f, 0f);
                smoothTime = 0.25f;
            }
            else if (motor.IsInKnockback)
            {
                // Knocked off the wall: hands let go and flail, body thrown back. Near-immediate so the
                // impact reads as abrupt.
                float flail = Mathf.Sin(Time.time * hitFlailFrequency);
                target = HoldPose();
                target.ArmLeft = Vector3.Lerp(armHoldDirection, armReachDirection, 0.5f + 0.5f * flail);
                target.ArmRight = Vector3.Lerp(armHoldDirection, armReachDirection, 0.5f - 0.5f * flail);
                target.LegLeft = Vector3.Lerp(legHangDirection, legLiftDirection, 0.5f + 0.5f * flail);
                target.LegRight = Vector3.Lerp(legHangDirection, legLiftDirection, 0.5f - 0.5f * flail);
                target.BodyOffset = new Vector3(0f, 0f, -hitPushDistance);
                target.BodyEuler = new Vector3(-hitTiltDegrees, 0f, flail * bodyRollDegrees);
                smoothTime = 0.03f;
            }
            else if (ascendingNow)
            {
                target = ClimbPose(_phase);
                smoothTime = 0.04f;
            }
            else
            {
                // Released, re-gripping after a hit, or waiting on the menu: both hands on the tower.
                target = HoldPose();
                smoothTime = 0.12f;
            }

            float blend = 1f - Mathf.Exp(-Time.deltaTime / smoothTime);
            _pose.ArmLeft = Vector3.Slerp(_pose.ArmLeft, target.ArmLeft, blend);
            _pose.ArmRight = Vector3.Slerp(_pose.ArmRight, target.ArmRight, blend);
            _pose.LegLeft = Vector3.Slerp(_pose.LegLeft, target.LegLeft, blend);
            _pose.LegRight = Vector3.Slerp(_pose.LegRight, target.LegRight, blend);
            _pose.BodyOffset = Vector3.Lerp(_pose.BodyOffset, target.BodyOffset, blend);
            _pose.BodyEuler = Vector3.Lerp(_pose.BodyEuler, target.BodyEuler, blend);
            Apply(_pose);
        }

        private Pose HoldPose()
        {
            return new Pose
            {
                ArmLeft = armHoldDirection,
                ArmRight = armHoldDirection,
                LegLeft = legHangDirection,
                LegRight = legHangDirection,
            };
        }

        /// <summary>
        /// The left hand reaches over [0, reachFraction) of the cycle and grabs at reachFraction; the
        /// right hand runs half a cycle behind. Swing, twist and leg pendulum all key off the left
        /// hand's grab so they lean toward whichever hand is pulling.
        /// </summary>
        private Pose ClimbPose(float phase)
        {
            float leftU = phase;
            float rightU = Mathf.Repeat(phase + 0.5f, 1f);
            float swing = Mathf.Sin(2f * Mathf.PI * (phase - reachFraction));     // +1 mid-way through the left hand's pull.
            float legLag = Mathf.Sin(2f * Mathf.PI * (phase - reachFraction - 0.1f));
            float pull = Mathf.Sin(Mathf.PI * Mathf.Repeat(2f * (phase - reachFraction), 1f));

            Vector3 legSwingOffset = new Vector3(legSwing * legLag, 0f, 0f); // feet trail the body's sway.
            return new Pose
            {
                ArmLeft = Vector3.Lerp(armPulledDirection, armReachDirection, HandHeight(leftU)),
                ArmRight = Vector3.Lerp(armPulledDirection, armReachDirection, HandHeight(rightU)),
                // The leg opposite the reaching arm draws its knee up.
                LegLeft = Vector3.Lerp(legHangDirection, legLiftDirection, ReachBump(rightU)) + Mirror(legSwingOffset, -1f),
                LegRight = Vector3.Lerp(legHangDirection, legLiftDirection, ReachBump(leftU)) + legSwingOffset,
                BodyOffset = new Vector3(-bodySway * swing, bodyPullRise * pull, 0f),
                BodyEuler = new Vector3(0f, bodyTwistDegrees * swing, bodyRollDegrees * swing),
            };
        }

        /// <summary>0 = pulled down to shoulder height, 1 = at full reach. Quick eased reach, then a steady pull.</summary>
        private float HandHeight(float u)
        {
            if (u < reachFraction)
            {
                float t = u / reachFraction;
                return 1f - (1f - t) * (1f - t) * (1f - t);
            }

            return 1f - (u - reachFraction) / (1f - reachFraction);
        }

        private float ReachBump(float u)
        {
            return u < reachFraction ? Mathf.Sin(Mathf.PI * u / reachFraction) : 0f;
        }

        /// <summary>Limb directions are authored per side with x pointing outward; this flips x to the left side's frame (or back).</summary>
        private static Vector3 Mirror(Vector3 direction, float side)
        {
            return new Vector3(direction.x * side, direction.y, direction.z);
        }

        private void Apply(Pose pose)
        {
            if (_limbs == null)
            {
                return;
            }

            if (bodyRoot != null)
            {
                // In this transform's unscaled space, not the scaled/axis-converted FBX root's.
                bodyRoot.position = transform.TransformPoint(_bodyRestPosition + pose.BodyOffset);
                bodyRoot.rotation = transform.rotation * Quaternion.Euler(pose.BodyEuler) * _bodyRestRotation;
            }

            Aim(_limbs[0], pose.ArmLeft);
            Aim(_limbs[1], pose.ArmRight);
            Aim(_limbs[2], pose.LegLeft);
            Aim(_limbs[3], pose.LegRight);
        }

        private void Aim(Limb limb, Vector3 outwardDirection)
        {
            if (limb.Transform == null)
            {
                return;
            }

            Vector3 world = transform.TransformDirection(Mirror(outwardDirection, limb.Side).normalized);
            Vector3 parentSpace = limb.Transform.parent.InverseTransformDirection(world);
            limb.Transform.localRotation = Quaternion.FromToRotation(limb.RestDirection, parentSpace) * limb.RestLocalRotation;
        }

        private void CaptureRest()
        {
            if (_restCaptured)
            {
                return;
            }

            if (bodyRoot != null)
            {
                _bodyRestPosition = transform.InverseTransformPoint(bodyRoot.position);
                _bodyRestRotation = Quaternion.Inverse(transform.rotation) * bodyRoot.rotation;
            }

            Vector3 down = transform.TransformDirection(Vector3.down);
            _limbs = new[]
            {
                MeasureLimb(armLeft, -1f, down),
                MeasureLimb(armRight, 1f, down),
                MeasureLimb(legLeft, -1f, down),
                MeasureLimb(legRight, 1f, down),
            };
            _restCaptured = true;
        }

        /// <summary>
        /// The blocky character's limbs hang straight down from their joints at rest, so "down" (in
        /// the parent's space) is each limb's rest direction. Measuring the mesh centre instead picks
        /// up the blocks' sideways offset from their pivots and skews every aimed angle.
        /// </summary>
        private static Limb MeasureLimb(Transform limb, float side, Vector3 worldDown)
        {
            var result = new Limb { Transform = limb, Side = side };
            if (limb == null)
            {
                return result;
            }

            result.RestLocalRotation = limb.localRotation;
            result.RestDirection = limb.parent.InverseTransformDirection(worldDown).normalized;
            return result;
        }
    }
}
