using System.Collections;
using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// Lives and pause-menu features against the real Level1 scene (names as built by Level1Ui/Level1Hud).
    /// The shipped flags are asserted from the asset the scene is bound to; every other test binds a throwaway
    /// in-memory GameFeatures through Level1Harness.UseFeatures, never the on-disk asset.
    /// </summary>
    public sealed class Level1FeatureTests : Level1PlayModeTestBase
    {
        private const float BandHeight = 12f;

        [UnityTest]
        public IEnumerator Scene_HasFeaturePanelsButtonsAndHearts_BoundToTheSession()
        {
            var features = Level1Harness.ReadField<GameFeatures>(H.Session, "features");
            Assert.IsNotNull(features, "GameSession.features is unbound");

            AssertButtonCalls(H.FindGameObject("PauseButton").GetComponent<Button>(), "Pause");
            AssertButtonCalls(H.FindButton("PausePanel", "ResumeButton"), "Resume");
            AssertButtonCalls(H.FindButton("PausePanel", "MenuButton"), "ReturnToMenu");
            AssertButtonCalls(H.FindButton("LosePanel", "RetryButton"), "Retry");
            AssertButtonCalls(H.FindButton("LosePanel", "MenuButton"), "ReturnToMenu");

            Assert.IsTrue(H.FindGameObject("PauseButton").transform.IsChildOf(H.FindGameObject("HudPanel").transform), "PauseButton must live under HudPanel");
            Transform lives = H.FindGameObject("Lives").transform;
            Assert.IsTrue(lives.IsChildOf(H.FindGameObject("HudPanel").transform), "Lives row must live under HudPanel");
            Assert.AreEqual(GameFeatures.MaxLives, HeartCount(), "one heart per possible life");

            var hud = Object.FindFirstObjectByType<Game.Presentation.HudView>(FindObjectsInactive.Include);
            Assert.AreSame(H.FindGameObject("Lives"), Level1Harness.ReadField<GameObject>(hud, "livesRoot"));
            Assert.AreSame(H.FindGameObject("PauseButton"), Level1Harness.ReadField<GameObject>(hud, "pauseButton"));
            var icons = Level1Harness.ReadField<GameObject[]>(hud, "lifeIcons");
            Assert.AreEqual(GameFeatures.MaxLives, icons.Length);

            var menu = Object.FindFirstObjectByType<Game.Presentation.MenuView>(FindObjectsInactive.Include);
            Assert.AreSame(H.FindGameObject("PausePanel"), Level1Harness.ReadField<GameObject>(menu, "pausePanel"));
            Assert.AreSame(H.FindGameObject("LosePanel"), Level1Harness.ReadField<GameObject>(menu, "losePanel"));
            yield break;
        }

        [UnityTest]
        public IEnumerator ShippedFlags_AreBothOn()
        {
            var features = Level1Harness.ReadField<GameFeatures>(H.Session, "features");
            Assert.IsNotNull(features, "GameSession.features is unbound");
            Assert.IsTrue(features.LivesEnabled, "shipped GameFeatures.asset must have lives on");
            Assert.IsTrue(features.PauseMenuEnabled, "shipped GameFeatures.asset must have the pause menu on");
            Assert.IsTrue(H.Session.LivesEnabled);
            Assert.IsTrue(H.Session.PauseMenuEnabled);
            yield break;
        }

        [UnityTest]
        public IEnumerator FlagsOff_PauseButtonAndHeartsHidden_PanelsHidden_HazardHitsAreFree()
        {
            H.UseFeatures(livesEnabled: false, startingLives: 3, pauseMenuEnabled: false);
            yield return H.PressStart();
            H.TakeOverClimbInput();

            Assert.IsFalse(H.FindGameObject("PauseButton").activeInHierarchy, "pause button must be hidden while the flag is off");
            Assert.IsFalse(H.FindGameObject("Lives").activeInHierarchy, "hearts must be hidden while lives are off");
            Assert.IsFalse(H.FindGameObject("PausePanel").activeSelf);
            Assert.IsFalse(H.FindGameObject("LosePanel").activeSelf);

            for (int i = 0; i < 6; i++)
            {
                H.Motor.ResetState(10f);
                H.Session.OnHazardHit(BandHeight);
            }

            yield return null;
            Assert.AreEqual(SessionState.Playing, H.Session.State);
            Assert.IsFalse(H.FindGameObject("LosePanel").activeSelf);
        }

        [UnityTest]
        public IEnumerator FlagsOff_BackgroundThenReturn_AutoResumes_WithNoPausePanel()
        {
            H.UseFeatures(livesEnabled: false, startingLives: 3, pauseMenuEnabled: false);
            yield return H.PressStart();

            H.Session.SendMessage("OnApplicationPause", true);
            yield return null;
            Assert.AreEqual(SessionState.Paused, H.Session.State);
            Assert.IsFalse(H.FindGameObject("PausePanel").activeSelf, "no pause panel while the flag is off");

            H.Session.SendMessage("OnApplicationPause", false);
            yield return null;
            Assert.AreEqual(SessionState.Playing, H.Session.State);
            Assert.IsTrue(H.FindGameObject("HudPanel").activeSelf);
        }

        [UnityTest]
        public IEnumerator PauseMenuOn_ButtonPauses_ContinueResumes_ExitReturnsToMenu()
        {
            H.UseFeatures(livesEnabled: false, startingLives: 3, pauseMenuEnabled: true);
            yield return H.PressStart();

            Assert.IsTrue(H.FindGameObject("PauseButton").activeInHierarchy, "pause button must show with the flag on");
            Assert.IsFalse(H.FindGameObject("Lives").activeInHierarchy, "hearts stay hidden with lives off");

            H.FindGameObject("PauseButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.AreEqual(SessionState.Paused, H.Session.State);
            Assert.IsTrue(H.FindGameObject("PausePanel").activeSelf);

            H.FindButton("PausePanel", "ResumeButton").onClick.Invoke();
            yield return null;
            Assert.AreEqual(SessionState.Playing, H.Session.State);
            Assert.IsFalse(H.FindGameObject("PausePanel").activeSelf);

            H.FindGameObject("PauseButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            H.FindButton("PausePanel", "MenuButton").onClick.Invoke();
            yield return null;
            Assert.AreEqual(SessionState.Menu, H.Session.State);
            Assert.IsFalse(H.FindGameObject("PausePanel").activeSelf);
            Assert.IsTrue(H.FindGameObject("MainMenuPanel").activeSelf);
        }

        [UnityTest]
        public IEnumerator PauseMenuOn_BackgroundThenReturn_StaysPausedWithPanel()
        {
            H.UseFeatures(livesEnabled: false, startingLives: 3, pauseMenuEnabled: true);
            yield return H.PressStart();

            H.Session.SendMessage("OnApplicationPause", true);
            H.Session.SendMessage("OnApplicationPause", false);
            yield return null;

            Assert.AreEqual(SessionState.Paused, H.Session.State);
            Assert.IsTrue(H.FindGameObject("PausePanel").activeSelf);
        }

        [UnityTest]
        public IEnumerator LivesOn_HeartsTrackLives_LoseShowsPanel_ContinueRetries_ExitLeaves()
        {
            H.UseFeatures(livesEnabled: true, startingLives: 3, pauseMenuEnabled: false);
            yield return H.PressStart();
            H.TakeOverClimbInput();

            Assert.IsTrue(H.FindGameObject("Lives").activeInHierarchy, "hearts must show with lives on");
            Assert.IsFalse(H.FindGameObject("PauseButton").activeInHierarchy, "pause button stays hidden with the pause flag off");
            Assert.AreEqual(3, VisibleHearts());

            H.Motor.ResetState(10f);
            H.Session.OnHazardHit(BandHeight);
            yield return null;
            Assert.AreEqual(2, VisibleHearts());

            H.Motor.ResetState(10f);
            H.Session.OnHazardHit(BandHeight);
            H.Motor.ResetState(10f);
            H.Session.OnHazardHit(BandHeight);
            yield return null;

            Assert.AreEqual(SessionState.Lost, H.Session.State);
            Assert.IsTrue(H.FindGameObject("LosePanel").activeSelf, "GAME OVER panel must show when lost");

            H.FindButton("LosePanel", "RetryButton").onClick.Invoke();
            yield return null;
            Assert.AreEqual(SessionState.Playing, H.Session.State);
            Assert.AreEqual(3, VisibleHearts());
            Assert.AreEqual(0f, H.Motor.Height, 1e-4f);
            Assert.IsFalse(H.FindGameObject("LosePanel").activeSelf);

            H.Motor.ResetState(10f);
            H.Session.OnHazardHit(BandHeight);
            H.Motor.ResetState(10f);
            H.Session.OnHazardHit(BandHeight);
            H.Motor.ResetState(10f);
            H.Session.OnHazardHit(BandHeight);
            yield return null;
            Assert.AreEqual(SessionState.Lost, H.Session.State);

            H.FindButton("LosePanel", "MenuButton").onClick.Invoke();
            yield return null;
            Assert.AreEqual(SessionState.Menu, H.Session.State);
            Assert.IsFalse(H.FindGameObject("LosePanel").activeSelf);
        }

        [UnityTest]
        public IEnumerator LivesOn_HttpBump_NeverCostsALife()
        {
            H.UseFeatures(livesEnabled: true, startingLives: 3, pauseMenuEnabled: false);
            yield return H.PressStart();
            H.TakeOverClimbInput();

            int status = 0;
            yield return H.PostBump(s => status = s, "{\"polarity\":\"negative\"}");
            yield return null;

            Assert.AreEqual(200, status);
            Assert.AreEqual(3, H.Session.Lives);
            Assert.AreEqual(3, VisibleHearts());
            Assert.AreEqual(SessionState.Playing, H.Session.State);
        }

        // ---- helpers -----------------------------------------------------------------------

        private int HeartCount()
        {
            int n = 0;
            foreach (Transform child in H.FindGameObject("Lives").transform)
            {
                if (child.name.StartsWith("Heart"))
                {
                    n++;
                }
            }

            return n;
        }

        private int VisibleHearts()
        {
            int n = 0;
            foreach (Transform child in H.FindGameObject("Lives").transform)
            {
                if (child.name.StartsWith("Heart") && child.gameObject.activeSelf)
                {
                    n++;
                }
            }

            return n;
        }

        /// <summary>The button has a persistent (scene-authored) listener on the scene's GameSession calling the named method.</summary>
        private void AssertButtonCalls(Button button, string method)
        {
            Assert.IsNotNull(button);
            UnityEvent e = button.onClick;
            var found = new List<string>();
            for (int i = 0; i < e.GetPersistentEventCount(); i++)
            {
                if (e.GetPersistentTarget(i) == H.Session)
                {
                    found.Add(e.GetPersistentMethodName(i));
                }
            }

            CollectionAssert.Contains(found, method, button.name + " is not bound to GameSession." + method);
        }
    }
}
