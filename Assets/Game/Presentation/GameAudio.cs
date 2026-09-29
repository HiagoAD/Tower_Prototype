using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// The game's audio director: a quiet music loop (menu and gameplay, ducked while paused or on the
    /// win screen) and a small pool of sound-effect voices for the grab, hit, positive-bump and win cues.
    /// A negative bump's punch stays with BumpBurstView. Cue limits and delayed cues live in
    /// SoundCueScheduler; a bump's cue is held to its impact moment on session time, so pause freezes it.
    /// Pause pauses the voices and ducks the music rather than using AudioListener.pause, which would
    /// also mute UI clicks.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private ClimberPoseDriver poseDriver;
        [Tooltip("Tuning asset; the sound section sets volumes, cooldowns and overlap limits.")]
        [SerializeField] private GameSettings settings;
        [SerializeField] private AudioClip musicClip;
        [SerializeField] private AudioClip grabClip;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip positiveBumpClip;
        [SerializeField] private AudioClip levelWinClip;
        [SerializeField] private AudioClip finalWinClip;
        [SerializeField] private AudioClip uiClickClip;

        private SoundSettings Sound => GameSettings.OrDefaults(settings).sound;

        private SoundCueScheduler _scheduler;
        private readonly List<SoundCue> _due = new List<SoundCue>();
        private AudioSource _music;
        private AudioSource _ui;
        private AudioSource[] _voices;
        private int _nextVoice;
        private float _duck = 1f;

        private void Awake()
        {
            _scheduler = new SoundCueScheduler(Sound);
            _music = CreateSource("Music", true);
            _music.clip = musicClip;
            _ui = CreateSource("UiClick", false);
            _voices = new AudioSource[Mathf.Max(1, Sound.sfxVoices)];
            for (int i = 0; i < _voices.Length; i++)
            {
                _voices[i] = CreateSource("Sfx" + i, false);
            }
        }

        private void OnEnable()
        {
            if (session != null)
            {
                session.StateChanged += OnStateChanged;
                session.LevelStarted += OnLevelStarted;
                session.HazardHit += OnHazardHit;
                session.BumpAccepted += OnBumpAccepted;
            }

            if (poseDriver != null)
            {
                poseDriver.HandPlanted += OnHandPlanted;
            }

            if (session != null)
            {
                OnStateChanged(session.State);
                _duck = DuckTarget(session.State);
            }

            ApplyMusicVolume();
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.StateChanged -= OnStateChanged;
                session.LevelStarted -= OnLevelStarted;
                session.HazardHit -= OnHazardHit;
                session.BumpAccepted -= OnBumpAccepted;
            }

            if (poseDriver != null)
            {
                poseDriver.HandPlanted -= OnHandPlanted;
            }
        }

        private void Update()
        {
            if (session != null)
            {
                _due.Clear();
                _scheduler.Advance(session.PlayDeltaTime, _due);
                foreach (SoundCue cue in _due)
                {
                    Play(cue);
                }

                _duck = Mathf.MoveTowards(_duck, DuckTarget(session.State), Sound.musicFadePerSecond * Time.unscaledDeltaTime);
            }

            ApplyMusicVolume();
        }

        /// <summary>Plays the UI click; for button handlers. Independent of the session, so it also works on the menu.</summary>
        public void PlayUiClick()
        {
            Play(SoundCue.UiClick);
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        private float DuckTarget(SessionState state)
        {
            return state == SessionState.Playing || state == SessionState.Menu ? 1f : Sound.musicDuckScale;
        }

        private void ApplyMusicVolume()
        {
            SoundSettings sound = Sound;
            _music.volume = sound.masterVolume * sound.musicVolume * _duck;
        }

        private void SetMusicPlaying(bool playing)
        {
            if (playing && musicClip != null)
            {
                if (!_music.isPlaying)
                {
                    _music.Play();
                }
            }
            else if (_music.isPlaying)
            {
                _music.Stop();
            }
        }

        private void StopVoices()
        {
            foreach (AudioSource voice in _voices)
            {
                voice.Stop();
            }
        }

        private void SetVoicesPaused(bool paused)
        {
            foreach (AudioSource voice in _voices)
            {
                if (paused)
                {
                    voice.Pause();
                }
                else
                {
                    voice.UnPause();
                }
            }
        }

        private void OnStateChanged(SessionState state)
        {
            SetMusicPlaying(state != SessionState.Menu || Sound.musicInMenu);
            SetVoicesPaused(state == SessionState.Paused);

            switch (state)
            {
                case SessionState.Menu:
                    _scheduler.Cancel();
                    StopVoices();
                    break;
                case SessionState.Won:
                    _scheduler.Cancel();
                    StopVoices();
                    Play(SoundCueScheduler.WinCue(session.IsFinalLevel));
                    break;
                case SessionState.Lost:
                    _scheduler.Cancel();
                    break;
            }
        }

        private void OnLevelStarted()
        {
            _scheduler.Cancel();
            StopVoices();
        }

        private void OnHandPlanted(Vector3 _)
        {
            if (session != null && session.State == SessionState.Playing)
            {
                Play(SoundCue.Grab);
            }
        }

        private void OnHazardHit()
        {
            Play(SoundCue.Hit);
        }

        private void OnBumpAccepted(BumpEvent bump)
        {
            SoundCue? cue = SoundCueScheduler.BumpCue(bump);
            if (cue.HasValue)
            {
                _scheduler.Schedule(cue.Value, session.BumpImpactDelaySeconds);
            }
        }

        private AudioClip ClipOf(SoundCue cue)
        {
            switch (cue)
            {
                case SoundCue.Grab: return grabClip;
                case SoundCue.Hit: return hitClip;
                case SoundCue.PositiveBump: return positiveBumpClip;
                case SoundCue.LevelWin: return levelWinClip;
                case SoundCue.FinalWin: return finalWinClip;
                case SoundCue.UiClick: return uiClickClip;
                default: return null;
            }
        }

        private void Play(SoundCue cue)
        {
            AudioClip clip = ClipOf(cue);
            if (clip == null || !_scheduler.TryPlay(cue, Time.unscaledTime, clip.length))
            {
                return;
            }

            SoundSettings sound = Sound;
            float volume = sound.masterVolume * sound.sfxVolume * _scheduler.VolumeOf(cue);
            if (cue == SoundCue.UiClick)
            {
                _ui.PlayOneShot(clip, volume);
                return;
            }

            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            voice.pitch = cue == SoundCue.Grab ? 1f + Random.Range(-sound.grabPitchJitter, sound.grabPitchJitter) : 1f;
            voice.clip = clip;
            voice.volume = volume;
            voice.Play();
        }
    }
}
