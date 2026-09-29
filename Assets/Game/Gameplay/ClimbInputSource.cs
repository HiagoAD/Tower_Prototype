using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Gameplay
{
    /// <summary>
    /// Hold-to-climb input: drives PlayerMotor.ClimbHeld from Space/W (Editor) or any touch on the screen
    /// (device), so a hold climbs from the top, middle or bottom. A touch that starts on a button/toggle
    /// belongs to that control and never climbs, even if it slides off; ClimbTouchGate holds the rules.
    /// Kept in its own component so swapping control schemes later does not touch PlayerMotor or GameSession.
    /// </summary>
    public sealed class ClimbInputSource : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [Tooltip("GameSettings.input; currently nothing to tune. Without it the built-in defaults apply.")]
        [SerializeField] private GameSettings settings;

        private readonly ClimbTouchGate _gate = new ClimbTouchGate();
        private readonly List<ClimbTouchGate.Touch> _touches = new List<ClimbTouchGate.Touch>();
        private readonly List<RaycastResult> _hits = new List<RaycastResult>();
        private PointerEventData _pointer;

        private void OnDisable()
        {
            _gate.Clear();
            if (motor != null)
            {
                motor.ClimbHeld = false;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            _gate.Clear();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                _gate.Clear();
            }
        }

        private void Update()
        {
            if (motor == null)
            {
                return;
            }

            // Always evaluated so the gate sees CanClimb drop and forgets touches held through a menu.
            bool touchHeld = EvaluateTouches();
            motor.ClimbHeld = IsKeyHeld() || touchHeld;
        }

        private static bool IsKeyHeld()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.spaceKey.isPressed || keyboard.wKey.isPressed);
        }

        private bool EvaluateTouches()
        {
            _touches.Clear();
            Touchscreen screen = Touchscreen.current;
            if (screen != null && motor.CanClimb)
            {
                var all = screen.touches;
                for (int i = 0; i < all.Count; i++)
                {
                    var touch = all[i];
                    if (!touch.press.isPressed)
                    {
                        continue;
                    }

                    bool began = touch.press.wasPressedThisFrame;
                    bool overUi = began && IsOverInteractiveUi(touch.position.ReadValue());
                    _touches.Add(new ClimbTouchGate.Touch(touch.touchId.ReadValue(), began, overUi));
                }
            }

            return _gate.Evaluate(motor.CanClimb, _touches);
        }

        /// <summary>True only for a Selectable (button, toggle...) under the point; non-interactive HUD graphics never block a climb.</summary>
        private bool IsOverInteractiveUi(Vector2 screenPosition)
        {
            EventSystem system = EventSystem.current;
            if (system == null)
            {
                return false;
            }

            _pointer ??= new PointerEventData(system);
            _pointer.position = screenPosition;
            _hits.Clear();
            system.RaycastAll(_pointer, _hits);
            for (int i = 0; i < _hits.Count; i++)
            {
                GameObject hit = _hits[i].gameObject;
                if (hit != null && hit.GetComponentInParent<Selectable>() != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
