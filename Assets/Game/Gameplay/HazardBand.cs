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
        private HazardSpec _spec;
        private PlayerMotor _motor;
        private System.Action _onHit;

        public void Initialize(HazardSpec spec, PlayerMotor motor, System.Action onHit)
        {
            _spec = spec;
            _motor = motor;
            _onHit = onHit;

            Vector3 pos = transform.position;
            pos.y = spec.height;
            transform.position = pos;

            _motor.HeightChanged += OnHeightChanged;
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
