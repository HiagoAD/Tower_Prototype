using Game.Core;
using Game.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Covers Lose -> Retry's reset (HitPoints back to starting value, height back to 0) through
    /// GameSession's public API only -- no reflection into private state.
    ///
    /// This deliberately does not attempt full webhook/DrainBumpQueue integration coverage (see the
    /// report for why that path is brittle in EditMode: no Update() ticking, and GameSession.OnEnable
    /// starts a real BumpListener on a hardcoded port as a side effect of construction). Lose() and
    /// Retry() don't need either of those -- they're driven entirely by direct method calls -- so this
    /// one test is safe on that front. It does still open and close a real (unused) loopback listener
    /// for its lifetime; the GameObject is destroyed in every exit path to close it promptly.
    ///
    /// Historically (pre-G3 fix), on-device manual play could not reach Lose in a few honest climb
    /// attempts: each hazard band's own invulnerability window (1.2s) comfortably outlasted the time
    /// needed to climb back up to it after a knockback, so a single continuous climb yielded at most
    /// one hit per band. PlayerMotor's invulnerability is now clamped to the knockback+re-grip
    /// lockout (see PlayerMotorKnockbackTests), so that is no longer true, but this test still uses
    /// PlayerMotor.ResetState (public, and the same call GameSession.StartLevel already makes)
    /// between hits to clear invulnerability instantly, instead of depending on real-time hazard-cycle
    /// luck for a test that is only exercising GameSession's Lose/Retry bookkeeping.
    /// </summary>
    public sealed class GameSessionLoseRetryTests
    {
        [Test]
        public void Lose_ThenRetry_ResetsHitPointsAndHeight()
        {
            var motorGo = new GameObject("Motor");
            var sessionGo = new GameObject("Session");
            TextAsset level = null;
            try
            {
                PlayerMotor motor = motorGo.AddComponent<PlayerMotor>();

                level = new TextAsset("{\"levelId\":1,\"displayName\":\"Test\",\"finishHeight\":30,\"climbSpeed\":2.5,\"hazards\":[]}");

                GameSession session = sessionGo.AddComponent<GameSession>();
                SetPrivate(session, "motor", motor);
                SetPrivate(session, "levelJson", level);
                SetPrivate(session, "startingHitPoints", 3);

                session.StartLevel();
                Assert.AreEqual(Game.Core.SessionState.Playing, session.State);
                Assert.AreEqual(3, session.HitPoints);

                motor.ResetState(7f);
                Assert.AreEqual(7f, motor.Height, "test setup must begin loss above zero so retry height is meaningful");

                session.OnHazardHit();
                Assert.AreEqual(2, session.HitPoints);
                motor.ResetState(motor.Height); // clears invulnerability, as if enough time had passed

                session.OnHazardHit();
                Assert.AreEqual(1, session.HitPoints);
                motor.ResetState(motor.Height);

                session.OnHazardHit();
                Assert.AreEqual(0, session.HitPoints);
                Assert.AreEqual(Game.Core.SessionState.Lost, session.State);

                session.Retry();

                Assert.AreEqual(Game.Core.SessionState.Playing, session.State);
                Assert.AreEqual(3, session.HitPoints);
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

        private static void SetPrivate(Object target, string fieldName, object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            switch (value)
            {
                case Object unityObj:
                    prop.objectReferenceValue = unityObj;
                    break;
                case int i:
                    prop.intValue = i;
                    break;
                default:
                    Assert.Fail("Unsupported bind value type " + value.GetType());
                    break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
