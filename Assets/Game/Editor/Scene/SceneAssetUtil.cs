using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Asset, importer, material and bounds helpers shared by scene builders. No scene-specific paths or values.</summary>
    public static class SceneAssetUtil
    {
        public static Sprite LoadSprite(string path)
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

        public static void ConfigureModelImport(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError("[SceneAssetUtil] Missing model importer for " + path);
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

        public static void ConfigureWorldTextureImport(string path, int maxSize)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError("[SceneAssetUtil] Missing texture importer for " + path);
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

        public static Material CreateOrUpdateColorMaterial(string path, Color color)
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
        public static Material CreateOrUpdateTexturedMaterial(string path, Texture2D texture)
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

        public static Material LoadOrCreateMaterial(string path, string shaderName)
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

        public static void SaveMaterial(Material material)
        {
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Renderer bounds of a freshly instantiated, identity-transform probe, so sizing never depends on the importer's unit conversion.</summary>
        public static Bounds MeasurePrefab(GameObject prefab, Transform parent)
        {
            GameObject probe = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            probe.transform.localPosition = Vector3.zero;
            probe.transform.localRotation = Quaternion.identity;
            probe.transform.localScale = Vector3.one;
            Bounds bounds = ComputeWorldBounds(probe);
            Object.DestroyImmediate(probe);
            return bounds;
        }

        public static Bounds ComputeWorldBounds(GameObject root)
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

        public static void ApplyMaterialToRenderers(GameObject root, Material material)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
            }
        }

        public static void MakeStatic(GameObject go)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>())
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            }
        }
    }
}
