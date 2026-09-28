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
        private const string LevelAssetPath = "Assets/Game/Levels/Level1.asset";
        private const string GlovePngPath = "Assets/Game/Art/Licensed/BoxingGlove/boxing-glove-white.png";
        private const string ImpactSfxPath = "Assets/Game/Art/Licensed/ImpactSounds/impactPunch_heavy_000.ogg";
        private const string HazardVisualPrefabPath = "Assets/Game/Prefabs/HazardVisual.prefab";
        private const string HazardActiveMaterialPath = "Assets/Game/Art/Materials/HazardActive.mat";
        private const string HazardSafeMaterialPath = "Assets/Game/Art/Materials/HazardSafe.mat";

        private const string CharacterFbxPath = "Assets/Game/Art/Licensed/Character/character-c.fbx";
        private const string CharacterTexturePath = "Assets/Game/Art/Licensed/Character/texture-c.png";
        private const string CharacterMaterialPath = "Assets/Game/Art/Materials/CharacterC.mat";

        private const string TowerBaseFbxPath = "Assets/Game/Art/Licensed/Tower/tower-base.fbx";
        private const string TowerTopFbxPath = "Assets/Game/Art/Licensed/Tower/tower-top.fbx";
        private const string TowerColormapPath = "Assets/Game/Art/Licensed/Tower/colormap.png";
        private const string TowerMaterialPath = "Assets/Game/Art/Materials/TowerStone.mat";

        private const string SkyTexturePath = "Assets/Game/Art/Licensed/Sky/skybox-day.png";
        private const string SkyMaterialPath = "Assets/Game/Art/Materials/SkyDay.mat";

        private static readonly Color HazardActiveColor = new Color(0.9f, 0.15f, 0.1f, 0.85f);
        private static readonly Color HazardSafeColor = new Color(0.95f, 0.85f, 0.1f, 0.6f);

        // The tower's world radius is derived (not hardcoded) from these framing choices: at
        // CameraDistance world units away, a CameraVerticalFovDeg-tall lens should show the tower
        // spanning TargetTowerWidthFraction of the portrait width. NominalDeviceAspect is the actual
        // verification device's portrait aspect (1080x2520) -- the CanvasScaler already matches UI
        // width the same way across the reference (1080x1920) and the device, so calibrating the 3D
        // camera against the device we actually verify on keeps the tower reading as "narrow column"
        // there too.
        //
        // TargetTowerWidthFraction is measured directly off
        // Docs/Reference/Unity-technical-test/attachments/ref.png: the tower's lit+shaded width holds
        // steady at roughly 150-160px of the image's 1484px width (checked at several clean bands away
        // from text/character) -- about 0.10-0.11 of the frame. TargetCharacterHeightFraction is the
        // same kind of measurement, taken on the reference avatar (hair to boot sole, clear of the
        // tower's window trim) at roughly 180-200px of the image's 1060px height -- about 0.18. The
        // previous 0.30 tower fraction (and the character's un-scaled raw FBX height) were guessed
        // constants that made the tower fill most of the frame and the character loom over it edge to
        // edge; these two replace both with the same at-distance FOV derivation, calibrated once
        // against the actual reference image instead.
        private const float TargetTowerWidthFraction = 0.10f;
        private const float TargetCharacterHeightFraction = 0.18f;
        private const float CameraDistance = 8f;
        private const float CameraVerticalFovDeg = 45f;
        private const float NominalDeviceAspect = 1080f / 2520f;
        private const float TowerHeadroom = 14f; // world units of tower visible above the finish height.

        // Fraction of the character's total (feet-to-head) bounds height used both for HitTarget
        // placement (glove burst aim) and hazard hit-detection contact height (see HazardBand).
        private const float CharacterChestHeightFraction = 0.55f;

        private const float HazardVisualDiameterMultiplier = 1.15f; // slightly wider than the tower so the band visibly wraps around it.

        [MenuItem("Tower/Build Level 1 Scene")]
        public static void Build()
        {
            ConfigureImportSettings();

            Sprite gloveSprite = LoadGloveSprite();
            AudioClip impactClip = LoadImpactClip();

            // EditorSceneManager.NewScene unloads not-yet-referenced assets created earlier in this
            // same batch invocation (a freshly created-and-saved ScriptableObject has no scene/asset
            // referrer yet), which silently turns a held C# reference into a destroyed ("fake null")
            // UnityEngine.Object. Build the level asset AFTER the scene reset so it survives.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LevelDefinition level = BuildLevelAsset();
            (GameObject hazardVisualPrefab, Material hazardActiveMaterial, Material hazardSafeMaterial) = BuildHazardVisualAssets();

            BuildEnvironment();

            TowerMetrics tower = BuildTower(level.finishHeight);
            PlayerBuildResult player = BuildPlayer(tower);
            Camera camera = BuildCamera(player.Motor, player.Metrics.Height, out CameraShake shake);

            GameObject sessionGo = new GameObject("GameSession");
            GameSession session = sessionGo.AddComponent<GameSession>();
            BindPrivate(session, "motor", player.Motor);
            BindPrivate(session, "level", level);
            BindPrivate(session, "startingHitPoints", 3);
            BindPrivate(session, "hazardVisualPrefab", hazardVisualPrefab);
            BindPrivate(session, "hazardActiveMaterial", hazardActiveMaterial);
            BindPrivate(session, "hazardSafeMaterial", hazardSafeMaterial);
            BindPrivate(session, "hazardVisualDiameter", tower.Radius * 2f * HazardVisualDiameterMultiplier);
            BindPrivate(session, "hazardContactHeightOffset", player.Metrics.ChestHeight);

            BindPrivate(player.PoseDriver, "session", session);

            var climbInput = player.Motor.gameObject.AddComponent<ClimbInputSource>();
            BindPrivate(climbInput, "motor", player.Motor);

            BuildUi(session, player.HitTarget, camera, shake, gloveSprite, impactClip);

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

        private static AudioClip LoadImpactClip()
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(ImpactSfxPath);
        }

        /// <summary>
        /// Sets import settings once, idempotently, for the licensed character/tower/sky assets.
        /// Materials are never imported from the FBX (we assign our own explicit ones below, the
        /// same pattern the old primitive-based tower/hazard materials already used) -- this avoids
        /// depending on Unity's relative-path texture search finding texture-c.png/colormap.png,
        /// which sit flat next to their FBX here rather than in the zip's own Textures subfolder.
        /// </summary>
        private static void ConfigureImportSettings()
        {
            ConfigureModelImport(CharacterFbxPath);
            ConfigureModelImport(TowerBaseFbxPath);
            ConfigureModelImport(TowerTopFbxPath);

            ConfigureWorldTextureImport(CharacterTexturePath, 1024);
            ConfigureWorldTextureImport(TowerColormapPath, 512);
            ConfigureWorldTextureImport(SkyTexturePath, 2048);
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

        /// <summary>Shared helper for the character/tower materials: a URP/Lit material with an explicit base map, create-if-missing / update-in-place like CreateOrUpdateColorMaterial.</summary>
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

        /// <summary>Skybox (RenderSettings.skybox, skybox-ambient) + a directional light, so the tower/character read clearly instead of flat-lit.</summary>
        private static void BuildEnvironment()
        {
            Texture2D skyTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SkyTexturePath);
            Material skyMaterial = CreateOrUpdateSkyMaterial(skyTexture);

            RenderSettings.skybox = skyMaterial;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;

            var lightGo = new GameObject("Sun", typeof(Light));
            Light light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.98f, 0.92f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.None; // no shadow receivers worth the mobile cost in this scene.
            lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.sun = light;
        }

        /// <summary>
        /// skybox-day.png is a single 4096x2048 (2:1) equirectangular/latitude-longitude panorama,
        /// not a 6-sided cross -- Skybox/Panoramic with Mapping = Latitude Longitude Layout is the
        /// matching shader/layout for that image shape.
        /// </summary>
        private static Material CreateOrUpdateSkyMaterial(Texture2D skyTexture)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
            Material material = existing;
            if (material == null)
            {
                material = new Material(Shader.Find("Skybox/Panoramic"));
                AssetDatabase.CreateAsset(material, SkyMaterialPath);
            }

            material.SetTexture("_MainTex", skyTexture);
            material.SetFloat("_Mapping", 1f); // Latitude Longitude Layout (Cylindrical).
            material.SetFloat("_ImageType", 0f); // 360 Degrees.
            material.SetFloat("_Exposure", 1.1f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
        }

        private readonly struct TowerMetrics
        {
            public readonly float Radius;

            public TowerMetrics(float radius)
            {
                Radius = radius;
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
        /// Stacks tower-base pieces along the full climb height plus headroom, capped with one
        /// tower-top. The piece height and world radius both come from the imported mesh's actual
        /// Renderer bounds (measured on a freshly instantiated, identity-transform probe) rather than
        /// the raw FBX unit numbers, so this stays correct regardless of what unit conversion the
        /// model importer applied. The radius itself is derived from the chosen camera
        /// distance/FOV/target screen-width fraction -- see the constants above Build().
        /// </summary>
        private static TowerMetrics BuildTower(float finishHeight)
        {
            var root = new GameObject("Tower");

            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TowerBaseFbxPath);
            GameObject topPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TowerTopFbxPath);
            Texture2D colormap = AssetDatabase.LoadAssetAtPath<Texture2D>(TowerColormapPath);
            Material towerMaterial = CreateOrUpdateTexturedMaterial(TowerMaterialPath, colormap);

            GameObject baseProbe = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, root.transform);
            baseProbe.transform.localPosition = Vector3.zero;
            baseProbe.transform.localRotation = Quaternion.identity;
            baseProbe.transform.localScale = Vector3.one;
            Bounds baseBounds = ComputeWorldBounds(baseProbe);
            float naturalRadius = Mathf.Max(baseBounds.extents.x, baseBounds.extents.z);
            float naturalPieceHeight = baseBounds.size.y;
            Object.DestroyImmediate(baseProbe);

            float verticalFovRad = CameraVerticalFovDeg * Mathf.Deg2Rad;
            float horizontalFovRad = 2f * Mathf.Atan(Mathf.Tan(verticalFovRad * 0.5f) * NominalDeviceAspect);
            float worldHalfWidthAtTower = CameraDistance * Mathf.Tan(horizontalFovRad * 0.5f);
            float desiredRadius = TargetTowerWidthFraction * worldHalfWidthAtTower;

            float scaleFactor = naturalRadius > 0f ? desiredRadius / naturalRadius : 1f;
            float segmentHeight = naturalPieceHeight * scaleFactor;

            float totalClimbSpan = finishHeight + TowerHeadroom;
            int segmentCount = Mathf.Max(1, Mathf.CeilToInt(totalClimbSpan / segmentHeight));

            for (int i = 0; i < segmentCount; i++)
            {
                GameObject seg = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, root.transform);
                seg.name = "TowerSegment" + i;
                seg.transform.localPosition = new Vector3(0f, i * segmentHeight, 0f);
                seg.transform.localRotation = Quaternion.identity;
                seg.transform.localScale = Vector3.one * scaleFactor;
                ApplyMaterialToRenderers(seg, towerMaterial);
            }

            GameObject topInstance = (GameObject)PrefabUtility.InstantiatePrefab(topPrefab, root.transform);
            topInstance.name = "TowerTop";
            topInstance.transform.localPosition = new Vector3(0f, segmentCount * segmentHeight, 0f);
            topInstance.transform.localRotation = Quaternion.identity;
            topInstance.transform.localScale = Vector3.one * scaleFactor;
            ApplyMaterialToRenderers(topInstance, towerMaterial);

            Debug.Log(string.Format(
                "[Level1SceneSetup] Tower: naturalRadius={0:F3} naturalPieceHeight={1:F3} desiredRadius={2:F3} scaleFactor={3:F3} segmentHeight={4:F3} segments={5}",
                naturalRadius, naturalPieceHeight, desiredRadius, scaleFactor, segmentHeight, segmentCount));

            return new TowerMetrics(desiredRadius);
        }

        /// <summary>
        /// Instantiates character-c on the tower's camera-facing (-Z) surface, centered on the
        /// tower's X, facing +Z (toward the tower, back to the camera). The character's own depth
        /// bounds (measured the same way as the tower's, on a freshly instantiated identity-transform
        /// probe) plus the tower radius derive the Z offset -- see the class doc for why this is
        /// measured rather than assumed. The character is also scaled (see TargetCharacterHeightFraction)
        /// so its rendered height matches the reference proportion instead of keeping the raw FBX's
        /// unrelated-to-the-tower size -- left unscaled, character-c renders roughly 3x taller than the
        /// tower is wide and dominates the frame, nothing like the reference's small climber on a narrow
        /// column. Also wires up ClimberPoseDriver's limb bindings (character-c has no bones --
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

            float verticalFovRadForHeight = CameraVerticalFovDeg * Mathf.Deg2Rad;
            float worldHalfHeightAtTower = CameraDistance * Mathf.Tan(verticalFovRadForHeight * 0.5f);
            float desiredCharacterHeight = TargetCharacterHeightFraction * (2f * worldHalfHeightAtTower);
            float characterScaleFactor = naturalCharacterHeight > 0f ? desiredCharacterHeight / naturalCharacterHeight : 1f;
            characterInstance.transform.localScale = Vector3.one * characterScaleFactor;

            Bounds characterBounds = ComputeWorldBounds(characterInstance);
            float characterHeight = characterBounds.size.y;
            float characterHalfDepth = characterBounds.extents.z;
            float chestHeight = characterHeight * CharacterChestHeightFraction;

            float characterZ = -(tower.Radius + characterHalfDepth);
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

        private static Camera BuildCamera(PlayerMotor motor, float characterHeight, out CameraShake shake)
        {
            // Places the character in the lower-middle third of the portrait frame with plenty of
            // tower visible above, tuned against Docs/Reference/Unity-technical-test/attachments/ref.png.
            float cameraHeightAboveFeet = characterHeight * 1.35f;
            var cameraOffset = new Vector3(0f, cameraHeightAboveFeet, -CameraDistance);

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
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = CameraVerticalFovDeg;

            return camera;
        }

        private static void BuildUi(GameSession session, Transform hitTarget, Camera camera, CameraShake shake, Sprite gloveSprite, AudioClip impactClip)
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

            GameObject mainMenuPanel = BuildPanel(canvasRect, "MainMenuPanel");
            Text title = AddText(mainMenuPanel.transform, "Title", "Tower Prototype", 96, TextAnchor.MiddleCenter, new Vector2(0f, 300f));
            title.rectTransform.sizeDelta = new Vector2(900f, 200f);
            Button startButton = AddButton(mainMenuPanel.transform, "StartButton", "Start", new Vector2(0f, 0f));
            UnityEventTools.AddPersistentListener(startButton.onClick, session.StartLevel);

            GameObject hudPanel = BuildPanel(canvasRect, "HudPanel");
            hudPanel.SetActive(false);
            // The test phone has a camera cutout; shrink the whole HUD to Screen.safeArea so every
            // top/bottom-anchored child below clears it instead of drawing under the notch.
            hudPanel.AddComponent<SafeAreaFitter>();

            const float hudMargin = 32f;
            const float topRowHeight = 100f;
            const float rowGap = 16f;

            Text heightText = AddText(hudPanel.transform, "HeightText", "0m / 0m", 56, TextAnchor.UpperLeft, new Vector2(hudMargin, -hudMargin),
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f), pivot: new Vector2(0f, 1f), sizeDelta: new Vector2(480f, topRowHeight));
            Text hpText = AddText(hudPanel.transform, "HitPointsText", "HP: 3", 56, TextAnchor.UpperRight, new Vector2(-hudMargin, -hudMargin),
                anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f), pivot: new Vector2(1f, 1f), sizeDelta: new Vector2(280f, topRowHeight));
            Text bumpFeedText = AddText(hudPanel.transform, "BumpFeedText", string.Empty, 64, TextAnchor.UpperCenter, new Vector2(0f, 800f));

            // Second row, top-left: a different row from HeightText and a different corner from
            // HitPointsText, so Pause never overlaps either HUD label.
            Button pauseButton = AddButton(hudPanel.transform, "PauseButton", "II", new Vector2(hudMargin, -(hudMargin + topRowHeight + rowGap)),
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f), pivot: new Vector2(0f, 1f), sizeDelta: new Vector2(160f, 90f));
            UnityEventTools.AddPersistentListener(pauseButton.onClick, session.Pause);

            BuildControlsHint(hudPanel, session);

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
            BindPrivate(gloveBurst, "hitTarget", hitTarget);
            BindPrivate(gloveBurst, "worldCamera", camera);
        }

        /// <summary>
        /// A faint bottom band plus a short prompt over ClimbInputSource's exact touch region
        /// (ClimbInputSource.TouchRegionNormalizedHeight, not a copy of the value), both
        /// raycastTarget = false so neither ever intercepts a real UI button underneath. Fades out
        /// once the player has climbed a little (see ControlsHintView).
        /// </summary>
        private static void BuildControlsHint(GameObject hudPanel, GameSession session)
        {
            float regionFraction = ClimbInputSource.TouchRegionNormalizedHeight;

            var bandGo = new GameObject("ClimbHintBand", typeof(Image));
            bandGo.transform.SetParent(hudPanel.transform, false);
            var bandRect = bandGo.GetComponent<RectTransform>();
            bandRect.anchorMin = new Vector2(0f, 0f);
            bandRect.anchorMax = new Vector2(1f, regionFraction);
            bandRect.offsetMin = Vector2.zero;
            bandRect.offsetMax = Vector2.zero;
            var bandImage = bandGo.GetComponent<Image>();
            bandImage.color = new Color(1f, 1f, 1f, 0.08f);
            bandImage.raycastTarget = false;

            Text hintText = AddText(hudPanel.transform, "ClimbHintText", "Hold below to climb · release to grip", 40, TextAnchor.LowerCenter,
                new Vector2(0f, 20f), anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, regionFraction), pivot: new Vector2(0.5f, 0f), sizeDelta: new Vector2(0f, 90f));
            hintText.raycastTarget = false;

            var hintViewGo = new GameObject("ControlsHintView");
            hintViewGo.transform.SetParent(hudPanel.transform, false);
            ControlsHintView hintView = hintViewGo.AddComponent<ControlsHintView>();
            BindPrivate(hintView, "session", session);
            BindPrivate(hintView, "hintBand", bandImage);
            BindPrivate(hintView, "hintText", hintText);
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

        /// <summary>
        /// Defaults to a centered anchor/pivot at the default 1000x150 size (the original
        /// center-anchored panel titles/buttons rely on this). Pass explicit anchors/pivot/size for
        /// anything that needs to sit at a screen edge instead of drifting off it in a taller aspect.
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
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
            rect.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta ?? new Vector2(1000f, 150f);
            rect.anchoredPosition = anchoredPos;
            return text;
        }

        private static Button AddButton(Transform parent, string name, string label, Vector2 anchoredPos,
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
            go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            Text text = AddText(go.transform, "Label", label, 48, TextAnchor.MiddleCenter, Vector2.zero);
            text.rectTransform.sizeDelta = size;

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
