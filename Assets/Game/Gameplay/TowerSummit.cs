using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// Puts the top of the tower where the current level ends. The scene is built tall enough for
    /// the largest finish any level can reach at the fastest pace; each time a level starts this
    /// moves the crown, ends the shaft at the crown's base and hides the collars and windows above
    /// it, so the tower stops where the climb does. The climber's feet reach FinishHeight while
    /// its hands are at the crown's lip, so the standing surface sits Lip above FinishHeight.
    /// Before any level has started it shows DefaultFinishHeight. Pieces are only toggled, never
    /// destroyed, so the next level re-shows whatever it needs.
    /// </summary>
    public sealed class TowerSummit : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private PlayerMotor motor;
        [Tooltip("The crown instance. Must not be static: it moves.")]
        [SerializeField] private Transform crown;
        [Tooltip("The shaft cylinder (a 2 unit tall primitive centred on its position). Must not be static: it is scaled.")]
        [SerializeField] private Transform shaft;
        [Tooltip("Collars and windows, in the same space as the crown; hidden when they would poke past the crown's base.")]
        [SerializeField] private Transform[] pieces;
        [Tooltip("Height of the crown's pivot above its lowest point.")]
        [SerializeField] private float crownPivotAboveBase;
        [Tooltip("Height of the surface the climber stands on above the crown's lowest point.")]
        [SerializeField] private float crownBaseToSurface;
        [Tooltip("How far above FinishHeight the standing surface sits: about where the climber's raised hands grip when its feet reach FinishHeight.")]
        [SerializeField] private float lip;
        [Tooltip("A piece is hidden when its centre plus this margin reaches the crown's base.")]
        [SerializeField] private float pieceMargin;
        [Tooltip("The finish height shown before any level has started (Level 1 at the current pace).")]
        [SerializeField] private float defaultFinishHeight;

        private SessionState _previous;

        /// <summary>Height of the surface the climber ends up standing on.</summary>
        public float SurfaceHeight { get; private set; }

        /// <summary>Height at which the shaft ends and the crown begins.</summary>
        public float CrownBaseHeight { get; private set; }

        private void OnEnable()
        {
            _previous = session != null ? session.State : SessionState.Menu;
            Place(motor != null && _previous != SessionState.Menu ? motor.FinishHeight : defaultFinishHeight);
            if (session != null)
            {
                session.StateChanged += OnSessionStateChanged;
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.StateChanged -= OnSessionStateChanged;
            }
        }

        /// <summary>Re-places the top when a level starts; resuming from pause keeps it. StartLevel sets FinishHeight before raising Playing.</summary>
        public void OnSessionStateChanged(SessionState state)
        {
            if (state == SessionState.Playing && _previous != SessionState.Paused && motor != null)
            {
                Place(motor.FinishHeight);
            }

            _previous = state;
        }

        /// <summary>Puts the standing surface at finishHeight plus the lip and shows/hides the pieces around it.</summary>
        public void Place(float finishHeight)
        {
            SurfaceHeight = finishHeight + lip;
            CrownBaseHeight = Mathf.Max(0.01f, SurfaceHeight - crownBaseToSurface);

            if (crown != null)
            {
                Vector3 position = crown.localPosition;
                position.y = CrownBaseHeight + crownPivotAboveBase;
                crown.localPosition = position;
            }

            if (shaft != null)
            {
                Vector3 scale = shaft.localScale;
                scale.y = CrownBaseHeight * 0.5f;
                shaft.localScale = scale;
                Vector3 position = shaft.localPosition;
                position.y = CrownBaseHeight * 0.5f;
                shaft.localPosition = position;
            }

            if (pieces != null)
            {
                foreach (Transform piece in pieces)
                {
                    if (piece != null)
                    {
                        piece.gameObject.SetActive(piece.localPosition.y + pieceMargin < CrownBaseHeight);
                    }
                }
            }
        }
    }
}
