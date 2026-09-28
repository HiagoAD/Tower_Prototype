using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// One instantiated obstacle band. While active it hits whenever its height lies inside the
    /// climber's body span (feet at PlayerMotor.Height up to the head, one body height above). The
    /// span swept over each PlayerMotor.HeightChanged interval is tested, so a fast frame cannot
    /// tunnel through, and every tick re-tests the current span, so a band that switches to active
    /// while already overlapping a stationary climber still hits. GameSession's hit handler decides
    /// what a hit does (and PlayerMotor's invulnerability stops repeats).
    ///
    /// Never creates a primitive or a Material at runtime -- both are baked at editor time
    /// (Level1SceneSetup) into a shared visual prefab and two shared material assets, because
    /// GameObject.CreatePrimitive on-device can throw ("class 'CapsuleCollider' doesn't exist")
    /// when the collider type it implicitly adds has been stripped from the build.
    /// </summary>
    public sealed class HazardBand : MonoBehaviour
    {
        // Ring thickness as a share of its diameter, so the band keeps its look at any presentation scale.
        private const float VisualThicknessToDiameter = 0.07f;

        private HazardSpec _spec;
        private PlayerMotor _motor;
        private System.Action _onHit;
        private System.Func<float> _levelClock;
        private float _bodyHeight;
        private Renderer _renderer;
        private Material _activeMaterial;
        private Material _safeMaterial;
        private bool? _lastActive;

        /// <summary>
        /// levelClock replaces Time.time as the hazard's notion of "now": it is GameSession's own
        /// clock, which does not advance while paused and resets on StartLevel/Retry, so pausing
        /// never shifts a band's safe/active windows.
        ///
        /// visualDiameter sizes the disc to wrap the actual (imported-asset) tower radius, instead of
        /// a hardcoded constant sized for the old primitive tower.
        ///
        /// bodyHeight is the character's full feet-to-head height: the body span a band can touch is
        /// PlayerMotor.Height (the gameplay root, at the feet) up to Height + bodyHeight.
        /// </summary>
        public void Initialize(HazardSpec spec, PlayerMotor motor, System.Action onHit, GameObject visualPrefab, Material activeMaterial, Material safeMaterial, System.Func<float> levelClock, float visualDiameter, float bodyHeight)
        {
            _spec = spec;
            _motor = motor;
            _onHit = onHit;
            _activeMaterial = activeMaterial;
            _safeMaterial = safeMaterial;
            _levelClock = levelClock;
            _bodyHeight = bodyHeight;

            Vector3 pos = transform.position;
            pos.y = spec.height;
            transform.position = pos;

            BuildVisual(visualPrefab, visualDiameter);

            _motor.HeightChanged += OnHeightChanged;
        }

        private void BuildVisual(GameObject visualPrefab, float visualDiameter)
        {
            if (visualPrefab == null)
            {
                return; // no prefab assigned -- hazard still functions (hit detection), just invisible.
            }

            GameObject disc = Object.Instantiate(visualPrefab, transform);
            disc.name = "HazardVisual";
            disc.transform.localPosition = Vector3.zero;
            disc.transform.localRotation = Quaternion.identity;
            disc.transform.localScale = new Vector3(visualDiameter, visualDiameter * VisualThicknessToDiameter, visualDiameter);

            _renderer = disc.GetComponent<Renderer>();
        }

        private void Update()
        {
            Tick();
        }

        /// <summary>Per-frame work, split out from Update() so EditMode tests can drive it: refreshes the look and re-tests a stationary overlap.</summary>
        public void Tick()
        {
            bool active = _spec.IsActiveAt(_levelClock());
            if (_lastActive != active)
            {
                _lastActive = active;
                if (_renderer != null)
                {
                    _renderer.sharedMaterial = active ? _activeMaterial : _safeMaterial;
                }
            }

            if (active && OverlapsBody(_motor.Height, _motor.Height))
            {
                _onHit?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (_motor != null)
            {
                _motor.HeightChanged -= OnHeightChanged;
            }
        }

        private void OnHeightChanged(float previous, float next)
        {
            if (!_spec.IsActiveAt(_levelClock()) || !OverlapsBody(previous, next))
            {
                return;
            }

            _onHit?.Invoke();
        }

        /// <summary>True if the band's height falls inside the body span swept while the feet moved from previous to next.</summary>
        private bool OverlapsBody(float previous, float next)
        {
            float low = Mathf.Min(previous, next);
            float high = Mathf.Max(previous, next) + _bodyHeight;
            return _spec.height >= low && _spec.height <= high;
        }
    }
}
