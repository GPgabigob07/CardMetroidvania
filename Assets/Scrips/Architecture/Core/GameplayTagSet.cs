using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class GameplayTagSet
    {
        [Header(header: "Tags")]
        [Tooltip(tooltip: "Tags included in this set.")]
        [SerializeField] private List<GameplayTagSO> tags = new List<GameplayTagSO>();

        public IReadOnlyList<GameplayTagSO> Tags => tags;

        public bool Contains(GameplayTagSO tag)
        {
            return tag != null && tags.Contains(item: tag);
        }

        public bool ContainsAll(GameplayTagSet requiredTags)
        {
            if (requiredTags == null)
            {
                return true;
            }

            foreach (var requiredTag in requiredTags.tags)
            {
                if (!Contains(tag: requiredTag))
                {
                    return false;
                }
            }

            return true;
        }

        public bool ContainsAny(GameplayTagSet candidateTags)
        {
            if (candidateTags == null)
            {
                return false;
            }

            foreach (var candidateTag in candidateTags.tags)
            {
                if (Contains(tag: candidateTag))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
