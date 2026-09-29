using System.Collections.Generic;
using Game.Gameplay;
using Game.Webhook;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Owns menu/playing/paused/won/lost state, the campaign (the ordered level files and which one is
    /// current), the active level instance id, and bump dispatch. Playing -> Won when the climber
    /// reaches the summit; from Won, StartNextLevel begins the next level or, on the last one, the
    /// view offers a return to the menu. Two optional features (GameFeatures, both off by default) add
    /// lives, where the last hazard hit ends the run as Lost and Retry replays the level with full
    /// lives, and the pause menu, where Paused waits for a deliberate Resume. Without them Paused
    /// exists only for app backgrounding and returning resumes.
    /// Sole authority for whether a hit (hazard or webhook) is allowed to apply right now.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [Tooltip("The campaign, in play order. Main menu Start begins at the first.")]
        [SerializeField] private TextAsset[] levelFiles;
        [Tooltip("Every tuning value the game reads (pace, features, bump types, motor, webhook). Without it the built-in defaults apply.")]
        [SerializeField] private GameSettings settings;
        [SerializeField] private GameObject hazardVisualPrefab;
        [SerializeField] private Material hazardActiveMaterial;
        [SerializeField] private Material hazardSafeMaterial;

        // Both derived at editor time (Level1SceneSetup) from the imported tower/character bounds --
        // see HazardBand.Initialize's doc comment for what each controls.
        [SerializeField] private float hazardVisualDiameter = 2.6f;
        // The climber's full feet-to-head height in world units. Also the unit a BumpType's
        // lift/drop distances are measured in. (Name kept: the scene binding serializes it.)
        [SerializeField] private float hazardBodyHeight = 2.7f;

        private readonly System.Collections.Generic.List<HazardBand> _spawnedHazards = new System.Collections.Generic.List<HazardBand>();
        private BumpListener _listener;
        private bool _loggedEmptyCatalog;

        private volatile bool _accepting;
        private volatile int _levelInstanceId;

        // GameSession's own clock, passed to every HazardBand instead of Time.time: it only
        // advances while State == Playing (never while paused) and resets to 0 on StartLevel/Retry,
        // so pausing never shifts a band's safe/active windows and a retry always starts hazards
        // from the same phase.
        private float _levelClock;

        private float _distanceScale = 1f;

        // Each level is parsed lazily on first use and cached by index: the files are authored data,
        // so a malformed one throws FormatException from StartLevel rather than falling back to
        // made-up numbers.
        private LevelDefinition[] _parsedLevels;

        /// <summary>0-based index of the current level in the campaign.</summary>
        public int LevelIndex { get; private set; }

        public int LevelCount => levelFiles != null ? levelFiles.Length : 0;

        public bool IsFinalLevel => LevelIndex == LevelCount - 1;

        /// <summary>The current level's definition. Throws if the campaign is empty or the file is malformed.</summary>
        public LevelDefinition CurrentLevel
        {
            get
            {
                if (LevelCount == 0)
                {
                    throw new System.InvalidOperationException("GameSession has no level files assigned; the campaign is empty.");
                }

                if (LevelIndex < 0 || LevelIndex >= LevelCount)
                {
                    throw new System.InvalidOperationException("Level index " + LevelIndex + " is outside the campaign (" + LevelCount + " levels).");
                }

                if (_parsedLevels == null || _parsedLevels.Length != LevelCount)
                {
                    _parsedLevels = new LevelDefinition[LevelCount];
                }

                LevelDefinition level = _parsedLevels[LevelIndex];
                if (level == null)
                {
                    TextAsset file = levelFiles[LevelIndex];
                    level = LevelDefinition.FromJson(file != null ? file.text : null);
                    _parsedLevels[LevelIndex] = level;
                }

                return level;
            }
        }

        /// <summary>The tuning asset, or the built-in defaults when none is assigned.</summary>
        public GameSettings Settings => GameSettings.OrDefaults(settings);

        /// <summary>Authored-to-play distance factor for the current level (see ClimbPace).</summary>
        public float DistanceScale => _distanceScale;

        /// <summary>The climber's full body height in world units; the unit bump distances are authored in.</summary>
        public float BodyHeight => hazardBodyHeight;

        /// <summary>Delay between an accepted bump and its glove impact (the climber's move starting).</summary>
        public float BumpImpactDelaySeconds => Settings.motor.bumpImpactDelaySeconds;

        public bool LivesEnabled => Settings.features.LivesEnabled;

        public bool PauseMenuEnabled => Settings.features.PauseMenuEnabled;

        /// <summary>Lives left in this attempt; 0 whenever lives are disabled.</summary>
        public int Lives { get; private set; }

        public SessionState State { get; private set; } = SessionState.Menu;

        public event System.Action<SessionState> StateChanged;
        public event System.Action<float, float> HeightUpdated;
        public event System.Action<int> LivesChanged;
        public event System.Action<BumpEvent> BumpAccepted;

        /// <summary>
        /// Raised by every StartLevel/Retry, including one made while already Playing (which raises no
        /// StateChanged transition worth acting on). Time-based presentation cancels its leftovers here.
        /// </summary>
        public event System.Action LevelStarted;

        /// <summary>
        /// Seconds a time-based effect may advance this frame: the frame time while Playing, 0 in every
        /// other state. Presentation that advances by this instead of Time.deltaTime freezes with the
        /// pause for free; pair it with StateChanged/LevelStarted to cancel on menu, win and restart.
        /// </summary>
        public float PlayDeltaTime => State == SessionState.Playing ? Time.deltaTime : 0f;

        private void Awake()
        {
            _listener = new BumpListener(Settings.webhook.port);
            _listener.StateProvider = () => (_accepting, _levelInstanceId);
            // The listener checks `type` on its worker threads against this copy of the ids; null (any id) while the catalog has none.
            IReadOnlyCollection<string> typeIds = Settings.bumps.KnownTypeIds();
            _listener.KnownTypeIds = typeIds.Count > 0 ? typeIds : null;
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

                // Nonlethal spectacle: never costs a life, never blocks continued play.
                float bodyHeights = bump.Polarity == BumpPolarity.Positive ? bump.Type.liftBodyHeights : -bump.Type.dropBodyHeights;
                motor.ApplyBump(bodyHeights * hazardBodyHeight, BumpImpactDelaySeconds);
                BumpAccepted?.Invoke(bump);
            }
        }

        /// <summary>Fills the fields the sender left out from the catalog defaults.</summary>
        private BumpEvent ResolveBump(BumpRequest request)
        {
            BumpCatalog catalog = Settings.bumps;
            BumpType type = null;
            if (!catalog.TryGet(request.Command.TypeId, out type) && !catalog.TryGet(catalog.defaultTypeId, out type))
            {
                type = FirstType(catalog);
            }

            return new BumpEvent(
                request.RequestId,
                request.Command.Polarity ?? catalog.defaultPolarity,
                type,
                request.Command.Tag ?? catalog.fallbackTag);
        }

        /// <summary>The catalog's first type. A catalog emptied in the Inspector still plays: one built-in boxing type, logged once.</summary>
        private BumpType FirstType(BumpCatalog catalog)
        {
            foreach (BumpType candidate in catalog.types ?? System.Array.Empty<BumpType>())
            {
                if (candidate != null)
                {
                    return candidate;
                }
            }

            if (!_loggedEmptyCatalog)
            {
                _loggedEmptyCatalog = true;
                Debug.LogError("[GameSession] GameSettings.bumps has no types; using a built-in boxing type.");
            }

            return new BumpType { id = catalog.defaultTypeId, displayName = "Boxing", liftBodyHeights = 1f, dropBodyHeights = 1f };
        }

        /// <summary>Begins the campaign at its first level (the main menu's Start).</summary>
        public void StartCampaign()
        {
            LevelIndex = 0;
            StartLevel();
        }

        /// <summary>Advances to the next level after a win. No-op unless Won and not on the final level.</summary>
        public void StartNextLevel()
        {
            if (State != SessionState.Won || IsFinalLevel)
            {
                return;
            }

            LevelIndex++;
            StartLevel();
        }

        /// <summary>(Re)starts the current level from the base.</summary>
        public void StartLevel()
        {
            LevelDefinition level = CurrentLevel;
            _levelInstanceId++;
            _levelClock = 0f;
            // The pace scales the level's speed and every distance alike, so its timing is unchanged.
            _distanceScale = Settings.pace.DistanceScaleFor(level);
            motor.DistanceScale = _distanceScale;
            motor.FinishHeight = level.finishHeight * _distanceScale;
            motor.ClimbSpeed = level.climbSpeed * _distanceScale;
            motor.ResetState(0f);
            motor.CanClimb = true;

            SpawnHazards();

            SetState(SessionState.Playing);
            _accepting = true;

            Lives = LivesEnabled ? Settings.features.StartingLives : 0;

            LevelStarted?.Invoke();
            LivesChanged?.Invoke(Lives);
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
            // A backgrounded app answers /bump with 409 (not a 503 timeout) and nothing advances
            // while away. Without the pause menu returning resumes; with it the player resumes
            // deliberately from the pause panel.
            if (pauseStatus)
            {
                Pause();
            }
            else if (!PauseMenuEnabled)
            {
                Resume();
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
            if (!motor.TryApplyHazardHit(bandHeight, hazardBodyHeight, hazardBodyHeight * Settings.motor.hazardClearanceBodyHeights) || !LivesEnabled)
            {
                return;
            }

            Lives--;
            LivesChanged?.Invoke(Lives);
            if (Lives <= 0)
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

            foreach (HazardSpec authored in CurrentLevel.hazards)
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
