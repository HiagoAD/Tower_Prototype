using System.Collections.Generic;

namespace Game.Gameplay
{
    /// <summary>
    /// Decides which touches hold the climb. A touch counts only if it BEGAN while climbing was possible and not on
    /// an interactive control; it then keeps counting until it lifts or is cancelled, wherever it slides. A touch that
    /// began on a button, or before climbing was possible (the menu or win tap), never counts, so it must be lifted and
    /// pressed again. Pure logic so the rules are testable without a Touchscreen.
    /// </summary>
    public sealed class ClimbTouchGate
    {
        private readonly HashSet<int> _claimed = new HashSet<int>();
        private readonly List<int> _scratch = new List<int>();

        public readonly struct Touch
        {
            public readonly int Id;
            public readonly bool BeganThisFrame;
            public readonly bool OverInteractiveUi;

            public Touch(int id, bool beganThisFrame, bool overInteractiveUi)
            {
                Id = id;
                BeganThisFrame = beganThisFrame;
                OverInteractiveUi = overInteractiveUi;
            }
        }

        public int HeldCount => _claimed.Count;

        /// <param name="canClimb">False in the menu, paused, won or lost: every claim is dropped.</param>
        /// <param name="activeTouches">Only touches currently pressed; a lifted or cancelled touch is simply absent.</param>
        public bool Evaluate(bool canClimb, IReadOnlyList<Touch> activeTouches)
        {
            if (!canClimb)
            {
                _claimed.Clear();
                return false;
            }

            _scratch.Clear();
            foreach (int id in _claimed)
            {
                bool stillDown = false;
                for (int i = 0; i < activeTouches.Count; i++)
                {
                    if (activeTouches[i].Id == id)
                    {
                        stillDown = true;
                        break;
                    }
                }

                if (!stillDown)
                {
                    _scratch.Add(id);
                }
            }

            for (int i = 0; i < _scratch.Count; i++)
            {
                _claimed.Remove(_scratch[i]);
            }

            for (int i = 0; i < activeTouches.Count; i++)
            {
                Touch t = activeTouches[i];
                if (t.BeganThisFrame && !t.OverInteractiveUi)
                {
                    _claimed.Add(t.Id);
                }
            }

            return _claimed.Count > 0;
        }

        public void Clear()
        {
            _claimed.Clear();
        }
    }
}
