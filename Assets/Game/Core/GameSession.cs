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
        [SerializeField] private LevelDefinition level;
        [SerializeField] private int startingHitPoints = 3;
        [SerializeField] private GameObject hazardVisualPrefab;
        [SerializeField] private Material hazardActiveMaterial;
        [SerializeField] private Material hazardSafeMaterial;

        private readonly System.Collections.Generic.List<HazardBand> _spawnedHazards = new System.Collections.Generic.List<HazardBand>();
        private BumpListener _listener;

        private volatile bool _accepting;
        private volatile int _levelInstanceId;

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

            HeightUpdated?.Invoke(motor.Height, level.finishHeight);

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
            _levelInstanceId++;
            HitPoints = startingHitPoints;
            motor.FinishHeight = level.finishHeight;
            motor.ClimbSpeed = level.climbSpeed;
            motor.ResetState(0f);
            motor.CanClimb = true;

            SpawnHazards();

            SetState(SessionState.Playing);
            _accepting = true;

            HitPointsChanged?.Invoke(HitPoints);
            HeightUpdated?.Invoke(0f, level.finishHeight);
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
            _accepting = true;
            SetState(SessionState.Playing);
        }

        public void ReturnToMenu()
        {
            motor.CanClimb = false;
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
            _accepting = false;
            SetState(SessionState.Won);
        }

        private void Lose()
        {
            motor.CanClimb = false;
            _accepting = false;
            SetState(SessionState.Lost);
        }

        private void SpawnHazards()
        {
            ClearHazards();

            foreach (HazardSpec spec in level.hazards)
            {
                var go = new GameObject("HazardBand");
                go.transform.SetParent(transform, false);
                HazardBand band = go.AddComponent<HazardBand>();
                band.Initialize(spec, motor, OnHazardHit, hazardVisualPrefab, hazardActiveMaterial, hazardSafeMaterial);
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
