using Game.Core;
using Game.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class GameSessionPauseTests
    {
        [Test]
        public void PauseFreezesActiveKnockback_ResumeAllowsItToSettle()
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
                SetObjectReference(session, "motor", motor);
                SetObjectReference(session, "level", level);

                session.StartLevel();
                motor.ResetState(10f);
                Assert.IsTrue(motor.TryApplyHit());

                session.Pause();
                float pausedHeight = motor.Height;
                motor.Step(motor.KnockbackSeconds * 2f);

                Assert.AreEqual(Game.Core.SessionState.Paused, session.State);
                Assert.IsTrue(motor.Paused);
                Assert.IsTrue(motor.IsInKnockback, "session pause must freeze an in-flight hit reaction");
                Assert.AreEqual(pausedHeight, motor.Height, 0.0001f);

                session.Resume();
                motor.Step(motor.KnockbackSeconds);

                Assert.AreEqual(Game.Core.SessionState.Playing, session.State);
                Assert.IsFalse(motor.Paused);
                Assert.IsFalse(motor.IsInKnockback, "resuming the session must allow the hit reaction to finish");
                Assert.Less(motor.Height, pausedHeight);
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

        private static void SetObjectReference(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(fieldName);
            Assert.IsNotNull(property, "missing serialized field " + fieldName);
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
