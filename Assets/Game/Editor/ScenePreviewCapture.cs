using System.IO;
using System.Reflection;
using Game.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// Renders still frames of the saved Level 1 scene (world + UI) without entering Play mode, for
    /// side-by-side comparison with the reference capture. Poses the player at a few heights, shows
    /// one panel per shot, and writes PNGs at the reference's portrait aspect and the test device's.
    /// Run via -executeMethod Game.Editor.ScenePreviewCapture.Capture; output goes to
    /// Logs/previews (or the -previewOut argument).
    /// </summary>
    public static class ScenePreviewCapture
    {
        private const string ScenePath = "Assets/Game/Scenes/Level1.unity";

        private static readonly (string name, float height, string panel)[] Shots =
        {
            ("menu", 0f, "MainMenuPanel"),
            ("start", 0f, "HudPanel"),
            ("climb", 14f, "HudPanel"),
            ("bump", 21f, "HudPanel"),
            ("lose", 9f, "LosePanel"),
        };

        private static readonly Vector2Int[] Resolutions = { new Vector2Int(1080, 2025), new Vector2Int(1080, 2520) };

        [MenuItem("Tower/Capture Scene Previews")]
        public static void Capture()
        {
            string outDir = ReadArg("-previewOut") ?? "Logs/previews";
            Directory.CreateDirectory(outDir);

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Camera camera = Camera.main;
            Transform player = GameObject.Find("Player").transform;
            Transform rig = GameObject.Find("CameraRig").transform;
            Vector3 rigOffset = rig.position - player.position;
            var canvas = Object.FindFirstObjectByType<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;

            ApplyGripPose(Object.FindFirstObjectByType<ClimberPoseDriver>());
            Transform hazard = AddPreviewHazard();

            // The first render after opening the scene can run on placeholder shaders while the
            // editor still compiles them asynchronously; render once and discard.
            ShaderUtil.allowAsyncCompilation = false;
            Render(camera, canvas, Resolutions[0], null);

            foreach ((string name, float height, string panel) in Shots)
            {
                player.position = new Vector3(player.position.x, height, player.position.z);
                rig.position = player.position + rigOffset;
                if (hazard != null)
                {
                    hazard.position = new Vector3(0f, height + 2.4f, 0f);
                }
                ShowPanel(canvas.transform, panel);
                FeedHud(height);
                ShowEventCards(name == "bump");

                foreach (Vector2Int resolution in Resolutions)
                {
                    string path = Path.Combine(outDir, name + "_" + resolution.x + "x" + resolution.y + ".png");
                    Render(camera, canvas, resolution, path);
                }
            }

            Debug.Log("[ScenePreviewCapture] Wrote previews to " + outDir);
        }

        private static void ShowPanel(Transform canvas, string panelName)
        {
            foreach (string name in new[] { "MainMenuPanel", "HudPanel", "PausePanel", "WinPanel", "LosePanel" })
            {
                Transform panel = canvas.Find(name);
                if (panel == null)
                {
                    continue;
                }

                panel.gameObject.SetActive(name == panelName);
                if (name == "HudPanel")
                {
                    // SafeAreaFitter only runs in Play mode; preview the full-screen layout.
                    var rect = (RectTransform)panel;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                }
            }
        }

        private static void FeedHud(float height)
        {
            var hud = Object.FindFirstObjectByType<HudView>(FindObjectsInactive.Include);
            if (hud == null)
            {
                return;
            }

            MethodInfo onHeight = typeof(HudView).GetMethod("OnHeightUpdated", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo onHp = typeof(HudView).GetMethod("OnHitPointsChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            onHeight?.Invoke(hud, new object[] { height, 30f });
            onHp?.Invoke(hud, new object[] { 3 });
        }

        private static void ShowEventCards(bool visible)
        {
            var feed = Object.FindFirstObjectByType<EventFeedView>(FindObjectsInactive.Include);
            if (feed == null)
            {
                return;
            }

            var so = new SerializedObject(feed);
            SerializedProperty cards = so.FindProperty("cards");
            SerializedProperty senders = so.FindProperty("senderTexts");
            SerializedProperty details = so.FindProperty("detailTexts");
            string[] ids = { "3f9c2a71", "b07e44d2", "5a1d9e08" };
            for (int i = 0; i < cards.arraySize; i++)
            {
                ((CanvasGroup)cards.GetArrayElementAtIndex(i).objectReferenceValue).alpha = visible ? 1f : 0f;
                ((Text)senders.GetArrayElementAtIndex(i).objectReferenceValue).text = ids[i % ids.Length];
                ((Text)details.GetArrayElementAtIndex(i).objectReferenceValue).text = "Boxing*1";
            }
        }

        private static void ApplyGripPose(ClimberPoseDriver driver)
        {
            if (driver == null)
            {
                return;
            }

            var so = new SerializedObject(driver);
            float arm = so.FindProperty("armGripAngle").floatValue;
            float leg = so.FindProperty("legGripAngle").floatValue;
            float splay = so.FindProperty("armSplayDegrees").floatValue;
            SetLimb(so, "armLeft", arm + 10f, -splay);
            SetLimb(so, "armRight", arm - 25f, splay);
            SetLimb(so, "legLeft", leg, 0f);
            SetLimb(so, "legRight", leg * 0.4f, 0f);
        }

        private static void SetLimb(SerializedObject so, string field, float angle, float splay)
        {
            var limb = so.FindProperty(field).objectReferenceValue as Transform;
            if (limb != null)
            {
                limb.localRotation = Quaternion.Euler(angle, 0f, splay);
            }
        }

        /// <summary>Hazard bands spawn at runtime; drop one active band into the preview so its look can be judged.</summary>
        private static Transform AddPreviewHazard()
        {
            var session = Object.FindFirstObjectByType<Game.Core.GameSession>();
            var so = new SerializedObject(session);
            var prefab = so.FindProperty("hazardVisualPrefab").objectReferenceValue as GameObject;
            var active = so.FindProperty("hazardActiveMaterial").objectReferenceValue as Material;
            float diameter = so.FindProperty("hazardVisualDiameter").floatValue;
            if (prefab == null)
            {
                return null;
            }

            GameObject disc = Object.Instantiate(prefab);
            disc.transform.localScale = new Vector3(diameter, 0.06f, diameter);
            disc.GetComponent<Renderer>().sharedMaterial = active;
            return disc.transform;
        }

        private static void Render(Camera camera, Canvas canvas, Vector2Int size, string path)
        {
            var rt = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            camera.aspect = (float)size.x / size.y;

            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.enabled = false;
            scaler.enabled = true;
            Canvas.ForceUpdateCanvases();

            camera.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            if (path != null)
            {
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }

            camera.targetTexture = null;
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        private static string ReadArg(string name)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
