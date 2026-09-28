using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Game.Core;
using Game.Gameplay;
using Game.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// G2 follow-up: shared plumbing for the PlayMode tests. Loads the real built Level1 scene and
    /// exposes it through the same public surface a player or the webhook uses; nothing here
    /// invokes a private lifecycle method. Reflection is used only to READ serialized wiring.
    /// </summary>
    internal sealed class Level1Harness
    {
        public const string ScenePath = "Assets/Game/Scenes/Level1.unity";
        public const string SceneName = "Level1";
        public const int Port = 56789;

        public GameSession Session { get; private set; }
        public PlayerMotor Motor { get; private set; }
        public BumpBurstView Burst { get; private set; }
        public CameraShake Shake { get; private set; }
        public RectTransform BurstRoot { get; private set; }
        public Image Flash { get; private set; }
        public Scene Scene { get; private set; }

        /// <summary>Loads Level1 (replacing whatever scene is loaded, which releases the listener port) and finds the components.</summary>
        public IEnumerator Load()
        {
#if UNITY_EDITOR
            AsyncOperation op = EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            AsyncOperation op = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
#endif
            Assert.IsNotNull(op, "Level1 failed to start loading (is it in build settings?)");
            yield return op;
            yield return null;
            yield return null;

            Scene = SceneManager.GetSceneByName(SceneName);
            Assert.IsTrue(Scene.isLoaded, "Level1 is not loaded");

            Session = UnityEngine.Object.FindFirstObjectByType<GameSession>();
            Motor = UnityEngine.Object.FindFirstObjectByType<PlayerMotor>();
            Burst = UnityEngine.Object.FindFirstObjectByType<BumpBurstView>();
            Shake = UnityEngine.Object.FindFirstObjectByType<CameraShake>();
            Assert.IsNotNull(Session, "GameSession missing from Level1");
            Assert.IsNotNull(Motor, "PlayerMotor missing from Level1");
            Assert.IsNotNull(Burst, "BumpBurstView missing from Level1");
            Assert.IsNotNull(Shake, "CameraShake missing from Level1");

            BurstRoot = ReadField<RectTransform>(Burst, "burstRoot");
            Flash = ReadField<Image>(Burst, "flashImage");
            Assert.IsNotNull(BurstRoot, "BumpBurstView.burstRoot unbound");
            Assert.IsNotNull(Flash, "BumpBurstView.flashImage unbound");
        }

        /// <summary>Swaps in an empty scene and unloads Level1 so its GameSession stops listening, then gives the OS a moment to free the port.</summary>
        public IEnumerator Unload()
        {
            if (Scene.IsValid() && Scene.isLoaded)
            {
                Scene empty = SceneManager.CreateScene("PlayModeEmpty");
                SceneManager.SetActiveScene(empty);
                AsyncOperation op = SceneManager.UnloadSceneAsync(Scene);
                if (op != null)
                {
                    yield return op;
                }
            }

            yield return null;
            float until = Time.realtimeSinceStartup + 0.15f;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }

        /// <summary>Stops the touch/keyboard source from overwriting ClimbHeld, so a test can drive climbing through the public motor member.</summary>
        public void TakeOverClimbInput()
        {
            foreach (ClimbInputSource source in UnityEngine.Object.FindObjectsByType<ClimbInputSource>(FindObjectsSortMode.None))
            {
                source.enabled = false;
            }

            Motor.ClimbHeld = false;
        }

        public GameObject FindGameObject(string name)
        {
            foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == name && t.gameObject.scene == Scene)
                {
                    return t.gameObject;
                }
            }

            Assert.Fail("No GameObject named '" + name + "' in Level1");
            return null;
        }

        /// <summary>The Button called buttonName under the panel called panelName (names as built by Level1SceneSetup).</summary>
        public Button FindButton(string panelName, string buttonName)
        {
            Transform panel = FindGameObject(panelName).transform;
            foreach (Button b in panel.GetComponentsInChildren<Button>(true))
            {
                if (b.name == buttonName)
                {
                    return b;
                }
            }

            Assert.Fail("No button '" + buttonName + "' under '" + panelName + "'");
            return null;
        }

        /// <summary>Menu -> Playing through the real Start button.</summary>
        public IEnumerator PressStart()
        {
            FindButton("MainMenuPanel", "StartButton").onClick.Invoke();
            yield return null;
            Assert.AreEqual(SessionState.Playing, Session.State);
        }

        // ---- burst inspection -------------------------------------------------------------

        public List<RectTransform> BurstElements()
        {
            var result = new List<RectTransform>();
            foreach (Transform child in BurstRoot)
            {
                if (IsBurstElementName(child.name))
                {
                    result.Add((RectTransform)child);
                }
            }

            return result;
        }

        private static bool IsBurstElementName(string n)
        {
            return n == "ImpactGlow" || IsIndexed(n, "Glove") || IsIndexed(n, "Star");
        }

        private static bool IsIndexed(string n, string prefix)
        {
            return n.Length > prefix.Length && n.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(n.Substring(prefix.Length), out _);
        }

        public List<RectTransform> Elements(string prefix)
        {
            var result = new List<RectTransform>();
            foreach (RectTransform r in BurstElements())
            {
                if (r.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    result.Add(r);
                }
            }

            return result;
        }

        public bool AnyActive(string prefix)
        {
            foreach (RectTransform r in Elements(prefix))
            {
                if (r.gameObject.activeSelf)
                {
                    return true;
                }
            }

            return false;
        }

        public bool AnyBurstElementActive()
        {
            foreach (RectTransform r in BurstElements())
            {
                if (r.gameObject.activeSelf)
                {
                    return true;
                }
            }

            return false;
        }

        public void AssertBurstHidden(string context)
        {
            List<RectTransform> elements = BurstElements();
            Assert.Greater(elements.Count, 0, "no burst elements found (" + context + ")");
            foreach (RectTransform r in elements)
            {
                Assert.IsFalse(r.gameObject.activeSelf, r.name + " still active (" + context + ")");
            }

            Assert.AreEqual(0f, Flash.color.a, 1e-4f, "flash alpha not reset (" + context + ")");
        }

        /// <summary>Every element's active flag and rect state plus flash alpha and shake offset, as one comparable string.</summary>
        public string BurstSnapshot()
        {
            var sb = new StringBuilder();
            foreach (RectTransform r in BurstElements())
            {
                sb.Append(r.name).Append(r.gameObject.activeSelf ? "+" : "-")
                  .Append(r.anchoredPosition.ToString("F5")).Append(r.localRotation.ToString("F5")).Append(r.localScale.ToString("F5"))
                  .Append(r.GetComponent<Image>() != null ? r.GetComponent<Image>().color.a.ToString("F5") : "").Append('|');
            }

            sb.Append("flash=").Append(Flash.color.a.ToString("F5"));
            sb.Append(";shake=").Append(Shake.transform.localPosition.ToString("F5"));
            return sb.ToString();
        }

        // ---- HTTP -------------------------------------------------------------------------

        /// <summary>POSTs /bump from a worker thread (the main thread must keep running to answer it) and returns the HTTP status code.</summary>
        public IEnumerator PostBump(Action<int> onStatus, string jsonBody = null)
        {
            Task<int> task = Task.Run(() => PostBumpBlocking(jsonBody));
            float until = Time.realtimeSinceStartup + 10f;
            while (!task.IsCompleted)
            {
                if (Time.realtimeSinceStartup > until)
                {
                    Assert.Fail("HTTP /bump did not answer within 10s");
                }

                yield return null;
            }

            if (task.IsFaulted)
            {
                Assert.Fail("HTTP /bump failed: " + task.Exception?.GetBaseException().Message);
            }

            onStatus(task.Result);
        }

        private static int PostBumpBlocking(string jsonBody)
        {
            byte[] body = jsonBody == null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(jsonBody);
            string head = "POST /bump HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n"
                + (jsonBody != null ? "Content-Type: application/json\r\n" : string.Empty)
                + "Content-Length: " + body.Length + "\r\n\r\n";

            using var client = new TcpClient();
            client.ReceiveTimeout = 8000;
            client.SendTimeout = 8000;
            client.Connect(IPAddress.Loopback, Port);
            using NetworkStream stream = client.GetStream();
            byte[] headBytes = Encoding.ASCII.GetBytes(head);
            stream.Write(headBytes, 0, headBytes.Length);
            if (body.Length > 0)
            {
                stream.Write(body, 0, body.Length);
            }

            var buffer = new byte[512];
            var sb = new StringBuilder();
            while (sb.ToString().IndexOf("\r\n", StringComparison.Ordinal) < 0)
            {
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    break;
                }

                sb.Append(Encoding.ASCII.GetString(buffer, 0, read));
            }

            string statusLine = sb.ToString();
            string[] parts = statusLine.Split(' ');
            if (parts.Length < 2 || !int.TryParse(parts[1], out int status))
            {
                throw new InvalidOperationException("Unparseable HTTP status line: '" + statusLine + "'");
            }

            return status;
        }

        // ---- waiting ----------------------------------------------------------------------

        /// <summary>Yields frames until condition holds; fails the test after timeoutSeconds of real time.</summary>
        public static IEnumerator Until(Func<bool> condition, float timeoutSeconds, string what)
        {
            float until = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > until)
                {
                    Assert.Fail("Timed out after " + timeoutSeconds + "s waiting for: " + what);
                }

                yield return null;
            }
        }

        public static IEnumerator RealSeconds(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }

        public static T ReadField<T>(object target, string name) where T : class
        {
            for (Type t = target.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (f != null)
                {
                    return f.GetValue(target) as T;
                }
            }

            Assert.Fail(target.GetType().Name + " has no field '" + name + "'");
            return null;
        }

        /// <summary>
        /// Plays until the burst is well under way: positive or default bump accepted (HTTP 200),
        /// gloves visible and the post-impact stars showing, camera shake displaced.
        /// </summary>
        public IEnumerator StartVisibleBurst(string jsonBody = null)
        {
            int status = 0;
            yield return PostBump(s => status = s, jsonBody);
            Assert.AreEqual(200, status, "bump not accepted");
            yield return Until(() => AnyActive("Star") && Shake.transform.localPosition != Vector3.zero, 3f, "burst stars and camera shake visible");
        }
    }
}
