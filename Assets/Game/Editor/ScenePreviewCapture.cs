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
    /// one panel per shot, and writes PNGs at the reference's portrait aspect and the test device's,
    /// plus a simulated climb frame by frame (cycle_N.png) and the released hold (cycle_hold.png).
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

        private const float PreviewSafeBandOffset = 4.5f; // the safe band sits this far above the active one.
        private const int CycleFrames = 16;
        private const int CycleFrameInterval = 3; // 20 fps at the simulated 60
        private const float SimulationStep = 1f / 60f;
        private const string ClimbPacePath = "Assets/Game/Levels/ClimbPace.asset";

        private static float _climbSpeed = 2.5f; // the paced world speed, read from ClimbPace when Capture starts

        private static readonly Vector2Int[] Resolutions = { new Vector2Int(1080, 2025), new Vector2Int(1080, 2520) };

        [MenuItem("Tower/Capture Scene Previews")]
        public static void Capture()
        {
            string outDir = ReadArg("-previewOut") ?? "Logs/previews";
            Directory.CreateDirectory(outDir);

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var pace = AssetDatabase.LoadAssetAtPath<Game.Core.ClimbPace>(ClimbPacePath);
            _climbSpeed = pace != null ? pace.WorldSpeed : 2.5f;

            Camera camera = Camera.main;
            Transform player = GameObject.Find("Player").transform;
            Transform rig = GameObject.Find("CameraRig").transform;
            Vector3 rigOffset = rig.position - player.position;
            var canvas = Object.FindFirstObjectByType<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;

            var poseDriver = Object.FindFirstObjectByType<ClimberPoseDriver>();
            Transform[] hazards = AddPreviewHazards();

            // The first render after opening the scene can run on placeholder shaders while the
            // editor still compiles them asynchronously; render once and discard.
            ShaderUtil.allowAsyncCompilation = false;
            Render(camera, canvas, Resolutions[0], null);

            foreach ((string name, float height, string panel) in Shots)
            {
                if (poseDriver != null && (name == "climb" || name == "bump"))
                {
                    SimulateClimb(poseDriver, player, rig, rigOffset, height - 3f, height, null);
                }
                else
                {
                    Place(player, rig, rigOffset, height);
                    poseDriver?.Advance(height, SimulationStep);
                }

                if (hazards != null)
                {
                    hazards[0].position = new Vector3(0f, height + 2.4f, 0f);
                    hazards[1].position = new Vector3(0f, height + 2.4f + PreviewSafeBandOffset, 0f);
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

            CaptureClimbCycle(camera, canvas, poseDriver, player, rig, rigOffset, outDir);
            Debug.Log("[ScenePreviewCapture] Wrote previews to " + outDir);
        }

        /// <summary>
        /// Climbs into a steady rhythm, then keeps climbing at the level's speed and saves a frame
        /// every CycleFrameInterval simulation steps (cycle_N.png), HUD hidden, at the device aspect.
        /// Ends released, after the grip has settled (cycle_hold.png).
        /// </summary>
        private static void CaptureClimbCycle(Camera camera, Canvas canvas, ClimberPoseDriver driver, Transform player, Transform rig, Vector3 rigOffset, string outDir)
        {
            if (driver == null)
            {
                return;
            }

            ShowPanel(canvas.transform, null);
            SimulateClimb(driver, player, rig, rigOffset, 11f, 14f, null);
            float end = 14f + _climbSpeed * SimulationStep * CycleFrames * CycleFrameInterval;
            int step = 0;
            SimulateClimb(driver, player, rig, rigOffset, 14f, end, () =>
            {
                if (step % CycleFrameInterval == 0 && step / CycleFrameInterval < CycleFrames)
                {
                    Render(camera, canvas, Resolutions[1], Path.Combine(outDir, "cycle_" + step / CycleFrameInterval + ".png"));
                }

                step++;
            });

            for (int i = 0; i < 60; i++)
            {
                driver.Advance(end, SimulationStep);
            }

            Render(camera, canvas, Resolutions[1], Path.Combine(outDir, "cycle_hold.png"));
        }

        /// <summary>Raises the player from one height to another at the paced climb speed, one pose step per simulated frame.</summary>
        private static void SimulateClimb(ClimberPoseDriver driver, Transform player, Transform rig, Vector3 rigOffset, float from, float to, System.Action afterStep)
        {
            for (float h = from; h < to; h += _climbSpeed * SimulationStep)
            {
                Place(player, rig, rigOffset, h);
                driver.Advance(h, SimulationStep);
                afterStep?.Invoke();
            }

            Place(player, rig, rigOffset, to);
            driver.Advance(to, SimulationStep);
        }

        private static void Place(Transform player, Transform rig, Vector3 rigOffset, float height)
        {
            player.position = new Vector3(player.position.x, height, player.position.z);
            rig.position = player.position + rigOffset;
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
            onHeight?.Invoke(hud, new object[] { height, 30f });
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

        /// <summary>Hazard bands spawn at runtime; drop an active and a safe band into the preview so both looks can be judged. Returns { active, safe }.</summary>
        private static Transform[] AddPreviewHazards()
        {
            var session = Object.FindFirstObjectByType<Game.Core.GameSession>();
            var so = new SerializedObject(session);
            var prefab = so.FindProperty("hazardVisualPrefab").objectReferenceValue as GameObject;
            var active = so.FindProperty("hazardActiveMaterial").objectReferenceValue as Material;
            var safe = so.FindProperty("hazardSafeMaterial").objectReferenceValue as Material;
            float diameter = so.FindProperty("hazardVisualDiameter").floatValue;
            if (prefab == null)
            {
                return null;
            }

            return new[] { InstantiatePreviewBand(prefab, active, diameter), InstantiatePreviewBand(prefab, safe, diameter) };
        }

        private static Transform InstantiatePreviewBand(GameObject prefab, Material material, float diameter)
        {
            GameObject disc = Object.Instantiate(prefab);
            disc.transform.localScale = new Vector3(diameter, diameter * 0.07f, diameter);
            disc.GetComponent<Renderer>().sharedMaterial = material;
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
