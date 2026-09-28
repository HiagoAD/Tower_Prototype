using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class BuildScript
    {
        private const string ApkOutputPath = "Builds/Android/TowerPrototype.apk";

        [MenuItem("Tower/Build Android APK")]
        public static void BuildAndroid()
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
                locationPathName = ApkOutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging,
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
