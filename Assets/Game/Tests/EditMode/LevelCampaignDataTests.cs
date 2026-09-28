using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// The five shipped campaign files (Assets/Game/Levels/Level1..5.json): they parse, form a strictly
    /// harder ordered progression, and each can be finished without viewer help and without a hit by a
    /// deterministic cautious climber (a data-only simulation, not the scene).
    /// </summary>
    public sealed class LevelCampaignDataTests
    {
        private const int LevelCount = 5;
        private const float SimStep = 1f / 120f;
        private const float GuardSeconds = 0.02f;
        private const float MinSafeToCrossingRatio = 1.5f;

        private static LevelDefinition[] LoadAll()
        {
            var levels = new LevelDefinition[LevelCount];
            for (int i = 0; i < LevelCount; i++)
            {
                string path = "Assets/Game/Levels/Level" + (i + 1) + ".json";
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                Assert.IsNotNull(asset, "missing " + path);
                levels[i] = LevelDefinition.FromJson(asset.text);
            }

            return levels;
        }

        private static float BodyHeightFor(LevelDefinition level)
        {
            return level.climbSpeed / ClimbPace.TunedBodyHeightsPerSecond;
        }

        [Test]
        public void AllFiveLevels_ParseInOrderWithProgressionInData()
        {
            LevelDefinition[] levels = LoadAll();
            var names = new System.Collections.Generic.HashSet<string>();

            for (int i = 0; i < LevelCount; i++)
            {
                Assert.AreEqual(i + 1, levels[i].levelId, "levelId of file " + (i + 1));
                Assert.IsTrue(names.Add(levels[i].displayName), "duplicate display name " + levels[i].displayName);
                if (i > 0)
                {
                    Assert.Greater(levels[i].finishHeight, levels[i - 1].finishHeight, "finishHeight must increase at level " + (i + 1));
                    Assert.GreaterOrEqual(levels[i].hazards.Length, levels[i - 1].hazards.Length, "hazard count must not drop at level " + (i + 1));
                }
            }
        }

        [Test]
        public void EveryBand_SafeWindowComfortablyExceedsCrossingTime()
        {
            foreach (LevelDefinition level in LoadAll())
            {
                float crossing = BodyHeightFor(level) / level.climbSpeed;
                foreach (HazardSpec band in level.hazards)
                {
                    float safe = band.periodSeconds - band.activeSeconds;
                    Assert.GreaterOrEqual(safe, MinSafeToCrossingRatio * crossing,
                        "level " + level.levelId + " band at " + band.height + " safe window " + safe + " s vs crossing " + crossing + " s");
                }
            }
        }

        [Test]
        public void EveryLevel_IsCompletableWithoutHelpOrHits_AndGrowsLonger()
        {
            LevelDefinition[] levels = LoadAll();
            float previous = 0f;

            foreach (LevelDefinition level in levels)
            {
                float seconds = SimulateCautiousClimb(level, out string failure);
                Assert.IsNull(failure, "level " + level.levelId + ": " + failure);

                float direct = level.finishHeight / level.climbSpeed;
                Assert.GreaterOrEqual(seconds, direct - 0.05f, "level " + level.levelId + " finished faster than the climb speed allows");
                Assert.LessOrEqual(seconds, direct * 2.5f + 10f, "level " + level.levelId + " clean run took " + seconds + " s");
                Assert.Greater(seconds, previous, "level " + level.levelId + " clean run (" + seconds + " s) must be longer than the previous level's (" + previous + " s)");
                previous = seconds;
                TestContext.WriteLine("Level " + level.levelId + " clean run: " + seconds.ToString("F1") + " s");
            }
        }

        /// <summary>
        /// A cautious climber. Each step it climbs only if, for every band whose body span it is in or about
        /// to enter, the band stays inactive for the whole time needed to fully cross it (plus a small
        /// guard); otherwise it holds, always outside every band's span. Returns the finish time, or a
        /// failure description (a hit, or no finish within the time cap) via <paramref name="failure"/>.
        /// </summary>
        private static float SimulateCautiousClimb(LevelDefinition level, out string failure)
        {
            failure = null;
            float speed = level.climbSpeed;
            float body = BodyHeightFor(level);
            float feet = 0f;
            float time = 0f;
            float cap = level.finishHeight / speed * 6f + 30f;

            while (feet < level.finishHeight)
            {
                if (time > cap)
                {
                    failure = "did not finish within " + cap + " s (stuck at height " + feet + ")";
                    return time;
                }

                if (CanClimb(level, feet, time, speed, body))
                {
                    feet = Mathf.Min(feet + speed * SimStep, level.finishHeight);
                }

                time += SimStep;

                foreach (HazardSpec band in level.hazards)
                {
                    if (band.IsActiveAt(time) && band.height >= feet && band.height <= feet + body)
                    {
                        failure = "hit by band at " + band.height + " at t=" + time + " s, feet " + feet;
                        return time;
                    }
                }
            }

            return time;
        }

        private static bool CanClimb(LevelDefinition level, float feet, float time, float speed, float body)
        {
            foreach (HazardSpec band in level.hazards)
            {
                if (band.height < feet)
                {
                    continue; // already crossed
                }

                float enter = Mathf.Max(0f, (band.height - body - feet) / speed);
                if (enter > 2f * SimStep)
                {
                    continue; // not in or about to enter this band's span
                }

                float leave = (band.height - feet) / speed;
                for (float t = enter - GuardSeconds; t <= leave + GuardSeconds; t += SimStep)
                {
                    if (band.IsActiveAt(time + Mathf.Max(0f, t)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
