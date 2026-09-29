using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Features the brief describes but the reference game does not show, so both are off by default:
    /// lives (running out ends the run) and the pause menu (a pause button and panel). Views read
    /// these at runtime through GameSession, so toggling one needs no scene rebuild.
    /// </summary>
    [CreateAssetMenu(menuName = "Tower/Game Features", fileName = "GameFeatures")]
    public sealed class GameFeatures : ScriptableObject
    {
        public const int MaxLives = 5;

        [Tooltip("Hazard hits cost a life and the run is lost at zero. Off: hits only knock the climber back.")]
        [SerializeField] private bool livesEnabled = false;
        [Tooltip("Lives at the start of every level attempt, when lives are enabled.")]
        [Range(1, MaxLives)]
        [SerializeField] private int startingLives = 3;
        [Tooltip("Shows the pause button and panel. Off: backgrounding the app still pauses, and returning resumes.")]
        [SerializeField] private bool pauseMenuEnabled = false;

        public bool LivesEnabled => livesEnabled;

        public int StartingLives => startingLives;

        public bool PauseMenuEnabled => pauseMenuEnabled;
    }
}
