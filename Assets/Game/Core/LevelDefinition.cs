using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// One level's authored numbers, parsed from a JSON TextAsset (Assets/Game/Levels/*.json) so a level
    /// is added by adding a file. Field names are the JSON keys. Defaults deliberately fail validation,
    /// so a key missing from the file is caught by FromJson instead of silently becoming a value.
    /// </summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        public int levelId;
        public string displayName;
        public float finishHeight;
        public float climbSpeed;
        public HazardSpec[] hazards;

        /// <summary>Parses and validates a level file; throws FormatException naming the offending field (and hazard index, for a bad band).</summary>
        public static LevelDefinition FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException("Level JSON is empty.");
            }

            LevelDefinition level;
            try
            {
                level = JsonUtility.FromJson<LevelDefinition>(json);
            }
            catch (ArgumentException ex)
            {
                throw new FormatException("Level JSON is malformed: " + ex.Message, ex);
            }

            if (level == null)
            {
                throw new FormatException("Level JSON is malformed: no level object.");
            }

            if (level.levelId < 1)
            {
                throw new FormatException("Level JSON field 'levelId' must be >= 1 (got " + level.levelId + ").");
            }

            if (string.IsNullOrWhiteSpace(level.displayName))
            {
                throw new FormatException("Level JSON field 'displayName' must not be blank.");
            }

            if (float.IsNaN(level.finishHeight) || float.IsInfinity(level.finishHeight) || !(level.finishHeight >= 1f))
            {
                throw new FormatException("Level JSON field 'finishHeight' must be >= 1 (got " + level.finishHeight + ").");
            }

            if (float.IsNaN(level.climbSpeed) || float.IsInfinity(level.climbSpeed) || !(level.climbSpeed >= 0.1f))
            {
                throw new FormatException("Level JSON field 'climbSpeed' must be >= 0.1 (got " + level.climbSpeed + ").");
            }

            level.hazards ??= Array.Empty<HazardSpec>();

            // HazardSpec is a struct, so JsonUtility turns a JSON null entry into a default (all-zero)
            // hazard; that fails height > 0 below and is reported by index like any other bad band.
            for (int i = 0; i < level.hazards.Length; i++)
            {
                string problem = level.hazards[i].Validate(level.finishHeight);
                if (problem != null)
                {
                    throw new FormatException("Level JSON hazards[" + i + "]." + problem + ".");
                }
            }

            return level;
        }
    }
}
