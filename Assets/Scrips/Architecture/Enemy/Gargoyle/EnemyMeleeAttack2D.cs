using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class EnemyMeleeAttack2D : MonoBehaviour
    {
        [Header("Actor Bindings")]
        [SerializeField] private EnemyActor actor;
        [SerializeField] private GargoyleBrain brain;
        private readonly Dictionary<GameObject, int> acceptedHits = new();
        private readonly HashSet<GameObject> reservedTargets = new();
        private readonly HashSet<long> cancelledTokens = new();
        private long currentToken;
        private string currentStep;
        private int cancellationRevision;

        public void Initialize(EnemyActor actor, GargoyleBrain brain = null)
        {
            this.actor = actor;
            this.brain = brain;
        }

        public void Sample(in EnemyAttackExecutionSnapshot execution)
        {
            if (!isActiveAndEnabled || actor == null || !actor.IsOperational || Time.timeScale <= 0
                || !execution.IsRunning || execution.Phase != EnemyAttackPhase.Active
                || execution.Kind != EnemyAttackPayloadKind.Melee || execution.ExecutionToken <= 0
                || cancelledTokens.Contains(execution.ExecutionToken)
                || (brain != null && (!brain.CanEmit || brain.CurrentAttack.ExecutionToken != execution.ExecutionToken
                    || brain.CurrentAttack.StepId != execution.StepId))) return;
            if (currentToken != execution.ExecutionToken || currentStep != execution.StepId)
            {
                currentToken = execution.ExecutionToken;
                currentStep = execution.StepId;
                acceptedHits.Clear(); reservedTargets.Clear();
            }
            var revision = cancellationRevision;
            var payload = execution.Step.Payload;
            var facing = execution.Aim.x < 0 ? -1 : 1;
            var center = (Vector2)transform.position + new Vector2(payload.Offset.x * facing, payload.Offset.y);
            foreach (var collider in Physics2D.OverlapBoxAll(center, payload.HitboxSize, payload.HitboxAngle * facing))
            {
                if (revision != cancellationRevision || !actor.IsOperational || (brain != null && !brain.CanEmit)) return;
                var target = EnemyPlayerTargeting.CanonicalTarget(collider);
                if (target == null || reservedTargets.Contains(target)
                    || (acceptedHits.TryGetValue(target, out var hits) && hits >= payload.AcceptedHitLimit)) continue;
                var profile = execution.Step.DamageProfile;
                if (profile == null || !float.IsFinite(profile.BaseDamage) || profile.BaseDamage <= 0) continue;
                reservedTargets.Add(target);
                var report = DamageResolver.Resolve(new DamageRequest(new DamageInstance(
                    $"gargoyle-{currentToken}-{currentStep}", actor.gameObject, profile,
                    new DamageFormulaValues(0, 0, 0, 0, profile.BaseDamage, 0, 1),
                    attackExecutionId: $"gargoyle-{currentToken}-{currentStep}", procPolicy: DamageProcPolicy.None),
                    new[] { target }, center, execution.Aim));
                reservedTargets.Remove(target);
                if (report.EffectiveHitCount > 0) acceptedHits[target] = hits + 1;
            }
        }

        public void Cancel()
        {
            cancellationRevision++;
            if (currentToken > 0) cancelledTokens.Add(currentToken);
            if (brain != null && brain.CurrentAttack.ExecutionToken > 0) cancelledTokens.Add(brain.CurrentAttack.ExecutionToken);
        }
        private void OnDisable() => Cancel();
    }
}
