using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Enemies/Gargoyle Tuning")]
    public sealed class GargoyleTuningSO : ScriptableObject
    {
        [Header("Encounter Definitions")]
        [Tooltip("Default three-strike combo; each strike supplies its own anticipation.")]
        [SerializeField] private EnemyAttackDefinitionSO basicAttack;
        [SerializeField] private EnemyAttackDefinitionSO wingbreakerAttack;
        [SerializeField] private EnemyAttackDefinitionSO volleyAttack;
        [SerializeField] private EnemyAttackDefinitionSO beamAttack;
        [Tooltip("Reactor nova definition, including its future radius/stability payload settings.")]
        [SerializeField] private EnemyAttackDefinitionSO novaAttack;
        [SerializeField] private GargoylePresentationSO presentation;
        [Tooltip("Delayed claw used for the bounded Basic feint; not another shuffled family.")]
        [SerializeField] private EnemyAttackDefinitionSO feintAttack;
        [Header("Live Simulation")]
        [SerializeField] private GargoyleTuningValues simulation = new GargoyleTuningValues();
        [SerializeField, Tooltip("Environment layer for the grounded motor. Configure in the asset, not the enemy component.")]
        private LayerMask environmentLayer;

        [Header("Attack Selection")]
        [Tooltip("Each family occurs once in a shuffled bag. Changes apply at the next refill.")]
        [SerializeField] private GargoyleAttackFamily[] families =
        {
            GargoyleAttackFamily.Wingbreaker,
            GargoyleAttackFamily.Volley,
            GargoyleAttackFamily.Beam
        };

        [Tooltip("Authored projectile-count variants. Each occurs once per volley bag; edits apply on refill.")]
        [SerializeField] private int[] volleyCounts = { 1, 3, 5 };
        [SerializeField, Tooltip("Seed changes take effect on bag refill or explicit encounter reset.")]
        private int randomSeed;

        public IReadOnlyList<GargoyleAttackFamily> Families => Array.AsReadOnly(families ?? Array.Empty<GargoyleAttackFamily>());
        public IReadOnlyList<int> VolleyCounts => Array.AsReadOnly(volleyCounts ?? Array.Empty<int>());
        public EnemyAttackDefinitionSO BasicAttack => basicAttack;
        public EnemyAttackDefinitionSO NovaAttack => novaAttack;
        public GargoylePresentationSO Presentation => presentation;
        public EnemyAttackDefinitionSO FeintAttack => feintAttack;
        public LayerMask EnvironmentLayer => environmentLayer;
        public int RandomSeed => randomSeed;

        public bool TryReadSimulationValues(out GargoyleTuningValues values)
        {
            values = null;
            if (simulation == null || simulation.GetValidationErrors().Count != 0) return false;
            values = simulation.Copy();
            return true;
        }

        public EnemyAttackDefinitionSO GetAttackDefinition(GargoyleAttackFamily family) => family switch
        {
            GargoyleAttackFamily.Wingbreaker => wingbreakerAttack,
            GargoyleAttackFamily.Volley => volleyAttack,
            GargoyleAttackFamily.Beam => beamAttack,
            _ => null
        };

        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (!TryReadSelectorConfiguration(out var familySnapshot, out _))
                errors.Add("Attack selection bags must contain distinct valid entries.");
            if (basicAttack == null) errors.Add("Basic attack definition is required.");
            if (familySnapshot != null && familySnapshot.Any(family => GetAttackDefinition(family) == null))
                errors.Add("Each attack family requires a definition.");
            if (novaAttack == null) errors.Add("Nova attack definition is required.");
            if (presentation == null) errors.Add("Presentation definition is required.");
            if (simulation == null) errors.Add("Simulation tuning is required.");
            else foreach (var error in simulation.GetValidationErrors()) errors.Add(error);
            if (simulation != null && simulation.FeintProbability > 0 && simulation.FeintBudget > 0 && feintAttack == null)
                errors.Add("A delayed claw definition is required when feints are enabled.");
            if (environmentLayer.value == 0) errors.Add("Grounded movement requires an environment layer.");
            return errors;
        }

        public bool TryReadSelectorConfiguration(out GargoyleAttackFamily[] familySnapshot, out int[] volleySnapshot)
        {
            familySnapshot = null;
            volleySnapshot = null;
            if (families == null || families.Length < 2 || families.Distinct().Count() != families.Length
                || families.Any(family => !Enum.IsDefined(typeof(GargoyleAttackFamily), family))
                || volleyCounts == null || volleyCounts.Length < 2
                || volleyCounts.Any(count => count <= 0) || volleyCounts.Distinct().Count() != volleyCounts.Length)
                return false;

            familySnapshot = (GargoyleAttackFamily[])families.Clone();
            volleySnapshot = (int[])volleyCounts.Clone();
            return true;
        }
    }
}
