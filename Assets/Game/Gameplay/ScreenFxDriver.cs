using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// Sink for the screen post-processing pulses (chromatic aberration, bloom). CameraEffects decides
    /// the weights; the concrete driver, in the Game.PostFx assembly that references URP, applies them, so
    /// the Game assembly itself never depends on the render pipeline.
    /// </summary>
    public abstract class ScreenFxDriver : MonoBehaviour
    {
        /// <summary>Sets both effects, each 0..1 (0 = off). Called every frame while a pulse is active and once with (0, 0) when it ends.</summary>
        public abstract void Apply(float aberrationWeight, float bloomWeight);
    }
}
