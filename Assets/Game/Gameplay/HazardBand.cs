using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// One instantiated obstacle band. Checks the full previous-to-next height interval on every
    /// PlayerMotor.HeightChanged event (not a single-frame overlap test), so a slow/fast frame
    /// cannot skip a crossing. Only fires on the upward crossing of its height.
    /// </summary>
    public sealed class HazardBand : MonoBehaviour
    {
        private static readonly Color DangerColor = new Color(0.9f, 0.15f, 0.1f, 0.85f);
        private static readonly Color SafeColor = new Color(0.95f, 0.85f, 0.1f, 0.6f);

        private HazardSpec _spec;
        private PlayerMotor _motor;
        private System.Action _onHit;
        private Renderer _renderer;
        private MaterialPropertyBlock _propertyBlock;

        public void Initialize(HazardSpec spec, PlayerMotor motor, System.Action onHit)
        {
            _spec = spec;
            _motor = motor;
            _onHit = onHit;

            Vector3 pos = transform.position;
            pos.y = spec.height;
            transform.position = pos;

            BuildVisual();

            _motor.HeightChanged += OnHeightChanged;
        }

        private void BuildVisual()
        {
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "HazardVisual";
            disc.transform.SetParent(transform, false);
            disc.transform.localScale = new Vector3(2.6f, 0.06f, 2.6f);
            Object.Destroy(disc.GetComponent<Collider>());

            _renderer = disc.GetComponent<Renderer>();
            _renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (_renderer == null)
            {
                return;
            }

            Color color = _spec.IsActiveAt(Time.time) ? DangerColor : SafeColor;
            _propertyBlock.SetColor("_BaseColor", color);
            _renderer.SetPropertyBlock(_propertyBlock);
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
