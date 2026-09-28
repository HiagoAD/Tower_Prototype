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
    /// loopback listener for its lifetime, like GameSessionLoseRetryTests.
    /// </summary>
    public sealed class ClimbPaceTests
    {
        private const float Dt = 1f / 60f;

        [Test]
        public void DistanceScale_MapsAuthoredSpeedToPacedWorldSpeed()
        {
            ClimbPace pace = CreatePace(bodyHeightsPerSecond: 1.5f, bodyHeight: 2f);
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
                Object.DestroyImmediate(pace);
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void StartLevel_ScalesSpeedFinishAndKnockback_KeepingDuration()
        {
            var motorGo = new GameObject("Motor");
            var sessionGo = new GameObject("Session");
            ClimbPace pace = CreatePace(bodyHeightsPerSecond: 1.5f, bodyHeight: 2f);
            LevelDefinition level = CreateLevel();
            try
            {
                PlayerMotor motor = motorGo.AddComponent<PlayerMotor>();
                GameSession session = sessionGo.AddComponent<GameSession>();
                SetPrivate(session, "motor", motor);
                SetPrivate(session, "level", level);
                SetPrivate(session, "pace", pace);

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
                Object.DestroyImmediate(pace);
                Object.DestroyImmediate(level);
            }
        }

        private static LevelDefinition CreateLevel()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.finishHeight = 30f;
            level.climbSpeed = 2.5f;
            level.hazards = System.Array.Empty<HazardSpec>();
            return level;
        }

        private static ClimbPace CreatePace(float bodyHeightsPerSecond, float bodyHeight)
        {
            var pace = ScriptableObject.CreateInstance<ClimbPace>();
            var so = new SerializedObject(pace);
            so.FindProperty("bodyHeightsPerSecond").floatValue = bodyHeightsPerSecond;
            so.FindProperty("bodyHeight").floatValue = bodyHeight;
            so.ApplyModifiedPropertiesWithoutUndo();
            return pace;
        }

        private static void SetPrivate(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(fieldName).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
