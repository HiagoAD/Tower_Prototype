using Game.Core;
using Game.Gameplay;
using Game.Presentation;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct CharacterMetrics
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

    /// <summary>The climber's motor and presentation components plus the measurements other builders (camera, summit, UI) derive from.</summary>
    internal readonly struct PlayerBuildResult
    {
        public readonly PlayerMotor Motor;
        public readonly ClimberPoseDriver PoseDriver;
        public readonly Transform HitTarget;
        public readonly CharacterMetrics Metrics;
        public readonly SummitSlideView SlideView;

        public PlayerBuildResult(PlayerMotor motor, ClimberPoseDriver poseDriver, Transform hitTarget, CharacterMetrics metrics, SummitSlideView slideView)
        {
            SlideView = slideView;
            Motor = motor;
            PoseDriver = poseDriver;
            HitTarget = hitTarget;
            Metrics = metrics;
        }
    }

    /// <summary>Level 1's climber: the Kenney character on the tower face with its motor, slide and pose driver.</summary>
    internal static class Level1Player
    {
        /// <summary>
        /// Instantiates the climber (Kenney blocky character-b, palette-recoloured to the reference
        /// climber's colours: black hair, golden-orange gi, azure sleeves and boots) on the tower's camera-facing (-Z)
        /// surface, centered on the tower's X, facing +Z (toward the tower, back to the camera). The
        /// character's own depth bounds (measured on a freshly instantiated identity-transform probe)
        /// plus the tower radius derive the Z offset. The character is scaled (see
        /// Level1Layout.CharacterHeightToTowerDiameter) so its rendered size matches the reference proportion
        /// instead of keeping the raw FBX's unrelated-to-the-tower size. Also wires up
        /// ClimberPoseDriver's limb bindings (the blocky characters have no bones --
        /// root/leg-left, root/leg-right, root/torso/arm-left, root/torso/arm-right are separate rigid
        /// meshes, each pivoted at its own joint) and a chest-height HitTarget child for the glove burst
        /// to aim at.
        /// </summary>
        public static PlayerBuildResult Build(TowerMetrics tower, GameSettings settings)
        {
            GameObject playerGo = new GameObject("Player");
            PlayerMotor motor = playerGo.AddComponent<PlayerMotor>();
            SceneBinding.Bind(motor, "settings", settings);

            GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Level1Paths.CharacterFbx);
            Texture2D characterTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Level1Paths.CharacterTexture);
            Material characterMaterial = SceneAssetUtil.CreateOrUpdateTexturedMaterial(Level1Paths.CharacterMaterial, characterTexture);

            // Between the motor's Player and the Visual (which carries the pose driver): the summit
            // slide moves this, so the pose driver's maths, relative to its own transform, is untouched.
            var slideGo = new GameObject("SummitSlide");
            slideGo.transform.SetParent(playerGo.transform, false);
            SummitSlideView slideView = slideGo.AddComponent<SummitSlideView>();
            SceneBinding.Bind(slideView, "settings", settings);

            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(slideGo.transform, false);

            GameObject characterInstance = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab, visualGo.transform);
            characterInstance.name = "CharacterModel";
            characterInstance.transform.localPosition = Vector3.zero;
            characterInstance.transform.localRotation = Quaternion.identity;
            characterInstance.transform.localScale = Vector3.one;
            SceneAssetUtil.ApplyMaterialToRenderers(characterInstance, characterMaterial);

            float naturalCharacterHeight = SceneAssetUtil.ComputeWorldBounds(characterInstance).size.y;

            float desiredCharacterHeight = Level1Layout.CharacterHeightToTowerDiameter * (2f * tower.Radius);
            float characterScaleFactor = naturalCharacterHeight > 0f ? desiredCharacterHeight / naturalCharacterHeight : 1f;
            characterInstance.transform.localScale = Vector3.one * characterScaleFactor;

            Bounds characterBounds = SceneAssetUtil.ComputeWorldBounds(characterInstance);
            float characterHeight = characterBounds.size.y;
            float characterHalfDepth = characterBounds.extents.z;
            float chestHeight = characterHeight * Level1Layout.CharacterChestHeightFraction;

            // Against the collars' outer face rather than the shaft, so they never cut through the
            // climber's body as it passes them; the soft shadow on the shaft carries the contact.
            float characterZ = -(tower.Radius * Level1Layout.CollarRadiusToShaftRadius + characterHalfDepth);
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
            SceneBinding.Bind(poseDriver, "motor", motor);
            SceneBinding.Bind(poseDriver, "settings", settings);
            SceneBinding.BindIfNotNull(poseDriver, "armLeft", armLeft);
            SceneBinding.BindIfNotNull(poseDriver, "armRight", armRight);
            SceneBinding.BindIfNotNull(poseDriver, "legLeft", legLeft);
            SceneBinding.BindIfNotNull(poseDriver, "legRight", legRight);
            Transform bodyRootTransform = characterRoot != null ? characterRoot : characterInstance.transform;
            SceneBinding.Bind(poseDriver, "bodyRoot", bodyRootTransform);

            var hitTargetGo = new GameObject("HitTarget");
            hitTargetGo.transform.SetParent(playerGo.transform, false);
            hitTargetGo.transform.localPosition = new Vector3(0f, chestHeight, -0.1f);

            Debug.Log(string.Format(
                "[Level1SceneSetup] Character: naturalHeight={0:F3} scaleFactor={1:F3} height={2:F3} halfDepth={3:F3} chestHeight={4:F3} playerZ={5:F3}",
                naturalCharacterHeight, characterScaleFactor, characterHeight, characterHalfDepth, chestHeight, characterZ));

            return new PlayerBuildResult(motor, poseDriver, hitTargetGo.transform, new CharacterMetrics(characterHeight, characterHalfDepth, chestHeight), slideView);
        }
    }
}
