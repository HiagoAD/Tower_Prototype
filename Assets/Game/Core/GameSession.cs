using Game.Gameplay;
using Game.Webhook;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Owns menu/playing/paused/won/lost state, the active level instance id, and bump dispatch.
    /// Sole authority for whether a hit (hazard or webhook) is allowed to apply right now.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private TextAsset levelJson;
        [Tooltip("Optional global climb pace; without it the level plays exactly as authored.")]
        [SerializeField] private ClimbPace pace;
        [Tooltip("Bump types and the defaults for fields a /bump request omits. Without it every bump is a default negative boxing bump.")]
        [SerializeField] private BumpCatalog bumpCatalog;
        [SerializeField] private GameObject hazardVisualPrefab;
        [SerializeField] private Material hazardActiveMaterial;
        [SerializeField] private Material hazardSafeMaterial;

        // Both derived at editor time (Level1SceneSetup) from the imported tower/character bounds --
        // see HazardBand.Initialize's doc comment for what each controls.
        [SerializeField] private float hazardVisualDiameter = 2.6f;
        // The climber's full feet-to-head height in world units. Also the unit a BumpType's
        // lift/drop distances are measured in. (Name kept: the scene binding serializes it.)
        [SerializeField] private float hazardBodyHeight = 2.7f;

        [Tooltip("Seconds from an accepted bump to its glove impact, when the climber's move starts. BumpBurstView times its first glove's mid-flight to this.")]
        [SerializeField] private float bumpImpactDelaySeconds = 0.275f;

        // How far below a hazard band the knocked-back climber's head must end up, as a share of the body height.
        private const float HazardClearanceToBodyHeight = 0.1f;

        private readonly System.Collections.Generic.List<HazardBand> _spawnedHazards = new System.Collections.Generic.List<HazardBand>();
        private BumpListener _listener;
        private BumpCatalog _fallbackCatalog;

        private volatile bool _accepting;
        private volatile int _levelInstanceId;

        // GameSession's own clock, passed to every HazardBand instead of Time.time: it only
        // advances while State == Playing (never while paused) and resets to 0 on StartLevel/Retry,
        // so pausing never shifts a band's safe/active windows and a retry always starts hazards
        // from the same phase.
        private float _levelClock;

        private float _distanceScale = 1f;

        // Parsed lazily from levelJson on first use: the file is authored data, so a malformed one
        // throws FormatException from StartLevel rather than falling back to made-up numbers.
        private LevelDefinition _level;
        private LevelDefinition Level => _level ??= LevelDefinition.FromJson(levelJson != null ? levelJson.text : null);

        /// <summary>Authored-to-play distance factor for the current level (see ClimbPace).</summary>
        public float DistanceScale => _distanceScale;

        /// <summary>The climber's full body height in world units; the unit bump distances are authored in.</summary>
        public float BodyHeight => hazardBodyHeight;

        /// <summary>Delay between an accepted bump and its glove impact (the climber's move starting).</summary>
        public float BumpImpactDelaySeconds => bumpImpactDelaySeconds;

        public SessionState State { get; private set; } = SessionState.Menu;

        public event System.Action<SessionState> StateChanged;
        public event System.Action<float, float> HeightUpdated;
        public event System.Action<BumpEvent> BumpAccepted;

        private void Awake()
        {
            _listener = new BumpListener();
            _listener.StateProvider = () => (_accepting, _levelInstanceId);
            _listener.KnownTypeIds = bumpCatalog != null ? bumpCatalog.KnownTypeIds() : null;
            _listener.Faulted += msg => Debug.LogWarning("[GameSession] listener error: " + msg);
        }

        private void OnEnable()
        {
            _listener.Start();
        }

        private void OnDisable()
        {
            _listener.Stop();
        }

        private void OnDestroy()
        {
            if (_fallbackCatalog != null)
            {
                Destroy(_fallbackCatalog);
            }

            _listener.Dispose();
        }

        private void Update()
        {
            DrainBumpQueue();

            if (State != SessionState.Playing)
            {
                return;
            }

            _levelClock += Time.deltaTime;

            HeightUpdated?.Invoke(motor.Height, motor.FinishHeight);

            if (motor.NormalizedProgress >= 1f)
            {
                Win();
            }
        }

        /// <summary>
        /// The main thread is the sole authority over whether a queued request gets dispatched.
        /// Every request is resolved exactly once here (or left to the worker's own bounded
        /// timeout/expiry) -- the HTTP response the requester sees follows this decision, not the
        /// other way around. The bump effect only ever fires when our own Accept transition wins.
        /// </summary>
        private void DrainBumpQueue()
        {
            while (_listener.TryDequeue(out BumpRequest request))
            {
                if (request.LevelInstanceId != _levelInstanceId)
                {
                    request.TryResolve(BumpRequestState.Rejected); // stale request from a previous level/attempt.
                    continue;
                }

                if (State != SessionState.Playing)
                {
                    request.TryResolve(BumpRequestState.Rejected);
                    continue;
                }

                BumpEvent bump = ResolveBump(request);
                request.SetResolvedValues(bump.Polarity, bump.Type.id);
                if (!request.TryResolve(BumpRequestState.Accepted))
                {
                    continue; // the worker already gave up and expired this one -- never dispatch it.
                }

                // Nonlethal spectacle: never consumes a hit point, never blocks continued play.
                float bodyHeights = bump.Polarity == BumpPolarity.Positive ? bump.Type.liftBodyHeights : -bump.Type.dropBodyHeights;
                motor.ApplyBump(bodyHeights * hazardBodyHeight, bumpImpactDelaySeconds);
                BumpAccepted?.Invoke(bump);
            }
        }

        /// <summary>Fills the fields the sender left out from the catalog defaults.</summary>
        private BumpEvent ResolveBump(BumpRequest request)
        {
            BumpCatalog catalog = bumpCatalog != null && bumpCatalog.types.Length > 0 ? bumpCatalog : FallbackCatalog();
            if (!catalog.TryGet(request.Command.TypeId, out BumpType type) && !catalog.TryGet(catalog.defaultTypeId, out type))
            {
                type = catalog.types[0];
            }

            return new BumpEvent(
                request.RequestId,
                request.Command.Polarity ?? catalog.defaultPolarity,
                type,
                request.Command.Tag ?? catalog.fallbackTag);
        }

        /// <summary>A scene without a BumpCatalog still plays: one boxing type on the catalog's own defaults. Logged once.</summary>
        private BumpCatalog FallbackCatalog()
        {
            if (_fallbackCatalog == null)
            {
                Debug.LogError("[GameSession] No BumpCatalog assigned; using a built-in boxing type.");
                _fallbackCatalog = ScriptableObject.CreateInstance<BumpCatalog>();
                _fallbackCatalog.types = new[] { new BumpType { id = _fallbackCatalog.defaultTypeId, displayName = "Boxing", liftBodyHeights = 1f, dropBodyHeights = 1f } };
            }

            return _fallbackCatalog;
        }

        public void StartLevel()
        {
            LevelDefinition level = Level;
            _levelInstanceId++;
            _levelClock = 0f;
            // The pace scales the level's speed and every distance alike, so its timing is unchanged.
            _distanceScale = pace != null ? pace.DistanceScaleFor(level) : 1f;
            motor.DistanceScale = _distanceScale;
            motor.FinishHeight = level.finishHeight * _distanceScale;
            motor.ClimbSpeed = level.climbSpeed * _distanceScale;
            motor.ResetState(0f);
            motor.CanClimb = true;

            SpawnHazards();

            SetState(SessionState.Playing);
            _accepting = true;

            HeightUpdated?.Invoke(0f, motor.FinishHeight);
        }

        public void Retry()
        {
            StartLevel();
        }

        public void Pause()
        {
            if (State != SessionState.Playing)
            {
                return;
            }

            motor.CanClimb = false;
            motor.Paused = true; // freezes knockback/invulnerability/lockout timers too, not just movement.
            _accepting = false;
            SetState(SessionState.Paused);
        }

        public void Resume()
        {
            if (State != SessionState.Paused)
            {
                return;
            }

            motor.CanClimb = true;
            motor.Paused = false;
            _accepting = true;
            SetState(SessionState.Playing);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // A backgrounded app answers /bump with 409 (not a 503 timeout) and the player never
            // returns mid-level. No auto-resume: the player resumes deliberately.
            if (pauseStatus)
            {
                Pause();
            }
        }

        public void ReturnToMenu()
        {
            motor.ResetState(0f); // the menu shows the climber at the base, not wherever the run ended.
            motor.CanClimb = false;
            motor.Paused = true;
            _accepting = false;
            ClearHazards();
            SetState(SessionState.Menu);
        }

        /// <summary>A hazard band at bandHeight (play-scaled world units) touched the climber's body.</summary>
        public void OnHazardHit(float bandHeight)
        {
            if (State != SessionState.Playing)
            {
                return;
            }

            // Invulnerable from a recent hit: no-op, matches short recovery window.
            motor.TryApplyHazardHit(bandHeight, hazardBodyHeight, hazardBodyHeight * HazardClearanceToBodyHeight);
        }

        private void Win()
        {
            motor.CanClimb = false;
            motor.Paused = true;
            _accepting = false;
            SetState(SessionState.Won);
        }

        private void SpawnHazards()
        {
            ClearHazards();

            foreach (HazardSpec authored in Level.hazards)
            {
                HazardSpec spec = authored;
                spec.height *= _distanceScale;
                var go = new GameObject("HazardBand");
                go.transform.SetParent(transform, false);
                HazardBand band = go.AddComponent<HazardBand>();
                band.Initialize(spec, motor, () => OnHazardHit(spec.height), hazardVisualPrefab, hazardActiveMaterial, hazardSafeMaterial, () => _levelClock, hazardVisualDiameter, hazardBodyHeight);
                _spawnedHazards.Add(band);
            }
        }

        private void ClearHazards()
        {
            foreach (HazardBand band in _spawnedHazards)
            {
                if (band != null)
                {
                    Destroy(band.gameObject);
                }
            }

            _spawnedHazards.Clear();
        }

        private void SetState(SessionState next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
