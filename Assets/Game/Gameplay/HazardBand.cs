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
        private HazardSpec _spec;
        private PlayerMotor _motor;
        private System.Action _onHit;
        private Renderer _renderer;
        private Material _activeMaterial;
        private Material _safeMaterial;
        private bool? _lastActive;

        public void Initialize(HazardSpec spec, PlayerMotor motor, System.Action onHit, GameObject visualPrefab, Material activeMaterial, Material safeMaterial)
        {
            _spec = spec;
            _motor = motor;
            _onHit = onHit;
            _activeMaterial = activeMaterial;
            _safeMaterial = safeMaterial;

            Vector3 pos = transform.position;
            pos.y = spec.height;
            transform.position = pos;

            BuildVisual(visualPrefab);

            _motor.HeightChanged += OnHeightChanged;
        }

        private void BuildVisual(GameObject visualPrefab)
        {
            if (visualPrefab == null)
            {
                return; // no prefab assigned -- hazard still functions (hit detection), just invisible.
            }

            GameObject disc = Object.Instantiate(visualPrefab, transform);
            disc.name = "HazardVisual";
            disc.transform.localPosition = Vector3.zero;
            disc.transform.localRotation = Quaternion.identity;
            disc.transform.localScale = new Vector3(2.6f, 0.06f, 2.6f);

            _renderer = disc.GetComponent<Renderer>();
        }

        private void Update()
        {
            if (_renderer == null)
            {
                return;
            }

            bool active = _spec.IsActiveAt(Time.time);
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
            bool crossedUpward = previous < _spec.height && next >= _spec.height;
            if (!crossedUpward)
            {
                return;
            }

            if (!_spec.IsActiveAt(Time.time))
            {
                return;
            }

            _onHit?.Invoke();
        }
    }
}
