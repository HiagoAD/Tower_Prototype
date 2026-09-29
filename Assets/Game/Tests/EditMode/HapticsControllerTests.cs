using System.Collections.Generic;
using Game.Core;
using Game.Presentation;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class HapticsControllerTests
    {
        private sealed class FakeDevice : IHapticsDevice
        {
            public readonly List<(int ms, int amplitude)> Pulses = new List<(int, int)>();
            public readonly List<long[]> Patterns = new List<long[]>();
            public int Cancels;

            public void Pulse(int milliseconds, int amplitude) => Pulses.Add((milliseconds, amplitude));

            public void Pattern(long[] timingsMilliseconds, int[] amplitudes) => Patterns.Add(timingsMilliseconds);

            public void Cancel() => Cancels++;
        }

        private HapticsSettings _settings;
        private FakeDevice _device;
        private HapticsController _controller;

        [SetUp]
        public void SetUp()
        {
            _settings = new HapticsSettings();
            _device = new FakeDevice();
            _controller = new HapticsController(_device, () => _settings);
        }

        [Test]
        public void Grab_IsRateLimited()
        {
            Assert.IsTrue(_controller.Grab(SessionState.Playing, 1f));
            Assert.IsFalse(_controller.Grab(SessionState.Playing, 1f + _settings.grabMinIntervalSeconds * 0.5f));
            Assert.IsTrue(_controller.Grab(SessionState.Playing, 1f + _settings.grabMinIntervalSeconds + 0.01f));
            Assert.AreEqual(2, _device.Pulses.Count);
        }

        [TestCase(SessionState.Menu)]
        [TestCase(SessionState.Paused)]
        [TestCase(SessionState.Won)]
        [TestCase(SessionState.Lost)]
        public void Grab_NeverPlaysOutsidePlaying(SessionState state)
        {
            Assert.IsFalse(_controller.Grab(state, 1f));
            Assert.IsFalse(_controller.HazardHit(state, 1f));
            Assert.IsEmpty(_device.Pulses);
        }

        [Test]
        public void Win_OnlyPlaysWhenWon()
        {
            Assert.IsFalse(_controller.Won(SessionState.Playing, false, 1f));
            Assert.IsFalse(_controller.Won(SessionState.Paused, true, 1f));
            Assert.IsEmpty(_device.Pulses);
            Assert.IsEmpty(_device.Patterns);
        }

        [Test]
        public void Suspended_PlaysNothing()
        {
            _controller.Suspended = true;
            Assert.IsFalse(_controller.Grab(SessionState.Playing, 1f));
            Assert.IsFalse(_controller.HazardHit(SessionState.Playing, 1f));
            Assert.IsFalse(_controller.Won(SessionState.Won, false, 1f));
            _controller.Suspended = false;
            Assert.IsTrue(_controller.Grab(SessionState.Playing, 1f));
        }

        [Test]
        public void DisabledOrZeroIntensity_PlaysNothing()
        {
            _settings.enabled = false;
            Assert.IsFalse(_controller.Grab(SessionState.Playing, 1f));
            _settings.enabled = true;
            _settings.intensity = 0f;
            Assert.IsFalse(_controller.HazardHit(SessionState.Playing, 1f));
            Assert.IsEmpty(_device.Pulses);
        }

        [Test]
        public void HazardHit_IsStrongerThanGrab()
        {
            _controller.Grab(SessionState.Playing, 1f);
            _controller.HazardHit(SessionState.Playing, 2f);
            Assert.Greater(_device.Pulses[1].amplitude, _device.Pulses[0].amplitude);
            Assert.Greater(_device.Pulses[1].ms, _device.Pulses[0].ms);
        }

        [Test]
        public void Grab_IsQuietRightAfterAHeavyPulse()
        {
            _controller.HazardHit(SessionState.Playing, 1f);
            Assert.IsFalse(_controller.Grab(SessionState.Playing, 1f + _settings.grabQuietAfterHeavySeconds * 0.5f));
            Assert.IsTrue(_controller.Grab(SessionState.Playing, 1f + _settings.grabQuietAfterHeavySeconds + 0.01f));
        }

        [Test]
        public void Intensity_ScalesAmplitudeAndDuration()
        {
            _settings.intensity = 0.5f;
            _controller.HazardHit(SessionState.Playing, 1f);
            Assert.AreEqual(HapticsController.Amplitude(_settings.hitStrength, _settings), _device.Pulses[0].amplitude);
            Assert.AreEqual(System.Math.Round(255 * _settings.hitStrength * 0.5f), _device.Pulses[0].amplitude);
            Assert.AreEqual(_settings.hitMilliseconds / 2, _device.Pulses[0].ms);
        }

        [Test]
        public void Amplitude_StaysWithinPlatformRange()
        {
            _settings.intensity = 1f;
            Assert.AreEqual(255, HapticsController.Amplitude(5f, _settings));
            Assert.AreEqual(1, HapticsController.Amplitude(0f, _settings));
        }

        [Test]
        public void Win_IsOnePulseNormallyAndAPatternOnTheFinalLevel()
        {
            Assert.IsTrue(_controller.Won(SessionState.Won, false, 1f));
            Assert.AreEqual(1, _device.Pulses.Count);
            Assert.IsEmpty(_device.Patterns);

            Assert.IsTrue(_controller.Won(SessionState.Won, true, 2f));
            Assert.AreEqual(1, _device.Pulses.Count);
            Assert.AreEqual(1, _device.Patterns.Count);
            Assert.AreEqual(_settings.finalWinPulses * 2, _device.Patterns[0].Length);
        }

        [Test]
        public void Cancel_StopsTheDeviceAndClearsTheGrabLimit()
        {
            _controller.Grab(SessionState.Playing, 1f);
            _controller.Cancel();
            Assert.AreEqual(1, _device.Cancels);
            Assert.IsTrue(_controller.Grab(SessionState.Playing, 1.01f));
        }
    }
}
