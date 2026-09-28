using Game.Core;
using Game.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Covers Retry's reset (height back to 0, session back to Playing) through GameSession's
    /// public API only -- no reflection into private state.
    ///
    /// This deliberately does not attempt full webhook/DrainBumpQueue integration coverage: that
    /// path is brittle in EditMode (no Update() ticking, and GameSession.OnEnable starts a real
    /// BumpListener on a hardcoded port as a side effect of construction). Retry() doesn't need
    /// either of those -- it is driven entirely by direct method calls. It does still open and
    /// close a real (unused) loopback listener for its lifetime; the GameObject is destroyed in
    /// every exit path to close it promptly.
    /// </summary>
    public sealed class GameSessionLoseRetryTests
    {
        [Test]
        public void Retry_ResetsHeight()
        {
            var motorGo = new GameObject("Motor");
            var sessionGo = new GameObject("Session");
            LevelDefinition level = null;
            try
            {
                PlayerMotor motor = motorGo.AddComponent<PlayerMotor>();

                level = ScriptableObject.CreateInstance<LevelDefinition>();
                level.finishHeight = 30f;
                level.climbSpeed = 2.5f;
                level.hazards = System.Array.Empty<HazardSpec>();

                GameSession session = sessionGo.AddComponent<GameSession>();
                SetPrivate(session, "motor", motor);
                SetPrivate(session, "level", level);

                session.StartLevel();
                Assert.AreEqual(Game.Core.SessionState.Playing, session.State);

                motor.ResetState(7f);
                Assert.AreEqual(7f, motor.Height, "test setup must begin above zero so retry height is meaningful");

                session.Retry();

                Assert.AreEqual(Game.Core.SessionState.Playing, session.State);
                Assert.AreEqual(0f, motor.Height);
            }
            finally
            {
                Object.DestroyImmediate(sessionGo);
                Object.DestroyImmediate(motorGo);
                if (level != null)
                {
                    Object.DestroyImmediate(level);
                }
            }
        }

        private static void SetPrivate(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(fieldName).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
