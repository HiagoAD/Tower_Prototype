using System;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Levels are JSON files parsed by LevelDefinition.FromJson. Covers the shipped Level 1 file and
    /// that a bad or incomplete file is rejected with the offending field named.
    /// </summary>
    public sealed class LevelDefinitionJsonTests
    {
        private const string Level1Path = "Assets/Game/Levels/Level1.json";

        [Test]
        public void ShippedLevel1_ParsesWithItsAuthoredValues()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(Level1Path);
            Assert.IsNotNull(asset, "missing " + Level1Path);

            LevelDefinition level = LevelDefinition.FromJson(asset.text);

            Assert.AreEqual(1, level.levelId);
            Assert.AreEqual("First Ascent", level.displayName);
            Assert.AreEqual(30f, level.finishHeight);
            Assert.AreEqual(2.5f, level.climbSpeed);
            Assert.AreEqual(2, level.hazards.Length);
            Assert.AreEqual(10f, level.hazards[0].height);
            Assert.AreEqual(2.6f, level.hazards[1].phaseOffsetSeconds);
        }

        [Test]
        public void MissingHazardsKey_MeansNoHazards()
        {
            LevelDefinition level = LevelDefinition.FromJson(Json("1", "\"A\"", "10", "1"));

            Assert.IsNotNull(level.hazards);
            Assert.AreEqual(0, level.hazards.Length);
        }

        [TestCase("", "empty")]
        [TestCase("   ", "empty")]
        [TestCase("{ not json", "malformed")]
        public void EmptyOrMalformedJson_IsRejected(string json, string expectedInMessage)
        {
            var ex = Assert.Throws<FormatException>(() => LevelDefinition.FromJson(json));
            StringAssert.Contains(expectedInMessage, ex.Message);
        }

        [Test]
        public void NullJson_IsRejected()
        {
            Assert.Throws<FormatException>(() => LevelDefinition.FromJson(null));
        }

        [TestCase("0", "\"A\"", "10", "1", "levelId")]
        [TestCase("1", "\"  \"", "10", "1", "displayName")]
        [TestCase("1", "\"A\"", "0.5", "1", "finishHeight")]
        [TestCase("1", "\"A\"", "10", "0.05", "climbSpeed")]
        public void InvalidField_IsRejectedNamingTheField(string id, string name, string finish, string speed, string field)
        {
            var ex = Assert.Throws<FormatException>(() => LevelDefinition.FromJson(Json(id, name, finish, speed)));
            StringAssert.Contains(field, ex.Message);
        }

        [TestCase("levelId")]
        [TestCase("displayName")]
        [TestCase("finishHeight")]
        [TestCase("climbSpeed")]
        public void MissingKey_IsRejectedNamingTheField(string key)
        {
            string json = "{\"levelId\":1,\"displayName\":\"A\",\"finishHeight\":10,\"climbSpeed\":1}";
            json = System.Text.RegularExpressions.Regex.Replace(json, "\"" + key + "\":(\"A\"|[0-9]+),?", "").Replace(",}", "}");

            var ex = Assert.Throws<FormatException>(() => LevelDefinition.FromJson(json));
            StringAssert.Contains(key, ex.Message);
        }

        private static string Json(string id, string name, string finish, string speed)
        {
            return "{\"levelId\":" + id + ",\"displayName\":" + name + ",\"finishHeight\":" + finish + ",\"climbSpeed\":" + speed + "}";
        }
    }
}
