using UnityEngine;

namespace Game.Presentation
{
    /// <summary>The platform vibrator. Hidden behind an interface so the scheduling logic runs in EditMode tests.</summary>
    public interface IHapticsDevice
    {
        /// <summary>One vibration. Amplitude is 1..255; a device without amplitude control uses the duration alone.</summary>
        void Pulse(int milliseconds, int amplitude);

        /// <summary>A waveform: alternating off/on durations starting with an off gap, and the amplitude of each segment.</summary>
        void Pattern(long[] timingsMilliseconds, int[] amplitudes);

        void Cancel();
    }

    public sealed class NullHapticsDevice : IHapticsDevice
    {
        public void Pulse(int milliseconds, int amplitude) { }

        public void Pattern(long[] timingsMilliseconds, int[] amplitudes) { }

        public void Cancel() { }
    }

    public static class HapticsDevices
    {
        /// <summary>The Android vibrator on a device, a no-op everywhere else (Editor, iOS, desktop).</summary>
        public static IHapticsDevice Create()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidHapticsDevice();
#else
            return new NullHapticsDevice();
#endif
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// android.os.Vibrator through JNI. API 26+ uses VibrationEffect (amplitude when the motor supports it);
    /// older devices get duration-only vibrate(). If the vibrator service cannot be reached, heavy pulses
    /// fall back to Handheld.Vibrate (which is also what makes Unity add the VIBRATE permission to the manifest).
    /// Any JNI failure disables the device for the session; play is never affected.
    /// </summary>
    internal sealed class AndroidHapticsDevice : IHapticsDevice
    {
        private const int HeavyMilliseconds = 40;

        private AndroidJavaObject _vibrator;
        private AndroidJavaClass _effectClass;
        private bool _hasAmplitudeControl;
        private bool _modernApi;
        private bool _broken;

        public AndroidHapticsDevice()
        {
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    _modernApi = version.GetStatic<int>("SDK_INT") >= 26;
                }

                if (_vibrator != null && !_vibrator.Call<bool>("hasVibrator"))
                {
                    _vibrator = null; // no motor: nothing to do, and no need to fall back to Handheld.Vibrate either.
                    _broken = true;
                    return;
                }

                if (_vibrator != null && _modernApi)
                {
                    _effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                    _hasAmplitudeControl = _vibrator.Call<bool>("hasAmplitudeControl");
                }
            }
            catch (System.Exception e)
            {
                Fail(e);
            }
        }

        public void Pulse(int milliseconds, int amplitude)
        {
            if (_broken)
            {
                return;
            }

            if (_vibrator == null)
            {
                if (milliseconds >= HeavyMilliseconds)
                {
                    Handheld.Vibrate();
                }

                return;
            }

            try
            {
                if (_modernApi && _effectClass != null)
                {
                    using (var effect = _hasAmplitudeControl
                        ? _effectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)milliseconds, amplitude)
                        : _effectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)milliseconds, -1)) // DEFAULT_AMPLITUDE
                    {
                        _vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    _vibrator.Call("vibrate", (long)milliseconds);
                }
            }
            catch (System.Exception e)
            {
                Fail(e);
            }
        }

        public void Pattern(long[] timingsMilliseconds, int[] amplitudes)
        {
            if (_broken)
            {
                return;
            }

            if (_vibrator == null)
            {
                Handheld.Vibrate();
                return;
            }

            try
            {
                if (_modernApi && _effectClass != null && _hasAmplitudeControl)
                {
                    using (var effect = _effectClass.CallStatic<AndroidJavaObject>("createWaveform", timingsMilliseconds, amplitudes, -1))
                    {
                        _vibrator.Call("vibrate", effect);
                    }
                }
                else if (_modernApi && _effectClass != null)
                {
                    using (var effect = _effectClass.CallStatic<AndroidJavaObject>("createWaveform", timingsMilliseconds, -1))
                    {
                        _vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    _vibrator.Call("vibrate", timingsMilliseconds, -1);
                }
            }
            catch (System.Exception e)
            {
                Fail(e);
            }
        }

        public void Cancel()
        {
            if (_broken || _vibrator == null)
            {
                return;
            }

            try
            {
                _vibrator.Call("cancel");
            }
            catch (System.Exception e)
            {
                Fail(e);
            }
        }

        private void Fail(System.Exception e)
        {
            _broken = true;
            Debug.LogWarning("[Haptics] Vibration unavailable, continuing without it: " + e.Message);
        }
    }
#endif
}
