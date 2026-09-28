using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>What the tower builder measured and created: sizes for the environment and player, and the parts TowerSummit is wired to.</summary>
    internal struct TowerMetrics
    {
        public readonly float Radius;
        public readonly float ScaleFactor;
        public readonly float SegmentHeight;
        public readonly Material Material;

        // Summit wiring for TowerSummit, set by BuildTower.
        public GameObject Root;
        public Transform Crown;
        public Transform Shaft;
        public Object[] Pieces;
        public float CrownPivotAboveBase;
        public float CrownBaseToSurface;
        public float PieceMargin;

        public TowerMetrics(float radius, float scaleFactor, float segmentHeight, Material material) : this()
        {
            Radius = radius;
            ScaleFactor = scaleFactor;
            SegmentHeight = segmentHeight;
            Material = material;
        }
    }

    /// <summary>Level 1's tower: shaft, collars, windows and crown.</summary>
    internal static class Level1Tower
    {
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
        /// constants in Level1Layout.
        /// Everything here is static-batched: several hundred small pieces, a handful of draw calls.
        /// </summary>
        public static TowerMetrics Build(float climbTop)
        {
            var root = new GameObject("Tower");

            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Level1Paths.TowerBaseFbx);
            GameObject topPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Level1Paths.TowerTopFbx);
            Material towerMaterial = SceneAssetUtil.LoadOrCreateMaterial(Level1Paths.TowerMaterial, "Universal Render Pipeline/Lit");
            towerMaterial.mainTexture = null;
            towerMaterial.SetColor("_BaseColor", Level1Palette.TowerStone);
            towerMaterial.SetFloat("_Smoothness", 0.2f);
            SceneAssetUtil.SaveMaterial(towerMaterial);
            Material frameMaterial = SceneAssetUtil.CreateOrUpdateColorMaterial(Level1Paths.WindowFrameMaterial, Level1Palette.WindowFrame);
            Material paneMaterial = SceneAssetUtil.CreateOrUpdateColorMaterial(Level1Paths.WindowPaneMaterial, Level1Palette.WindowPane);

            Bounds baseBounds = SceneAssetUtil.MeasurePrefab(basePrefab, root.transform);
            Bounds topBounds = SceneAssetUtil.MeasurePrefab(topPrefab, root.transform);
            float naturalRadius = Mathf.Max(baseBounds.extents.x, baseBounds.extents.z);
            float naturalPieceHeight = baseBounds.size.y;

            float radius = Level1Layout.TowerRadius();
            float diameter = radius * 2f;
            float scaleFactor = naturalRadius > 0f ? radius / naturalRadius : 1f;

            float collarHeight = diameter * Level1Layout.CollarHeightToDiameter;
            float collarOuterRadius = radius * Level1Layout.CollarRadiusToShaftRadius;

            float totalClimbSpan = climbTop + Level1Layout.TowerHeadroom;
            float y = 0f;
            int storey = 0;
            while (y < totalClimbSpan)
            {
                int kind = storey % Level1Layout.StoreyHeightsToDiameter.Length;
                float storeyHeight = diameter * Level1Layout.StoreyHeightsToDiameter[kind];

                GameObject collar = CreateTowerCylinder(root.transform, "Collar" + storey, collarOuterRadius * 2f, collarHeight, y, towerMaterial);
                collar.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; // a soft line of shade under each collar.

                int rows = Level1Layout.StoreyWindowRows[kind];
                for (int row = 0; row < rows; row++)
                {
                    float rowY = y + storeyHeight * (row + 0.5f) / rows + collarHeight * 0.1f;
                    foreach (float side in new[] { -1f, 1f })
                    {
                        AddWindow(root.transform, radius, diameter, rowY, side * Level1Layout.StoreyWindowAnglesDeg[kind], frameMaterial, paneMaterial);
                    }
                }

                y += storeyHeight;
                storey++;
            }

            // The shaft and crown are moved and resized at runtime (TowerSummit), so neither is static.
            GameObject shaftGo = CreateTowerCylinder(root.transform, "Shaft", diameter, y, y * 0.5f, towerMaterial, isStatic: false);

            float topNaturalRadius = Mathf.Max(topBounds.extents.x, topBounds.extents.z);
            float topScale = topNaturalRadius > 0f ? collarOuterRadius / topNaturalRadius : scaleFactor;
            GameObject topInstance = (GameObject)PrefabUtility.InstantiatePrefab(topPrefab, root.transform);
            topInstance.name = "TowerTop";
            topInstance.transform.localPosition = new Vector3(0f, y - topBounds.min.y * topScale, 0f);
            topInstance.transform.localScale = Vector3.one * topScale;
            SceneAssetUtil.ApplyMaterialToRenderers(topInstance, towerMaterial);

            var summitPieces = new System.Collections.Generic.List<Object>();
            foreach (Transform child in root.transform)
            {
                if (child.name.StartsWith("Collar") || child.name.StartsWith("Window"))
                {
                    summitPieces.Add(child);
                }
            }

            float crownPivotAboveBase = -topBounds.min.y * topScale;
            float crownBaseToSurface = topBounds.size.y * topScale * Level1Layout.CrownStandSurfaceFraction;
            Debug.Log(string.Format("[Level1SceneSetup] Crown: height={0:F3} pivotAboveBase={1:F3} surfaceAboveBase={2:F3}",
                topBounds.size.y * topScale, crownPivotAboveBase, crownBaseToSurface));

            Debug.Log(string.Format(
                "[Level1SceneSetup] Tower: naturalRadius={0:F3} naturalPieceHeight={1:F3} radius={2:F3} collarHeight={3:F3} storeys={4} height={5:F2}",
                naturalRadius, naturalPieceHeight, radius, collarHeight, storey, y));

            return new TowerMetrics(radius, scaleFactor, naturalPieceHeight * scaleFactor, towerMaterial)
            {
                Root = root,
                Crown = topInstance.transform,
                Shaft = shaftGo.transform,
                Pieces = summitPieces.ToArray(),
                CrownPivotAboveBase = crownPivotAboveBase,
                CrownBaseToSurface = crownBaseToSurface,
                PieceMargin = collarHeight * 0.5f,
            };
        }

        /// <summary>A static, collider-free built-in cylinder centred at centreY. Casts no shadow unless the caller enables it.</summary>
        private static GameObject CreateTowerCylinder(Transform parent, string name, float diameter, float height, float centreY, Material material, bool isStatic = true)
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
            if (isStatic)
            {
                SceneAssetUtil.MakeStatic(cylinder);
            }

            return cylinder;
        }

        /// <summary>A small blue window: a dark frame with a lighter pane standing just proud of it, set into the shaft at the given angle from its camera-facing (-Z) side.</summary>
        private static void AddWindow(Transform parent, float radius, float diameter, float y, float angleDeg, Material frameMaterial, Material paneMaterial)
        {
            float width = diameter * Level1Layout.WindowWidthToDiameter;
            float height = diameter * Level1Layout.WindowHeightToDiameter;
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
            SceneAssetUtil.MakeStatic(part);
        }
    }
}
