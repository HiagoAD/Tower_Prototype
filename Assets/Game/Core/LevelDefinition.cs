using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(menuName = "Tower/Level Definition", fileName = "Level")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Min(1)] public int levelId = 1;
        public string displayName = "Level";
        [Min(1f)] public float finishHeight = 40f;
        [Min(0.1f)] public float climbSpeed = 4f;
        public HazardSpec[] hazards = System.Array.Empty<HazardSpec>();
    }
}
