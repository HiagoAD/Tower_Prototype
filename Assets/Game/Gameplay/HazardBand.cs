using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// One instantiated obstacle band. Checks the full previous-to-next height interval on every
    /// PlayerMotor.HeightChanged event (not a single-frame overlap test), so a slow/fast frame
    /// cannot skip a crossing. Only fires on the upward crossing of its height.
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
        private float _contactHeightOffset;
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
        /// contactHeightOffset shifts hit detection from PlayerMotor.Height (the gameplay root, at
        /// the character's feet) up to roughly chest height, so a hit registers when the band
        /// visibly reaches the character's body rather than only once it reaches their feet.
        /// </summary>
        public void Initialize(HazardSpec spec, PlayerMotor motor, System.Action onHit, GameObject visualPrefab, Material activeMaterial, Material safeMaterial, System.Func<float> levelClock, float visualDiameter, float contactHeightOffset)
        {
            _spec = spec;
            _motor = motor;
            _onHit = onHit;
            _activeMaterial = activeMaterial;
            _safeMaterial = safeMaterial;
            _levelClock = levelClock;
            _contactHeightOffset = contactHeightOffset;

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
            if (_renderer == null)
            {
                return;
            }

            bool active = _spec.IsActiveAt(_levelClock());
            if (_lastActive == active)
            {
                return;
            }

            _lastActive = active;
            _renderer.sharedMaterial = active ? _activeMaterial : _safeMaterial;
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
            bool crossedUpward = (previous + _contactHeightOffset) < _spec.height && (next + _contactHeightOffset) >= _spec.height;
            if (!crossedUpward)
            {
                return;
            }

            if (!_spec.IsActiveAt(_levelClock()))
            {
                return;
            }

            _onHit?.Invoke();
        }
    }
}
