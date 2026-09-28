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
        [SerializeField] private int startingHitPoints = 3;
        [SerializeField] private GameObject hazardVisualPrefab;
        [SerializeField] private Material hazardActiveMaterial;
        [SerializeField] private Material hazardSafeMaterial;

        // Both derived at editor time (Level1SceneSetup) from the imported tower/character bounds --
        // see HazardBand.Initialize's doc comment for what each controls.
        [SerializeField] private float hazardVisualDiameter = 2.6f;
        [SerializeField] private float hazardContactHeightOffset = 0f;

        private readonly System.Collections.Generic.List<HazardBand> _spawnedHazards = new System.Collections.Generic.List<HazardBand>();
        private BumpListener _listener;

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

        public SessionState State { get; private set; } = SessionState.Menu;
        public int HitPoints { get; private set; }

        public event System.Action<SessionState> StateChanged;
        public event System.Action<int> HitPointsChanged;
        public event System.Action<float, float> HeightUpdated;
        public event System.Action HazardHitOccurred;
        public event System.Action<string> BumpAccepted;

        private void Awake()
        {
            _listener = new BumpListener();
            _listener.StateProvider = () => (_accepting, _levelInstanceId);
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
        /// other way around. The glove effect only ever fires when our own Accept transition wins.
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

                if (!request.TryResolve(BumpRequestState.Accepted))
                {
                    continue; // the worker already gave up and expired this one -- never dispatch it.
                }

                motor.TryApplyHit(); // nonlethal spectacle hit: never consumes a hit point, never blocks continued play.
                BumpAccepted?.Invoke(request.RequestId);
            }
        }

        public void StartLevel()
        {
            LevelDefinition level = Level;
            _levelInstanceId++;
            _levelClock = 0f;
            HitPoints = startingHitPoints;
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

            HitPointsChanged?.Invoke(HitPoints);
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

        public void ReturnToMenu()
        {
            motor.CanClimb = false;
            motor.Paused = true;
            _accepting = false;
            ClearHazards();
            SetState(SessionState.Menu);
        }

        public void OnHazardHit()
        {
            if (State != SessionState.Playing)
            {
                return;
            }

            if (!motor.TryApplyHit())
            {
                return; // currently invulnerable from a recent hit -- no-op, matches short recovery window.
            }

            HazardHitOccurred?.Invoke();

            HitPoints--;
            HitPointsChanged?.Invoke(HitPoints);

            if (HitPoints <= 0)
            {
                Lose();
            }
        }

        private void Win()
        {
            motor.CanClimb = false;
            motor.Paused = true;
            _accepting = false;
            SetState(SessionState.Won);
        }

        private void Lose()
        {
            motor.CanClimb = false;
            motor.Paused = true;
            _accepting = false;
            SetState(SessionState.Lost);
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
                band.Initialize(spec, motor, OnHazardHit, hazardVisualPrefab, hazardActiveMaterial, hazardSafeMaterial, () => _levelClock, hazardVisualDiameter, hazardContactHeightOffset);
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
