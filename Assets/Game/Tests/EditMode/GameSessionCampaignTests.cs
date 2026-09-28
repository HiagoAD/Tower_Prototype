using System;
using System.Collections.Concurrent;
using System.Reflection;
using Game.Core;
using Game.Gameplay;
using Game.Webhook;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// The campaign: level order, advancing after a win, and what a level change must not carry over.
    /// Like the other session tests, EditMode runs no Awake/Update, so Awake is invoked by reflection
    /// (the listener is never started, so no port is bound) and a win is forced by placing the climber
    /// at the finish and running Update once.
    /// </summary>
    public sealed class GameSessionCampaignTests
    {
        private const string LevelJson = "{{\"levelId\":{0},\"displayName\":\"Level {0}\",\"finishHeight\":{1},\"climbSpeed\":2.5,\"hazards\":[]}}";

        private GameObject _motorGo;
        private GameObject _sessionGo;
        private TextAsset[] _files;
        private PlayerMotor _motor;
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _motorGo = new GameObject("Motor");
            _sessionGo = new GameObject("Session");
            _motor = _motorGo.AddComponent<PlayerMotor>();
            _files = new[]
            {
                new TextAsset(string.Format(LevelJson, 1, 30)),
                new TextAsset(string.Format(LevelJson, 2, 40)),
                new TextAsset(string.Format(LevelJson, 3, 50)),
            };
            _session = _sessionGo.AddComponent<GameSession>();
            SetField(_session, "motor", _motor, _files);
            typeof(GameSession).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_session, null);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_sessionGo);
            UnityEngine.Object.DestroyImmediate(_motorGo);
            foreach (TextAsset file in _files)
            {
                UnityEngine.Object.DestroyImmediate(file);
            }
        }

        [Test]
        public void StartCampaign_BeginsAtTheFirstLevel()
        {
            _session.StartCampaign();

            Assert.AreEqual(0, _session.LevelIndex);
            Assert.AreEqual(3, _session.LevelCount);
            Assert.AreEqual(Game.Core.SessionState.Playing, _session.State);
            Assert.AreEqual(30f, _motor.FinishHeight, 1e-4f);
            Assert.IsFalse(_session.IsFinalLevel);
        }

        [Test]
        public void StartNextLevel_AfterAWin_StartsTheNextLevelAndRaisesLevelStarted()
        {
            _session.StartCampaign();
            Win();
            int started = 0;
            _session.LevelStarted += () => started++;

            _session.StartNextLevel();

            Assert.AreEqual(1, _session.LevelIndex);
            Assert.AreEqual("Level 2", _session.CurrentLevel.displayName);
            Assert.AreEqual(Game.Core.SessionState.Playing, _session.State);
            Assert.AreEqual(40f, _motor.FinishHeight, 1e-4f, "the second level's finish height");
            Assert.AreEqual(0f, _motor.Height);
            Assert.AreEqual(1, started);
        }

        [Test]
        public void StartNextLevel_WhilePlaying_DoesNothing()
        {
            _session.StartCampaign();
            int started = 0;
            _session.LevelStarted += () => started++;

            _session.StartNextLevel();

            Assert.AreEqual(0, _session.LevelIndex);
            Assert.AreEqual(0, started);
            Assert.AreEqual(Game.Core.SessionState.Playing, _session.State);
        }

        [Test]
        public void StartNextLevel_OnTheFinalLevel_DoesNothing()
        {
            _session.StartCampaign();
            Win();
            _session.StartNextLevel();
            Win();
            _session.StartNextLevel();
            Assert.IsTrue(_session.IsFinalLevel);
            Win();

            _session.StartNextLevel();

            Assert.AreEqual(2, _session.LevelIndex);
            Assert.AreEqual(Game.Core.SessionState.Won, _session.State, "the campaign is complete; the view returns to the menu");
        }

        [Test]
        public void ReturnToMenu_ThenStartCampaign_RestartsFromTheFirstLevel()
        {
            _session.StartCampaign();
            Win();
            _session.StartNextLevel();

            _session.ReturnToMenu();
            _session.StartCampaign();

            Assert.AreEqual(0, _session.LevelIndex);
            Assert.AreEqual(30f, _motor.FinishHeight, 1e-4f);
        }

        [Test]
        public void Retry_ReplaysTheCurrentLevelNotTheFirst()
        {
            _session.StartCampaign();
            Win();
            _session.StartNextLevel();

            _session.Retry();

            Assert.AreEqual(1, _session.LevelIndex);
            Assert.AreEqual(40f, _motor.FinishHeight, 1e-4f);
        }

        [Test]
        public void BumpQueuedDuringTheFirstLevel_IsRejectedOnceTheNextLevelHasStarted()
        {
            _session.StartCampaign();
            // Queued after the win (Win's Update would otherwise drain it while level 1 still plays),
            // but still carrying level 1's instance id.
            Win();
            var staleRequest = new BumpRequest("stale", "GET", DateTime.UtcNow, LevelInstanceId());
            Enqueue(staleRequest);
            _session.StartNextLevel();
            _motor.ResetState(10f);

            Drain();

            Assert.AreEqual(BumpRequestState.Rejected, staleRequest.State);
            Assert.AreEqual(0, _motor.PendingBumpCount);
        }

        [Test]
        public void EmptyCampaign_FailsLoudlyOnStart()
        {
            SetField(_session, "motor", _motor, new TextAsset[0]);

            Assert.AreEqual(0, _session.LevelCount);
            Assert.Throws<InvalidOperationException>(() => _session.StartCampaign());
            Assert.AreEqual(Game.Core.SessionState.Menu, _session.State);
        }

        [Test]
        public void MalformedLevelFile_ThrowsFormatExceptionFromStart()
        {
            var bad = new TextAsset("not json");
            try
            {
                SetField(_session, "motor", _motor, new[] { bad });

                Assert.Throws<FormatException>(() => _session.StartCampaign());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bad);
            }
        }

        // Forces the win the way play does: the climber at the finish, then one Update.
        private void Win()
        {
            _motor.ResetState(_motor.FinishHeight);
            typeof(GameSession).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_session, null);
            Assert.AreEqual(Game.Core.SessionState.Won, _session.State, "test setup must reach the summit");
        }

        private void Drain()
        {
            typeof(GameSession).GetMethod("DrainBumpQueue", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_session, null);
        }

        private int LevelInstanceId()
        {
            return (int)typeof(GameSession).GetField("_levelInstanceId", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_session);
        }

        // Injects straight into the listener's queue: what a worker thread would have queued mid-level.
        private void Enqueue(BumpRequest request)
        {
            var listener = (BumpListener)typeof(GameSession).GetField("_listener", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_session);
            var queue = (ConcurrentQueue<BumpRequest>)typeof(BumpListener).GetField("_queue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(listener);
            queue.Enqueue(request);
        }

        private static void SetField(UnityEngine.Object target, string motorField, UnityEngine.Object motor, TextAsset[] files)
        {
            var so = new SerializedObject(target);
            so.FindProperty(motorField).objectReferenceValue = motor;
            SerializedProperty levels = so.FindProperty("levelFiles");
            levels.arraySize = files.Length;
            for (int i = 0; i < files.Length; i++)
            {
                levels.GetArrayElementAtIndex(i).objectReferenceValue = files[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
