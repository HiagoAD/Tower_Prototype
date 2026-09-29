using System.Reflection;
using Game.Core;
using Game.Gameplay;
using Game.Webhook;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// CameraEffects through a real GameSession: bump impacts land at the glove delay with polarity-specific zoom,
    /// a bump plus a hit leave no offset, and every exit path restores the camera. EditMode does not run
    /// Awake/OnEnable, so the private wiring is invoked by reflection and session events are raised through their fields.
    /// </summary>
    public sealed class CameraEffectsTests
    {
        private const float Dt = 1f / 60f;
        private const float BaseFov = 45f;

        private sealed class RecordingFx : ScreenFxDriver
        {
            public float Aberration;
            public float Bloom;
            public float MaxAberration;
            public float MaxBloom;

            public override void Apply(float aberrationWeight, float bloomWeight)
            {
                Aberration = aberrationWeight;
                Bloom = bloomWeight;
                MaxAberration = Mathf.Max(MaxAberration, aberrationWeight);
                MaxBloom = Mathf.Max(MaxBloom, bloomWeight);
            }
        }

        private GameObject _root;
        private GameObject _motorGo;
        private TextAsset _level;
        private GameSettings _settings;
        private GameSession _session;
        private PlayerMotor _motor;
        private CameraEffects _effects;
        private CameraShake _shake;
        private Camera _camera;
        private Transform _offset;
        private RecordingFx _fx;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<GameSettings>();
            _level = new TextAsset("{\"levelId\":1,\"displayName\":\"Test\",\"finishHeight\":30,\"climbSpeed\":2.5,\"hazards\":[]}");

            _motorGo = new GameObject("Motor");
            _motor = _motorGo.AddComponent<PlayerMotor>();
            SettingsBinding.Bind(_motor, _settings);
            _session = _motorGo.AddComponent<GameSession>();
            SetObject(_session, "motor", _motor);
            SetArray(_session, "levelFiles", _level);
            SettingsBinding.Bind(_session, _settings);

            _root = new GameObject("Rig");
            var shakeGo = new GameObject("Shake");
            shakeGo.transform.SetParent(_root.transform, false);
            _shake = shakeGo.AddComponent<CameraShake>();
            SettingsBinding.Bind(_shake, _settings);
            var offsetGo = new GameObject("Offset");
            offsetGo.transform.SetParent(shakeGo.transform, false);
            _offset = offsetGo.transform;
            var cameraGo = new GameObject("Camera", typeof(Camera));
            cameraGo.transform.SetParent(_offset, false);
            cameraGo.transform.position = new Vector3(0f, 0f, -36f);
            _camera = cameraGo.GetComponent<Camera>();
            _camera.fieldOfView = BaseFov;

            _fx = _root.AddComponent<RecordingFx>();
            _effects = _root.AddComponent<CameraEffects>();
            SetObject(_effects, "session", _session);
            SetObject(_effects, "worldCamera", _camera);
            SetObject(_effects, "cameraOffset", _offset);
            SetObject(_effects, "focusTarget", _motorGo.transform);
            SetObject(_effects, "shake", _shake);
            SetObject(_effects, "screenFx", _fx);
            SettingsBinding.Bind(_effects, _settings);
            Invoke(_effects, "Awake");
            Invoke(_effects, "OnEnable");

            _session.StartLevel();
        }

        [TearDown]
        public void TearDown()
        {
            Invoke(_effects, "OnDisable");
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_motorGo);
            Object.DestroyImmediate(_level);
            Object.DestroyImmediate(_settings);
        }

        private static void SetObject(Object target, string field, Object value)
        {
            var so = new UnityEditor.SerializedObject(target);
            UnityEditor.SerializedProperty property = so.FindProperty(field);
            Assert.IsNotNull(property, target.GetType().Name + "." + field);
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, Object value)
        {
            var so = new UnityEditor.SerializedObject(target);
            UnityEditor.SerializedProperty property = so.FindProperty(field);
            property.arraySize = 1;
            property.GetArrayElementAtIndex(0).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Invoke(object target, string method)
        {
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(target, null);
        }

        private static void Raise(object target, string eventField, params object[] args)
        {
            var handler = (System.Delegate)target.GetType().GetField(eventField, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
            handler?.DynamicInvoke(args);
        }

        private void Bump(BumpPolarity polarity)
        {
            var type = new BumpType { id = "boxing", displayName = "Boxing", liftBodyHeights = 1f, dropBodyHeights = 1f };
            Raise(_session, "BumpAccepted", new BumpEvent("id", polarity, type, "Tag"));
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
            {
                _effects.Step(Dt);
                _shake.Step(Dt);
            }
        }

        private void AssertNeutral()
        {
            Assert.AreEqual(BaseFov, _camera.fieldOfView, 1e-4f, "FOV back to the base");
            Assert.AreEqual(Vector3.zero, _offset.localPosition);
            Assert.AreEqual(Quaternion.identity, _offset.localRotation);
            Assert.AreEqual(Vector3.zero, _shake.transform.localPosition);
            Assert.IsFalse(_shake.IsShaking);
            Assert.AreEqual(0, _effects.PendingImpactCount);
            Assert.AreEqual(0f, _fx.Aberration);
            Assert.AreEqual(0f, _fx.Bloom);
        }

        [Test]
        public void NegativeBump_PunchesInAtTheImpactMoment_NotBefore()
        {
            Bump(BumpPolarity.Negative);
            Run(_session.BumpImpactDelaySeconds - 0.05f);
            Assert.AreEqual(BaseFov, _camera.fieldOfView, 1e-4f, "nothing before the glove lands");
            Assert.IsFalse(_shake.IsShaking);

            Run(0.05f + 0.12f);

            Assert.Less(_camera.fieldOfView, BaseFov, "negative narrows the view");
            Assert.IsTrue(_shake.IsShaking);
        }

        [Test]
        public void PositiveBump_PullsOut_AndHasAWarmerPostMixThanNegative()
        {
            Bump(BumpPolarity.Positive);
            Run(_session.BumpImpactDelaySeconds + 0.12f);
            Assert.Greater(_camera.fieldOfView, BaseFov, "positive widens the view");
            float positiveBloom = _fx.MaxBloom;
            float positiveAberration = _fx.MaxAberration;
            Assert.Greater(positiveBloom, positiveAberration, "positive leans on bloom");

            Run(2f);
            _fx.MaxBloom = _fx.MaxAberration = 0f;
            Bump(BumpPolarity.Negative);
            Run(_session.BumpImpactDelaySeconds + 0.12f);
            Assert.Greater(_fx.MaxAberration, _fx.MaxBloom, "negative leans on aberration");
        }

        [Test]
        public void ZeroedSettings_TurnEachEffectOff()
        {
            _settings.cameraFx.negativeFovPercent = 0f;
            _settings.cameraFx.postProcessingEnabled = false;
            _settings.camera.shakeMagnitude = 0f;

            Bump(BumpPolarity.Negative);
            Run(_session.BumpImpactDelaySeconds + 0.3f);

            Assert.AreEqual(BaseFov, _camera.fieldOfView, 1e-4f);
            Assert.AreEqual(0f, _fx.MaxAberration);
            Assert.AreEqual(0f, _fx.MaxBloom);
            Assert.IsFalse(_shake.IsShaking);
        }

        [Test]
        public void BumpThenHazardHit_EndWithEverythingBackOnTheFollowPosition()
        {
            Bump(BumpPolarity.Negative);
            Run(_session.BumpImpactDelaySeconds + 0.1f);
            Raise(_session, "HazardHit");
            Run(0.1f);
            Assert.IsTrue(_shake.IsShaking);

            Run(3f);

            AssertNeutral();
        }

        [Test]
        public void HazardHit_ShakesLessThanABump()
        {
            Raise(_session, "HazardHit");
            float hit = _shake.CurrentAmplitude;
            _shake.Cancel();

            Bump(BumpPolarity.Positive);
            Run(_session.BumpImpactDelaySeconds + Dt);
            float bump = _shake.CurrentAmplitude;

            Assert.Greater(hit, 0f);
            Assert.Greater(bump, hit * 2f, "hit shake stays well under even the weaker (positive) bump shake");
        }

        [Test]
        public void Pause_FreezesPendingImpacts_AndResumeContinues()
        {
            Bump(BumpPolarity.Negative);
            Run(0.1f);
            _session.Pause();
            Run(1f);
            Assert.AreEqual(1, _effects.PendingImpactCount, "the impact waits while paused");
            Assert.AreEqual(BaseFov, _camera.fieldOfView, 1e-4f);

            _session.Resume();
            Run(_session.BumpImpactDelaySeconds);

            Assert.AreEqual(0, _effects.PendingImpactCount);
            Assert.AreNotEqual(BaseFov, _camera.fieldOfView);
        }

        [Test]
        public void LevelStart_MidEffect_RestoresEverythingAndDropsPendingImpacts()
        {
            Bump(BumpPolarity.Negative);
            Run(_session.BumpImpactDelaySeconds + 0.1f);
            Bump(BumpPolarity.Positive);
            Raise(_session, "HazardHit");

            _session.StartLevel();
            _effects.Step(Dt);

            AssertNeutral();
        }

        [Test]
        public void ReturnToMenu_MidEffect_RestoresEverything()
        {
            Bump(BumpPolarity.Negative);
            Run(_session.BumpImpactDelaySeconds + 0.1f);

            _session.ReturnToMenu();
            _effects.Step(Dt);

            AssertNeutral();
        }

        [Test]
        public void Win_PushesInAndArcs_ThenLevelStartRestoresTheCamera()
        {
            CameraFeedbackSettings fx = _settings.cameraFx;
            Invoke(_session, "Win");
            Run(fx.victoryStartDelaySeconds + fx.victoryMoveSeconds + 0.2f);

            Assert.IsTrue(_effects.VictoryActive);
            Assert.AreEqual(BaseFov * (1f + fx.victoryPushInFovPercent * 0.01f), _camera.fieldOfView, 0.05f);
            Assert.AreEqual(-fx.victoryCameraDropWorldUnits, _offset.localPosition.y, 1e-3f);
            Assert.Greater(_offset.localPosition.x, 0f, "the arc swings to the side");
            Assert.Less(_offset.localPosition.x, 0.25f * 36f, "a small arc");

            _session.StartLevel();
            _effects.Step(Dt);

            Assert.IsFalse(_effects.VictoryActive);
            AssertNeutral();
        }

        [Test]
        public void Win_WithTheArcDisabled_OnlyPushesIn_AndWithTheMoveDisabledDoesNothing()
        {
            _settings.cameraFx.victoryArcEnabled = false;
            Invoke(_session, "Win");
            Run(2.5f);
            Assert.AreEqual(0f, _offset.localPosition.x);
            Assert.AreEqual(Quaternion.identity, _offset.localRotation);
            Assert.Less(_camera.fieldOfView, BaseFov);

            _session.ReturnToMenu();
            _effects.Step(Dt);
            AssertNeutral();

            _settings.cameraFx.victoryCameraEnabled = false;
            _session.StartLevel();
            Invoke(_session, "Win");
            Run(2.5f);
            Assert.AreEqual(BaseFov, _camera.fieldOfView, 1e-4f);
            Assert.AreEqual(Vector3.zero, _offset.localPosition);
        }

        [Test]
        public void Win_CancelsARunningBumpEffect_BeforeTheMoveStarts()
        {
            Bump(BumpPolarity.Negative);
            Run(_session.BumpImpactDelaySeconds + 0.1f);
            Assert.IsTrue(_shake.IsShaking);

            Invoke(_session, "Win");

            Assert.IsFalse(_shake.IsShaking);
            Assert.AreEqual(0, _effects.PendingImpactCount);
            Assert.AreEqual(0f, _effects.PulseFovFraction);
        }
    }
}
