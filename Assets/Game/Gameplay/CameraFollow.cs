using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>Fixed-azimuth follow: tracks the player's height only. Shake is applied by a child offset (CameraShake), never here.</summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private PlayerMotor target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -12f);
        [SerializeField] private float followLerp = 6f;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = new Vector3(offset.x, target.Height + offset.y, offset.z);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followLerp * Time.deltaTime));
        }
    }
}
