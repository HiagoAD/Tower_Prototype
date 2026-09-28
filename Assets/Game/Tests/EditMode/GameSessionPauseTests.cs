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
            TextAsset level = null;
            try
            {
                PlayerMotor motor = motorGo.AddComponent<PlayerMotor>();
                level = new TextAsset("{\"levelId\":1,\"displayName\":\"Test\",\"finishHeight\":30,\"climbSpeed\":2.5,\"hazards\":[]}");

                GameSession session = sessionGo.AddComponent<GameSession>();
                SetObjectReference(session, "motor", motor);
                SetLevelFiles(session, level);

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

        private static void SetLevelFiles(Object target, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty("levelFiles");
            Assert.IsNotNull(property, "missing serialized field levelFiles");
            property.arraySize = 1;
            property.GetArrayElementAtIndex(0).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
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
