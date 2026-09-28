using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Level 1's world around the tower: lighting and sky, the sea and pedestal, and the cloud field.</summary>
    internal static class Level1Environment
    {
        /// <summary>
        /// ref.png's bright cyan backdrop (GradientSky skybox) under soft, even daylight: a strong
        /// fixed trilight ambient -- deterministic without a lighting bake -- does most of the work,
        /// and a gentle sun from above-right of the camera rounds the shaft and drops the climber's
        /// soft shadow down-left onto the tower face, as in the reference.
        /// </summary>
        public static void BuildLighting()
        {
            Material skyMaterial = SceneAssetUtil.LoadOrCreateMaterial(Level1Paths.SkyMaterial, "Game/GradientSky");
            skyMaterial.SetColor("_TopColor", Level1Palette.SkyTop);
            skyMaterial.SetColor("_MidColor", Level1Palette.SkyMid);
            skyMaterial.SetColor("_BottomColor", Level1Palette.SkyBottom);
            skyMaterial.SetFloat("_MidHeight", 0.45f);
            SceneAssetUtil.SaveMaterial(skyMaterial);

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

        /// <summary>
        /// A flat sea to the horizon and a broad stepped pedestal under the column, as in the
        /// reference's opening frames. The sea is finite on purpose: its far edge falls below the
        /// bottom of the frame once the camera has climbed, leaving only sky mid-climb.
        /// </summary>
        public static void BuildSeaAndPedestal(TowerMetrics tower)
        {
            Material seaMaterial = SceneAssetUtil.LoadOrCreateMaterial(Level1Paths.SeaMaterial, "Universal Render Pipeline/Unlit");
            seaMaterial.SetColor("_BaseColor", Level1Palette.Sea);
            SceneAssetUtil.SaveMaterial(seaMaterial);

            float pedestalHeight = tower.SegmentHeight * Level1Layout.PedestalHeightMultiplier;

            GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Object.DestroyImmediate(sea.GetComponent<Collider>());
            sea.name = "Sea";
            sea.transform.position = new Vector3(0f, -pedestalHeight * 0.6f, 0f);
            sea.transform.localScale = new Vector3(Level1Layout.SeaHalfExtent / 5f, 1f, Level1Layout.SeaHalfExtent / 5f); // Plane is 10x10 units.
            sea.GetComponent<Renderer>().sharedMaterial = seaMaterial;

            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Level1Paths.TowerBaseFbx);
            GameObject pedestal = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            pedestal.name = "TowerPedestal";
            float xz = tower.ScaleFactor * Level1Layout.PedestalRadiusMultiplier;
            pedestal.transform.localScale = new Vector3(xz, tower.ScaleFactor * Level1Layout.PedestalHeightMultiplier, xz);
            pedestal.transform.position = new Vector3(0f, -pedestalHeight, 0f);
            SceneAssetUtil.ApplyMaterialToRenderers(pedestal, tower.Material);
        }

        /// <summary>
        /// Soft, semi-transparent cloud streaks scattered behind the column along the whole climb, so
        /// they drift down past the camera as it rises. The broad cumulus windows are keyed gently
        /// and kept faint, giving ref.png's hazy wisps rather than isolated opaque puffs.
        /// Deterministic layout.
        /// </summary>
        public static void BuildClouds(float topHeight)
        {
            Vector4[] windows = Level1Layout.CloudWindows;
            Texture2D skyTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Level1Paths.SkyTexture);
            var materials = new Material[windows.Length];
            for (int i = 0; i < windows.Length; i++)
            {
                bool broad = i < 2;
                Material material = SceneAssetUtil.LoadOrCreateMaterial(string.Format(Level1Paths.CloudMaterialFormat, i), "Game/CloudCutout");
                material.SetTexture("_MainTex", skyTexture);
                material.SetTextureScale("_MainTex", new Vector2(windows[i].x, windows[i].y));
                material.SetTextureOffset("_MainTex", new Vector2(windows[i].z, windows[i].w));
                material.SetFloat("_KeyLow", broad ? 0.58f : 0.62f);
                material.SetFloat("_KeyHigh", broad ? 0.95f : 0.9f);
                material.SetFloat("_Opacity", broad ? 0.6f : 0.75f);
                material.SetFloat("_Brightness", 1.15f);
                SceneAssetUtil.SaveMaterial(material);
                materials[i] = material;
            }

            var root = new GameObject("Clouds");
            var random = new System.Random(7);
            for (int i = 0; i < Level1Layout.CloudCount; i++)
            {
                int window = i % windows.Length;
                bool broad = window < 2;
                GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(cloud.GetComponent<Collider>());
                cloud.name = "Cloud" + i;
                cloud.transform.SetParent(root.transform, false);

                float depth = (14f + (float)random.NextDouble() * 16f) * Level1Layout.WorldScale;
                float side = (i % 2 == 0 ? -1f : 1f) * ((float)random.NextDouble() * (depth * 0.2f));
                float spacing = (topHeight + 2f * Level1Layout.CloudMargin) / Level1Layout.CloudCount;
                float y = -Level1Layout.CloudMargin + (i + (float)random.NextDouble()) * spacing;

                float width = (broad ? 7f + (float)random.NextDouble() * 5f : 3f + (float)random.NextDouble() * 2.5f) * Level1Layout.WorldScale;
                cloud.transform.position = new Vector3(side, y, depth);
                cloud.transform.localScale = new Vector3(width, width / Level1Layout.CloudAspects[window], 1f);
                cloud.GetComponent<Renderer>().sharedMaterial = materials[window];
                cloud.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
