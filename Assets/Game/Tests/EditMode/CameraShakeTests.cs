using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>CameraShake's cancel contract: back on the follow position at once, and un-paused.</summary>
    public sealed class CameraShakeTests
    {
        [Test]
        public void Cancel_ResetsOffsetImmediately_AndClearsPause()
        {
            var go = new GameObject("Shake");
            try
            {
                CameraShake shake = go.AddComponent<CameraShake>();
                shake.Shake(1f);
                shake.Paused = true;
                go.transform.localPosition = new Vector3(0.2f, 0.1f, 0f);

                shake.Cancel();

                Assert.AreEqual(Vector3.zero, go.transform.localPosition);
                Assert.IsFalse(shake.Paused);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private const float Dt = 1f / 60f;

        private static void RunFor(CameraShake shake, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
            {
                shake.Step(Dt);
            }
        }

        [Test]
        public void OverlappingChannels_UseTheStrongestAmplitude_NotTheSum()
        {
            var go = new GameObject("Shake");
            try
            {
                CameraShake shake = go.AddComponent<CameraShake>();
                shake.Shake(ShakeChannel.Bump, 1f, 2f);
                shake.Shake(ShakeChannel.Hit, 1f, 0.5f);

                Assert.AreEqual(2f, shake.CurrentAmplitude, 1e-4f, "the hit shake must not add to the bump's");
                shake.Step(Dt);
                Assert.LessOrEqual(go.transform.localPosition.magnitude, 2f + 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BumpPlusHit_ReturnExactlyToTheFollowPosition()
        {
            var go = new GameObject("Shake");
            try
            {
                CameraShake shake = go.AddComponent<CameraShake>();
                shake.Shake(ShakeChannel.Bump, 0.9f, 1.5f);
                RunFor(shake, 0.3f);
                shake.Shake(ShakeChannel.Hit, 0.25f, 0.5f);
                RunFor(shake, 0.2f);
                Assert.IsTrue(shake.IsShaking);

                RunFor(shake, 1.5f);

                Assert.IsFalse(shake.IsShaking);
                Assert.AreEqual(Vector3.zero, go.transform.localPosition, "no persistent offset once every channel has ended");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AWeakerRequest_DoesNotReplaceAStrongerRunningShakeOnTheSameChannel()
        {
            var go = new GameObject("Shake");
            try
            {
                CameraShake shake = go.AddComponent<CameraShake>();
                shake.Shake(ShakeChannel.Hit, 1f, 2f);
                shake.Shake(ShakeChannel.Hit, 0.1f, 0.2f);
                Assert.AreEqual(2f, shake.CurrentAmplitude, 1e-4f);

                RunFor(shake, 0.5f);
                shake.Shake(ShakeChannel.Hit, 1f, 2f); // a stronger one restarts it
                Assert.AreEqual(2f, shake.CurrentAmplitude, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Paused_HoldsTheTimers_AndCancelChannelLeavesTheOtherRunning()
        {
            var go = new GameObject("Shake");
            try
            {
                CameraShake shake = go.AddComponent<CameraShake>();
                shake.Shake(ShakeChannel.Bump, 1f, 1f);
                shake.Shake(ShakeChannel.Hit, 1f, 0.5f);
                shake.Paused = true;
                float before = shake.CurrentAmplitude;
                RunFor(shake, 0.5f);
                Assert.AreEqual(before, shake.CurrentAmplitude, 1e-6f, "a paused shake does not advance");

                shake.Paused = false;
                shake.Cancel(ShakeChannel.Bump);
                Assert.AreEqual(0.5f, shake.CurrentAmplitude, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}