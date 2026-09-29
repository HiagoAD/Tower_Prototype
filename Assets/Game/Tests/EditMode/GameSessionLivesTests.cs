using SessionState = Game.Core.SessionState;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using Game.Core;
using Game.Gameplay;
using Game.Webhook;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Lives feature (behind GameFeatures.livesEnabled): hazard hits cost a life only when the flag
    /// is on and the motor accepted the hit; 0 lives is Lost; Retry/StartNextLevel reset lives.
    /// Two-level campaign so StartNextLevel is reachable. The webhook test drives the real
    /// DrainBumpQueue exactly as BumpSessionTests does (loopback listener on an OS-assigned port,
    /// disposed in TearDown). Invulnerability is cleared between hits with PlayerMotor.ResetState.
    /// </summary>
    public sealed class GameSessionLivesTests
    {
        private const string LevelJson = "{\"levelId\":1,\"displayName\":\"Test\",\"finishHeight\":30,\"climbSpeed\":2.5,\"hazards\":[]}";
        private const float BandHeight = 12f;

        private GameObject _motorGo;
        private GameObject _sessionGo;
        private TextAsset _level1;
        private TextAsset _level2;
        private GameFeatures _features;
        private BumpCatalog _catalog;
        private PlayerMotor _motor;
        private GameSession _session;
        private BumpListener _listener;

        [SetUp]
        public void SetUp()
        {
            _motorGo = new GameObject("Motor");
            _sessionGo = new GameObject("Session");
            _motor = _motorGo.AddComponent<PlayerMotor>();
            _level1 = new TextAsset(LevelJson);
            _level2 = new TextAsset(LevelJson);
            _features = ScriptableObject.CreateInstance<GameFeatures>();
            _catalog = ScriptableObject.CreateInstance<BumpCatalog>();
            _catalog.types = new[] { new BumpType { id = _catalog.defaultTypeId, displayName = "Boxing", liftBodyHeights = 1f, dropBodyHeights = 1f } };

            _session = _sessionGo.AddComponent<GameSession>();
            Bind(_session, "motor", so => so.objectReferenceValue = _motor);
            Bind(_session, "levelFiles", so =>
            {
                so.arraySize = 2;
                so.GetArrayElementAtIndex(0).objectReferenceValue = _level1;
                so.GetArrayElementAtIndex(1).objectReferenceValue = _level2;
            });
            Bind(_session, "bumpCatalog", so => so.objectReferenceValue = _catalog);
            Bind(_session, "hazardBodyHeight", so => so.floatValue = 2f);
            typeof(GameSession).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_session, null);

            FieldInfo field = typeof(GameSession).GetField("_listener", BindingFlags.Instance | BindingFlags.NonPublic);
            var awakeListener = (BumpListener)field.GetValue(_session);
            _listener = new BumpListener(0) { StateProvider = awakeListener.StateProvider, KnownTypeIds = awakeListener.KnownTypeIds };
            field.SetValue(_session, _listener);
            _listener.Start();
        }

        [TearDown]
        public void TearDown()
        {
            _listener.Dispose();
            Object.DestroyImmediate(_sessionGo);
            Object.DestroyImmediate(_motorGo);
            Object.DestroyImmediate(_level1);
            Object.DestroyImmediate(_level2);
            Object.DestroyImmediate(_features);
            Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void GameFeatures_DefaultsAreOff_AndMaxLivesIsFive()
        {
            var fresh = ScriptableObject.CreateInstance<GameFeatures>();
            try
            {
                Assert.IsFalse(fresh.LivesEnabled);
                Assert.IsFalse(fresh.PauseMenuEnabled);
                Assert.AreEqual(3, fresh.StartingLives);
                Assert.AreEqual(5, GameFeatures.MaxLives);
            }
            finally
            {
                Object.DestroyImmediate(fresh);
            }
        }

        [Test]
        public void NullFeatures_MeansBothOff_AndHazardHitsNeverCostLives()
        {
            _session.StartLevel();
            _motor.ResetState(10f);

            Assert.IsFalse(_session.LivesEnabled);
            Assert.IsFalse(_session.PauseMenuEnabled);
            for (int i = 0; i < 10; i++)
            {
                _session.OnHazardHit(BandHeight);
                _motor.ResetState(10f);
            }

            Assert.AreEqual(0, _session.Lives);
            Assert.AreEqual(SessionState.Playing, _session.State);
        }

        [Test]
        public void FlagsOff_HazardHitsNeverCostLives_OrLeavePlaying()
        {
            UseFeatures(livesEnabled: false, startingLives: 3, pauseMenuEnabled: false);
            _session.StartLevel();
            _motor.ResetState(10f);

            for (int i = 0; i < 10; i++)
            {
                _session.OnHazardHit(BandHeight);
                _motor.ResetState(10f);
            }

            Assert.IsFalse(_session.LivesEnabled);
            Assert.AreEqual(0, _session.Lives);
            Assert.AreEqual(SessionState.Playing, _session.State);
        }

        [Test]
        public void LivesOn_StartLevelGivesStartingLives()
        {
            UseFeatures(true, 3, false);
            var seen = new List<int>();
            _session.LivesChanged += seen.Add;

            _session.StartLevel();

            Assert.IsTrue(_session.LivesEnabled);
            Assert.AreEqual(3, _session.Lives);
            CollectionAssert.AreEqual(new[] { 3 }, seen);
        }

        [Test]
        public void LivesOn_EachAcceptedHitCostsOne_AndLivesChangedReportsEachValue()
        {
            UseFeatures(true, 3, false);
            _session.StartLevel();
            _motor.ResetState(10f);
            var seen = new List<int>();
            _session.LivesChanged += seen.Add;

            _session.OnHazardHit(BandHeight);
            Assert.AreEqual(2, _session.Lives);
            Assert.AreEqual(SessionState.Playing, _session.State);
            _motor.ResetState(10f);

            _session.OnHazardHit(BandHeight);
            Assert.AreEqual(1, _session.Lives);
            Assert.AreEqual(SessionState.Playing, _session.State);

            CollectionAssert.AreEqual(new[] { 2, 1 }, seen);
        }

        [Test]
        public void LivesOn_HitWhileInvulnerable_IsFree()
        {
            UseFeatures(true, 3, false);
            _session.StartLevel();
            _motor.ResetState(10f);
            var seen = new List<int>();
            _session.LivesChanged += seen.Add;

            _session.OnHazardHit(BandHeight);
            Assert.IsTrue(_motor.IsInvulnerable, "test setup: the first hit must leave the motor invulnerable");
            _session.OnHazardHit(BandHeight);
            _session.OnHazardHit(BandHeight);

            Assert.AreEqual(2, _session.Lives);
            CollectionAssert.AreEqual(new[] { 2 }, seen);
        }

        [Test]
        public void LivesOn_LastLifeLost_BecomesLost_StopsClimbing_AndRejectsBumps()
        {
            UseFeatures(true, 2, false);
            _session.StartLevel();
            _motor.ResetState(10f);
            var states = new List<SessionState>();
            var seen = new List<int>();
            _session.StateChanged += states.Add;
            _session.LivesChanged += seen.Add;

            _session.OnHazardHit(BandHeight);
            _motor.ResetState(10f);
            _session.OnHazardHit(BandHeight);

            Assert.AreEqual(0, _session.Lives);
            Assert.AreEqual(SessionState.Lost, _session.State);
            CollectionAssert.AreEqual(new[] { SessionState.Lost }, states);
            CollectionAssert.AreEqual(new[] { 1, 0 }, seen);
            Assert.IsFalse(_motor.CanClimb, "a lost run must not keep climbing");

            // Further hits do nothing once lost.
            _motor.ResetState(10f);
            _session.OnHazardHit(BandHeight);
            Assert.AreEqual(0, _session.Lives);
            Assert.AreEqual(SessionState.Lost, _session.State);

            // A webhook bump is rejected: no event, no move.
            bool accepted = false;
            _session.BumpAccepted += e => accepted = true;
            float before = _motor.Height;
            string response = SendAndDrain("GET /bump?polarity=positive HTTP/1.1\r\nHost: localhost\r\n\r\n");
            Settle();
            StringAssert.DoesNotStartWith("HTTP/1.1 200", response);
            Assert.IsFalse(accepted);
            Assert.AreEqual(before, _motor.Height, 0.001f);
        }

        [Test]
        public void Retry_FromLost_ResumesPlayingOnSameLevel_WithLivesReset_AndHeightZero()
        {
            UseFeatures(true, 1, false);
            _session.StartLevel();
            _motor.ResetState(10f);
            _session.OnHazardHit(BandHeight);
            Assert.AreEqual(SessionState.Lost, _session.State);
            var seen = new List<int>();
            _session.LivesChanged += seen.Add;

            _session.Retry();

            Assert.AreEqual(SessionState.Playing, _session.State);
            Assert.AreEqual(0, _session.LevelIndex);
            Assert.AreEqual(1, _session.Lives);
            Assert.AreEqual(0f, _motor.Height);
            Assert.IsTrue(_motor.CanClimb);
            CollectionAssert.AreEqual(new[] { 1 }, seen);
        }

        [Test]
        public void Retry_MidRun_RestoresFullLives()
        {
            UseFeatures(true, 3, false);
            _session.StartLevel();
            _motor.ResetState(10f);
            _session.OnHazardHit(BandHeight);
            Assert.AreEqual(2, _session.Lives);

            _session.Retry();

            Assert.AreEqual(3, _session.Lives);
        }

        [Test]
        public void StartNextLevel_ResetsLivesToStartingLives()
        {
            UseFeatures(true, 3, false);
            _session.StartLevel();
            _motor.ResetState(10f);
            _session.OnHazardHit(BandHeight);
            Assert.AreEqual(2, _session.Lives);

            // Won by reaching the finish, as Update does (invoked directly; no player loop in EditMode).
            _motor.ResetState(_motor.FinishHeight);
            typeof(GameSession).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_session, null);
            Assert.AreEqual(SessionState.Won, _session.State, "test setup: reaching the finish must win");
            var seen = new List<int>();
            _session.LivesChanged += seen.Add;

            _session.StartNextLevel();

            Assert.AreEqual(1, _session.LevelIndex);
            Assert.AreEqual(SessionState.Playing, _session.State);
            Assert.AreEqual(3, _session.Lives);
            CollectionAssert.AreEqual(new[] { 3 }, seen);
        }

        [Test]
        public void StartingLivesIsHonoured_ForOtherValues()
        {
            UseFeatures(true, 5, false);
            _session.StartLevel();
            Assert.AreEqual(5, _session.Lives);
        }

        [Test]
        public void WebhookBump_NeverCostsALife()
        {
            UseFeatures(true, 3, false);
            _session.StartLevel();
            _motor.ResetState(10f);
            var seen = new List<int>();
            _session.LivesChanged += seen.Add;
            bool accepted = false;
            _session.BumpAccepted += e => accepted = true;

            string response = SendAndDrain("GET /bump?polarity=negative HTTP/1.1\r\nHost: localhost\r\n\r\n");
            Settle();

            StringAssert.StartsWith("HTTP/1.1 200", response);
            Assert.IsTrue(accepted, "test setup: the bump must be accepted");
            Assert.AreEqual(3, _session.Lives);
            Assert.AreEqual(SessionState.Playing, _session.State);
            Assert.IsEmpty(seen);
        }

        // ---- helpers -----------------------------------------------------------------------

        private void UseFeatures(bool livesEnabled, int startingLives, bool pauseMenuEnabled)
        {
            var so = new SerializedObject(_features);
            so.FindProperty("livesEnabled").boolValue = livesEnabled;
            so.FindProperty("startingLives").intValue = startingLives;
            so.FindProperty("pauseMenuEnabled").boolValue = pauseMenuEnabled;
            so.ApplyModifiedPropertiesWithoutUndo();
            Bind(_session, "features", p => p.objectReferenceValue = _features);
        }

        private static void Bind(Object target, string field, System.Action<SerializedProperty> set)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            Assert.IsNotNull(p, "missing serialized field " + field);
            set(p);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void Settle()
        {
            for (int i = 0; i < 60; i++)
            {
                _motor.Step(1f / 60f);
            }
        }

        private string SendAndDrain(string rawRequest)
        {
            MethodInfo drain = typeof(GameSession).GetMethod("DrainBumpQueue", BindingFlags.Instance | BindingFlags.NonPublic);
            using var client = new TcpClient();
            client.Connect(IPAddress.Loopback, _listener.Port);
            client.ReceiveTimeout = 5000;
            using NetworkStream stream = client.GetStream();
            byte[] bytes = Encoding.UTF8.GetBytes(rawRequest);
            stream.Write(bytes, 0, bytes.Length);

            var buffer = new byte[4096];
            var sb = new StringBuilder();
            System.DateTime deadline = System.DateTime.UtcNow + System.TimeSpan.FromSeconds(3);
            while (System.DateTime.UtcNow < deadline)
            {
                drain.Invoke(_session, null);
                if (stream.DataAvailable)
                {
                    break;
                }

                Thread.Sleep(5);
            }

            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                sb.Append(Encoding.UTF8.GetString(buffer, 0, read));
                if (!stream.DataAvailable)
                {
                    break;
                }
            }

            return sb.ToString();
        }
    }
}
