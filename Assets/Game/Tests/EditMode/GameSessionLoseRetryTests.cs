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
    /// On-device manual play (see STATUS) could not reach Lose in a few honest climb attempts: each
    /// hazard band's own invulnerability window (1.2s) comfortably outlasts the time needed to climb
    /// back up to it after a knockback, so a single continuous climb yields at most one hit per band
    /// -- at most 2 hits total against this level's 2 bands, never the 3 needed to reach 0 HP. This
    /// test uses PlayerMotor.ResetState (public, and the same call GameSession.StartLevel already
    /// makes) between hits to clear that invulnerability, simulating "enough time passed", instead of
    /// depending on real-time hazard-cycle luck.
    /// </summary>
    public sealed class GameSessionLoseRetryTests
    {
        [Test]
        public void Lose_ThenRetry_ResetsHitPointsAndHeight()
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
                SetPrivate(session, "startingHitPoints", 3);

                session.StartLevel();
                Assert.AreEqual(Game.Core.SessionState.Playing, session.State);
                Assert.AreEqual(3, session.HitPoints);

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
