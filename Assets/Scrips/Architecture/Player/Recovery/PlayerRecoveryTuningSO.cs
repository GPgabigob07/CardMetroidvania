using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Player/Recovery Tuning", fileName = "PlayerRecoveryTuning")]
    public sealed class PlayerRecoveryTuningSO : ScriptableObject
    {
        [Header("Energy Reserve")]
        [SerializeField, Min(0)] private float energyPerEnemyHit = 3;
        [SerializeField, Min(0)] private float passiveEnergyPerSecond = 5;
        [SerializeField, Min(0)] private float passiveDelayAfterSpendSeconds = 3;
        [SerializeField, Min(.01f)] private float passiveReserveMultiplier = 1.5f;
        [SerializeField, Min(.01f)] private float fallbackNeutralEnergyCost = 20;
        [Header("Conversion")]
        [SerializeField, Min(.01f)] private float healingChunkMultiplier = 2;
        [SerializeField, Min(1)] private float sacrificeHealthCost = 1;
        [SerializeField, Min(.01f)] private float sacrificeEnergyMultiplier = 1;
        [SerializeField, Min(.01f)] private float consumableHealAmount = 2;
        public float PassiveEnergyPerSecond => passiveEnergyPerSecond;
        public float EnergyPerEnemyHit => energyPerEnemyHit;
        public float PassiveDelayAfterSpendSeconds => passiveDelayAfterSpendSeconds;
        public float PassiveReserveMultiplier => passiveReserveMultiplier;
        public float FallbackNeutralEnergyCost => fallbackNeutralEnergyCost;
        public float HealingChunkMultiplier => healingChunkMultiplier;
        public float SacrificeHealthCost => sacrificeHealthCost;
        public float SacrificeEnergyMultiplier => sacrificeEnergyMultiplier;
        public float ConsumableHealAmount => consumableHealAmount;
        public bool IsValid => Valid(energyPerEnemyHit, true) && Valid(passiveEnergyPerSecond, true) && Valid(passiveDelayAfterSpendSeconds, true)
            && Valid(passiveReserveMultiplier) && Valid(fallbackNeutralEnergyCost) && Valid(healingChunkMultiplier)
            && Valid(sacrificeHealthCost) && Valid(sacrificeEnergyMultiplier) && Valid(consumableHealAmount)
            && passiveReserveMultiplier < healingChunkMultiplier
            && sacrificeEnergyMultiplier < sacrificeHealthCost * healingChunkMultiplier;
        private static bool Valid(float value, bool allowZero = false) => float.IsFinite(value) && (allowZero ? value >= 0 : value > 0);
    }
}
