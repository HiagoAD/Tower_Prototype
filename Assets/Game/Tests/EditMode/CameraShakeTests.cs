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
    }
}
