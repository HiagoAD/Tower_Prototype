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
    /// One playable level, wired end-to-end to GameSession/PlayerMotor/webhook/glove burst, built
    /// from the licensed Kenney character/tower/sky assets under Assets/Game/Art/Licensed. Run via
    /// -executeMethod Game.Editor.Level1SceneSetup.Build in batch mode.
    /// </summary>
    public static class Level1SceneSetup
    {
        private const string ScenePath = "Assets/Game/Scenes/Level1.unity";
        private const string LevelJsonPath = "Assets/Game/Levels/Level1.json";
        private const string ClimbPacePath = "Assets/Game/Levels/ClimbPace.asset";
        private const string GlovePngPath = "Assets/Game/Art/Licensed/BoxingGlove/boxing-glove-white.png";
        private const string ImpactSfxPath = "Assets/Game/Art/Licensed/ImpactSounds/impactPunch_heavy_000.ogg";
        private const string HazardVisualPrefabPath = "Assets/Game/Prefabs/HazardVisual.prefab";
        private const string HazardActiveMaterialPath = "Assets/Game/Art/Materials/HazardActive.mat";
        private const string HazardSafeMaterialPath = "Assets/Game/Art/Materials/HazardSafe.mat";

        private const string CharacterFbxPath = "Assets/Game/Art/Licensed/Character/character-b.fbx";
        private const string CharacterTexturePath = "Assets/Game/Art/Licensed/Character/texture-b-goku.png";
        private const string CharacterMaterialPath = "Assets/Game/Art/Materials/Climber.mat";

        private const string TowerBaseFbxPath = "Assets/Game/Art/Licensed/Tower/tower-base.fbx";
        private const string TowerTopFbxPath = "Assets/Game/Art/Licensed/Tower/tower-top.fbx";
        private const string TowerMaterialPath = "Assets/Game/Art/Materials/TowerStone.mat";

        private const string SkyTexturePath = "Assets/Game/Art/Licensed/Sky/skybox-day.png";
        private const string SkyMaterialPath = "Assets/Game/Art/Materials/SkyGradient.mat";
        private const string CloudMaterialPathFormat = "Assets/Game/Art/Materials/Cloud{0}.mat";
        private const string SeaMaterialPath = "Assets/Game/Art/Materials/Sea.mat";
        private const string WindowFrameMaterialPath = "Assets/Game/Art/Materials/WindowFrame.mat";
        private const string WindowPaneMaterialPath = "Assets/Game/Art/Materials/WindowPane.mat";
        private const string StarPngPath = "Assets/Game/Art/Licensed/UI/star-yellow.png";
        private const string UiFontPath = "Assets/Game/Art/Licensed/UI/Fonts/KenneyFuture.ttf";

        // Palette sampled from the preview image (ref.png), the art-direction baseline: bright cyan
        // sky paling toward the bottom, a pale white/light-blue tower with blue windows.
        private static readonly Color SkyTopColor = new Color32(62, 166, 230, 255);
        private static readonly Color SkyMidColor = new Color32(112, 199, 238, 255);
        private static readonly Color SkyBottomColor = new Color32(128, 210, 242, 255);
        private static readonly Color TowerStoneColor = new Color32(214, 226, 242, 255);
        private static readonly Color WindowFrameColor = new Color32(70, 104, 158, 255);
        private static readonly Color WindowPaneColor = new Color32(150, 190, 236, 255);
        private static readonly Color SeaColor = new Color32(104, 182, 226, 255);

        // A safe band must read as a harmless, deliberate ring against both the pale tower stone and
        // the cyan sky, so it is a saturated mint green rather than another collar; active ones glow red.
        private static readonly Color HazardActiveColor = new Color(0.95f, 0.12f, 0.08f, 1f);
        private static readonly Color HazardSafeColor = new Color32(52, 201, 110, 255);

        // HUD palette from ref.png: dark altitude track, yellow fill, yellow current altitude and red
        // goal altitude, both heavy and outlined in near-black.
        private static readonly Color BarTrackColor = new Color32(44, 50, 60, 255);
        private static readonly Color BarFillColor = new Color32(252, 192, 14, 255);
        private static readonly Color HudYellowColor = new Color32(255, 214, 20, 255);
        private static readonly Color HudRedColor = new Color32(236, 24, 24, 255);
        private static readonly Color HudStrokeColor = new Color32(24, 16, 10, 255);
        private static readonly Color CardBannerColor = new Color32(40, 95, 168, 230);
        private static readonly Color PrimaryButtonColor = new Color32(240, 240, 240, 255);
        private static readonly Color PrimaryButtonTextColor = new Color32(50, 50, 50, 255);
        private static readonly Color SecondaryButtonColor = new Color32(60, 60, 60, 255);
        private static readonly Color MenuDimColor = new Color(0.02f, 0.04f, 0.1f, 0.6f);

        // Windows inside skybox-day.png (UV scale.xy, offset.zw) that the CloudCutout shader keys a
        // cloud out of: two broad soft cumulus masses, then four small puffs. Aspect = the window's
        // pixel aspect.
        private static readonly Vector4[] CloudWindows =
        {
            new Vector4(0.4600f, 0.2830f, 0.1270f, 0.5800f),
            new Vector4(0.3320f, 0.2640f, 0.5960f, 0.5800f),
            new Vector4(0.0830f, 0.0733f, 0.2441f, 0.5654f),
            new Vector4(0.1172f, 0.0732f, 0.0049f, 0.5850f),
            new Vector4(0.0635f, 0.0342f, 0.7617f, 0.5459f),
            new Vector4(0.0757f, 0.0439f, 0.9229f, 0.5801f),
        };
        private static readonly float[] CloudAspects = { 3.25f, 2.5f, 2.27f, 3.2f, 3.7f, 3.4f };

        // Composition follows ref.png, adapted to portrait rather than copied pixel-for-pixel. In the
        // landscape image the shaft spans only ~0.10 of the frame width, which would leave a sliver on
        // a phone; the relationships kept instead are: the climber (hair to feet, hands raised) is
        // about as tall as the shaft is wide, and sits well below the frame centre with the tower
        // running on above it. The shaft spans TargetTowerWidthFraction of the portrait width.
        //
        // At CameraDistance world units away, a CameraVerticalFovDeg lens on NominalDeviceAspect
        // gives the world width across the screen; the tower radius and character scale derive from
        // that. CharacterHeightToTowerDiameter applies to the character's arms-down bounds -- the
        // raised-arm grip pose adds roughly a fifth to its on-screen height.
        private const float TargetTowerWidthFraction = 0.26f;
        private const float CharacterHeightToTowerDiameter = 0.82f;
        private const float CharacterScreenHeightFraction = 0.34f; // climber's chest, measured up from the bottom of the frame.
        // World units per unit of the original framing. Level data (heights, speed, hazard spacing)
        // stays in world units; scaling the camera -- and with it the tower and climber, which derive
        // from the camera's view width -- slows the on-screen climb to a pace where hands can stay
        // planted on the tower between grabs (see ClimberPoseDriver): about 0.9 body heights/s at
        // speed 2.5, roughly 3 grabs a second.
        private const float WorldScale = 4.5f;
        private const float CameraDistance = 8f * WorldScale;
        private const float CameraVerticalFovDeg = 45f;
        private const float NominalDeviceAspect = 1080f / 2520f;
        private const float TowerHeadroom = 14f; // world units of tower visible above the finish height.

        // Tower rhythm from ref.png, as fractions of the shaft diameter: thin collars (a little wider
        // than the shaft) separating alternating short and tall storeys, each with rows of small
        // blue windows in two columns whose spacing alternates storey to storey.
        private static readonly float[] StoreyHeightsToDiameter = { 0.7f, 1.2f };
        private static readonly int[] StoreyWindowRows = { 2, 3 };
        private static readonly float[] StoreyWindowAnglesDeg = { 40f, 27f };
        private const float CollarHeightToDiameter = 0.12f;
        private const float WindowWidthToDiameter = 0.075f;
        private const float WindowHeightToDiameter = 0.1f;

        private const float CollarRadiusToShaftRadius = 1.11f;

        private const float PedestalRadiusMultiplier = 1.9f;
        private const float PedestalHeightMultiplier = 0.8f;
        private const float SeaHalfExtent = 14f * WorldScale;
        private const int CloudCount = 30;
        private const float CloudMargin = 10f * WorldScale; // cloud field extends this far above and below the climb, so the frame is never empty.
        private const float MainLightShadowDistance = 14f * WorldScale;
        private const float CameraShakeMagnitude = 0.35f * WorldScale;

        // Fraction of the character's total (feet-to-head) bounds height used both for HitTarget
        // placement (glove burst aim) and the camera's chest-height framing.
        private const float CharacterChestHeightFraction = 0.55f;

        private const float HazardVisualDiameterMultiplier = 1.15f; // slightly wider than the tower so the band visibly wraps around it.

        private static Font _uiFont;

        [MenuItem("Tower/Build Level 1 Scene")]
        public static void Build()
        {
            ConfigureImportSettings();
            ConfigureMainLightShadows();
            _uiFont = AssetDatabase.LoadAssetAtPath<Font>(UiFontPath);

            Sprite gloveSprite = LoadSprite(GlovePngPath);
            Sprite starSprite = LoadSprite(StarPngPath);
            AudioClip impactClip = LoadImpactClip();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            TextAsset levelJson = AssetDatabase.LoadAssetAtPath<TextAsset>(LevelJsonPath);
            if (levelJson == null)
            {
                throw new System.IO.FileNotFoundException("Level data missing", LevelJsonPath);
            }

            LevelDefinition level = LevelDefinition.FromJson(levelJson.text);
            (GameObject hazardVisualPrefab, Material hazardActiveMaterial, Material hazardSafeMaterial) = BuildHazardVisualAssets();

            BuildEnvironment();

            // The climb pace scales level distances at runtime (see ClimbPace); build the tower and
            // sky tall enough for the fastest pace allowed, so changing it never needs a rebuild.
            ClimbPace pace = LoadOrCreatePace();
            float bodyHeight = CharacterHeightToTowerDiameter * 2f * TowerRadius();
            float climbTop = level.finishHeight * ClimbPace.MaxDistanceScaleFor(level, bodyHeight);

            TowerMetrics tower = BuildTower(climbTop);
            BuildSeaAndPedestal(tower);
            BuildClouds(climbTop + TowerHeadroom);
            PlayerBuildResult player = BuildPlayer(tower);
            BindPrivate(pace, "bodyHeight", player.Metrics.Height);
            EditorUtility.SetDirty(pace);
            Camera camera = BuildCamera(player.Motor, player.Metrics.ChestHeight, pace, out CameraShake shake);

            GameObject sessionGo = new GameObject("GameSession");
            GameSession session = sessionGo.AddComponent<GameSession>();
            BindPrivate(session, "motor", player.Motor);
            BindPrivate(session, "levelJson", levelJson);
            BindPrivate(session, "pace", pace);
            BindPrivate(session, "hazardVisualPrefab", hazardVisualPrefab);
            BindPrivate(session, "hazardActiveMaterial", hazardActiveMaterial);
            BindPrivate(session, "hazardSafeMaterial", hazardSafeMaterial);
            BindPrivate(session, "hazardVisualDiameter", tower.Radius * 2f * HazardVisualDiameterMultiplier);
            BindPrivate(session, "hazardBodyHeight", player.Metrics.Height);

            BindPrivate(player.PoseDriver, "session", session);
            BindPrivate(player.PoseDriver, "pace", pace);

            var climbInput = player.Motor.gameObject.AddComponent<ClimbInputSource>();
            BindPrivate(climbInput, "motor", player.Motor);

            BuildUi(session, player.HitTarget, camera, shake, gloveSprite, starSprite, impactClip);

            AssetDatabase.SaveAssets();
            System.IO.Directory.CreateDirectory("Assets/Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            Debug.Log("[Level1SceneSetup] Built and saved " + ScenePath);
        }

        /// <summary>Create-if-missing: an existing asset keeps its tuned pace across rebuilds; only its measured body height is rewritten.</summary>
        private static ClimbPace LoadOrCreatePace()
        {
            var pace = AssetDatabase.LoadAssetAtPath<ClimbPace>(ClimbPacePath);
            if (pace == null)
            {
                pace = ScriptableObject.CreateInstance<ClimbPace>();
                AssetDatabase.CreateAsset(pace, ClimbPacePath);
                AssetDatabase.SaveAssets();
            }

            return pace;
        }

        private static Sprite LoadSprite(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static AudioClip LoadImpactClip()
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(ImpactSfxPath);
        }

        /// <summary>
        /// Sets import settings once, idempotently, for the licensed character/tower/sky assets.
        /// Materials are never imported from the FBX (we assign our own explicit ones below, the
        /// same pattern the old primitive-based tower/hazard materials already used) -- this avoids
        /// depending on Unity's relative-path texture search finding the character texture, which
        /// sits flat next to its FBX here rather than in the zip's own Textures subfolder.
        /// </summary>
        private static void ConfigureImportSettings()
        {
            ConfigureModelImport(CharacterFbxPath);
            ConfigureModelImport(TowerBaseFbxPath);
            ConfigureModelImport(TowerTopFbxPath);

            ConfigureWorldTextureImport(CharacterTexturePath, 1024);
            ConfigureWorldTextureImport(SkyTexturePath, 2048);
        }

        /// <summary>
        /// The climber's soft shadow on the tower face is the reference's main contact cue. Every
        /// quality level's URP asset gets soft main-light shadows over a short distance, so the one
        /// 1024 shadow map is spent on the few metres around the climber instead of the whole tower.
        /// </summary>
        private static void ConfigureMainLightShadows()
        {
            for (int i = 0; i < QualitySettings.count; i++)
            {
                UnityEngine.Rendering.RenderPipelineAsset asset = QualitySettings.GetRenderPipelineAssetAt(i);
                if (asset == null)
                {
                    continue;
                }

                var so = new SerializedObject(asset);
                SerializedProperty distance = so.FindProperty("m_ShadowDistance");
                SerializedProperty soft = so.FindProperty("m_SoftShadowsSupported");
                SerializedProperty supported = so.FindProperty("m_MainLightShadowsSupported");
                if (distance == null || soft == null || supported == null)
                {
                    Debug.LogError("[Level1SceneSetup] Unexpected render pipeline asset layout: " + asset.name);
                    continue;
                }

                distance.floatValue = MainLightShadowDistance;
                soft.boolValue = true;
                supported.boolValue = true;
                if (so.ApplyModifiedPropertiesWithoutUndo())
                {
                    EditorUtility.SetDirty(asset);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static void ConfigureModelImport(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError("[Level1SceneSetup] Missing model importer for " + path);
                return;
            }

            bool changed = false;
            if (importer.isReadable)
            {
                importer.isReadable = false; // no runtime CPU mesh access needed -- bounds/rendering only.
                changed = true;
            }

            if (importer.importAnimation)
            {
                importer.importAnimation = false; // ClimberPoseDriver rotates the rigid parts directly; clips are time-driven and unused (see report).
                changed = true;
            }

            if (importer.importCameras)
            {
                importer.importCameras = false;
                changed = true;
            }

            if (importer.importLights)
            {
                importer.importLights = false;
                changed = true;
            }

            if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                changed = true;
            }

            if (importer.meshCompression != ModelImporterMeshCompression.Medium)
            {
                importer.meshCompression = ModelImporterMeshCompression.Medium; // simple low-poly meshes -- compression is fine.
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureWorldTextureImport(string path, int maxSize)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError("[Level1SceneSetup] Missing texture importer for " + path);
                return;
            }

            bool changed = false;
            if (importer.isReadable)
            {
                importer.isReadable = false;
                changed = true;
            }

            if (importer.maxTextureSize != maxSize)
            {
                importer.maxTextureSize = maxSize;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        /// <summary>
        /// Builds, once at editor time, the shared hazard visual prefab (a plain cylinder mesh, no
        /// collider) and its two shared materials. HazardBand only ever Instantiates the prefab and
        /// swaps sharedMaterial between the two -- it never creates a primitive or a Material at
        /// runtime. Must run after the scene reset in Build(), for the same fake-null reason
        /// BuildLevelAsset() does. Create-if-missing, update-in-place otherwise, so re-running this
        /// tool (every level, every scene regeneration) keeps the same GUIDs instead of new assets
        /// replacing old ones on every run.
        /// </summary>
        private static (GameObject prefab, Material active, Material safe) BuildHazardVisualAssets()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Art/Materials");
            System.IO.Directory.CreateDirectory("Assets/Game/Prefabs");

            Material activeMaterial = CreateOrUpdateColorMaterial(HazardActiveMaterialPath, HazardActiveColor);
            Material safeMaterial = CreateOrUpdateColorMaterial(HazardSafeMaterialPath, HazardSafeColor);
            GameObject prefab = CreateOrUpdateHazardVisualPrefab(safeMaterial);

            return (prefab, activeMaterial, safeMaterial);
        }

        private static Material CreateOrUpdateColorMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                return existing;
            }

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        /// <summary>Character material: a URP/Lit material with an explicit base map, create-if-missing / update-in-place like CreateOrUpdateColorMaterial.</summary>
        private static Material CreateOrUpdateTexturedMaterial(string path, Texture2D texture)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.mainTexture = texture;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                return existing;
            }

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { mainTexture = texture };
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static GameObject CreateOrUpdateHazardVisualPrefab(Material defaultMaterial)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(HazardVisualPrefabPath);
            if (existing != null)
            {
                return existing; // shape/collider-free geometry never changes -- keep the existing asset (and GUID).
            }

            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.GetComponent<Renderer>().sharedMaterial = defaultMaterial;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(disc, HazardVisualPrefabPath);
            Object.DestroyImmediate(disc);
            return prefab;
        }

        /// <summary>
        /// ref.png's bright cyan backdrop (GradientSky skybox) under soft, even daylight: a strong
        /// fixed trilight ambient -- deterministic without a lighting bake -- does most of the work,
        /// and a gentle sun from above-right of the camera rounds the shaft and drops the climber's
        /// soft shadow down-left onto the tower face, as in the reference.
        /// </summary>
        private static void BuildEnvironment()
        {
            Material skyMaterial = LoadOrCreateMaterial(SkyMaterialPath, "Game/GradientSky");
            skyMaterial.SetColor("_TopColor", SkyTopColor);
            skyMaterial.SetColor("_MidColor", SkyMidColor);
            skyMaterial.SetColor("_BottomColor", SkyBottomColor);
            skyMaterial.SetFloat("_MidHeight", 0.45f);
            SaveMaterial(skyMaterial);

            RenderSettings.skybox = skyMaterial;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.66f, 0.76f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.56f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.4f, 0.45f, 0.54f);
            RenderSettings.fog = false;

            var lightGo = new GameObject("Sun", typeof(Light));
            Light light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 0.8f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
            lightGo.transform.rotation = Quaternion.Euler(32f, -18f, 0f);
            RenderSettings.sun = light;
        }

        private static Material LoadOrCreateMaterial(string path, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            return material;
        }

        private static void SaveMaterial(Material material)
        {
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// A flat sea to the horizon and a broad stepped pedestal under the column, as in the
        /// reference's opening frames. The sea is finite on purpose: its far edge falls below the
        /// bottom of the frame once the camera has climbed, leaving only sky mid-climb.
        /// </summary>
        private static void BuildSeaAndPedestal(TowerMetrics tower)
        {
            Material seaMaterial = LoadOrCreateMaterial(SeaMaterialPath, "Universal Render Pipeline/Unlit");
            seaMaterial.SetColor("_BaseColor", SeaColor);
            SaveMaterial(seaMaterial);

            float pedestalHeight = tower.SegmentHeight * PedestalHeightMultiplier;

            GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Object.DestroyImmediate(sea.GetComponent<Collider>());
            sea.name = "Sea";
            sea.transform.position = new Vector3(0f, -pedestalHeight * 0.6f, 0f);
            sea.transform.localScale = new Vector3(SeaHalfExtent / 5f, 1f, SeaHalfExtent / 5f); // Plane is 10x10 units.
            sea.GetComponent<Renderer>().sharedMaterial = seaMaterial;

            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TowerBaseFbxPath);
            GameObject pedestal = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            pedestal.name = "TowerPedestal";
            float xz = tower.ScaleFactor * PedestalRadiusMultiplier;
            pedestal.transform.localScale = new Vector3(xz, tower.ScaleFactor * PedestalHeightMultiplier, xz);
            pedestal.transform.position = new Vector3(0f, -pedestalHeight, 0f);
            ApplyMaterialToRenderers(pedestal, tower.Material);
        }

        /// <summary>
        /// Soft, semi-transparent cloud streaks scattered behind the column along the whole climb, so
        /// they drift down past the camera as it rises. The broad cumulus windows are keyed gently
        /// and kept faint, giving ref.png's hazy wisps rather than isolated opaque puffs.
        /// Deterministic layout.
        /// </summary>
        private static void BuildClouds(float topHeight)
        {
            Texture2D skyTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SkyTexturePath);
            var materials = new Material[CloudWindows.Length];
            for (int i = 0; i < CloudWindows.Length; i++)
            {
                bool broad = i < 2;
                Material material = LoadOrCreateMaterial(string.Format(CloudMaterialPathFormat, i), "Game/CloudCutout");
                material.SetTexture("_MainTex", skyTexture);
                material.SetTextureScale("_MainTex", new Vector2(CloudWindows[i].x, CloudWindows[i].y));
                material.SetTextureOffset("_MainTex", new Vector2(CloudWindows[i].z, CloudWindows[i].w));
                material.SetFloat("_KeyLow", broad ? 0.58f : 0.62f);
                material.SetFloat("_KeyHigh", broad ? 0.95f : 0.9f);
                material.SetFloat("_Opacity", broad ? 0.6f : 0.75f);
                material.SetFloat("_Brightness", 1.15f);
                SaveMaterial(material);
                materials[i] = material;
            }

            var root = new GameObject("Clouds");
            var random = new System.Random(7);
            for (int i = 0; i < CloudCount; i++)
            {
                int window = i % CloudWindows.Length;
                bool broad = window < 2;
                GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(cloud.GetComponent<Collider>());
                cloud.name = "Cloud" + i;
                cloud.transform.SetParent(root.transform, false);

                float depth = (14f + (float)random.NextDouble() * 16f) * WorldScale;
                float side = (i % 2 == 0 ? -1f : 1f) * ((float)random.NextDouble() * (depth * 0.2f));
                float spacing = (topHeight + 2f * CloudMargin) / CloudCount;
                float y = -CloudMargin + (i + (float)random.NextDouble()) * spacing;

                float width = (broad ? 7f + (float)random.NextDouble() * 5f : 3f + (float)random.NextDouble() * 2.5f) * WorldScale;
                cloud.transform.position = new Vector3(side, y, depth);
                cloud.transform.localScale = new Vector3(width, width / CloudAspects[window], 1f);
                cloud.GetComponent<Renderer>().sharedMaterial = materials[window];
                cloud.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private readonly struct TowerMetrics
        {
            public readonly float Radius;
            public readonly float ScaleFactor;
            public readonly float SegmentHeight;
            public readonly Material Material;

            public TowerMetrics(float radius, float scaleFactor, float segmentHeight, Material material)
            {
                Radius = radius;
                ScaleFactor = scaleFactor;
                SegmentHeight = segmentHeight;
                Material = material;
            }
        }

        private readonly struct CharacterMetrics
        {
            public readonly float Height;
            public readonly float HalfDepth;
            public readonly float ChestHeight;

            public CharacterMetrics(float height, float halfDepth, float chestHeight)
            {
                Height = height;
                HalfDepth = halfDepth;
                ChestHeight = chestHeight;
            }
        }

        private readonly struct PlayerBuildResult
        {
            public readonly PlayerMotor Motor;
            public readonly ClimberPoseDriver PoseDriver;
            public readonly Transform HitTarget;
            public readonly CharacterMetrics Metrics;

            public PlayerBuildResult(PlayerMotor motor, ClimberPoseDriver poseDriver, Transform hitTarget, CharacterMetrics metrics)
            {
                Motor = motor;
                PoseDriver = poseDriver;
                HitTarget = hitTarget;
                Metrics = metrics;
            }
        }

        /// <summary>
        /// ref.png's tower: a smooth pale shaft, thin collars between alternating short and tall
        /// storeys, and small blue windows in two staggered columns on the camera-facing side.
        ///
        /// Asset limitation: the supplied packs have no plain round shaft or single-band collar -- the
        /// castle kit's tower-base is a spool whose two wide bands fill over half its height (it
        /// reads as a heavily banded column at full size, and as a double line when squashed into a
        /// collar). The shaft and collars are therefore Unity's built-in cylinder mesh and each window
        /// two built-in cube meshes (engine primitives, no new model or texture); the licensed
        /// tower-top forms the crown and tower-base the pedestal. The crown is sized from its measured
        /// bounds, and the radius from the camera distance/FOV/target screen-width fraction -- see the
        /// constants above Build().
        /// Everything here is static-batched: several hundred small pieces, a handful of draw calls.
        /// </summary>
        private static TowerMetrics BuildTower(float climbTop)
        {
            var root = new GameObject("Tower");

            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TowerBaseFbxPath);
            GameObject topPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TowerTopFbxPath);
            Material towerMaterial = LoadOrCreateMaterial(TowerMaterialPath, "Universal Render Pipeline/Lit");
            towerMaterial.mainTexture = null;
            towerMaterial.SetColor("_BaseColor", TowerStoneColor);
            towerMaterial.SetFloat("_Smoothness", 0.2f);
            SaveMaterial(towerMaterial);
            Material frameMaterial = CreateOrUpdateColorMaterial(WindowFrameMaterialPath, WindowFrameColor);
            Material paneMaterial = CreateOrUpdateColorMaterial(WindowPaneMaterialPath, WindowPaneColor);

            Bounds baseBounds = MeasurePrefab(basePrefab, root.transform);
            Bounds topBounds = MeasurePrefab(topPrefab, root.transform);
            float naturalRadius = Mathf.Max(baseBounds.extents.x, baseBounds.extents.z);
            float naturalPieceHeight = baseBounds.size.y;

            float radius = TowerRadius();
            float diameter = radius * 2f;
            float scaleFactor = naturalRadius > 0f ? radius / naturalRadius : 1f;

            float collarHeight = diameter * CollarHeightToDiameter;
            float collarOuterRadius = radius * CollarRadiusToShaftRadius;

            float totalClimbSpan = climbTop + TowerHeadroom;
            float y = 0f;
            int storey = 0;
            while (y < totalClimbSpan)
            {
                int kind = storey % StoreyHeightsToDiameter.Length;
                float storeyHeight = diameter * StoreyHeightsToDiameter[kind];

                GameObject collar = CreateTowerCylinder(root.transform, "Collar" + storey, collarOuterRadius * 2f, collarHeight, y, towerMaterial);
                collar.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; // a soft line of shade under each collar.

                int rows = StoreyWindowRows[kind];
                for (int row = 0; row < rows; row++)
                {
                    float rowY = y + storeyHeight * (row + 0.5f) / rows + collarHeight * 0.1f;
                    foreach (float side in new[] { -1f, 1f })
                    {
                        AddWindow(root.transform, radius, diameter, rowY, side * StoreyWindowAnglesDeg[kind], frameMaterial, paneMaterial);
                    }
                }

                y += storeyHeight;
                storey++;
            }

            CreateTowerCylinder(root.transform, "Shaft", diameter, y, y * 0.5f, towerMaterial);

            float topNaturalRadius = Mathf.Max(topBounds.extents.x, topBounds.extents.z);
            float topScale = topNaturalRadius > 0f ? collarOuterRadius / topNaturalRadius : scaleFactor;
            GameObject topInstance = (GameObject)PrefabUtility.InstantiatePrefab(topPrefab, root.transform);
            topInstance.name = "TowerTop";
            topInstance.transform.localPosition = new Vector3(0f, y - topBounds.min.y * topScale, 0f);
            topInstance.transform.localScale = Vector3.one * topScale;
            ApplyMaterialToRenderers(topInstance, towerMaterial);
            MakeStatic(topInstance);

            Debug.Log(string.Format(
                "[Level1SceneSetup] Tower: naturalRadius={0:F3} naturalPieceHeight={1:F3} radius={2:F3} collarHeight={3:F3} storeys={4} height={5:F2}",
                naturalRadius, naturalPieceHeight, radius, collarHeight, storey, y));

            return new TowerMetrics(radius, scaleFactor, naturalPieceHeight * scaleFactor, towerMaterial);
        }

        /// <summary>A static, collider-free built-in cylinder centred at centreY. Casts no shadow unless the caller enables it.</summary>
        private static GameObject CreateTowerCylinder(Transform parent, string name, float diameter, float height, float centreY, Material material)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(cylinder.GetComponent<Collider>());
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localScale = new Vector3(diameter, height * 0.5f, diameter); // the primitive is 2 units tall.
            cylinder.transform.localPosition = new Vector3(0f, centreY, 0f);
            Renderer renderer = cylinder.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            MakeStatic(cylinder);
            return cylinder;
        }

        /// <summary>A small blue window: a dark frame with a lighter pane standing just proud of it, set into the shaft at the given angle from its camera-facing (-Z) side.</summary>
        private static void AddWindow(Transform parent, float radius, float diameter, float y, float angleDeg, Material frameMaterial, Material paneMaterial)
        {
            float width = diameter * WindowWidthToDiameter;
            float height = diameter * WindowHeightToDiameter;
            float depth = diameter * 0.012f;
            float angle = angleDeg * Mathf.Deg2Rad;
            var outward = new Vector3(Mathf.Sin(angle), 0f, -Mathf.Cos(angle));
            Quaternion facing = Quaternion.LookRotation(outward);

            AddWindowPart(parent, "WindowFrame", frameMaterial, outward * radius + Vector3.up * y, facing, new Vector3(width, height, depth));
            float inset = diameter * 0.01f;
            AddWindowPart(parent, "WindowPane", paneMaterial, outward * (radius + depth * 0.2f) + Vector3.up * y, facing,
                new Vector3(width - inset * 2f, height - inset * 2f, depth));
        }

        private static void AddWindowPart(Transform parent, string name, Material material, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation;
            part.transform.localScale = scale;
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            MakeStatic(part);
        }

        /// <summary>Renderer bounds of a freshly instantiated, identity-transform probe, so sizing never depends on the importer's unit conversion.</summary>
        private static Bounds MeasurePrefab(GameObject prefab, Transform parent)
        {
            GameObject probe = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            probe.transform.localPosition = Vector3.zero;
            probe.transform.localRotation = Quaternion.identity;
            probe.transform.localScale = Vector3.one;
            Bounds bounds = ComputeWorldBounds(probe);
            Object.DestroyImmediate(probe);
            return bounds;
        }

        private static void MakeStatic(GameObject go)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>())
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            }
        }

        /// <summary>
        /// Instantiates the climber (Kenney blocky character-b, palette-recoloured to the reference
        /// climber's colours: black hair, golden-orange gi, azure sleeves and boots) on the tower's camera-facing (-Z)
        /// surface, centered on the tower's X, facing +Z (toward the tower, back to the camera). The
        /// character's own depth bounds (measured on a freshly instantiated identity-transform probe)
        /// plus the tower radius derive the Z offset. The character is scaled (see
        /// CharacterHeightToTowerDiameter) so its rendered size matches the reference proportion
        /// instead of keeping the raw FBX's unrelated-to-the-tower size. Also wires up
        /// ClimberPoseDriver's limb bindings (the blocky characters have no bones --
        /// root/leg-left, root/leg-right, root/torso/arm-left, root/torso/arm-right are separate rigid
        /// meshes, each pivoted at its own joint) and a chest-height HitTarget child for the glove burst
        /// to aim at.
        /// </summary>
        private static PlayerBuildResult BuildPlayer(TowerMetrics tower)
        {
            GameObject playerGo = new GameObject("Player");
            PlayerMotor motor = playerGo.AddComponent<PlayerMotor>();

            GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterFbxPath);
            Texture2D characterTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(CharacterTexturePath);
            Material characterMaterial = CreateOrUpdateTexturedMaterial(CharacterMaterialPath, characterTexture);

            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(playerGo.transform, false);

            GameObject characterInstance = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab, visualGo.transform);
            characterInstance.name = "CharacterModel";
            characterInstance.transform.localPosition = Vector3.zero;
            characterInstance.transform.localRotation = Quaternion.identity;
            characterInstance.transform.localScale = Vector3.one;
            ApplyMaterialToRenderers(characterInstance, characterMaterial);

            float naturalCharacterHeight = ComputeWorldBounds(characterInstance).size.y;

            float desiredCharacterHeight = CharacterHeightToTowerDiameter * (2f * tower.Radius);
            float characterScaleFactor = naturalCharacterHeight > 0f ? desiredCharacterHeight / naturalCharacterHeight : 1f;
            characterInstance.transform.localScale = Vector3.one * characterScaleFactor;

            Bounds characterBounds = ComputeWorldBounds(characterInstance);
            float characterHeight = characterBounds.size.y;
            float characterHalfDepth = characterBounds.extents.z;
            float chestHeight = characterHeight * CharacterChestHeightFraction;

            // Against the collars' outer face rather than the shaft, so they never cut through the
            // climber's body as it passes them; the soft shadow on the shaft carries the contact.
            float characterZ = -(tower.Radius * CollarRadiusToShaftRadius + characterHalfDepth);
            playerGo.transform.position = new Vector3(0f, 0f, characterZ);
            playerGo.transform.rotation = Quaternion.identity; // faces +Z (Unity default forward) -- toward the tower, back to the camera behind it at -Z.

            Transform characterRoot = characterInstance.transform.Find("root");
            Transform legLeft = characterRoot != null ? characterRoot.Find("leg-left") : null;
            Transform legRight = characterRoot != null ? characterRoot.Find("leg-right") : null;
            Transform torso = characterRoot != null ? characterRoot.Find("torso") : null;
            Transform armLeft = torso != null ? torso.Find("arm-left") : null;
            Transform armRight = torso != null ? torso.Find("arm-right") : null;

            if (characterRoot == null || legLeft == null || legRight == null || torso == null || armLeft == null || armRight == null)
            {
                Debug.LogError("[Level1SceneSetup] character-c hierarchy did not match the expected " +
                    "root/leg-left/leg-right/torso/(arm-left/arm-right) layout -- ClimberPoseDriver will be missing limb bindings.");
            }

            ClimberPoseDriver poseDriver = visualGo.AddComponent<ClimberPoseDriver>();
            BindPrivate(poseDriver, "motor", motor);
            BindPrivateIfNotNull(poseDriver, "armLeft", armLeft);
            BindPrivateIfNotNull(poseDriver, "armRight", armRight);
            BindPrivateIfNotNull(poseDriver, "legLeft", legLeft);
            BindPrivateIfNotNull(poseDriver, "legRight", legRight);
            Transform bodyRootTransform = characterRoot != null ? characterRoot : characterInstance.transform;
            BindPrivate(poseDriver, "bodyRoot", bodyRootTransform);

            var hitTargetGo = new GameObject("HitTarget");
            hitTargetGo.transform.SetParent(playerGo.transform, false);
            hitTargetGo.transform.localPosition = new Vector3(0f, chestHeight, -0.1f);

            Debug.Log(string.Format(
                "[Level1SceneSetup] Character: naturalHeight={0:F3} scaleFactor={1:F3} height={2:F3} halfDepth={3:F3} chestHeight={4:F3} playerZ={5:F3}",
                naturalCharacterHeight, characterScaleFactor, characterHeight, characterHalfDepth, chestHeight, characterZ));

            return new PlayerBuildResult(motor, poseDriver, hitTargetGo.transform, new CharacterMetrics(characterHeight, characterHalfDepth, chestHeight));
        }

        private static float TowerRadius()
        {
            return TargetTowerWidthFraction * WorldHalfWidthAtTower();
        }

        /// <summary>Half the world-space width the camera sees at the tower's depth, on the verification device's aspect.</summary>
        private static float WorldHalfWidthAtTower()
        {
            float verticalFovRad = CameraVerticalFovDeg * Mathf.Deg2Rad;
            float horizontalFovRad = 2f * Mathf.Atan(Mathf.Tan(verticalFovRad * 0.5f) * NominalDeviceAspect);
            return CameraDistance * Mathf.Tan(horizontalFovRad * 0.5f);
        }

        private static Bounds ComputeWorldBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void ApplyMaterialToRenderers(GameObject root, Material material)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
            }
        }

        private static Camera BuildCamera(PlayerMotor motor, float characterChestHeight, ClimbPace pace, out CameraShake shake)
        {
            // A level camera raised above the climber, so their chest sits CharacterScreenHeightFraction
            // up from the bottom of the frame with a long run of tower overhead, as in ref.png. Raising
            // rather than tilting keeps the shaft vertical on screen.
            float playerDistance = CameraDistance + motor.transform.position.z;
            float visibleHeight = 2f * playerDistance * Mathf.Tan(CameraVerticalFovDeg * 0.5f * Mathf.Deg2Rad);
            float raise = (0.5f - CharacterScreenHeightFraction) * visibleHeight;
            var cameraOffset = new Vector3(0f, characterChestHeight + raise, -CameraDistance);

            var rigGo = new GameObject("CameraRig");
            rigGo.transform.position = cameraOffset; // avoid starting inside the tower and lerping out on frame 1
            var follow = rigGo.AddComponent<CameraFollow>();
            BindPrivate(follow, "target", motor);
            BindPrivate(follow, "offset", cameraOffset);
            BindPrivate(follow, "pace", pace);

            var shakeGo = new GameObject("CameraShakeOffset");
            shakeGo.transform.SetParent(rigGo.transform, false);
            shake = shakeGo.AddComponent<CameraShake>();
            BindPrivate(shake, "magnitude", CameraShakeMagnitude);

            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(shakeGo.transform, false);
            Camera camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = CameraVerticalFovDeg;

            return camera;
        }

        private static void BuildUi(GameSession session, Transform hitTarget, Camera camera, CameraShake shake, Sprite gloveSprite, Sprite starSprite, AudioClip impactClip)
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

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();

            // Every full-screen menu follows the reference's GAME OVER screen: dimmed scene, bold white
            // title, and stock Unity buttons -- light for the main action, dark for the way out.
            GameObject mainMenuPanel = BuildMenuPanel(canvasRect, "MainMenuPanel", "TOWER CLIMB");
            Button startButton = AddMenuButton(mainMenuPanel.transform, "StartButton", "Start", 0, primary: true);
            UnityEventTools.AddPersistentListener(startButton.onClick, session.StartLevel);

            GameObject hudPanel = BuildPanel(canvasRect, "HudPanel");
            hudPanel.SetActive(false);
            // The test phone has a camera cutout; shrink the whole HUD to Screen.safeArea so every
            // top/bottom-anchored child below clears it instead of drawing under the notch.
            hudPanel.AddComponent<SafeAreaFitter>();

            ProgressBarParts bar = BuildProgressBar(hudPanel.transform);

            const float hudMargin = 32f;
            const float pauseSize = 110f;
            Button pauseButton = AddButton(hudPanel.transform, "PauseButton", "II", new Vector2(-hudMargin, -hudMargin),
                new Color(BarTrackColor.r, BarTrackColor.g, BarTrackColor.b, 0.85f), HudYellowColor, anchorMin: Vector2.one, anchorMax: Vector2.one, pivot: Vector2.one,
                sizeDelta: new Vector2(pauseSize, pauseSize));
            StyleHeavyText(pauseButton.GetComponentInChildren<Text>(), HudYellowColor);
            UnityEventTools.AddPersistentListener(pauseButton.onClick, session.Pause);

            BuildControlsHint(hudPanel, session);
            BuildEventFeed(hudPanel, session, gloveSprite);

            GameObject pausePanel = BuildMenuPanel(canvasRect, "PausePanel", "PAUSED");
            pausePanel.SetActive(false);
            Button resumeButton = AddMenuButton(pausePanel.transform, "ResumeButton", "Continue", 0, primary: true);
            UnityEventTools.AddPersistentListener(resumeButton.onClick, session.Resume);
            Button pauseMenuButton = AddMenuButton(pausePanel.transform, "MenuButton", "Exit", 1, primary: false);
            UnityEventTools.AddPersistentListener(pauseMenuButton.onClick, session.ReturnToMenu);

            GameObject winPanel = BuildMenuPanel(canvasRect, "WinPanel", "SUMMIT REACHED");
            winPanel.SetActive(false);
            Button winMenuButton = AddMenuButton(winPanel.transform, "MenuButton", "Exit", 0, primary: true);
            UnityEventTools.AddPersistentListener(winMenuButton.onClick, session.ReturnToMenu);

            GameObject losePanel = BuildMenuPanel(canvasRect, "LosePanel", "GAME OVER");
            losePanel.SetActive(false);
            Button retryButton = AddMenuButton(losePanel.transform, "RetryButton", "Continue", 0, primary: true);
            UnityEventTools.AddPersistentListener(retryButton.onClick, session.Retry);
            Button loseMenuButton = AddMenuButton(losePanel.transform, "MenuButton", "Exit", 1, primary: false);
            UnityEventTools.AddPersistentListener(loseMenuButton.onClick, session.ReturnToMenu);

            var hudViewGo = new GameObject("HudView");
            hudViewGo.transform.SetParent(hudPanel.transform, false);
            HudView hudView = hudViewGo.AddComponent<HudView>();
            BindPrivate(hudView, "session", session);
            BindPrivate(hudView, "progressFill", bar.Fill);
            BindPrivate(hudView, "progressMarker", bar.Marker);
            BindPrivate(hudView, "heightLabel", bar.HeightLabel);
            BindPrivate(hudView, "finishLabel", bar.FinishLabel);

            var menuViewGo = new GameObject("MenuView");
            menuViewGo.transform.SetParent(canvasGo.transform, false);
            MenuView menuView = menuViewGo.AddComponent<MenuView>();
            BindPrivate(menuView, "session", session);
            BindPrivate(menuView, "mainMenuPanel", mainMenuPanel);
            BindPrivate(menuView, "hudPanel", hudPanel);
            BindPrivate(menuView, "pausePanel", pausePanel);
            BindPrivate(menuView, "winPanel", winPanel);
            BindPrivate(menuView, "losePanel", losePanel);

            Image flashImage = AddImage(canvasGo.transform, "FlashImage", null, new Color(1f, 1f, 1f, 0f));
            Stretch(flashImage.rectTransform);

            GameObject burstRootGo = new GameObject("GloveBurstRoot", typeof(RectTransform));
            burstRootGo.transform.SetParent(canvasGo.transform, false);
            var burstRect = burstRootGo.GetComponent<RectTransform>();
            burstRect.anchorMin = burstRect.anchorMax = new Vector2(0.5f, 0.5f);
            burstRect.sizeDelta = Vector2.zero;

            var audioGo = new GameObject("ImpactAudioSource", typeof(AudioSource));
            audioGo.transform.SetParent(canvasGo.transform, false);
            AudioSource audioSource = audioGo.GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = impactClip;

            var gloveBurstGo = new GameObject("GloveBurstView");
            gloveBurstGo.transform.SetParent(canvasGo.transform, false);
            GloveBurstView gloveBurst = gloveBurstGo.AddComponent<GloveBurstView>();
            BindPrivate(gloveBurst, "session", session);
            BindPrivate(gloveBurst, "burstRoot", burstRect);
            BindPrivate(gloveBurst, "flashImage", flashImage);
            BindPrivate(gloveBurst, "cameraShake", shake);
            BindPrivate(gloveBurst, "impactAudioSource", audioSource);
            BindPrivate(gloveBurst, "gloveSprite", gloveSprite);
            BindPrivate(gloveBurst, "starSprite", starSprite);
            BindPrivate(gloveBurst, "glowSprite", KnobSprite);
            BindPrivate(gloveBurst, "hitTarget", hitTarget);
            BindPrivate(gloveBurst, "worldCamera", camera);
        }

        private readonly struct ProgressBarParts
        {
            public readonly RectTransform Fill;
            public readonly RectTransform Marker;
            public readonly Text HeightLabel;
            public readonly Text FinishLabel;

            public ProgressBarParts(RectTransform fill, RectTransform marker, Text heightLabel, Text finishLabel)
            {
                Fill = fill;
                Marker = marker;
                HeightLabel = heightLabel;
                FinishLabel = finishLabel;
            }
        }

        /// <summary>
        /// ref.png's altitude meter: a substantial dark rounded track down the left edge filling
        /// yellow from the bottom, the goal altitude in heavy outlined red above it, and the current
        /// altitude in heavy outlined yellow riding beside the top of the fill. Proportions are the
        /// image's relative to screen height, on the taller portrait frame.
        /// </summary>
        private static ProgressBarParts BuildProgressBar(Transform hudPanel)
        {
            const float barLeft = 40f;
            const float barWidth = 48f;
            const float barBottom = 0.1f;
            const float barTop = 0.8f;

            Image track = AddImage(hudPanel, "AltitudeBar", UiSprite, BarTrackColor);
            RectTransform trackRect = track.rectTransform;
            trackRect.anchorMin = new Vector2(0f, barBottom);
            trackRect.anchorMax = new Vector2(0f, barTop);
            trackRect.pivot = new Vector2(0f, 0.5f);
            trackRect.sizeDelta = new Vector2(barWidth, 0f);
            trackRect.anchoredPosition = new Vector2(barLeft, 0f);

            Image fill = AddImage(trackRect, "Fill", UiSprite, BarFillColor);
            Stretch(fill.rectTransform);
            fill.rectTransform.anchorMax = new Vector2(1f, 0f);

            var markerGo = new GameObject("Marker", typeof(RectTransform));
            markerGo.transform.SetParent(trackRect, false);
            var marker = markerGo.GetComponent<RectTransform>();
            marker.anchorMin = marker.anchorMax = new Vector2(1f, 0f);
            marker.sizeDelta = Vector2.zero;

            Text heightLabel = AddText(marker, "HeightLabel", "0", 60, TextAnchor.MiddleLeft, new Vector2(10f, 0f),
                pivot: new Vector2(0f, 0.5f), sizeDelta: new Vector2(320f, 90f));
            StyleHeavyText(heightLabel, HudYellowColor);

            Text finishLabel = AddText(hudPanel, "FinishLabel", "0", 54, TextAnchor.LowerLeft, new Vector2(barLeft - 22f, 14f),
                anchorMin: new Vector2(0f, barTop), anchorMax: new Vector2(0f, barTop), pivot: Vector2.zero, sizeDelta: new Vector2(360f, 80f));
            StyleHeavyText(finishLabel, HudRedColor);

            return new ProgressBarParts(fill.rectTransform, marker, heightLabel, finishLabel);
        }

        /// <summary>ref.png's heavy HUD lettering: coloured fill inside a thick near-black stroke, plus a soft drop shadow.</summary>
        private static void StyleHeavyText(Text text, Color fill)
        {
            UseDisplayFont(text);
            text.color = fill;
            AddOutline(text, HudStrokeColor, 3f);
            AddOutline(text, HudStrokeColor, 2f);
            text.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.35f);
        }

        /// <summary>
        /// Three pre-built event-card slots on the right edge, styled on the reference's gift cards:
        /// a glove badge overlapping a blue banner, a sender line above and the event line inside.
        /// EventFeedView only shifts text/alpha between them.
        /// </summary>
        private static void BuildEventFeed(GameObject hudPanel, GameSession session, Sprite gloveSprite)
        {
            const int slotCount = 3;
            const float slotSpacing = 190f;
            const float cardScale = 0.8f;

            var cards = new CanvasGroup[slotCount];
            var senders = new Text[slotCount];
            var details = new Text[slotCount];

            for (int i = 0; i < slotCount; i++)
            {
                var cardGo = new GameObject("EventCard" + i, typeof(RectTransform), typeof(CanvasGroup));
                cardGo.transform.SetParent(hudPanel.transform, false);
                var card = cardGo.GetComponent<RectTransform>();
                // Right-hand sky, clear of the altitude meter and its labels on the left.
                card.anchorMin = card.anchorMax = new Vector2(1f, 0.72f);
                card.pivot = new Vector2(1f, 0.5f);
                card.sizeDelta = new Vector2(520f, 150f);
                card.localScale = Vector3.one * cardScale;
                card.anchoredPosition = new Vector2(-16f, -i * slotSpacing * cardScale);

                var group = cardGo.GetComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;

                Image banner = AddImage(card, "Banner", UiSprite, CardBannerColor);
                banner.rectTransform.anchorMin = banner.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                banner.rectTransform.pivot = new Vector2(0f, 0.5f);
                banner.rectTransform.sizeDelta = new Vector2(400f, 72f);
                banner.rectTransform.anchoredPosition = new Vector2(70f, -22f);

                Image badgeOutline = AddImage(card, "BadgeOutline", gloveSprite, new Color(0.12f, 0.02f, 0.02f, 1f));
                badgeOutline.rectTransform.anchorMin = badgeOutline.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                badgeOutline.rectTransform.sizeDelta = new Vector2(128f, 128f);
                badgeOutline.rectTransform.anchoredPosition = new Vector2(66f, -12f);
                Image badge = AddImage(card, "Badge", gloveSprite, new Color(0.9f, 0.08f, 0.08f, 1f));
                badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                badge.rectTransform.sizeDelta = new Vector2(114f, 114f);
                badge.rectTransform.anchoredPosition = new Vector2(66f, -12f);

                senders[i] = AddText(card, "Sender", string.Empty, 38, TextAnchor.MiddleLeft, new Vector2(140f, 38f),
                    anchorMin: new Vector2(0f, 0.5f), anchorMax: new Vector2(0f, 0.5f), pivot: new Vector2(0f, 0.5f), sizeDelta: new Vector2(380f, 52f));
                AddOutline(senders[i], new Color(0f, 0f, 0f, 0.6f), 2f);

                details[i] = AddText(card, "Detail", string.Empty, 34, TextAnchor.MiddleLeft, new Vector2(146f, -22f),
                    anchorMin: new Vector2(0f, 0.5f), anchorMax: new Vector2(0f, 0.5f), pivot: new Vector2(0f, 0.5f), sizeDelta: new Vector2(320f, 60f));

                cards[i] = group;
            }

            var feedGo = new GameObject("EventFeedView");
            feedGo.transform.SetParent(hudPanel.transform, false);
            EventFeedView feed = feedGo.AddComponent<EventFeedView>();
            BindPrivate(feed, "session", session);
            BindPrivateArray(feed, "cards", cards);
            BindPrivateArray(feed, "senderTexts", senders);
            BindPrivateArray(feed, "detailTexts", details);
        }

        /// <summary>
        /// A short prompt laid out over ClimbInputSource's exact touch region
        /// (ClimbInputSource.TouchRegionNormalizedHeight, not a copy of the value). No band graphic:
        /// the reference frame is clean sky. Never a raycast target. Fades out once the player has
        /// climbed a little (see ControlsHintView).
        /// </summary>
        private static void BuildControlsHint(GameObject hudPanel, GameSession session)
        {
            float regionFraction = ClimbInputSource.TouchRegionNormalizedHeight;

            Text hintText = AddText(hudPanel.transform, "ClimbHintText", "Hold below to climb - release to grip", 32, TextAnchor.LowerCenter,
                new Vector2(0f, 20f), anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, regionFraction), pivot: new Vector2(0.5f, 0f), sizeDelta: new Vector2(0f, 90f));
            UseDisplayFont(hintText);
            AddOutline(hintText, new Color(0f, 0f, 0f, 0.5f), 2f);

            var hintViewGo = new GameObject("ControlsHintView");
            hintViewGo.transform.SetParent(hudPanel.transform, false);
            ControlsHintView hintView = hintViewGo.AddComponent<ControlsHintView>();
            BindPrivate(hintView, "session", session);
            BindPrivate(hintView, "hintText", hintText);
        }

        private static Sprite UiSprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        private static Sprite KnobSprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        private static GameObject BuildPanel(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            return go;
        }

        /// <summary>
        /// Full-screen dimmed panel with a bold title at the reference GAME OVER title's height
        /// (~22% down the screen). The dim also blocks touches to the HUD/climb region beneath.
        /// </summary>
        private static GameObject BuildMenuPanel(RectTransform parent, string name, string title)
        {
            GameObject panel = BuildPanel(parent, name);
            Image dim = AddImage(panel.transform, "Dim", null, MenuDimColor);
            dim.raycastTarget = true;
            Stretch(dim.rectTransform);

            Text titleText = AddText(panel.transform, "Title", title, 92, TextAnchor.MiddleCenter, Vector2.zero,
                anchorMin: new Vector2(0.5f, 0.78f), anchorMax: new Vector2(0.5f, 0.78f), sizeDelta: new Vector2(1040f, 160f));
            StyleHeavyText(titleText, HudYellowColor);
            return panel;
        }

        /// <summary>Stacked menu buttons at the reference GAME OVER screen's size and spacing, from the screen centre down.</summary>
        private static Button AddMenuButton(Transform parent, string name, string label, int slot, bool primary)
        {
            return AddButton(parent, name, label, new Vector2(0f, -slot * 217f),
                primary ? PrimaryButtonColor : SecondaryButtonColor, primary ? PrimaryButtonTextColor : Color.white,
                sizeDelta: new Vector2(608f, 120f));
        }

        /// <summary>Kenney Future for titles, numbers, buttons and prompts; small event-card text keeps the built-in font, whose lowercase stays legible at that size.</summary>
        private static void UseDisplayFont(Text text)
        {
            if (_uiFont != null)
            {
                text.font = _uiFont;
            }
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void AddOutline(Text text, Color color, float distance)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
        }

        /// <summary>Non-interactive by default (raycastTarget = false) so decoration never swallows climb touches.</summary>
        private static Image AddImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// Defaults to a centered anchor/pivot at the default 1000x150 size. Pass explicit
        /// anchors/pivot/size for anything that needs to sit at a screen edge instead of drifting off
        /// it in a taller aspect. Never a raycast target.
        /// </summary>
        private static Text AddText(Transform parent, string name, string content, int size, TextAnchor anchor, Vector2 anchoredPos,
            Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null, Vector2? sizeDelta = null)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
            rect.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta ?? new Vector2(1000f, 150f);
            rect.anchoredPosition = anchoredPos;
            return text;
        }

        /// <summary>A stock Unity button (default UISprite skin), as the reference's menus use.</summary>
        private static Button AddButton(Transform parent, string name, string label, Vector2 anchoredPos, Color background, Color textColor,
            Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null, Vector2? sizeDelta = null)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
            rect.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            Vector2 size = sizeDelta ?? new Vector2(400f, 140f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            var image = go.GetComponent<Image>();
            image.sprite = UiSprite;
            image.type = Image.Type.Sliced;
            image.color = background;

            Text text = AddText(go.transform, "Label", label, 50, TextAnchor.MiddleCenter, Vector2.zero);
            UseDisplayFont(text);
            text.rectTransform.sizeDelta = size;
            text.color = textColor;

            return go.GetComponent<Button>();
        }

        private static void BindPrivateArray(Object target, string fieldName, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null || !prop.isArray)
            {
                Debug.LogError("[Level1SceneSetup] Missing array field " + fieldName + " on " + target.GetType());
                return;
            }

            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
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

        /// <summary>Same as BindPrivate, but silently leaves the field at its serialized default (usually null) instead of crashing when value is null -- used for the character limb transforms, which BuildPlayer already logs a loud error for if the FBX hierarchy didn't match.</summary>
        private static void BindPrivateIfNotNull(object target, string fieldName, Object value)
        {
            if (value == null)
            {
                return;
            }

            BindPrivate(target, fieldName, value);
        }

        private static void AssignSerializedValue(SerializedProperty prop, object value)
        {
            switch (value)
            {
                case null:
                    prop.objectReferenceValue = null;
                    break;
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
