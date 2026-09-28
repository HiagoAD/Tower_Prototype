using System.Collections;
using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// G2 follow-up: menu/pause/win/campaign transitions and HTTP bumps against the real Level1 scene,
    /// driven through real Buttons, the public GameSession API and real HTTP.
    /// </summary>
    public sealed class Level1FlowTests : Level1PlayModeTestBase
    {
        private const string PositiveBump = "{\"polarity\":\"positive\"}";

        [UnityTest]
        public IEnumerator StartButton_MovesFromMenuToPlaying_WithHudShown()
        {
            Assert.AreEqual(SessionState.Menu, H.Session.State);
            Assert.IsTrue(H.FindGameObject("MainMenuPanel").activeSelf, "menu should show at launch");

            yield return H.PressStart();

            Assert.IsTrue(H.FindGameObject("HudPanel").activeSelf, "HUD should be active while playing");
            Assert.IsFalse(H.FindGameObject("MainMenuPanel").activeSelf, "main menu should hide while playing");
        }

        [UnityTest]
        public IEnumerator HttpBump_InMenu_Returns409()
        {
            int status = 0;
            yield return H.PostBump(s => status = s);
            Assert.AreEqual(409, status);
        }

        [UnityTest]
        public IEnumerator HttpBump_WhilePaused_Returns409_AndShowsNoBurst()
        {
            yield return H.PressStart();
            H.Session.Pause();

            int status = 0;
            yield return H.PostBump(s => status = s);
            yield return null;

            Assert.AreEqual(409, status);
            Assert.IsFalse(H.AnyBurstElementActive(), "a rejected bump must not show a burst");
        }

        [UnityTest]
        public IEnumerator HttpBump_WhilePlaying_ShowsBurst_MovesClimber_ThenCleansUp_AndClimbingStillWorks()
        {
            yield return H.PressStart();
            H.TakeOverClimbInput();

            // A positive bump lifts the climber from the base (a negative one would clamp at 0).
            float before = H.Motor.Height;
            int status = 0;
            yield return H.PostBump(s => status = s, PositiveBump);
            Assert.AreEqual(200, status);

            yield return Level1Harness.Until(() => H.AnyActive("Glove"), 1f, "a Glove element to become active");
            // The climber only moves once the impact delay has elapsed.
            yield return Level1Harness.Until(() => H.Motor.Height > before + 0.1f * H.Session.BodyHeight,
                H.Session.BumpImpactDelaySeconds + 3f, "climber to rise after a positive bump");

            yield return Level1Harness.Until(() => !H.AnyBurstElementActive(), 4f, "burst to finish");
            H.AssertBurstHidden("after the burst finished");

            // The default bare POST is a negative bump (BumpCatalog.defaultPolarity = Negative).
            yield return Level1Harness.Until(() => !H.Motor.IsLockedOut && !H.Motor.IsInKnockback && H.Motor.PendingBumpCount == 0, 3f, "climber to settle");
            float high = H.Motor.Height;
            yield return H.PostBump(s => status = s);
            Assert.AreEqual(200, status);
            yield return Level1Harness.Until(() => H.Motor.Height < high - 0.1f * H.Session.BodyHeight,
                H.Session.BumpImpactDelaySeconds + 3f, "climber to drop after a default (negative) bump");

            yield return Level1Harness.Until(() => !H.AnyBurstElementActive(), 4f, "second burst to finish");
            yield return Level1Harness.Until(() => !H.Motor.IsLockedOut && !H.Motor.IsInKnockback && H.Motor.PendingBumpCount == 0, 3f, "climber to settle again");

            Assert.IsTrue(H.Motor.CanClimb, "climbing must stay enabled after a burst");
            float rest = H.Motor.Height;
            H.Motor.ClimbHeld = true;
            yield return Level1Harness.Until(() => H.Motor.Height > rest + 0.05f, 3f, "climber to climb after the burst");
            H.Motor.ClimbHeld = false;
        }

        [UnityTest]
        public IEnumerator Pause_DuringBurst_FreezesIt_AndResumeFinishesIt()
        {
            yield return H.PressStart();
            yield return H.StartVisibleBurst(PositiveBump);

            H.Session.Pause();
            yield return null;
            string frozen = H.BurstSnapshot();
            for (int i = 0; i < 12; i++)
            {
                yield return null;
                Assert.AreEqual(frozen, H.BurstSnapshot(), "burst or camera shake moved while paused (frame " + i + ")");
            }

            yield return Level1Harness.RealSeconds(0.25f);
            Assert.AreEqual(frozen, H.BurstSnapshot(), "burst or camera shake moved while paused (after 0.25s)");

            H.Session.Resume();
            yield return Level1Harness.Until(() => H.BurstSnapshot() != frozen || !H.AnyBurstElementActive(), 1f, "burst to continue after resume");
            yield return Level1Harness.Until(() => !H.AnyBurstElementActive(), 4f, "burst to finish after resume");
            H.AssertBurstHidden("after resume and finish");
        }

        [UnityTest]
        public IEnumerator ReturnToMenu_DuringBurst_HidesBurstSynchronously_AndResetsShake()
        {
            yield return H.PressStart();
            yield return H.StartVisibleBurst(PositiveBump);

            H.Session.ReturnToMenu();
            H.AssertBurstHidden("synchronously after ReturnToMenu");

            yield return null;
            Assert.AreEqual(Vector3.zero, H.Shake.transform.localPosition, "shake offset not reset by the next frame");
            Assert.AreEqual(SessionState.Menu, H.Session.State);
        }

        [UnityTest]
        public IEnumerator StartLevel_DuringBurst_HidesBurstSynchronously_AndResetsShake()
        {
            yield return H.PressStart();
            yield return H.StartVisibleBurst(PositiveBump);

            H.Session.StartLevel(); // already Playing: a restart in place.
            H.AssertBurstHidden("synchronously after StartLevel while Playing");

            yield return null;
            Assert.AreEqual(Vector3.zero, H.Shake.transform.localPosition, "shake offset not reset by the next frame");
            Assert.AreEqual(SessionState.Playing, H.Session.State);
        }

        [UnityTest]
        public IEnumerator Retry_DuringBurst_HidesBurstSynchronously_AndResetsShake()
        {
            yield return H.PressStart();
            yield return H.StartVisibleBurst(PositiveBump);

            H.Session.Retry();
            H.AssertBurstHidden("synchronously after Retry");

            yield return null;
            Assert.AreEqual(Vector3.zero, H.Shake.transform.localPosition, "shake offset not reset by the next frame");
            Assert.AreEqual(SessionState.Playing, H.Session.State);
        }

        [UnityTest]
        public IEnumerator Win_DuringBurst_HidesBurstAndResetsShake()
        {
            yield return H.PressStart();
            yield return H.StartVisibleBurst(PositiveBump);
            yield return Level1Harness.Until(() => H.Motor.Height > 0.001f, 2f, "the bump to start lifting the climber");

            ForceWinThroughPublicMotor();
            // The coroutine resumes after every Update of the frame, so this is the first observation after Win() ran.
            yield return Level1Harness.Until(() => H.Session.State == SessionState.Won, 5f, "session to win");
            H.AssertBurstHidden("in the frame the session became Won");

            yield return null;
            Assert.AreEqual(Vector3.zero, H.Shake.transform.localPosition, "shake offset not reset by the next frame");
        }

        [UnityTest]
        public IEnumerator Win_ShowsWinPanelAfterDelay_AndExitButtonReturnsToMenuAtHeightZero()
        {
            yield return H.PressStart();
            ForceWinThroughPublicMotor();
            yield return Level1Harness.Until(() => H.Session.State == SessionState.Won, 5f, "session to win");
            Assert.Greater(H.Motor.Height, 0f);

            GameObject winPanel = H.FindGameObject("WinPanel");
            yield return Level1Harness.Until(() => winPanel.activeSelf, 5f, "win panel to appear after MenuView's delay");
            Assert.IsFalse(H.FindGameObject("HudPanel").activeSelf);

            Assert.IsNotNull(H.FindButton("WinPanel", "NextButton"), "a non-final win offers the next level");
            H.FindButton("WinPanel", "MenuButton").onClick.Invoke();
            yield return null;

            Assert.AreEqual(SessionState.Menu, H.Session.State);
            Assert.AreEqual(0f, H.Motor.Height, 1e-4f, "climber should be back at the base");
            Assert.IsTrue(H.FindGameObject("MainMenuPanel").activeSelf);
            Assert.IsFalse(winPanel.activeSelf);
        }

        [UnityTest]
        public IEnumerator Win_NextButton_StartsLevelTwo_WithItsFinish_Label_AndWorkingBumps()
        {
            yield return H.PressStart();
            Assert.AreEqual(0, H.Session.LevelIndex);
            Assert.AreEqual(5, H.Session.LevelCount);
            Assert.AreEqual("LEVEL 1/5", LevelLabelFirstLine());

            yield return WinCurrentLevel("WinPanel");
            H.FindButton("WinPanel", "NextButton").onClick.Invoke();
            yield return null;

            Assert.AreEqual(SessionState.Playing, H.Session.State);
            Assert.AreEqual(1, H.Session.LevelIndex);
            Assert.AreEqual(H.Session.CurrentLevel.finishHeight * H.Session.DistanceScale, H.Motor.FinishHeight, 1e-3f);
            Assert.AreEqual(0f, H.Motor.Height, 1e-4f, "level 2 should start at the base");
            Assert.IsTrue(H.FindGameObject("HudPanel").activeSelf);
            Assert.IsFalse(H.FindGameObject("WinPanel").activeSelf);
            Assert.AreEqual("LEVEL 2/5", LevelLabelFirstLine());
            StringAssert.Contains(H.Session.CurrentLevel.displayName.ToUpperInvariant(), LevelLabel().text);

            float before = H.Motor.Height;
            int status = 0;
            yield return H.PostBump(s => status = s, PositiveBump);
            Assert.AreEqual(200, status, "a real HTTP bump must work on level 2");
            yield return Level1Harness.Until(() => H.Motor.Height > before + 0.1f * H.Session.BodyHeight,
                H.Session.BumpImpactDelaySeconds + 3f, "climber to rise after a bump on level 2");
        }

        [UnityTest]
        public IEnumerator Campaign_AllLevels_EndsWithFinalWinPanel_ExitReturnsToMenu_AndStartRestartsAtLevelOne()
        {
            yield return H.PressStart();
            int last = H.Session.LevelCount - 1;
            for (int i = 0; i < last; i++)
            {
                Assert.AreEqual(i, H.Session.LevelIndex);
                yield return WinCurrentLevel("WinPanel");
                Assert.IsFalse(H.FindGameObject("FinalWinPanel").activeSelf, "the final panel must not show before the last level");
                H.FindButton("WinPanel", "NextButton").onClick.Invoke();
                yield return null;
                Assert.AreEqual(SessionState.Playing, H.Session.State);
            }

            Assert.AreEqual(last, H.Session.LevelIndex);
            Assert.IsTrue(H.Session.IsFinalLevel);
            yield return WinCurrentLevel("FinalWinPanel");
            Assert.IsFalse(H.FindGameObject("WinPanel").activeSelf, "the plain win panel must not show on the final level");

            H.Session.StartNextLevel(); // no next level: must stay won on the last one.
            Assert.AreEqual(SessionState.Won, H.Session.State);
            Assert.AreEqual(last, H.Session.LevelIndex);

            H.FindButton("FinalWinPanel", "MenuButton").onClick.Invoke();
            yield return null;
            Assert.AreEqual(SessionState.Menu, H.Session.State);
            Assert.IsTrue(H.FindGameObject("MainMenuPanel").activeSelf);
            Assert.IsFalse(H.FindGameObject("FinalWinPanel").activeSelf);

            yield return H.PressStart();
            Assert.AreEqual(0, H.Session.LevelIndex, "Start from the menu begins the campaign again");
            Assert.AreEqual("LEVEL 1/5", LevelLabelFirstLine());
        }

        private Text LevelLabel()
        {
            return H.FindGameObject("LevelLabel").GetComponent<Text>();
        }

        private string LevelLabelFirstLine()
        {
            return LevelLabel().text.Split('\n')[0];
        }

        /// <summary>Forces a win on the current level and waits for the named panel to appear after MenuView's delay.</summary>
        private IEnumerator WinCurrentLevel(string panelName)
        {
            ForceWinThroughPublicMotor();
            yield return Level1Harness.Until(() => H.Session.State == SessionState.Won, 5f, "session to win");
            H.Motor.ClimbHeld = false; // let go, as a player does to tap the panel's buttons.
            GameObject panel = H.FindGameObject(panelName);
            yield return Level1Harness.Until(() => panel.activeSelf, 5f, panelName + " to appear after MenuView's delay");
        }

        /// <summary>
        /// No public "win now" exists, so use public motor members. If the climber is already off the base
        /// the finish is lowered to its height; otherwise the input source is stopped from overwriting
        /// ClimbHeld and the climber climbs a short finish fast. GameSession.Update then wins through its normal check.
        /// </summary>
        private void ForceWinThroughPublicMotor()
        {
            if (H.Motor.Height > 0.001f)
            {
                H.Motor.FinishHeight = H.Motor.Height;
                return;
            }

            H.TakeOverClimbInput();
            H.Motor.FinishHeight = Mathf.Max(0.05f, H.Motor.Height + 0.3f);
            H.Motor.ClimbSpeed = 30f;
            H.Motor.ClimbHeld = true;
        }
    }
}
