using System.Collections.Generic;
using Game.Gameplay;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>Full-screen hold rules: which touches climb, and when a held touch stops counting.</summary>
    public sealed class ClimbTouchGateTests
    {
        private ClimbTouchGate _gate;

        [SetUp]
        public void SetUp()
        {
            _gate = new ClimbTouchGate();
        }

        private static ClimbTouchGate.Touch T(int id, bool began = false, bool overUi = false)
        {
            return new ClimbTouchGate.Touch(id, began, overUi);
        }

        private bool Eval(bool canClimb, params ClimbTouchGate.Touch[] touches)
        {
            return _gate.Evaluate(canClimb, new List<ClimbTouchGate.Touch>(touches));
        }

        [Test]
        public void NoTouches_DoNotClimb()
        {
            Assert.IsFalse(Eval(true));
        }

        [Test]
        public void TouchBegunOnFreeArea_ClimbsUntilLifted()
        {
            Assert.IsTrue(Eval(true, T(1, began: true)));
            Assert.IsTrue(Eval(true, T(1)));
            Assert.IsFalse(Eval(true), "lift or cancel: the touch is absent");
        }

        [Test]
        public void TouchBegunOnInteractiveUi_NeverClimbs_EvenAfterSlidingOff()
        {
            Assert.IsFalse(Eval(true, T(1, began: true, overUi: true)));
            Assert.IsFalse(Eval(true, T(1)), "still down after sliding off the button");
        }

        [Test]
        public void TouchHeldThroughAMenu_DoesNotClimbOnNextLevelUntilPressedAgain()
        {
            Assert.IsFalse(Eval(false, T(1)), "menu/win tap still down");
            Assert.IsFalse(Eval(true, T(1)), "level started, same finger");
            Assert.IsFalse(Eval(true));
            Assert.IsTrue(Eval(true, T(2, began: true)), "a fresh press climbs");
        }

        [Test]
        public void ClimbingBecomingImpossible_DropsHeldTouch_AndItDoesNotResume()
        {
            Assert.IsTrue(Eval(true, T(1, began: true)));
            Assert.IsFalse(Eval(false, T(1)), "paused");
            Assert.IsFalse(Eval(true, T(1)), "resumed with the finger still down");
        }

        [Test]
        public void MultiTouch_ClimbsWhileAnyClaimedTouchIsDown()
        {
            Assert.IsTrue(Eval(true, T(1, began: true)));
            Assert.IsTrue(Eval(true, T(1), T(2, began: true, overUi: true)));
            Assert.IsTrue(Eval(true, T(2), T(1)), "button finger does not cancel the climb finger");
            Assert.IsFalse(Eval(true, T(2)), "only the button finger is left");
        }

        [Test]
        public void Clear_DropsClaims()
        {
            Eval(true, T(1, began: true));
            _gate.Clear();
            Assert.IsFalse(Eval(true, T(1)));
        }
    }
}
