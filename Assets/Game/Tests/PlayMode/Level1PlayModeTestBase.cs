using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>Loads a fresh Level1 before every test and unloads it (releasing the bump listener port) after.</summary>
    public abstract class Level1PlayModeTestBase
    {
        internal Level1Harness H;

        [UnitySetUp]
        public IEnumerator LoadLevel()
        {
            H = new Level1Harness();
            yield return H.Load();
        }

        [UnityTearDown]
        public IEnumerator UnloadLevel()
        {
            if (H != null)
            {
                yield return H.Unload();
            }
        }
    }
}
