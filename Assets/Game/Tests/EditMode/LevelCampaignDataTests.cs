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
        private const float ReactionMarginSeconds = 0.25f;
        private const float NumericalGuardSeconds = 0.02f;

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

        private static ClimbPace LoadPace()
        {
            const string path = "Assets/Game/Levels/ClimbPace.asset";
            var pace = AssetDatabase.LoadAssetAtPath<ClimbPace>(path);
            Assert.IsNotNull(pace, "missing " + path);
            return pace;
        }

        [Test]
        public void AllFiveLevels_ParseInOrderWithProgressionInData()
        {
            int shippedLevelFiles = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/Game/Levels" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (path.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase) &&
                    name.StartsWith("Level", System.StringComparison.OrdinalIgnoreCase))
                {
                    shippedLevelFiles++;
                }
            }
            Assert.AreEqual(LevelCount, shippedLevelFiles, "campaign hard cap: ship exactly five Level*.json files");

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
        public void EveryBand_HasReactionMarginAtMinimumSupportedPace()
        {
            foreach (LevelDefinition level in LoadAll())
            {
                float crossing = 1f / ClimbPace.MinBodyHeightsPerSecond;
                foreach (HazardSpec band in level.hazards)
                {
                    float safe = band.periodSeconds - band.activeSeconds;
                    Assert.GreaterOrEqual(safe, crossing + ReactionMarginSeconds,
                        "level " + level.levelId + " band at " + band.height + " safe window " + safe + " s vs crossing " + crossing + " s");
                }
            }
        }

        [Test]
        public void ShippedPace_IsWithinSupportedRange()
        {
            ClimbPace pace = LoadPace();
            Assert.GreaterOrEqual(pace.BodyHeightsPerSecond, ClimbPace.MinBodyHeightsPerSecond);
            Assert.LessOrEqual(pace.BodyHeightsPerSecond, ClimbPace.MaxBodyHeightsPerSecond);
            Assert.Greater(pace.WorldSpeed, 0f);
        }

        [TestCase(30)]
        [TestCase(60)]
        public void EveryLevel_IsCompletableWithoutHits_AcrossSupportedPacesAndFrameRates(int framesPerSecond)
        {
            float[] paces = { ClimbPace.MinBodyHeightsPerSecond, LoadPace().BodyHeightsPerSecond, ClimbPace.MaxBodyHeightsPerSecond };
            float[] startDelays = { 0f, 0.37f, 1.13f, 2.41f };
            foreach (float bodyHeightsPerSecond in paces)
            {
                foreach (LevelDefinition level in LoadAll())
                {
                    foreach (float startDelay in startDelays)
                    {
                        float seconds = SimulateCautiousClimb(level, bodyHeightsPerSecond, framesPerSecond, startDelay, out string failure);
                        Assert.IsNull(failure, "level " + level.levelId + ", pace " + bodyHeightsPerSecond + ", " + framesPerSecond + " fps, delay " + startDelay + ": " + failure);
                        Assert.LessOrEqual(seconds - startDelay, level.finishHeight / level.climbSpeed * 3f + 12f,
                            "cautious run took too long");
                    }
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
                float seconds = SimulateCautiousClimb(level, LoadPace().BodyHeightsPerSecond, 120, 0f, out string failure);
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
        private static float SimulateCautiousClimb(LevelDefinition level, float pace, int framesPerSecond, float startDelay, out string failure)
        {
            failure = null;
            float simStep = 1f / framesPerSecond;
            float speed = pace;
            float body = 1f;
            float feet = 0f;
            float time = startDelay;
            float finish = level.finishHeight * pace / level.climbSpeed;
            float cap = startDelay + level.finishHeight / level.climbSpeed * 6f + 30f;

            while (feet < finish)
            {
                if (time > cap)
                {
                    failure = "did not finish within " + cap + " s (stuck at height " + feet + ")";
                    return time;
                }

                if (CanClimb(level, pace, feet, time, speed, body, simStep))
                {
                    feet = Mathf.Min(feet + speed * simStep, finish);
                }

                time += simStep;

                foreach (HazardSpec band in level.hazards)
                {
                    float bandHeight = band.height * pace / level.climbSpeed;
                    if (band.IsActiveAt(time) && bandHeight >= feet && bandHeight <= feet + body)
                    {
                        failure = "hit by band at " + band.height + " at t=" + time + " s, feet " + feet;
                        return time;
                    }
                }
            }

            return time;
        }

        private static bool CanClimb(LevelDefinition level, float pace, float feet, float time, float speed, float body, float simStep)
        {
            foreach (HazardSpec band in level.hazards)
            {
                float bandHeight = band.height * pace / level.climbSpeed;
                if (bandHeight < feet)
                {
                    continue; // already crossed
                }

                float enter = Mathf.Max(0f, (bandHeight - body - feet) / speed);
                if (enter > 2f * simStep)
                {
                    continue; // not in or about to enter this band's span
                }

                float leave = (bandHeight - feet) / speed;
                for (float t = enter - NumericalGuardSeconds; t <= leave + NumericalGuardSeconds; t += simStep)
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
