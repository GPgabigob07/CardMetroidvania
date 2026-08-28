using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Architecture/Enemy Definition", fileName = "Enemy_")]
    public sealed class EnemyDefinitionSO : ScriptableObject, IIdentified
    {
        [Header(header: "Identity")]
        [Tooltip(tooltip: "Stable enemy id used by saves, encounters and debug tooling. Falls back to the asset name when empty.")]
        [SerializeField] private string id;

        [Tooltip(tooltip: "Human-readable enemy name shown in tools. Falls back to the stable id when empty.")]
        [SerializeField] private string displayName;

        [Header(header: "Health")]
        [Min(min: 1f)]
        [Tooltip(tooltip: "Maximum health assigned to each enemy instance using this definition.")]
        [SerializeField] private float maxHealth = 5f;

        [Header("Rewards")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Energy granted when this enemy is defeated by the player.")]
        [SerializeField] private float defeatEnergyReward = 1f;

        public string Id => string.IsNullOrWhiteSpace(value: id) ? name : id;
        public string DisplayName => string.IsNullOrWhiteSpace(value: displayName) ? Id : displayName;
        public float MaxHealth => Mathf.Max(a: 1f, b: maxHealth);
        public float DefeatEnergyReward => Mathf.Max(a: 0f, b: defeatEnergyReward);

        private void OnValidate()
        {
            maxHealth = Mathf.Max(a: 1f, b: maxHealth);
            defeatEnergyReward = Mathf.Max(a: 0f, b: defeatEnergyReward);
        }
    }
}
