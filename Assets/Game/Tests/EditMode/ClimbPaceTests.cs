using Game.Core;
using Game.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// ClimbPace is the single climb-speed setting: a level's speed and all of its distances scale by
    /// the same factor, so the level keeps its authored duration. Covers the factor itself and
    /// GameSession/PlayerMotor applying it. The session test opens GameSession's real (unused)
    /// loopback listener for its lifetime, like GameSessionRetryTests.
    /// </summary>
    public sealed class ClimbPaceTests
    {
        private const float Dt = 1f / 60f;

        [Test]
        public void DistanceScale_MapsAuthoredSpeedToPacedWorldSpeed()
        {
            GameSettings settings = CreateSettings(bodyHeightsPerSecond: 1.5f, bodyHeight: 2f);
            ClimbPace pace = settings.pace;
            LevelDefinition level = CreateLevel();
            try
            {
                Assert.AreEqual(3f, pace.WorldSpeed, 1e-5f);
                Assert.AreEqual(1.2f, pace.DistanceScaleFor(level), 1e-5f, "3 world units/s over an authored 2.5");
                Assert.AreEqual(1.5f / ClimbPace.TunedBodyHeightsPerSecond, pace.PresentationRate, 1e-5f);
                Assert.AreEqual(1f, pace.DistanceScaleFor(null), "no level: authored units unchanged");
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void StartLevel_ScalesSpeedFinishAndKnockback_KeepingDuration()
        {
            var motorGo = new GameObject("Motor");
            var sessionGo = new GameObject("Session");
            GameSettings settings = CreateSettings(bodyHeightsPerSecond: 1.5f, bodyHeight: 2f);
            LevelDefinition level = CreateLevel();
            var levelJson = new TextAsset(LevelJson);
            try
            {
                PlayerMotor motor = motorGo.AddComponent<PlayerMotor>();
                GameSession session = sessionGo.AddComponent<GameSession>();
                SetPrivate(session, "motor", motor);
                SetLevelFiles(session, levelJson);
                SetPrivate(session, "settings", settings);
                SetPrivate(motor, "settings", settings);

                session.StartLevel();

                Assert.AreEqual(3f, motor.ClimbSpeed, 1e-5f, "paced world speed");
                Assert.AreEqual(36f, motor.FinishHeight, 1e-4f, "finish scaled by the same 1.2 factor");
                Assert.AreEqual(level.finishHeight / level.climbSpeed, motor.FinishHeight / motor.ClimbSpeed, 1e-4f,
                    "the level must take as long as it was authored to");

                motor.ResetState(10f);
                Assert.IsTrue(motor.TryApplyHit());
                for (float t = 0f; t < motor.KnockbackSeconds + Dt; t += Dt)
                {
                    motor.Step(Dt);
                }

                Assert.AreEqual(10f - 1.5f * 1.2f, motor.Height, 1e-3f, "authored 1.5 knockback scaled by 1.2");
            }
            finally
            {
                Object.DestroyImmediate(sessionGo);
                Object.DestroyImmediate(motorGo);
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(levelJson);
            }
        }

        private const string LevelJson =
            "{\"levelId\":1,\"displayName\":\"Test\",\"finishHeight\":30,\"climbSpeed\":2.5,\"hazards\":[]}";

        private static LevelDefinition CreateLevel()
        {
            return LevelDefinition.FromJson(LevelJson);
        }

        private static GameSettings CreateSettings(float bodyHeightsPerSecond, float bodyHeight)
        {
            var settings = ScriptableObject.CreateInstance<GameSettings>();
            var so = new SerializedObject(settings);
            so.FindProperty("pace.bodyHeightsPerSecond").floatValue = bodyHeightsPerSecond;
            so.FindProperty("pace.bodyHeight").floatValue = bodyHeight;
            so.ApplyModifiedPropertiesWithoutUndo();
            return settings;
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

        private static void SetPrivate(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(fieldName).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
