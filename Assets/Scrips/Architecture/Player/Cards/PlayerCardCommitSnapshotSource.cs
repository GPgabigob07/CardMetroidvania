using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerCardCommitSnapshotSource :
        MonoBehaviour,
        IPlayerCardCommitSnapshotSource
    {
        [Header("Volatile Values")]
        [Tooltip("Wallet supplying volatile resource values such as Energy.")]
        [SerializeField] private PlayerResourceWallet wallet;

        [Tooltip("Health source supplying volatile health values.")]
        [SerializeField] private SimpleHealth health;

        [Tooltip("Resources captured into card commit snapshots.")]
        [SerializeField] private List<ResourceDefinitionSO> capturedResources = new();

        public void Configure(
            PlayerResourceWallet resourceWallet,
            SimpleHealth healthSource,
            IEnumerable<ResourceDefinitionSO> resources)
        {
            wallet = resourceWallet;
            health = healthSource;
            capturedResources = resources != null
                ? new List<ResourceDefinitionSO>(resources)
                : new List<ResourceDefinitionSO>();
        }

        public PlayerCardCommitSnapshot Capture(
            PlayerCardTimeState category,
            string attackExecutionId,
            bool isAirborne)
        {
            var resources = new List<PlayerCardResourceSnapshot>();
            foreach (var resource in capturedResources)
            {
                if (resource == null)
                {
                    continue;
                }

                resources.Add(new PlayerCardResourceSnapshot(
                    resource,
                    wallet != null ? wallet.GetCurrent(resource) : 0f,
                    wallet != null ? wallet.GetMaximum(resource) : 0f));
            }

            return new PlayerCardCommitSnapshot(
                category,
                attackExecutionId,
                isAirborne,
                health != null ? health.CurrentHealth : 0f,
                health != null ? health.MaximumHealth : 0f,
                resources);
        }
    }
}
