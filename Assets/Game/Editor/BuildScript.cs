using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class BuildScript
    {
        private const string ApkOutputPath = "Builds/Android/TowerPrototype.apk";
        private const string ReleaseApkOutputPath = "Builds/Android/TowerPrototype-release.apk";

        [MenuItem("Tower/Build Android APK")]
        public static void BuildAndroid()
        {
            Build(ApkOutputPath, BuildOptions.Development | BuildOptions.AllowDebugging);
        }

        /// <summary>The delivery APK: no development overlay, profiler hooks or script debugging.</summary>
        [MenuItem("Tower/Build Android APK (Release)")]
        public static void BuildAndroidRelease()
        {
            Build(ReleaseApkOutputPath, BuildOptions.None);
        }

        private static void Build(string outputPath, BuildOptions buildOptions)
        {
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.forceInternetPermission = true;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.towerprototype.game");

            string[] scenes = Array.ConvertAll(EditorBuildSettings.scenes, s => s.path);
            if (scenes.Length == 0)
            {
                scenes = new[] { "Assets/Game/Scenes/Smoke.unity" };
            }

            System.IO.Directory.CreateDirectory("Builds/Android");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = buildOptions,
            };

            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            Debug.Log("[BuildScript] Result=" + summary.result + " Size=" + summary.totalSize + " Errors=" + summary.totalErrors);

            if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new Exception("Android build failed: " + summary.result);
            }
        }
    }
}
