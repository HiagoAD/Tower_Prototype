using Game.Core;
using Game.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Inputs Level 1's builders share: art and audio loaded once, the parsed levels, and the settings asset.</summary>
    internal readonly struct Level1Inputs
    {
        public readonly Sprite GloveSprite;
        public readonly Sprite StarSprite;
        public readonly AudioClip ImpactClip;
        public readonly TextAsset[] LevelFiles;
        public readonly LevelDefinition[] Levels;
        public readonly GameSettings Settings;

        public Level1Inputs(Sprite gloveSprite, Sprite starSprite, AudioClip impactClip, TextAsset[] levelFiles, LevelDefinition[] levels, GameSettings settings)
        {
            Settings = settings;
            GloveSprite = gloveSprite;
            StarSprite = starSprite;
            ImpactClip = impactClip;
            LevelFiles = levelFiles;
            Levels = levels;
        }
    }

    internal readonly struct HazardVisualAssets
    {
        public readonly GameObject Prefab;
        public readonly Material Active;
        public readonly Material Safe;

        public HazardVisualAssets(GameObject prefab, Material active, Material safe)
        {
            Prefab = prefab;
            Active = active;
            Safe = safe;
        }
    }

    /// <summary>Import settings, project-level rendering settings and the create-if-missing data assets Level 1 needs.</summary>
    internal static class Level1Assets
    {
        /// <summary>
        /// Sets import settings once, idempotently, for the licensed character/tower/sky assets.
        /// Materials are never imported from the FBX (we assign our own explicit ones below, the
        /// same pattern the old primitive-based tower/hazard materials already used) -- this avoids
        /// depending on Unity's relative-path texture search finding the character texture, which
        /// sits flat next to its FBX here rather than in the zip's own Textures subfolder.
        /// </summary>
        public static void ConfigureImportSettings()
        {
            SceneAssetUtil.ConfigureModelImport(Level1Paths.CharacterFbx);
            SceneAssetUtil.ConfigureModelImport(Level1Paths.TowerBaseFbx);
            SceneAssetUtil.ConfigureModelImport(Level1Paths.TowerTopFbx);

            SceneAssetUtil.ConfigureWorldTextureImport(Level1Paths.CharacterTexture, 1024);
            SceneAssetUtil.ConfigureWorldTextureImport(Level1Paths.SkyTexture, 2048);
        }

        /// <summary>
        /// The climber's soft shadow on the tower face is the reference's main contact cue. Every
        /// quality level's URP asset gets soft main-light shadows over a short distance, so the one
        /// 1024 shadow map is spent on the few metres around the climber instead of the whole tower.
        /// </summary>
        public static void ConfigureMainLightShadows()
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

                distance.floatValue = Level1Layout.MainLightShadowDistance;
                soft.boolValue = true;
                supported.boolValue = true;
                if (so.ApplyModifiedPropertiesWithoutUndo())
                {
                    EditorUtility.SetDirty(asset);
                }
            }

            AssetDatabase.SaveAssets();
        }

        // Lilita One, not Kenney Future: Future's X is drawn as an H with two tiny notches, so EXIT read as EHIT.
        public static Font LoadUiFont()
        {
            return AssetDatabase.LoadAssetAtPath<Font>(Level1Paths.UiFont);
        }

        public static Sprite LoadGloveSprite()
        {
            return SceneAssetUtil.LoadSprite(Level1Paths.GlovePng);
        }

        public static Sprite LoadStarSprite()
        {
            return SceneAssetUtil.LoadSprite(Level1Paths.StarPng);
        }

        public static AudioClip LoadImpactClip()
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(Level1Paths.ImpactSfx);
        }

        /// <summary>Loads and parses every campaign level file, in order. Must run after the scene reset in Build().</summary>
        public static (TextAsset[] files, LevelDefinition[] levels) LoadLevels()
        {
            string[] paths = Level1Paths.LevelFiles;
            var files = new TextAsset[paths.Length];
            var levels = new LevelDefinition[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                files[i] = AssetDatabase.LoadAssetAtPath<TextAsset>(paths[i]);
                if (files[i] == null)
                {
                    throw new System.IO.FileNotFoundException("Level data missing", paths[i]);
                }

                levels[i] = LevelDefinition.FromJson(files[i].text);
            }

            return (files, levels);
        }

        /// <summary>
        /// Create-if-missing, and only populated when newly created: every tuned value, added bump type
        /// and changed default survives a scene rebuild. The builder only ever writes the measured
        /// pace.bodyHeight (Level1Wiring.BindPlayerMeasurements). The default boxing distances equal
        /// the old fixed hit displacement (1.5), so a negative bump feels like the hit it replaced.
        /// </summary>
        public static GameSettings LoadOrCreateSettings(Sprite gloveSprite)
        {
            var settings = AssetDatabase.LoadAssetAtPath<GameSettings>(Level1Paths.GameSettings);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<GameSettings>();
                settings.bumps.types[0].icon = gloveSprite;
                if (!AssetDatabase.IsValidFolder(Level1Paths.SettingsFolder))
                {
                    AssetDatabase.CreateFolder("Assets/Game", "Settings");
                }

                AssetDatabase.CreateAsset(settings, Level1Paths.GameSettings);
                AssetDatabase.SaveAssets();
            }

            return settings;
        }

        /// <summary>
        /// Builds, once at editor time, the shared hazard visual prefab (a plain cylinder mesh, no
        /// collider) and its two shared materials. HazardBand only ever Instantiates the prefab and
        /// swaps sharedMaterial between the two -- it never creates a primitive or a Material at
        /// runtime. Must run after the scene reset in Build(), for the same fake-null reason
        /// LoadLevel() does. Create-if-missing, update-in-place otherwise, so re-running this
        /// tool (every level, every scene regeneration) keeps the same GUIDs instead of new assets
        /// replacing old ones on every run.
        /// </summary>
        public static HazardVisualAssets BuildHazardVisualAssets()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Art/Materials");
            System.IO.Directory.CreateDirectory("Assets/Game/Prefabs");

            Material activeMaterial = SceneAssetUtil.CreateOrUpdateColorMaterial(Level1Paths.HazardActiveMaterial, Level1Palette.HazardActive);
            Material safeMaterial = SceneAssetUtil.CreateOrUpdateColorMaterial(Level1Paths.HazardSafeMaterial, Level1Palette.HazardSafe);
            GameObject prefab = CreateOrUpdateHazardVisualPrefab(safeMaterial);

            return new HazardVisualAssets(prefab, activeMaterial, safeMaterial);
        }

        private static GameObject CreateOrUpdateHazardVisualPrefab(Material defaultMaterial)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(Level1Paths.HazardVisualPrefab);
            if (existing != null)
            {
                return existing; // shape/collider-free geometry never changes -- keep the existing asset (and GUID).
            }

            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.GetComponent<Renderer>().sharedMaterial = defaultMaterial;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(disc, Level1Paths.HazardVisualPrefab);
            Object.DestroyImmediate(disc);
            return prefab;
        }
    }
}
