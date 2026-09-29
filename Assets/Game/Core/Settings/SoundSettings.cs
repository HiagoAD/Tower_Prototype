using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>Music loop and sound-effect volumes and overlap limits.</summary>
    [Serializable]
    public sealed class SoundSettings
    {
        [Header("Mix")]
        [Tooltip("Overall volume, 0-1; multiplies music and every sound effect.")]
        [Range(0f, 1f)] public float masterVolume = 1f;

        [Tooltip("Music loop volume, 0-1, before master. Kept low so the cues sit on top of it.")]
        [Range(0f, 1f)] public float musicVolume = 0.22f;

        [Tooltip("Sound-effect volume, 0-1, before master and the per-cue volume.")]
        [Range(0f, 1f)] public float sfxVolume = 1f;

        [Tooltip("Whether the music loop also plays on the menu. Off means gameplay only.")]
        public bool musicInMenu = true;

        [Tooltip("Music volume multiplier while paused or on the win/lose screen (the jingle sits on top).")]
        [Range(0f, 1f)] public float musicDuckScale = 0.3f;

        [Tooltip("How fast the music volume eases between full and ducked, in volume-units per second.")]
        public float musicFadePerSecond = 1.5f;

        [Tooltip("Sound-effect voices that can play at once. The oldest is cut when all are busy.")]
        public int sfxVoices = 6;

        [Header("Per cue: volume, minimum seconds between plays, and how many may sound at once")]
        [Tooltip("Hand grab: the quietest cue.")]
        [Range(0f, 1f)] public float grabVolume = 0.3f;
        public float grabCooldownSeconds = 0.14f;
        public int grabMaxOverlap = 2;

        [Tooltip("Random pitch spread (+/-) on grabs so a fast climb does not sound like one repeated sample.")]
        public float grabPitchJitter = 0.08f;

        [Tooltip("Hazard hit: louder than a grab, smaller than a bump.")]
        [Range(0f, 1f)] public float hitVolume = 0.55f;
        public float hitCooldownSeconds = 0.2f;
        public int hitMaxOverlap = 2;

        [Tooltip("Positive bump (boost) rising cue, played at the glove impact moment. The negative bump's punch is BumpBurstView's.")]
        [Range(0f, 1f)] public float bumpVolume = 0.8f;
        public float bumpCooldownSeconds = 0.1f;
        public int bumpMaxOverlap = 2;

        [Tooltip("Level cleared jingle (levels before the last).")]
        [Range(0f, 1f)] public float winVolume = 0.7f;

        [Tooltip("TOWER CLEARED jingle (final level); louder than the level win.")]
        [Range(0f, 1f)] public float finalWinVolume = 0.9f;

        [Tooltip("UI button click.")]
        [Range(0f, 1f)] public float clickVolume = 0.5f;
        public float clickCooldownSeconds = 0.05f;
    }
}
