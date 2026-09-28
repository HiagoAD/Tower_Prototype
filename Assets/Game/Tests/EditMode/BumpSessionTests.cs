using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using Game.Core;
using Game.Gameplay;
using Game.Presentation;
using Game.Webhook;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Typed-bump behaviour through GameSession (the real drain, a real loopback listener) and the
    /// per-polarity EventFeedView filter. EditMode does not run Awake/OnEnable, so the private
    /// wiring they do is invoked by reflection.
    /// </summary>
    public sealed class BumpSessionTests
    {
        private const float BodyHeight = 2f;

        private GameObject _motorGo;
        private GameObject _sessionGo;
        private TextAsset _level;
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
            _level = new TextAsset("{\"levelId\":1,\"displayName\":\"Test\",\"finishHeight\":30,\"climbSpeed\":2.5,\"hazards\":[]}");

            _catalog = ScriptableObject.CreateInstance<BumpCatalog>();
            _catalog.defaultPolarity = BumpPolarity.Negative;
            _catalog.defaultTypeId = "boxing";
            _catalog.fallbackTag = "Guest";
            _catalog.types = new[]
            {
                new BumpType { id = "boxing", displayName = "Boxing", liftBodyHeights = 1f, dropBodyHeights = 1f },
                new BumpType { id = "heavy", displayName = "Heavy", liftBodyHeights = 2f, dropBodyHeights = 1.5f },
            };

            _session = _sessionGo.AddComponent<GameSession>();
            SetObjectReference(_session, "motor", _motor);
            SetArray(new SerializedObject(_session), "levelFiles", _level);
            SetObjectReference(_session, "bumpCatalog", _catalog);
            SetFloat(_session, "hazardBodyHeight", BodyHeight);
            typeof(GameSession).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_session, null);

            // Swap the fixed-port listener Awake made for an OS-assigned one with the same wiring.
            FieldInfo field = typeof(GameSession).GetField("_listener", BindingFlags.Instance | BindingFlags.NonPublic);
            var awakeListener = (BumpListener)field.GetValue(_session);
            _listener = new BumpListener(0) { StateProvider = awakeListener.StateProvider, KnownTypeIds = awakeListener.KnownTypeIds };
            field.SetValue(_session, _listener);
            _listener.Start();

            _session.StartLevel();
            _motor.ResetState(10f);
        }

        [TearDown]
        public void TearDown()
        {
            _listener.Dispose();
            UnityEngine.Object.DestroyImmediate(_sessionGo);
            UnityEngine.Object.DestroyImmediate(_motorGo);
            UnityEngine.Object.DestroyImmediate(_level);
            UnityEngine.Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void PositiveBump_RaisesHeight_AndRaisesEventWithPositivePolarity()
        {
            BumpEvent? received = null;
            _session.BumpAccepted += e => received = e;
            float start = _motor.Height;

            string response = SendAndDrain("GET /bump?polarity=positive&type=heavy&tag=Ana HTTP/1.1\r\nHost: localhost\r\n\r\n");
            Settle();

            StringAssert.StartsWith("HTTP/1.1 200", response);
            StringAssert.Contains("\"polarity\":\"positive\"", response);
            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(BumpPolarity.Positive, received.Value.Polarity);
            Assert.AreEqual("heavy", received.Value.Type.id);
            Assert.AreEqual("Ana", received.Value.Tag);
            Assert.AreEqual(start + 2f * BodyHeight, _motor.Height, 0.001f, "two body heights, in world units");
            Assert.AreEqual(Game.Core.SessionState.Playing, _session.State, "a bump must leave the session playable");
        }

        [Test]
        public void MissingFields_ResolveToCatalogDefaults()
        {
            BumpEvent? received = null;
            _session.BumpAccepted += e => received = e;
            float start = _motor.Height;

            string response = SendAndDrain("POST /bump HTTP/1.1\r\nHost: localhost\r\nContent-Length: 0\r\n\r\n");
            Settle();

            StringAssert.Contains("\"polarity\":\"negative\"", response);
            StringAssert.Contains("\"type\":\"boxing\"", response);
            Assert.AreEqual(BumpPolarity.Negative, received.Value.Polarity);
            Assert.AreEqual("boxing", received.Value.Type.id);
            Assert.AreEqual("Guest", received.Value.Tag);
            Assert.AreEqual(start - 1f * BodyHeight, _motor.Height, 0.001f);
        }

        [Test]
        public void EventFeedView_IgnoresBumpsOfTheOtherPolarity()
        {
            var root = new GameObject("Feed");
            try
            {
                var feed = root.AddComponent<EventFeedView>();
                var groups = new CanvasGroup[3];
                var senders = new Text[3];
                var details = new Text[3];
                for (int i = 0; i < 3; i++)
                {
                    var card = new GameObject("Card" + i, typeof(CanvasGroup));
                    card.transform.SetParent(root.transform);
                    groups[i] = card.GetComponent<CanvasGroup>();
                    senders[i] = new GameObject("S" + i, typeof(Text)).GetComponent<Text>();
                    details[i] = new GameObject("D" + i, typeof(Text)).GetComponent<Text>();
                    senders[i].transform.SetParent(card.transform);
                    details[i].transform.SetParent(card.transform);
                }

                var so = new SerializedObject(feed);
                so.FindProperty("polarity").enumValueIndex = (int)BumpPolarity.Positive;
                SetArray(so.FindProperty("cards"), groups);
                SetArray(so.FindProperty("senderTexts"), senders);
                SetArray(so.FindProperty("detailTexts"), details);
                so.ApplyModifiedPropertiesWithoutUndo();
                MethodInfo awake = typeof(EventFeedView).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo onBump = typeof(EventFeedView).GetMethod("OnBumpAccepted", BindingFlags.Instance | BindingFlags.NonPublic);
                awake.Invoke(feed, null);

                var type = new BumpType { id = "boxing", displayName = "Boxing" };
                onBump.Invoke(feed, new object[] { new BumpEvent("r1", BumpPolarity.Negative, type, "Neg") });
                Assert.AreEqual(string.Empty, senders[0].text, "a negative bump must not appear on the positive column");

                onBump.Invoke(feed, new object[] { new BumpEvent("r2", BumpPolarity.Positive, type, "Pos") });
                Assert.AreEqual("Pos", senders[0].text);
                Assert.AreEqual("Boxing*1", details[0].text);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EventFeedView_TypeWithoutIcon_ClearsTheBadgeInsteadOfKeepingThePreviousOne()
        {
            var root = new GameObject("Feed");
            try
            {
                var feed = root.AddComponent<EventFeedView>();
                var card = new GameObject("Card", typeof(CanvasGroup));
                card.transform.SetParent(root.transform);
                var sender = new GameObject("S", typeof(Text)).GetComponent<Text>();
                var detail = new GameObject("D", typeof(Text)).GetComponent<Text>();
                var badge = new GameObject("B", typeof(Image)).GetComponent<Image>();
                var so = new SerializedObject(feed);
                so.FindProperty("polarity").enumValueIndex = (int)BumpPolarity.Negative;
                SetArray(so.FindProperty("cards"), new UnityEngine.Object[] { card.GetComponent<CanvasGroup>() });
                SetArray(so.FindProperty("senderTexts"), new UnityEngine.Object[] { sender });
                SetArray(so.FindProperty("detailTexts"), new UnityEngine.Object[] { detail });
                SetArray(so.FindProperty("badgeIcons"), new UnityEngine.Object[] { badge });
                so.ApplyModifiedPropertiesWithoutUndo();
                typeof(EventFeedView).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(feed, null);
                MethodInfo onBump = typeof(EventFeedView).GetMethod("OnBumpAccepted", BindingFlags.Instance | BindingFlags.NonPublic);

                var tex = new Texture2D(2, 2);
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), Vector2.zero);
                onBump.Invoke(feed, new object[] { new BumpEvent("r1", BumpPolarity.Negative, new BumpType { id = "a", displayName = "A", icon = sprite }, "T") });
                Assert.AreSame(sprite, badge.sprite);
                Assert.IsTrue(badge.enabled);

                onBump.Invoke(feed, new object[] { new BumpEvent("r2", BumpPolarity.Negative, new BumpType { id = "b", displayName = "B" }, "T") });
                Assert.IsNull(badge.sprite);
                Assert.IsFalse(badge.enabled);
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(tex);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AcceptedBump_WaitsForTheImpactDelay_ThenMovesExactlyOneBodyHeight()
        {
            SetFloat(_session, "bumpImpactDelaySeconds", 0.275f);
            Assert.AreEqual(0.275f, _session.BumpImpactDelaySeconds, 0.0001f);
            float start = _motor.Height;

            SendAndDrain("GET /bump?polarity=positive&type=boxing HTTP/1.1\r\nHost: localhost\r\n\r\n");

            for (int i = 0; i < 12; i++)
            {
                _motor.Step(1f / 60f); // 0.2 s: the gloves have not landed yet.
            }

            Assert.AreEqual(start, _motor.Height, "nothing moves before the impact");
            Assert.IsFalse(_motor.IsInKnockback);

            Settle();
            Assert.AreEqual(start + BodyHeight, _motor.Height, 0.001f);
        }

        [Test]
        public void DefaultBodyHeights_UseTheSceneBoundBodyHeight()
        {
            Assert.AreEqual(BodyHeight, _session.BodyHeight, 0.0001f);
        }

        [Test]
        public void ReturnToMenu_ResetsTheClimberToTheBase()
        {
            _motor.ResetState(20f);

            _session.ReturnToMenu();

            Assert.AreEqual(0f, _motor.Height);
            Assert.AreEqual(Game.Core.SessionState.Menu, _session.State);
        }

        [Test]
        public void ApplicationPause_PausesAPlayingSession_AndResumesOnReturn()
        {
            MethodInfo onPause = typeof(GameSession).GetMethod("OnApplicationPause", BindingFlags.Instance | BindingFlags.NonPublic);

            onPause.Invoke(_session, new object[] { true });
            Assert.AreEqual(Game.Core.SessionState.Paused, _session.State);

            onPause.Invoke(_session, new object[] { false });
            Assert.AreEqual(Game.Core.SessionState.Playing, _session.State, "no pause UI, so returning resumes");
        }

        [Test]
        public void StartLevel_DropsABumpStillWaitingForItsImpact()
        {
            _motor.ApplyBump(3f, 0.275f);

            _session.StartLevel();
            for (int i = 0; i < 120; i++)
            {
                _motor.Step(1f / 60f);
            }

            Assert.AreEqual(0f, _motor.Height);
            Assert.AreEqual(0, _motor.PendingBumpCount);
        }

        private void Settle()
        {
            for (int i = 0; i < 60; i++)
            {
                _motor.Step(1f / 60f);
            }
        }

        /// <summary>Sends the request from a client while this thread plays Update: draining the queue via the session's real DrainBumpQueue.</summary>
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
            DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(3);
            while (DateTime.UtcNow < deadline)
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

        private static void SetArray(SerializedObject so, string fieldName, UnityEngine.Object value)
        {
            SerializedProperty property = so.FindProperty(fieldName);
            Assert.IsNotNull(property, "missing serialized field " + fieldName);
            SetArray(property, new[] { value });
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(SerializedProperty property, UnityEngine.Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void SetFloat(UnityEngine.Object target, string fieldName, float value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(fieldName);
            Assert.IsNotNull(property, "missing serialized field " + fieldName);
            property.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectReference(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(fieldName);
            Assert.IsNotNull(property, "missing serialized field " + fieldName);
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
