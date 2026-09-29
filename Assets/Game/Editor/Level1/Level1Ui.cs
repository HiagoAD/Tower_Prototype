using Game.Core;
using Game.Presentation;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// Level 1's UI, assembled in the scene's draw order: canvas and event system, then the menu,
    /// HUD, pause, win and lose panels, MenuView, and the bump burst on top. Each part is its own builder
    /// (Level1Hud, Level1Burst); this class owns the canvas root and the full-screen menu panels.
    /// </summary>
    internal static class Level1Ui
    {
        public static void Build(UiKit kit, GameSession session, Transform hitTarget, CameraBuildResult camera, Level1Inputs inputs)
        {
            GameObject canvasGo = BuildCanvasRoot();
            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();

            // Every full-screen menu follows the reference's GAME OVER screen: dimmed scene, bold white
            // title, and stock Unity buttons -- light for the main action, dark for the way out.
            GameObject mainMenuPanel = BuildMenuPanel(kit, canvasRect, "MainMenuPanel", "TOWER CLIMB");
            Button startButton = AddMenuButton(kit, mainMenuPanel.transform, "StartButton", "Start", 0, primary: true);
            UnityEventTools.AddPersistentListener(startButton.onClick, session.StartCampaign);

            GameObject hudPanel = Level1Hud.Build(kit, canvasRect, session, inputs.GloveSprite);

            GameObject pausePanel = BuildMenuPanel(kit, canvasRect, "PausePanel", "PAUSED");
            pausePanel.SetActive(false);
            Button resumeButton = AddMenuButton(kit, pausePanel.transform, "ResumeButton", "Continue", 0, primary: true);
            UnityEventTools.AddPersistentListener(resumeButton.onClick, session.Resume);
            Button pauseMenuButton = AddMenuButton(kit, pausePanel.transform, "MenuButton", "Exit", 1, primary: false);
            UnityEventTools.AddPersistentListener(pauseMenuButton.onClick, session.ReturnToMenu);

            GameObject winPanel = BuildMenuPanel(kit, canvasRect, "WinPanel", "SUMMIT REACHED");
            winPanel.SetActive(false);
            Button nextButton = AddMenuButton(kit, winPanel.transform, "NextButton", "Next Level", Level1Layout.WinButtonSlot - 1, primary: true);
            UnityEventTools.AddPersistentListener(nextButton.onClick, session.StartNextLevel);
            Button winMenuButton = AddMenuButton(kit, winPanel.transform, "MenuButton", "Exit", Level1Layout.WinButtonSlot, primary: false);
            UnityEventTools.AddPersistentListener(winMenuButton.onClick, session.ReturnToMenu);

            GameObject finalWinPanel = BuildMenuPanel(kit, canvasRect, "FinalWinPanel", "TOWER CLEARED");
            finalWinPanel.SetActive(false);
            Button finalMenuButton = AddMenuButton(kit, finalWinPanel.transform, "MenuButton", "Exit", Level1Layout.WinButtonSlot, primary: true);
            UnityEventTools.AddPersistentListener(finalMenuButton.onClick, session.ReturnToMenu);

            GameObject losePanel = BuildMenuPanel(kit, canvasRect, "LosePanel", "GAME OVER");
            losePanel.SetActive(false);
            Button retryButton = AddMenuButton(kit, losePanel.transform, "RetryButton", "Continue", 0, primary: true);
            UnityEventTools.AddPersistentListener(retryButton.onClick, session.Retry);
            Button loseMenuButton = AddMenuButton(kit, losePanel.transform, "MenuButton", "Exit", 1, primary: false);
            UnityEventTools.AddPersistentListener(loseMenuButton.onClick, session.ReturnToMenu);

            var menuViewGo = new GameObject("MenuView");
            menuViewGo.transform.SetParent(canvasGo.transform, false);
            MenuView menuView = menuViewGo.AddComponent<MenuView>();
            SceneBinding.Bind(menuView, "session", session);
            SceneBinding.Bind(menuView, "mainMenuPanel", mainMenuPanel);
            SceneBinding.Bind(menuView, "hudPanel", hudPanel);
            SceneBinding.Bind(menuView, "pausePanel", pausePanel);
            SceneBinding.Bind(menuView, "losePanel", losePanel);
            SceneBinding.Bind(menuView, "winPanel", winPanel);
            SceneBinding.Bind(menuView, "finalWinPanel", finalWinPanel);
            SceneBinding.Bind(menuView, "winPanelDelaySeconds", Level1Layout.SummitSlideSeconds);

            Level1Burst.Build(canvasGo.transform, session, hitTarget, camera, inputs);
        }

        private static GameObject BuildCanvasRoot()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            // Match width only (matchWidthOrHeight = 0): both the reference (1080x1920) and the test
            // device (1080x2520) share the same 1080 width, so width-matching keeps 1 canvas unit ==
            // 1 screen pixel on both, and top/bottom-anchored geometry below needs no per-aspect math.
            // Only the taller device's extra vertical canvas space differs, which anchor-based
            // placement (rather than centered absolute offsets) already absorbs correctly.
            scaler.matchWidthOrHeight = 0f;

            var eventSystemGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            var uiModule = eventSystemGo.AddComponent<InputSystemUIInputModule>();
            uiModule.AssignDefaultActions();

            return canvasGo;
        }

        /// <summary>ref.png's heavy HUD lettering: coloured fill inside a thick near-black stroke, plus a soft drop shadow.</summary>
        public static void StyleHeavyText(UiKit kit, Text text, Color fill)
        {
            kit.UseDisplayFont(text);
            text.color = fill;
            UiKit.AddOutline(text, Level1Palette.HudStroke, 3f);
            UiKit.AddOutline(text, Level1Palette.HudStroke, 2f);
            text.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.35f);
        }

        /// <summary>
        /// Full-screen dimmed panel with a bold title at the reference GAME OVER title's height
        /// (~22% down the screen). The dim also blocks touches to the HUD/climb region beneath.
        /// </summary>
        private static GameObject BuildMenuPanel(UiKit kit, RectTransform parent, string name, string title)
        {
            GameObject panel = UiKit.BuildPanel(parent, name);
            Image dim = UiKit.AddImage(panel.transform, "Dim", null, Level1Palette.MenuDim);
            dim.raycastTarget = true;
            UiKit.Stretch(dim.rectTransform);

            Text titleText = UiKit.AddText(panel.transform, "Title", title, 92, TextAnchor.MiddleCenter, Vector2.zero,
                anchorMin: new Vector2(0.5f, 0.78f), anchorMax: new Vector2(0.5f, 0.78f), sizeDelta: new Vector2(1040f, 160f));
            StyleHeavyText(kit, titleText, Level1Palette.HudYellow);
            return panel;
        }

        /// <summary>Stacked menu buttons at the reference GAME OVER screen's size and spacing, from the screen centre down.</summary>
        private static Button AddMenuButton(UiKit kit, Transform parent, string name, string label, int slot, bool primary)
        {
            return kit.AddButton(parent, name, label, new Vector2(0f, -slot * 217f),
                primary ? Level1Palette.PrimaryButton : Level1Palette.SecondaryButton, primary ? Level1Palette.PrimaryButtonText : Color.white,
                sizeDelta: new Vector2(608f, 120f));
        }
    }
}
