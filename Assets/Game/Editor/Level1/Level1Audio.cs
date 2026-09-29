using Game.Core;
using Game.Presentation;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Music loop and the grab/hit/bump/win sound cues.</summary>
    internal static class Level1Audio
    {
        private const string Root = "Assets/Game/Art/Licensed/Audio/";
        private const string Music = Root + "HappyLullaby/song17.mp3";
        private const string Grab = Root + "RpgAudio/handleSmallLeather.ogg";
        private const string Hit = Root + "ImpactSoundsExtra/impactPlank_medium_000.ogg";
        private const string PositiveBump = Root + "DigitalAudio/powerUp4.ogg";
        private const string LevelWin = Root + "MusicJingles/jingles_STEEL01.ogg";
        private const string FinalWin = Root + "MusicJingles/jingles_SAX07.ogg";
        private const string UiClick = Root + "InterfaceSounds/click_001.ogg";

        public static void Build(GameSession session, GameSettings settings, PlayerBuildResult player, CameraBuildResult camera)
        {
            ConfigureImportSettings();

            var go = new GameObject("GameAudio");
            var audio = go.AddComponent<GameAudio>();
            SceneBinding.Bind(audio, "session", session);
            SceneBinding.Bind(audio, "poseDriver", player.PoseDriver);
            SceneBinding.Bind(audio, "settings", settings);
            SceneBinding.Bind(audio, "musicClip", Load(Music));
            SceneBinding.Bind(audio, "grabClip", Load(Grab));
            SceneBinding.Bind(audio, "hitClip", Load(Hit));
            SceneBinding.Bind(audio, "positiveBumpClip", Load(PositiveBump));
            SceneBinding.Bind(audio, "levelWinClip", Load(LevelWin));
            SceneBinding.Bind(audio, "finalWinClip", Load(FinalWin));
            SceneBinding.Bind(audio, "uiClickClip", Load(UiClick));

            // Every button in the scene (menus, win/lose panels, HUD pause) clicks; the UI is built before this runs.
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                UnityEventTools.AddPersistentListener(button.onClick, audio.PlayUiClick);
            }
        }

        internal static AudioClip Load(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                throw new System.InvalidOperationException("[Level1Audio] Missing audio clip at " + path);
            }

            return clip;
        }

        /// <summary>Short cues decompress on load as mono; the music streams, compressed.</summary>
        private static void ConfigureImportSettings()
        {
            foreach (string path in new[] { Grab, Hit, PositiveBump, LevelWin, FinalWin, UiClick })
            {
                Configure(path, AudioClipLoadType.DecompressOnLoad, true);
            }

            Configure(Music, AudioClipLoadType.Streaming, false);
        }

        internal static void Configure(string path, AudioClipLoadType loadType, bool forceMono)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                throw new System.InvalidOperationException("[Level1Audio] No audio importer for " + path);
            }

            AudioImporterSampleSettings sample = importer.defaultSampleSettings;
            bool changed = importer.forceToMono != forceMono
                || sample.loadType != loadType
                || sample.compressionFormat != AudioCompressionFormat.Vorbis;
            if (!changed)
            {
                return;
            }

            importer.forceToMono = forceMono;
            sample.loadType = loadType;
            sample.compressionFormat = AudioCompressionFormat.Vorbis;
            sample.quality = 0.6f;
            importer.defaultSampleSettings = sample;
            importer.SaveAndReimport();
        }
    }
}
