using Game.Core;
using Game.Gameplay;
using Game.Presentation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// G2 vertical slice: one playable level built from primitives (tower/character art import is
    /// a follow-up), wired end-to-end to GameSession/PlayerMotor/webhook/glove burst. Run via
    /// -executeMethod Game.Editor.Level1SceneSetup.Build in batch mode.
    /// </summary>
    public static class Level1SceneSetup
    {
        private const string ScenePath = "Assets/Game/Scenes/Level1.unity";
        private const string LevelAssetPath = "Assets/Game/Levels/Level1.asset";
        private const string GlovePngPath = "Assets/Game/Art/Licensed/BoxingGlove/boxing-glove-white.png";

        [MenuItem("Tower/Build Level 1 Scene")]
        public static void Build()
        {
            Sprite gloveSprite = LoadGloveSprite();

            // EditorSceneManager.NewScene unloads not-yet-referenced assets created earlier in this
            // same batch invocation (a freshly created-and-saved ScriptableObject has no scene/asset
            // referrer yet), which silently turns a held C# reference into a destroyed ("fake null")
            // UnityEngine.Object. Build the level asset AFTER the scene reset so it survives.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LevelDefinition level = BuildLevelAsset();

            GameObject towerRoot = BuildTower();
            PlayerMotor motor = BuildPlayer(towerRoot);
            Camera camera = BuildCamera(motor, out CameraShake shake);

            GameObject sessionGo = new GameObject("GameSession");
            GameSession session = sessionGo.AddComponent<GameSession>();
            BindPrivate(session, "motor", motor);
            BindPrivate(session, "level", level);
            BindPrivate(session, "startingHitPoints", 3);

            var climbInput = motor.gameObject.AddComponent<ClimbInputSource>();
            BindPrivate(climbInput, "motor", motor);

            BuildUi(session, camera, shake, gloveSprite);

            System.IO.Directory.CreateDirectory("Assets/Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            Debug.Log("[Level1SceneSetup] Built and saved " + ScenePath);
        }

        private static LevelDefinition BuildLevelAsset()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Levels");

            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.levelId = 1;
            level.displayName = "First Ascent";
            level.finishHeight = 30f;
            level.climbSpeed = 2.5f;
            level.hazards = new[]
            {
                new HazardSpec { height = 10f, periodSeconds = 4f, activeSeconds = 1.5f, phaseOffsetSeconds = 0f },
                new HazardSpec { height = 20f, periodSeconds = 5f, activeSeconds = 1.5f, phaseOffsetSeconds = 2f },
            };

            AssetDatabase.DeleteAsset(LevelAssetPath);
            AssetDatabase.CreateAsset(level, LevelAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelAssetPath);
        }

        private static Sprite LoadGloveSprite()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(GlovePngPath);
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(GlovePngPath);
        }

        private static GameObject BuildTower()
        {
            var root = new GameObject("Tower");
            var paleMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(0.85f, 0.82f, 0.74f),
            };

            const float segmentHeight = 5f;
            const int segmentCount = 8;
            const float radius = 2f;

            for (int i = 0; i < segmentCount; i++)
            {
                GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                seg.name = "TowerSegment" + i;
                seg.transform.SetParent(root.transform, false);
                seg.transform.localScale = new Vector3(radius, segmentHeight * 0.5f, radius);
                seg.transform.localPosition = new Vector3(0f, i * segmentHeight, 0f);
                seg.GetComponent<Renderer>().sharedMaterial = paleMaterial;
                Object.DestroyImmediate(seg.GetComponent<Collider>());
            }

            return root;
        }

        private static PlayerMotor BuildPlayer(GameObject towerRoot)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            go.transform.position = new Vector3(2.6f, 0f, 0f);
            go.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(0.9f, 0.45f, 0.1f),
            };
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());

            return go.AddComponent<PlayerMotor>();
        }

        private static Camera BuildCamera(PlayerMotor motor, out CameraShake shake)
        {
            var cameraOffset = new Vector3(2.6f, 1.5f, -12f);
            var rigGo = new GameObject("CameraRig");
            rigGo.transform.position = cameraOffset; // avoid starting inside the tower and lerping out on frame 1
            var follow = rigGo.AddComponent<CameraFollow>();
            BindPrivate(follow, "target", motor);
            BindPrivate(follow, "offset", cameraOffset);

            var shakeGo = new GameObject("CameraShakeOffset");
            shakeGo.transform.SetParent(rigGo.transform, false);
            shake = shakeGo.AddComponent<CameraShake>();

            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(shakeGo.transform, false);
            Camera camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.75f, 0.92f);
            camera.fieldOfView = 50f;

            return camera;
        }

        private static void BuildUi(GameSession session, Camera camera, CameraShake shake, Sprite gloveSprite)
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            var eventSystemGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            var uiModule = eventSystemGo.AddComponent<InputSystemUIInputModule>();
            uiModule.AssignDefaultActions();

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();

            GameObject mainMenuPanel = BuildPanel(canvasRect, "MainMenuPanel");
            Text title = AddText(mainMenuPanel.transform, "Title", "Tower Prototype", 96, TextAnchor.MiddleCenter, new Vector2(0f, 300f));
            title.rectTransform.sizeDelta = new Vector2(900f, 200f);
            Button startButton = AddButton(mainMenuPanel.transform, "StartButton", "Start", new Vector2(0f, 0f));
            UnityEventTools.AddPersistentListener(startButton.onClick, session.StartLevel);

            GameObject hudPanel = BuildPanel(canvasRect, "HudPanel");
            hudPanel.SetActive(false);
            Text heightText = AddText(hudPanel.transform, "HeightText", "0m / 0m", 56, TextAnchor.UpperLeft, new Vector2(-350f, 900f));
            Text hpText = AddText(hudPanel.transform, "HitPointsText", "HP: 3", 56, TextAnchor.UpperRight, new Vector2(350f, 900f));
            Text bumpFeedText = AddText(hudPanel.transform, "BumpFeedText", string.Empty, 64, TextAnchor.UpperCenter, new Vector2(0f, 800f));
            Button pauseButton = AddButton(hudPanel.transform, "PauseButton", "II", new Vector2(370f, 900f));
            UnityEventTools.AddPersistentListener(pauseButton.onClick, session.Pause);

            GameObject pausePanel = BuildPanel(canvasRect, "PausePanel");
            pausePanel.SetActive(false);
            AddText(pausePanel.transform, "PauseTitle", "Paused", 80, TextAnchor.MiddleCenter, new Vector2(0f, 250f));
            Button resumeButton = AddButton(pausePanel.transform, "ResumeButton", "Resume", new Vector2(0f, 50f));
            UnityEventTools.AddPersistentListener(resumeButton.onClick, session.Resume);
            Button pauseMenuButton = AddButton(pausePanel.transform, "MenuButton", "Menu", new Vector2(0f, -100f));
            UnityEventTools.AddPersistentListener(pauseMenuButton.onClick, session.ReturnToMenu);

            GameObject winPanel = BuildPanel(canvasRect, "WinPanel");
            winPanel.SetActive(false);
            AddText(winPanel.transform, "WinTitle", "Summit Reached!", 80, TextAnchor.MiddleCenter, new Vector2(0f, 250f));
            Button winMenuButton = AddButton(winPanel.transform, "MenuButton", "Menu", new Vector2(0f, 0f));
            UnityEventTools.AddPersistentListener(winMenuButton.onClick, session.ReturnToMenu);

            GameObject losePanel = BuildPanel(canvasRect, "LosePanel");
            losePanel.SetActive(false);
            AddText(losePanel.transform, "LoseTitle", "You Fell", 80, TextAnchor.MiddleCenter, new Vector2(0f, 250f));
            Button retryButton = AddButton(losePanel.transform, "RetryButton", "Retry", new Vector2(0f, 50f));
            UnityEventTools.AddPersistentListener(retryButton.onClick, session.Retry);
            Button loseMenuButton = AddButton(losePanel.transform, "MenuButton", "Menu", new Vector2(0f, -100f));
            UnityEventTools.AddPersistentListener(loseMenuButton.onClick, session.ReturnToMenu);

            var hudViewGo = new GameObject("HudView");
            hudViewGo.transform.SetParent(canvasGo.transform, false);
            HudView hudView = hudViewGo.AddComponent<HudView>();
            BindPrivate(hudView, "session", session);
            BindPrivate(hudView, "heightText", heightText);
            BindPrivate(hudView, "hitPointsText", hpText);
            BindPrivate(hudView, "bumpFeedText", bumpFeedText);

            var menuViewGo = new GameObject("MenuView");
            menuViewGo.transform.SetParent(canvasGo.transform, false);
            MenuView menuView = menuViewGo.AddComponent<MenuView>();
            BindPrivate(menuView, "session", session);
            BindPrivate(menuView, "mainMenuPanel", mainMenuPanel);
            BindPrivate(menuView, "hudPanel", hudPanel);
            BindPrivate(menuView, "pausePanel", pausePanel);
            BindPrivate(menuView, "winPanel", winPanel);
            BindPrivate(menuView, "losePanel", losePanel);

            GameObject flashGo = new GameObject("FlashImage", typeof(Image));
            flashGo.transform.SetParent(canvasGo.transform, false);
            var flashRect = flashGo.GetComponent<RectTransform>();
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.sizeDelta = Vector2.zero;
            var flashImage = flashGo.GetComponent<Image>();
            flashImage.color = new Color(1f, 1f, 1f, 0f);
            flashImage.raycastTarget = false;

            GameObject burstRootGo = new GameObject("GloveBurstRoot", typeof(RectTransform));
            burstRootGo.transform.SetParent(canvasGo.transform, false);
            var burstRect = burstRootGo.GetComponent<RectTransform>();
            burstRect.anchorMin = burstRect.anchorMax = new Vector2(0.5f, 0.5f);
            burstRect.sizeDelta = Vector2.zero;

            var audioGo = new GameObject("ImpactAudioSource", typeof(AudioSource));
            audioGo.transform.SetParent(canvasGo.transform, false);
            AudioSource audioSource = audioGo.GetComponent<AudioSource>();
            audioSource.playOnAwake = false;

            var gloveBurstGo = new GameObject("GloveBurstView");
            gloveBurstGo.transform.SetParent(canvasGo.transform, false);
            GloveBurstView gloveBurst = gloveBurstGo.AddComponent<GloveBurstView>();
            BindPrivate(gloveBurst, "session", session);
            BindPrivate(gloveBurst, "burstRoot", burstRect);
            BindPrivate(gloveBurst, "flashImage", flashImage);
            BindPrivate(gloveBurst, "cameraShake", shake);
            BindPrivate(gloveBurst, "impactAudioSource", audioSource);
            BindPrivate(gloveBurst, "gloveSprite", gloveSprite);
        }

        private static GameObject BuildPanel(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            return go;
        }

        private static Text AddText(Transform parent, string name, string content, int size, TextAnchor anchor, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1000f, 150f);
            rect.anchoredPosition = anchoredPos;
            return text;
        }

        private static Button AddButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400f, 140f);
            rect.anchoredPosition = anchoredPos;
            go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            Text text = AddText(go.transform, "Label", label, 48, TextAnchor.MiddleCenter, Vector2.zero);
            text.rectTransform.sizeDelta = new Vector2(400f, 140f);

            return go.GetComponent<Button>();
        }

        private static void BindPrivate(object target, string fieldName, object value)
        {
            var so = new SerializedObject((Object)target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError("[Level1SceneSetup] Missing field " + fieldName + " on " + target.GetType());
                return;
            }

            AssignSerializedValue(prop, value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignSerializedValue(SerializedProperty prop, object value)
        {
            switch (value)
            {
                case Object unityObj:
                    prop.objectReferenceValue = unityObj;
                    break;
                case int i:
                    prop.intValue = i;
                    break;
                case float f:
                    prop.floatValue = f;
                    break;
                case Vector3 v3:
                    prop.vector3Value = v3;
                    break;
                default:
                    Debug.LogError("[Level1SceneSetup] Unsupported bind value type " + value.GetType());
                    break;
            }
        }
    }
}
