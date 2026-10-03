using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class EnemyNovaAttack2D : MonoBehaviour
    {
        [Header("Encounter Bindings")]
        [SerializeField] private EnemyActor actor;
        [SerializeField] private GargoyleBrain brain;
        [SerializeField] private GargoyleDamagePolicy policy;
        private EnemyAttackDefinitionSO definition;
        private EnemyAttackStep step;
        private long token;
        private bool armed;
        private bool warnedInvalid;
        private bool warnedDamage;
        private float accumulatedDamage;
        private float lastValidHealthDamage;
        private readonly HashSet<long> usedTokens = new();
        private readonly HashSet<(GameObject source, string execution)> coreExecutions = new();
        private EnemyActor subscribedActor;
        private GargoyleBrain subscribedBrain;
        private GargoyleDamagePolicy subscribedPolicy;
        public bool IsTelegraphing { get; private set; }
        public float RemainingStability => step != null ? Mathf.Max(0, step.Payload.ReactorStability - accumulatedDamage) : 0;
        public float Radius => step?.Payload.NovaRadius ?? 0;

        public void Initialize(EnemyActor actor, GargoyleBrain brain = null, GargoyleDamagePolicy policy = null)
        {
            Unsubscribe(); this.actor = actor; this.brain = brain; this.policy = policy;
            subscribedActor = actor; subscribedBrain = brain; subscribedPolicy = policy;
            if (actor != null) actor.Defeated += OnDefeated;
            if (brain != null) brain.OffenseCancelled += Cancel;
            if (policy != null) policy.AcceptedCoreHit += OnCoreHit;
        }
        public void Begin(long castToken, EnemyAttackDefinitionSO value)
        {
            if (value == null || !value.TryCaptureSteps(out var steps)) return;
            var captured = steps.FirstOrDefault(candidate => candidate.Payload.Kind == EnemyAttackPayloadKind.Nova);
            if (captured != null) Begin(castToken, value, captured);
        }
        public void Begin(in EnemyAttackExecutionSnapshot execution)
        {
            if (execution.IsRunning && execution.Kind == EnemyAttackPayloadKind.Nova && execution.Phase == EnemyAttackPhase.Windup)
                Begin(execution.ExecutionToken, execution.Definition, execution.Step);
        }
        private void Begin(long castToken, EnemyAttackDefinitionSO value, EnemyAttackStep captured)
        {
            if (armed || castToken <= 0 || usedTokens.Contains(castToken) || actor == null || !actor.IsOperational
                || captured.DamageProfile == null || !float.IsFinite(captured.DamageProfile.BaseDamage) || captured.DamageProfile.BaseDamage < 0) return;
            usedTokens.Add(castToken); token = castToken; definition = value; step = captured;
            accumulatedDamage = 0; coreExecutions.Clear(); warnedInvalid = warnedDamage = false;
            lastValidHealthDamage = captured.DamageProfile.BaseDamage;
            armed = IsTelegraphing = true;
        }
        public bool TryApplyCoreHit(in DamageContext context)
        {
            if (!CanOperate() || !IsTelegraphing || context.Provenance?.OriginKind != DamageOriginKind.Primary
                || string.IsNullOrWhiteSpace(context.AttackExecutionId) || !context.IsCardEnhancedMelee
                || !float.IsFinite(context.PoiseDamage) || context.PoiseDamage <= 0 || !float.IsFinite(context.Amount) || context.Amount <= 0
                || (brain != null && brain.CurrentAttack.Phase != EnemyAttackPhase.Windup)
                || !coreExecutions.Add((context.Source, context.AttackExecutionId))) return false;
            Refresh(); accumulatedDamage += context.PoiseDamage;
            BreakCoreIfDue(); return true;
        }
        public void Tick(float scaledDelta)
        {
            if (!float.IsFinite(scaledDelta) || scaledDelta < 0) throw new ArgumentOutOfRangeException(nameof(scaledDelta));
            if (!CanOperate()) return;
            Refresh();
            if (brain != null && brain.CurrentAttack.Phase != EnemyAttackPhase.Windup) { IsTelegraphing = false; CloseCore(); }
            if (IsTelegraphing) BreakCoreIfDue();
        }
        public void Release(Vector2 center)
        {
            if (!CanOperate() || !float.IsFinite(center.x) || !float.IsFinite(center.y)) return;
            Refresh();
            // Reserve the release before any accepted hit callback can reenter.
            armed = IsTelegraphing = false; CloseCore();
            var budget = new EnemyCastHitBudget(token, step.Payload.AcceptedHitLimit);
            var targets = Physics2D.OverlapCircleAll(center, Radius).Select(EnemyPlayerTargeting.CanonicalTarget)
                .Where(target => target != null).Distinct().ToArray();
            foreach (var target in targets)
            {
                if (!actor.IsOperational || !isActiveAndEnabled || Time.timeScale <= 0 || (brain != null && !brain.CanEmit)) return;
                var profile = step.DamageProfile;
                if (profile != null && float.IsFinite(profile.BaseDamage) && profile.BaseDamage >= 0)
                { lastValidHealthDamage = profile.BaseDamage; warnedDamage = false; }
                else if (!warnedDamage)
                { warnedDamage = true; Debug.LogWarning("Invalid live nova damage profile; retaining last valid damage.", this); }
                if (lastValidHealthDamage <= 0 || !EnemyPlayerTargeting.IsAvailable(target) || !budget.TryReserve(target)) continue;
                var report = DamageResolver.Resolve(new DamageRequest(new DamageInstance($"gargoyle-nova-{token}", actor.gameObject,
                    profile, new DamageFormulaValues(0, 0, 0, 0, lastValidHealthDamage, 0, 1),
                    attackExecutionId: $"gargoyle-nova-{token}", procPolicy: DamageProcPolicy.None),
                    new[] { target }, center, ((Vector2)target.transform.position - center).normalized));
                budget.Complete(target, report.EffectiveHitCount > 0);
            }
        }
        public void Cancel() { armed = IsTelegraphing = false; CloseCore(); }
        private bool CanOperate() => armed && isActiveAndEnabled && actor != null && actor.IsOperational && Time.timeScale > 0
            && (brain == null || (brain.CanEmit && brain.CurrentAttack.ExecutionToken == token));
        private void BreakCoreIfDue()
        {
            if (RemainingStability > 0) return;
            Cancel(); if (actor.IsOperational) brain?.RequestStun();
        }
        private void OnCoreHit(DamageContext context) => TryApplyCoreHit(context);
        private void CloseCore() => policy?.SetResponseState(false, policy.HeadExposed, brain != null && brain.IsPoiseResistant,
            brain != null ? brain.FacingDirection : policy.FacingDirection);
        private void Refresh()
        {
            if (definition != null && definition.GetValidationErrors().Count == 0)
            {
                if (!definition.TryReadStep(step.Id, out var current)) { warnedInvalid = false; return; }
                current = current.CopyForKind(EnemyAttackPayloadKind.Nova);
                if (current.GetValidationErrors().Count == 0) { step = current; warnedInvalid = false; return; }
            }
            if (warnedInvalid) return;
            warnedInvalid = true; Debug.LogWarning("Invalid live nova revision; retaining last valid attack data.", this);
        }
        private void OnDefeated(EnemyDamageEvent _) => Cancel();
        private void OnDisable() => Cancel();
        private void OnDestroy() => Unsubscribe();
        private void Unsubscribe()
        {
            if (subscribedActor != null) subscribedActor.Defeated -= OnDefeated;
            if (subscribedBrain != null) subscribedBrain.OffenseCancelled -= Cancel;
            if (subscribedPolicy != null) subscribedPolicy.AcceptedCoreHit -= OnCoreHit;
            subscribedActor = null; subscribedBrain = null; subscribedPolicy = null;
        }
    }
}
