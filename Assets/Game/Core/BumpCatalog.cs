using System;
using System.Collections.Generic;
using Game.Webhook;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The bump types the game knows and the defaults applied to a /bump request that leaves a
    /// field out. The server says which polarity and type; this asset says how far each type moves
    /// the climber.
    /// </summary>
    [CreateAssetMenu(menuName = "Tower/Bump Catalog", fileName = "BumpCatalog")]
    public sealed class BumpCatalog : ScriptableObject
    {
        [Tooltip("Polarity used when a request omits it. Negative keeps a bare POST /bump the full glove burst the brief asks for.")]
        public BumpPolarity defaultPolarity = BumpPolarity.Negative;

        [Tooltip("Type id used when a request omits it; must match an entry below.")]
        public string defaultTypeId = "boxing";

        [Tooltip("Sender tag shown when a request carries none.")]
        public string fallbackTag = "Guest";

        [Tooltip("Every bump type the server may name.")]
        public BumpType[] types = Array.Empty<BumpType>();

        private void OnValidate()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (BumpType type in types ?? Array.Empty<BumpType>())
            {
                if (type != null && !string.IsNullOrEmpty(type.id) && !seen.Add(type.id))
                {
                    Debug.LogWarning("[BumpCatalog] Duplicate bump type id '" + type.id + "'; only the first entry is used.", this);
                }
            }
        }

        /// <summary>Looks a type up by id, ignoring case.</summary>
        public bool TryGet(string id, out BumpType type)
        {
            if (id != null && types != null)
            {
                foreach (BumpType candidate in types)
                {
                    if (candidate != null && string.Equals(candidate.id, id, StringComparison.OrdinalIgnoreCase))
                    {
                        type = candidate;
                        return true;
                    }
                }
            }

            type = null;
            return false;
        }

        /// <summary>A copy of every type id, for the listener to validate `type` against on its worker threads.</summary>
        public IReadOnlyCollection<string> KnownTypeIds()
        {
            var ids = new List<string>();
            if (types != null)
            {
                foreach (BumpType type in types)
                {
                    if (type != null && !string.IsNullOrEmpty(type.id))
                    {
                        ids.Add(type.id);
                    }
                }
            }

            return ids;
        }
    }
}
