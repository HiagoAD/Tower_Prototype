using Game.Core;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// A small spark burst where a hand lands. One pooled ParticleSystem, emitted into by position on each
    /// real grab (ClimberPoseDriver.HandPlanted), capped at GrabFeedbackSettings.maxParticles. Frozen while paused
    /// and cleared on level start, menu, win and loss.
    /// </summary>
    public sealed class GrabParticlesView : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private ClimberPoseDriver poseDriver;
        [SerializeField] private ParticleSystem sparks;
        [Tooltip("Tuning asset (grab section).")]
        [SerializeField] private GameSettings settings;

        private GrabFeedbackSettings Grab => GameSettings.OrDefaults(settings).grab;

        private void OnEnable()
        {
            poseDriver.HandPlanted += OnHandPlanted;
            session.StateChanged += OnStateChanged;
            session.LevelStarted += OnLevelStarted;
            ParticleSystem.MainModule main = sparks.main;
            main.maxParticles = Grab.maxParticles;
            sparks.Clear();
            sparks.Play();
        }

        private void OnDisable()
        {
            poseDriver.HandPlanted -= OnHandPlanted;
            session.StateChanged -= OnStateChanged;
            session.LevelStarted -= OnLevelStarted;
            sparks.Clear();
        }

        private void OnHandPlanted(Vector3 hand)
        {
            if (session.State != SessionState.Playing)
            {
                return;
            }

            Emit(hand, session.BodyHeight, Grab, sparks);
        }

        /// <summary>Bursts the configured particles outward from a hand position, nudged toward the camera (-Z, the tower's visible face).</summary>
        public static void Emit(Vector3 hand, float bodyHeight, GrabFeedbackSettings s, ParticleSystem system)
        {
            if (!s.enabled || s.particlesPerGrab <= 0)
            {
                return;
            }

            var emit = new ParticleSystem.EmitParams
            {
                position = hand + Vector3.back * (s.towardCameraBodyHeights * bodyHeight),
                startLifetime = s.lifetimeSeconds,
                startColor = s.color,
            };

            float phase = Random.value * Mathf.PI * 2f;
            for (int i = 0; i < s.particlesPerGrab; i++)
            {
                float angle = phase + Mathf.PI * 2f * i / s.particlesPerGrab;
                float speed = s.speedBodyHeights * bodyHeight * Random.Range(0.6f, 1f);
                emit.velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * speed;
                emit.startSize = s.sizeBodyHeights * bodyHeight * (1f + Random.Range(-s.sizeJitter, s.sizeJitter));
                system.Emit(emit, 1);
            }
        }

        private void OnStateChanged(SessionState state)
        {
            switch (state)
            {
                case SessionState.Playing:
                    sparks.Play(); // resumes a paused system; a no-op otherwise.
                    break;
                case SessionState.Paused:
                    sparks.Pause();
                    break;
                default: // menu, win, loss, and anything added later: nothing left over.
                    sparks.Clear();
                    break;
            }
        }

        private void OnLevelStarted()
        {
            sparks.Clear();
            sparks.Play();
        }
    }
}
