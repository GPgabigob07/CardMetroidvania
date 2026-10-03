using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Enemies/Attack Definition")]
    public sealed class EnemyAttackDefinitionSO : ScriptableObject
    {
        [Header("Ordered Attack Steps")]
        [Tooltip("Each strike has its own telegraph, active sample, recovery and stable ID. Step-list edits apply to the next execution.")]
        [SerializeField] private EnemyAttackStep[] steps = Array.Empty<EnemyAttackStep>();

        public IReadOnlyList<EnemyAttackStep> Steps => Array.AsReadOnly(steps ?? Array.Empty<EnemyAttackStep>());

        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (steps == null || steps.Length == 0)
            {
                errors.Add("Attack definition requires at least one step.");
                return errors;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var step in steps)
            {
                if (step == null)
                {
                    errors.Add("Attack definition cannot contain a missing step.");
                    continue;
                }
                if (!ids.Add(step.Id)) errors.Add("Attack step IDs must be unique.");
                foreach (var error in step.GetValidationErrors()) errors.Add(error);
            }
            return errors;
        }
    }
}
