using Game.Core;
using Game.Gameplay;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// Procedurally poses the character's existing rigid limb parts (the Kenney blocky character has no bones --
    /// arm-left/arm-right/leg-left/leg-right/torso/head are separate rigid meshes, each pivoted at
    /// its own joint) from PlayerMotor/GameSession state. Lives on the "Visual" child, well below
    /// the gameplay root -- it only ever writes rotations/positions on limb and body transforms
    /// under itself, never PlayerMotor's own transform.
    ///
    /// Hand-over-hand climbing with planted grips. Each hand holds a fixed point on the tower: as
    /// PlayerMotor raises the character, the planted hand stays where it grabbed and its arm swings
    /// down past the shoulder. Once it is low and the other hand is holding, it lets go, arcs away
    /// from the wall and grabs a new point overhead. The body hangs from the hands on springs: it
    /// swings toward whichever hand is holding alone, is yanked upward on every grab, and the legs
    /// trail it like a pendulum. The climb's pace stays PlayerMotor's; this only decides where the
    /// hands and body are.
    ///
    /// The arms are rigid, so the distance from shoulder to grip cannot change as the shoulder moves
    /// past it. That slack is taken up along the view axis (into the tower, away from the camera):
    /// the hand stays exactly on its grip on screen while only its depth varies. At the grip's
    /// sideways offset the tower's curved flank is far enough back that the hand stays in front of it.
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
        [Tooltip("Tuning asset. The pose values are its pose section; swing, yank and reach timings scale with its pace section (ClimbPace.TunedBodyHeightsPerSecond).")]
        [SerializeField] private GameSettings settings;

        private GameSettings Settings => GameSettings.OrDefaults(settings);
        private ClimberPoseSettings Pose => Settings.pose;

        /// <summary>
        /// Raised when a reaching hand closes on its new grip (the grab), with the hand's world position on the tower.
        /// Grab feedback (particles, audio, haptics) keys off this. Only real grabs raise it: a reach that lands while
        /// the climber is moving up, including a post-hit regrip if the player is already climbing again. A reach that
        /// finishes by time while the climber has stopped, or during a pause, win, loss or menu, is animation only.
        /// </summary>
        public event System.Action<Vector3> HandPlanted;

        private struct Limb
        {
            public Transform Transform;
            public Quaternion RestLocalRotation;
            public Vector3 RestDirection;      // parent space
            public Vector3 RestDirectionLocal; // limb's own space
            public Vector3 RestPivot;          // this transform's space
            public float Side;                 // -1 left, +1 right
        }

        private struct Hand
        {
            public bool Reaching;
            public float GripY;       // world height of the held (or targeted) grip
            public float FromY;       // world height the reach started from
            public float Progress;    // 0..1 through the reach
            public float TimedReach;  // > 0: finishing over this many seconds rather than by climbed distance
        }

        private Limb _armL, _armR, _legL, _legR;
        private Hand _handL, _handR;
        private float _armLength;
        private float _legLength;
        private Vector3 _bodyRestPosition;
        private Quaternion _bodyRestRotation;
        private bool _restCaptured;
        private bool _seeded;

        private float _lastHeight;
        private bool _hasLastHeight;
        private SessionState _sessionState = SessionState.Menu;

        private float _swayX, _swayVelocity;
        private float _yankY, _yankVelocity;
        private float _legX, _legVelocity;
        private float _twist;
        private float _overrideWeight; // 0 = climbing grips, 1 = flail/win/lose directions
        private bool _wasKnockedBack;
        private float _rate = 1f; // ClimbPace.PresentationRate: grabs come faster at a quicker pace, so the body responds faster.

        private void Awake()
        {
            CaptureRest();
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
            // Frozen only by the session's pause. motor.Paused is also set on Won, Lost and Menu, where the
            // win/lose pose must still play out and the pose must settle back to climbing.
            bool paused = session != null ? session.State == SessionState.Paused : motor != null && motor.Paused;
            if (motor == null || paused)
            {
                return; // paused: hold the frame exactly, flail included.
            }

            Advance(motor.Height, Time.deltaTime);
        }

        /// <summary>
        /// One pose step at the given gameplay height. LateUpdate drives it from PlayerMotor; editor
        /// previews call it directly after moving the player, since they never run LateUpdate.
        /// </summary>
        public void Advance(float height, float deltaTime)
        {
            CaptureRest();
            if (_armL.Transform == null || _armR.Transform == null)
            {
                return;
            }

            float dt = Mathf.Min(deltaTime, 0.05f);
            _rate = Settings.pace.PresentationRate;
            float deltaHeight = _hasLastHeight ? height - _lastHeight : 0f;
            _lastHeight = height;
            _hasLastHeight = true;

            bool knockedBack = motor != null && motor.IsInKnockback;
            // Outside a knockback the motor only ever moves down when a level (re)starts.
            if (!_seeded || (!knockedBack && deltaHeight < -0.0001f))
            {
                SeedGrips();
            }

            if (_wasKnockedBack && !knockedBack)
            {
                Regrip();
            }

            _wasKnockedBack = knockedBack;

            // An upward bump is help, not a hit: it gets the boosted pose instead of the flail.
            bool boosted = knockedBack && motor.IsBumpMove && motor.BumpMoveDirection > 0;
            bool flailing = knockedBack && !boosted;

            bool overridePose = knockedBack || _sessionState == SessionState.Won || _sessionState == SessionState.Lost;
            _overrideWeight = Mathf.MoveTowards(_overrideWeight, overridePose ? 1f : 0f, dt / (knockedBack ? 0.05f : 0.2f));
            if (!overridePose)
            {
                StepHands(Mathf.Max(deltaHeight, 0f), dt);
            }

            StepBody(dt, knockedBack);
            ApplyBody(flailing, boosted);
            ApplyArms(flailing, boosted);
            ApplyLegs(flailing, boosted);
        }

        // ---- grips ---------------------------------------------------------------------------

        private float ShoulderRestWorldY(Limb arm)
        {
            return transform.TransformPoint(arm.RestPivot).y;
        }

        private float CycleLength => (Pose.gripHigh - Pose.gripLow) + Pose.reachDistance;

        /// <summary>Both hands holding, one high and the other half a cycle lower.</summary>
        private void SeedGrips()
        {
            _handL = new Hand { GripY = ShoulderRestWorldY(_armL) + Pose.gripHigh * _armLength };
            _handR = new Hand { GripY = ShoulderRestWorldY(_armR) + (Pose.gripHigh - CycleLength * 0.5f) * _armLength };
            _swayX = _swayVelocity = _yankY = _yankVelocity = _legX = _legVelocity = _twist = 0f;
            // A restart leaves nothing of the previous run: no win/flail blend, no stale regrip.
            _overrideWeight = 0f;
            _wasKnockedBack = false;
            _seeded = true;
        }

        /// <summary>After a knockback, both hands grab fresh holds from wherever they flailed to.</summary>
        private void Regrip()
        {
            StartTimedReach(ref _handL, _armL, Pose.gripHigh);
            StartTimedReach(ref _handR, _armR, Pose.gripHigh - CycleLength * 0.5f);
        }

        private void StartTimedReach(ref Hand hand, Limb arm, float targetAboveShoulder)
        {
            Vector3 pointing = arm.Transform.rotation * arm.RestDirectionLocal;
            hand.FromY = arm.Transform.position.y + pointing.y * _armLength;
            hand.GripY = ShoulderRestWorldY(arm) + targetAboveShoulder * _armLength;
            hand.Progress = 0f;
            hand.Reaching = true;
            hand.TimedReach = Pose.regripSeconds / _rate;
        }

        private void StepHands(float climbed, float dt)
        {
            // The lower hand moves first, so it is the one that lets go when both qualify.
            if (_handL.GripY <= _handR.GripY)
            {
                StepHand(ref _handL, _armL, _handR, climbed, dt);
                StepHand(ref _handR, _armR, _handL, climbed, dt);
            }
            else
            {
                StepHand(ref _handR, _armR, _handL, climbed, dt);
                StepHand(ref _handL, _armL, _handR, climbed, dt);
            }
        }

        private void StepHand(ref Hand hand, Limb arm, Hand other, float climbed, float dt)
        {
            float shoulderY = ShoulderRestWorldY(arm);
            if (hand.Reaching)
            {
                if (climbed <= 0f && hand.TimedReach <= 0f)
                {
                    // Stopped mid-reach: finish by time, onto a hold level with where the shoulder is now.
                    hand.TimedReach = Pose.idleReachSeconds / _rate;
                    hand.GripY = Mathf.Min(hand.GripY, shoulderY + Pose.gripHigh * _armLength);
                }

                hand.Progress += hand.TimedReach > 0f
                    ? dt / hand.TimedReach
                    : climbed / (Pose.reachDistance * _armLength);

                if (hand.Progress >= 1f)
                {
                    hand.Reaching = false;
                    hand.TimedReach = 0f;
                    _yankVelocity += Pose.grabYank * _rate * _armLength;
                    if (climbed > 0f)
                    {
                        HandPlanted?.Invoke(GripWorldPoint(arm, hand));
                    }
                }

                return;
            }

            bool low = hand.GripY - shoulderY <= Pose.gripLow * _armLength;
            if (climbed > 0f && low && !other.Reaching)
            {
                hand.Reaching = true;
                hand.TimedReach = 0f;
                hand.Progress = 0f;
                hand.FromY = hand.GripY;
                // Aim where the shoulder will be once the reach completes, so the new hold lands gripHigh above it.
                hand.GripY = shoulderY + (Pose.reachDistance + Pose.gripHigh) * _armLength;
            }
        }

        /// <summary>
        /// Where the hand lands, in world space: the same point AimArm reaches for (grip x and y on the tower face,
        /// and the depth the rigid arm needs from the shoulder's current position), not the shoulder's own plane.
        /// </summary>
        private Vector3 GripWorldPoint(Limb arm, Hand hand)
        {
            Vector3 shoulder = transform.InverseTransformPoint(arm.Transform.position);
            float gripX = arm.RestPivot.x + arm.Side * Pose.gripOutward * _armLength;
            float gripY = HandWorldY(hand) - transform.position.y;
            float dz = ArmDepth(gripX - shoulder.x, gripY - shoulder.y);
            return transform.TransformPoint(new Vector3(gripX, gripY, shoulder.z + dz));
        }

        /// <summary>Depth along the view axis that a rigid arm needs to reach a grip dx sideways and dy up from the shoulder.</summary>
        private float ArmDepth(float dx, float dy)
        {
            float minDepth = 0.2f * _armLength;
            return Mathf.Sqrt(Mathf.Max(_armLength * _armLength - dx * dx - dy * dy, minDepth * minDepth));
        }

        private static float HandWorldY(Hand hand)
        {
            if (!hand.Reaching)
            {
                return hand.GripY;
            }

            float t = Mathf.Clamp01(hand.Progress);
            return Mathf.Lerp(hand.FromY, hand.GripY, t * t * (3f - 2f * t));
        }

        // ---- body ----------------------------------------------------------------------------

        private void StepBody(float dt, bool knockedBack)
        {
            // Hang beneath whichever hand holds alone; centred when both hold.
            float hangSide = (_handL.Reaching ? 1f : 0f) - (_handR.Reaching ? 1f : 0f); // left reaching -> hang right (+x)
            float swayTarget = knockedBack ? 0f : hangSide * Pose.swayDistance * _armLength;
            Spring(ref _swayX, ref _swayVelocity, swayTarget, Pose.swayFrequency * _rate, Pose.swayDamping, dt);
            Spring(ref _yankY, ref _yankVelocity, 0f, Pose.yankFrequency * _rate, Pose.yankDamping, dt);
            Spring(ref _legX, ref _legVelocity, _swayX, Pose.legPendulumFrequency * _rate, Pose.legPendulumDamping, dt);
            _twist = Mathf.Lerp(_twist, -hangSide * Pose.twistDegrees, 1f - Mathf.Exp(-dt * _rate / 0.08f));
        }

        /// <summary>Damped spring, semi-implicit Euler in fixed substeps so it stays stable at fast paces and low frame rates.</summary>
        private static void Spring(ref float x, ref float v, float target, float frequency, float damping, float dt)
        {
            const float maxStep = 1f / 120f;
            float omega = 2f * Mathf.PI * frequency;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / maxStep));
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                v += (-omega * omega * (x - target) - 2f * damping * omega * v) * h;
                x += v * h;
            }
        }

        private void ApplyBody(bool flailing, bool boosted)
        {
            if (bodyRoot == null)
            {
                return;
            }

            float roll = -Pose.rollDegreesPerSway * _swayX / Mathf.Max(Pose.swayDistance * _armLength, 0.0001f);
            Vector3 offset = new Vector3(_swayX, _yankY, 0f);
            Vector3 euler = new Vector3(0f, _twist, roll);

            if (boosted)
            {
                // Upright and squared to the camera; only the springs' yank remains.
                offset = Vector3.Lerp(offset, new Vector3(0f, _yankY, 0f), _overrideWeight);
                euler = Vector3.Lerp(euler, Vector3.zero, _overrideWeight);
            }
            else if (flailing)
            {
                float flail = Mathf.Sin(Time.time * Pose.hitFlailFrequency);
                offset = Vector3.Lerp(offset, new Vector3(0f, 0f, -Pose.hitPushDistance * _armLength), _overrideWeight);
                euler = Vector3.Lerp(euler, new Vector3(-Pose.hitTiltDegrees, 0f, flail * Pose.rollDegreesPerSway), _overrideWeight);
            }
            else if (_sessionState == SessionState.Lost)
            {
                euler = Vector3.Lerp(euler, new Vector3(Pose.loseBodyTiltDegrees, 0f, 0f), _overrideWeight);
            }

            // In this transform's unscaled space, not the scaled/axis-converted FBX root's.
            bodyRoot.position = transform.TransformPoint(_bodyRestPosition + offset);
            bodyRoot.rotation = transform.rotation * Quaternion.Euler(euler) * _bodyRestRotation;
        }

        // ---- limbs ---------------------------------------------------------------------------

        private void ApplyArms(bool flailing, bool boosted)
        {
            Vector3 overrideDirection = boosted ? Pose.boostArmDirection
                : flailing ? Pose.flailArmDirection
                : _sessionState == SessionState.Won ? Pose.winArmDirection
                : Pose.loseArmDirection;
            float flail = flailing ? Mathf.Sin(Time.time * Pose.hitFlailFrequency) * 0.5f : 0f;
            AimArm(_armL, _handL, overrideDirection + new Vector3(0f, flail, 0f));
            AimArm(_armR, _handR, overrideDirection - new Vector3(0f, flail, 0f));
        }

        private void AimArm(Limb arm, Hand hand, Vector3 overrideOutward)
        {
            // The hold is anchored to the gameplay root, not the body: the body sways, the hold does not.
            Vector3 shoulder = transform.InverseTransformPoint(arm.Transform.position);
            float gripX = arm.RestPivot.x + arm.Side * Pose.gripOutward * _armLength;
            float gripY = HandWorldY(hand) - transform.position.y;

            float arc = hand.Reaching ? Mathf.Sin(Mathf.PI * Mathf.Clamp01(hand.Progress)) : 0f;
            float dx = gripX - shoulder.x + arm.Side * Pose.reachArcOutward * arc * _armLength;
            float dy = gripY - shoulder.y;
            float dz = ArmDepth(dx, dy) - Pose.reachArcAway * arc * _armLength;

            Vector3 gripDirection = new Vector3(dx, dy, dz).normalized;
            Vector3 overrideDirection = Mirror(overrideOutward, arm.Side).normalized;
            Aim(arm, Vector3.Slerp(gripDirection, overrideDirection, _overrideWeight));
        }

        private void ApplyLegs(bool flailing, bool boosted)
        {
            if (boosted)
            {
                Vector3 hang = Vector3.Slerp(Pose.legHangDirection, Pose.boostLegDirection, _overrideWeight);
                Aim(_legL, Mirror(hang, -1f));
                Aim(_legR, hang);
                return;
            }

            // The legs' pendulum lags the body's sway, so the feet trail out opposite it.
            float trail = (_legX - _swayX) / Mathf.Max(_legLength, 0.0001f);
            float flail = flailing ? Mathf.Sin(Time.time * Pose.hitFlailFrequency) * 0.4f * _overrideWeight : 0f;
            Aim(_legL, Mirror(Pose.legHangDirection, -1f) + new Vector3(trail, 0f, -flail));
            Aim(_legR, Pose.legHangDirection + new Vector3(trail, 0f, flail));
        }

        private static Vector3 Mirror(Vector3 direction, float side)
        {
            return new Vector3(direction.x * side, direction.y, direction.z);
        }

        /// <summary>Turns a limb from its rest axis to point along a direction given in this transform's space.</summary>
        private void Aim(Limb limb, Vector3 direction)
        {
            if (limb.Transform == null)
            {
                return;
            }

            Vector3 world = transform.TransformDirection(direction.normalized);
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
            _armL = MeasureLimb(armLeft, -1f, down);
            _armR = MeasureLimb(armRight, 1f, down);
            _legL = MeasureLimb(legLeft, -1f, down);
            _legR = MeasureLimb(legRight, 1f, down);
            _armLength = MeasureLength(armLeft);
            _legLength = MeasureLength(legLeft);
            _restCaptured = true;
        }

        /// <summary>
        /// The blocky character's limbs hang straight down from their joints at rest, so "down" is
        /// each limb's rest direction. Measuring the mesh centre instead picks up the blocks'
        /// sideways offset from their pivots and skews every aimed angle.
        /// </summary>
        private Limb MeasureLimb(Transform limb, float side, Vector3 worldDown)
        {
            var result = new Limb { Transform = limb, Side = side };
            if (limb == null)
            {
                return result;
            }

            result.RestLocalRotation = limb.localRotation;
            result.RestDirection = limb.parent.InverseTransformDirection(worldDown).normalized;
            result.RestDirectionLocal = limb.InverseTransformDirection(worldDown).normalized;
            result.RestPivot = transform.InverseTransformPoint(limb.position);
            return result;
        }

        /// <summary>Joint-to-tip length of a limb hanging at rest, from its mesh bounds.</summary>
        private static float MeasureLength(Transform limb)
        {
            Renderer renderer = limb != null ? limb.GetComponentInChildren<Renderer>() : null;
            return renderer != null ? Mathf.Max(limb.position.y - renderer.bounds.min.y, 0.01f) : 0.25f;
        }
    }
}
