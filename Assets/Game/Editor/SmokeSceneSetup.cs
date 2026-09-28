using Game.Webhook;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// G1 smoke build only: assembles a scene containing nothing but a camera, a Canvas with a
    /// visible bump counter, and the BumpRunner behind it. Run via
    /// -executeMethod Game.Editor.SmokeSceneSetup.Build in batch mode.
    /// </summary>
    public static class SmokeSceneSetup
    {
        private const string ScenePath = "Assets/Game/Scenes/Smoke.unity";

        [MenuItem("Tower/Build Smoke Scene")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            var textGo = new GameObject("BumpCounterText", typeof(Text));
            textGo.transform.SetParent(canvasGo.transform, false);
            var text = textGo.GetComponent<Text>();
            text.text = "Bumps: 0";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 72;
            text.alignment = TextAnchor.UpperCenter;
            text.color = Color.white;
            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -80f);
            rect.sizeDelta = new Vector2(0f, 200f);

            var runnerGo = new GameObject("BumpRunner", typeof(BumpRunner));
            var so = new SerializedObject(runnerGo.GetComponent<BumpRunner>());
            so.FindProperty("counterText").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory("Assets/Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            Debug.Log("[SmokeSceneSetup] Built and saved " + ScenePath);
        }
    }
}
