using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game.Gameplay
{
    /// <summary>
    /// Hold-to-climb input: drives PlayerMotor.ClimbHeld from Space/W (Editor) or a touch inside
    /// the bottom screen region (device). This is a provisional interpretation, not confirmed
    /// reference behaviour -- see Docs/Development/REFERENCE_OBSERVATION_ADDENDUM.md. Kept in its
    /// own component so swapping control schemes later does not touch PlayerMotor or GameSession.
    /// </summary>
    public sealed class ClimbInputSource : MonoBehaviour
    {
        /// <summary>
        /// Single source of truth for the bottom-of-screen climb touch region, as a fraction of
        /// Screen.height. The controls hint visual (see Level1SceneSetup) reads this same constant
        /// so its drawn band can never drift out of sync with the actual input region below.
        /// </summary>
        public const float TouchRegionNormalizedHeight = 0.35f;

        [SerializeField] private PlayerMotor motor;
        [Range(0f, 1f)][SerializeField] private float touchRegionNormalizedHeight = TouchRegionNormalizedHeight;

        private void Update()
        {
            if (motor == null)
            {
                return;
            }

            motor.ClimbHeld = IsHoldDetected();
        }

        private bool IsHoldDetected()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.isPressed || keyboard.wKey.isPressed))
            {
                return true;
            }

            Touchscreen touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                int touchId = touch.primaryTouch.touchId.ReadValue();
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touchId))
                {
                    return false; // a menu/HUD button is being touched, not the climb region.
                }

                Vector2 pos = touch.primaryTouch.position.ReadValue();
                float regionPixels = Screen.height * touchRegionNormalizedHeight;
                if (pos.y <= regionPixels)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
