using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Enemies/Attack Definition")]
    public sealed class EnemyAttackDefinitionSO : ScriptableObject
    {
        [Header("Ordered Attack Steps")]
        [Tooltip("Each strike has its own telegraph, active sample, recovery and stable ID. Step-list edits apply to the next execution.")]
        [SerializeField] private EnemyAttackStep[] steps = Array.Empty<EnemyAttackStep>();
        [Header("Volley Variants")]
        [SerializeField] private EnemyProjectilePattern[] patterns = Array.Empty<EnemyProjectilePattern>();
        [SerializeField, Tooltip("Spawned projectile behavior; authored geometry/motion remains in the attack step.")]
        private EnemyProjectile2D projectilePrefab;

        public IReadOnlyList<EnemyAttackStep> Steps => Array.AsReadOnly(steps ?? Array.Empty<EnemyAttackStep>());
        public EnemyProjectile2D ProjectilePrefab => projectilePrefab;
        public void SetSteps(EnemyAttackStep[] value) => steps = value != null ? (EnemyAttackStep[])value.Clone() : null;
        public void SetPatterns(EnemyProjectilePattern[] value) => patterns = value != null ? (EnemyProjectilePattern[])value.Clone() : null;
        public EnemyProjectilePattern FindPattern(int count) => patterns?.FirstOrDefault(pattern => pattern != null && pattern.Count == count);

        public bool TryCaptureSteps(out EnemyAttackStep[] value)
        {
            value = null;
            if (GetValidationErrors().Count != 0) return false;
            var captured = new List<EnemyAttackStep>();
            foreach (var step in steps)
            {
                if (!step.TryRead(out var snapshot)) return false;
                captured.Add(snapshot);
            }
            value = captured.ToArray();
            return true;
        }

        public bool TryReadStep(string id, out EnemyAttackStep value)
        {
            value = null;
            if (GetValidationErrors().Count != 0) return false;
            var step = steps.FirstOrDefault(candidate => candidate.Id == id);
            return step != null && step.TryRead(out value);
        }

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
            if (steps.Any(step => step?.Payload?.Kind == EnemyAttackPayloadKind.Volley))
            {
                if (patterns == null || patterns.Length == 0) errors.Add("Volley requires authored patterns.");
                else
                {
                    var counts = new HashSet<int>();
                    foreach (var pattern in patterns)
                    {
                        if (pattern == null) { errors.Add("Volley cannot contain a missing pattern."); continue; }
                        if (!counts.Add(pattern.Count)) errors.Add("Volley pattern counts must be unique.");
                        foreach (var error in pattern.GetValidationErrors()) errors.Add(error);
                    }
                }
            }
            return errors;
        }
    }
}
