using SessionState = Game.Core.SessionState;
using System.Reflection;
using Game.Core;
using Game.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Returning from the background (OnApplicationPause(false)) auto-resumes only while the pause
    /// menu flag is off; with it on the session stays Paused until Resume() (the Continue button).
    /// The private callback is invoked by reflection, as Unity would.
    /// </summary>
    public sealed class GameSessionPauseFlagTests
    {
        private GameObject _motorGo;
        private GameObject _sessionGo;
        private TextAsset _level;
        private GameSettings _settings;
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _motorGo = new GameObject("Motor");
            _sessionGo = new GameObject("Session");
            PlayerMotor motor = _motorGo.AddComponent<PlayerMotor>();
            _level = new TextAsset("{\"levelId\":1,\"displayName\":\"Test\",\"finishHeight\":30,\"climbSpeed\":2.5,\"hazards\":[]}");
            _settings = ScriptableObject.CreateInstance<GameSettings>();
            _session = _sessionGo.AddComponent<GameSession>();
            Bind("motor", p => p.objectReferenceValue = motor);
            Bind("levelFiles", p =>
            {
                p.arraySize = 1;
                p.GetArrayElementAtIndex(0).objectReferenceValue = _level;
            });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_sessionGo);
            Object.DestroyImmediate(_motorGo);
            Object.DestroyImmediate(_level);
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void NullSettings_BackgroundThenReturn_AutoResumes()
        {
            _session.StartLevel();
            Background(true);
            Assert.AreEqual(SessionState.Paused, _session.State);

            Background(false);

            Assert.AreEqual(SessionState.Playing, _session.State);
        }

        [Test]
        public void PauseMenuFlagOff_BackgroundThenReturn_AutoResumes()
        {
            UseFeatures(pauseMenuEnabled: false);
            _session.StartLevel();

            Background(true);
            Assert.AreEqual(SessionState.Paused, _session.State);
            Background(false);

            Assert.AreEqual(SessionState.Playing, _session.State);
        }

        [Test]
        public void PauseMenuFlagOn_BackgroundThenReturn_StaysPaused_UntilResume()
        {
            UseFeatures(pauseMenuEnabled: true);
            _session.StartLevel();
            Assert.IsTrue(_session.PauseMenuEnabled);

            Background(true);
            Assert.AreEqual(SessionState.Paused, _session.State);
            Background(false);

            Assert.AreEqual(SessionState.Paused, _session.State, "with the pause menu on, return must not auto-resume");

            _session.Resume();
            Assert.AreEqual(SessionState.Playing, _session.State);
        }

        [Test]
        public void PauseMenuFlagOn_ManualPause_ThenReturnFromBackground_StaysPaused()
        {
            UseFeatures(pauseMenuEnabled: true);
            _session.StartLevel();
            _session.Pause();

            Background(false);

            Assert.AreEqual(SessionState.Paused, _session.State);
        }

        [Test]
        public void ReturnFromBackground_WhileMenu_DoesNotStartAnything()
        {
            Background(false);
            Assert.AreEqual(SessionState.Menu, _session.State);
        }

        private void Background(bool paused)
        {
            typeof(GameSession).GetMethod("OnApplicationPause", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_session, new object[] { paused });
        }

        private void UseFeatures(bool pauseMenuEnabled)
        {
            var so = new SerializedObject(_settings);
            so.FindProperty("features.pauseMenuEnabled").boolValue = pauseMenuEnabled;
            so.ApplyModifiedPropertiesWithoutUndo();
            Bind("settings", p => p.objectReferenceValue = _settings);
        }

        private void Bind(string field, System.Action<SerializedProperty> set)
        {
            var so = new SerializedObject(_session);
            SerializedProperty p = so.FindProperty(field);
            Assert.IsNotNull(p, "missing serialized field " + field);
            set(p);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
