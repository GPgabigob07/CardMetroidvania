using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    public sealed class AreaSpawnPoint : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("Stable local identifier used to resolve this authored transform after an area load.")]
        [SerializeField] private string spawnId;

        public string SpawnId => spawnId;
        public Transform AuthoredTransform => transform;

        /// <summary>
        /// Assigns the stable identifier used by this marker.
        /// </summary>
        public void Configure(string configuredSpawnId)
        {
            if (string.IsNullOrWhiteSpace(configuredSpawnId))
            {
                throw new ArgumentException("Spawn identifier cannot be empty or whitespace.", nameof(configuredSpawnId));
            }

            spawnId = configuredSpawnId;
        }
    }
}
