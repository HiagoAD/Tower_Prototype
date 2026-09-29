using Game.Core;
using Game.Gameplay;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.PostFx
{
    /// <summary>
    /// Applies CameraEffects' aberration and bloom weights through two global URP volumes whose weight
    /// is tweened (no asset is written: the profiles are built in memory). Lives in its own assembly so
    /// Game does not reference the render pipeline. To keep the mobile cost at nothing between pulses,
    /// the camera's post-processing is switched on only while a weight is above zero.
    /// </summary>
    public sealed class ScreenFxVolumeDriver : ScreenFxDriver
    {
        private const float ActiveThreshold = 0.001f;

        [SerializeField] private Camera worldCamera;
        [Tooltip("Tuning asset; the cameraFx section sets the peak intensities. Without it the built-in defaults apply.")]
        [SerializeField] private GameSettings settings;

        private Volume _aberrationVolume;
        private Volume _bloomVolume;
        private ChromaticAberration _aberration;
        private Bloom _bloom;
        private UniversalAdditionalCameraData _cameraData;
        private bool _ready;

        private void Awake()
        {
            Build();
        }

        private void Build()
        {
            CameraFeedbackSettings fx = GameSettings.OrDefaults(settings).cameraFx;

            // Each effect gets its own volume so its weight is independent; priority above any default volume.
            var aberrationProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            aberrationProfile.hideFlags = HideFlags.HideAndDontSave;
            _aberration = aberrationProfile.Add<ChromaticAberration>(true);
            _aberration.intensity.Override(fx.aberrationIntensity);
            _aberrationVolume = CreateVolume("AberrationVolume", aberrationProfile);

            var bloomProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            bloomProfile.hideFlags = HideFlags.HideAndDontSave;
            _bloom = bloomProfile.Add<Bloom>(true);
            _bloom.intensity.Override(fx.bloomIntensity);
            _bloom.threshold.Override(fx.bloomThreshold);
            _bloom.scatter.Override(fx.bloomScatter);
            _bloom.highQualityFiltering.Override(false); // the cheap path on mobile.
            _bloom.maxIterations.Override(4);
            _bloomVolume = CreateVolume("BloomVolume", bloomProfile);

            if (worldCamera != null)
            {
                _cameraData = worldCamera.GetUniversalAdditionalCameraData();
            }

            _ready = true;
            Apply(0f, 0f);
        }

        private Volume CreateVolume(string volumeName, VolumeProfile profile)
        {
            var go = new GameObject(volumeName);
            go.transform.SetParent(transform, false);
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.sharedProfile = profile;
            volume.weight = 0f;
            volume.enabled = false;
            return volume;
        }

        public override void Apply(float aberrationWeight, float bloomWeight)
        {
            if (!_ready)
            {
                return;
            }

            CameraFeedbackSettings fx = GameSettings.OrDefaults(settings).cameraFx;
            bool aberrationOn = fx.postProcessingEnabled && aberrationWeight > ActiveThreshold && fx.aberrationIntensity > 0f;
            bool bloomOn = fx.postProcessingEnabled && bloomWeight > ActiveThreshold && fx.bloomIntensity > 0f;

            if (aberrationOn)
            {
                _aberration.intensity.value = fx.aberrationIntensity;
            }

            if (bloomOn)
            {
                _bloom.intensity.value = fx.bloomIntensity;
                _bloom.threshold.value = fx.bloomThreshold;
                _bloom.scatter.value = fx.bloomScatter;
            }

            _aberrationVolume.weight = aberrationOn ? Mathf.Clamp01(aberrationWeight) : 0f;
            _aberrationVolume.enabled = aberrationOn;
            _bloomVolume.weight = bloomOn ? Mathf.Clamp01(bloomWeight) : 0f;
            _bloomVolume.enabled = bloomOn;

            if (_cameraData != null)
            {
                _cameraData.renderPostProcessing = aberrationOn || bloomOn;
            }
        }

        private void OnDisable()
        {
            Apply(0f, 0f);
        }

        private void OnDestroy()
        {
            if (_aberrationVolume != null && _aberrationVolume.sharedProfile != null)
            {
                Destroy(_aberrationVolume.sharedProfile);
            }

            if (_bloomVolume != null && _bloomVolume.sharedProfile != null)
            {
                Destroy(_bloomVolume.sharedProfile);
            }
        }
    }
}
