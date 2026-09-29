using Game.Core;
using Game.Presentation;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Grab feedback on the climber: hand-grab particles and device haptics.</summary>
    internal static class Level1GrabFx
    {
        private const string SparkMaterialPath = "Assets/Game/Art/Materials/GrabSpark.mat";

        public static void Build(GameSession session, GameSettings settings, PlayerBuildResult player, CameraBuildResult camera)
        {
            ParticleSystem sparks = BuildSparks();

            var particlesGo = new GameObject("GrabParticles");
            GrabParticlesView particles = particlesGo.AddComponent<GrabParticlesView>();
            sparks.transform.SetParent(particlesGo.transform, false);
            SceneBinding.Bind(particles, "session", session);
            SceneBinding.Bind(particles, "poseDriver", player.PoseDriver);
            SceneBinding.Bind(particles, "sparks", sparks);
            SceneBinding.Bind(particles, "settings", settings);

            var hapticsGo = new GameObject("Haptics");
            HapticsView haptics = hapticsGo.AddComponent<HapticsView>();
            SceneBinding.Bind(haptics, "session", session);
            SceneBinding.Bind(haptics, "poseDriver", player.PoseDriver);
            SceneBinding.Bind(haptics, "settings", settings);
        }

        /// <summary>
        /// One world-space system that never emits on its own (bursts are emitted by position at each grab): star
        /// sprites that shrink and fade over their life. Per-burst size, speed, lifetime and tint come from the settings.
        /// </summary>
        private static ParticleSystem BuildSparks()
        {
            var go = new GameObject("GrabSparks");
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            color.color = new ParticleSystem.MinMaxGradient(fade);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.sharedMaterial = SparkMaterial();
            return system;
        }

        /// <summary>The built-in alpha-blended sprite shader with the licensed star sprite: no generated art, no extra shader to strip.</summary>
        private static Material SparkMaterial()
        {
            Material material = SceneAssetUtil.LoadOrCreateMaterial(SparkMaterialPath, "Sprites/Default");
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Level1Paths.StarPng);
            SceneAssetUtil.SaveMaterial(material);
            return material;
        }
    }
}
