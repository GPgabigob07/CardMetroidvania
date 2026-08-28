using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public readonly struct PlayerCardResourceSnapshot
    {
        public PlayerCardResourceSnapshot(
            ResourceDefinitionSO resource,
            float current,
            float maximum)
        {
            Resource = resource;
            Current = Mathf.Max(0f, current);
            Maximum = Mathf.Max(0f, maximum);
        }

        public ResourceDefinitionSO Resource { get; }
        public float Current { get; }
        public float Maximum { get; }
    }
}
