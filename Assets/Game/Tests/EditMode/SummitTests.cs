using System.Reflection;
using Game.Core;
using Game.Gameplay;
using Game.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// TowerSummit placement and SummitSlideView motion. State changes are fed through the views'
    /// public handlers, since EditMode runs neither OnEnable nor a real session's events.
    /// </summary>
    public sealed class SummitTests
    {
        private const float Lip = 2f;
        private const float BaseToSurface = 1.5f;
        private const float PivotAboveBase = 0.25f;

        private GameObject _root;
        private GameObject _motorGo;
        private PlayerMotor _motor;
        private TowerSummit _summit;
        private Transform _crown;
        private Transform _shaft;
        private Transform _low;
        private Transform _high;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Tower");
            _motorGo = new GameObject("Motor");
            _motor = _motorGo.AddComponent<PlayerMotor>();
            _crown = Child("Crown", 0f);
            _shaft = Child("Shaft", 50f);
            _shaft.localScale = new Vector3(3f, 100f, 3f);
            _low = Child("Collar0", 10f);
            _high = Child("Window9", 80f);
            Transform mid = Child("Collar5", 40f);

            _summit = _root.AddComponent<TowerSummit>();
            Set(_summit, "motor", _motor);
            Set(_summit, "crown", _crown);
            Set(_summit, "shaft", _shaft);
            Set(_summit, "pieces", new[] { _low, mid, _high });
            Set(_summit, "crownPivotAboveBase", PivotAboveBase);
            Set(_summit, "crownBaseToSurface", BaseToSurface);
            Set(_summit, "lip", Lip);
            Set(_summit, "pieceMargin", 0.5f);
            Set(_summit, "defaultFinishHeight", 30f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_motorGo);
        }

        [Test]
        public void Place_PutsSurfaceAtFinishPlusLip_AndCrownBaseBelowIt()
        {
            _summit.Place(30f);

            Assert.AreEqual(32f, _summit.SurfaceHeight, 1e-4f);
            Assert.AreEqual(32f - BaseToSurface, _summit.CrownBaseHeight, 1e-4f);
            Assert.AreEqual(_summit.CrownBaseHeight + PivotAboveBase, _crown.localPosition.y, 1e-4f);
        }

        [Test]
        public void Place_EndsShaftAtCrownBase()
        {
            _summit.Place(30f);

            float top = _shaft.localPosition.y + _shaft.localScale.y; // the primitive is 2 tall.
            Assert.AreEqual(_summit.CrownBaseHeight, top, 1e-4f);
            Assert.AreEqual(0f, _shaft.localPosition.y - _shaft.localScale.y, 1e-4f, "the shaft still starts at the ground");
            Assert.AreEqual(3f, _shaft.localScale.x, 1e-4f, "the diameter is untouched");
        }

        [Test]
        public void Place_HidesPiecesAboveCrownBase_AndShowsThoseBelow()
        {
            _summit.Place(60f); // crown base 60.5: the collars at 10 and 40 stay, the window at 80 goes.

            Assert.IsTrue(_low.gameObject.activeSelf);
            Assert.IsTrue(Piece("Collar5").gameObject.activeSelf);
            Assert.IsFalse(_high.gameObject.activeSelf);
        }

        [Test]
        public void Place_ReappliesForLowerAndHigherFinish()
        {
            _summit.Place(30f);
            _summit.Place(5f);
            Assert.AreEqual(5f + Lip, _summit.SurfaceHeight, 1e-4f);
            Assert.IsFalse(_low.gameObject.activeSelf, "a collar above the lower top is hidden");
            Assert.IsFalse(Piece("Collar5").gameObject.activeSelf);

            _summit.Place(90f);
            Assert.AreEqual(90f + Lip, _summit.SurfaceHeight, 1e-4f);
            Assert.IsTrue(_low.gameObject.activeSelf);
            Assert.IsTrue(Piece("Collar5").gameObject.activeSelf);
            Assert.IsTrue(_high.gameObject.activeSelf, "pieces hidden by a lower level come back for a taller one");
            Assert.AreEqual(_summit.CrownBaseHeight, _shaft.localPosition.y + _shaft.localScale.y, 1e-4f);
        }

        [Test]
        public void PlayingFromMenu_UsesMotorFinishHeight_ButResumeKeepsIt()
        {
            _motor.FinishHeight = 60f;
            _summit.OnSessionStateChanged(SessionState.Playing);
            Assert.AreEqual(60f + Lip, _summit.SurfaceHeight, 1e-4f);

            _summit.OnSessionStateChanged(SessionState.Paused);
            _motor.FinishHeight = 10f; // would be a different level; a resume must not re-place.
            _summit.OnSessionStateChanged(SessionState.Playing);
            Assert.AreEqual(60f + Lip, _summit.SurfaceHeight, 1e-4f);

            _summit.OnSessionStateChanged(SessionState.Won);
            _summit.OnSessionStateChanged(SessionState.Playing);
            Assert.AreEqual(10f + Lip, _summit.SurfaceHeight, 1e-4f, "a new level after a win re-places the top");
        }

        [Test]
        public void MenuDefault_IsAppliedOnEnable()
        {
            typeof(TowerSummit).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_summit, null);

            Assert.AreEqual(30f + Lip, _summit.SurfaceHeight, 1e-4f);
        }

        [Test]
        public void SlideView_ReachesStandingPoseAfterItsDuration_AndHolds()
        {
            SummitSlideView view = SlideView(new Vector3(0f, 2f, 3f));

            view.OnSessionStateChanged(SessionState.Won);
            view.Step(0.4f);
            Assert.Greater(view.transform.localPosition.y, 0f);
            Assert.Less(view.transform.localPosition.y, 2f);

            view.Step(0.5f);
            AssertStanding(view);
            view.Step(5f);
            AssertStanding(view);
        }

        [Test]
        public void SlideView_RisesBeforeMovingInward()
        {
            SummitSlideView view = SlideView(new Vector3(0f, 2f, 3f));

            view.OnSessionStateChanged(SessionState.Won);
            view.Step(0.2f); // a quarter through: still rising, not yet across.

            Assert.Greater(view.transform.localPosition.y, 0f);
            Assert.AreEqual(0f, view.transform.localPosition.z, 1e-4f);
        }

        [TestCase(SessionState.Playing)]
        [TestCase(SessionState.Menu)]
        public void SlideView_SnapsBackOnPlayingOrMenu(SessionState next)
        {
            SummitSlideView view = SlideView(new Vector3(0f, 2f, 3f));
            view.OnSessionStateChanged(SessionState.Won);
            view.Step(1f);

            view.OnSessionStateChanged(next);

            Assert.AreEqual(Vector3.zero, view.transform.localPosition);
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, view.transform.localRotation), 1e-3f);
            view.Step(1f);
            Assert.AreEqual(Vector3.zero, view.transform.localPosition, "an idle view does not move");
        }

        private static void AssertStanding(SummitSlideView view)
        {
            Assert.AreEqual(2f, view.transform.localPosition.y, 1e-4f);
            Assert.AreEqual(3f, view.transform.localPosition.z, 1e-4f);
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.Euler(0f, 180f, 0f), view.transform.localRotation), 1e-3f);
        }

        private SummitSlideView SlideView(Vector3 offset)
        {
            var go = new GameObject("SummitSlide");
            go.transform.SetParent(_root.transform, false);
            var view = go.AddComponent<SummitSlideView>();
            Set(view, "standOffset", offset);
            Set(view, "slideSeconds", 0.8f);
            return view;
        }

        private Transform Piece(string name)
        {
            return _root.transform.Find(name);
        }

        private Transform Child(string name, float y)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            go.transform.localPosition = new Vector3(0f, y, 0f);
            return go.transform;
        }

        private static void Set(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, field);
            info.SetValue(target, value);
        }
    }
}
