using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class EnemyBeamAttack2D : MonoBehaviour
    {
        [Header("Encounter Bindings")]
        [SerializeField] private EnemyActor actor;
        [SerializeField] private GargoyleTuningSO tuning;
        [SerializeField] private GargoyleBrain brain;
        private EnemyAttackDefinitionSO definition;
        private EnemyAttackStep step;
        private EnemyCastHitBudget budget;
        private long token;
        private readonly HashSet<long> usedTokens = new();
        private bool warnedInvalid;
        private bool warnedDamage;
        private float lastValidDamage;
        private EnemyActor subscribedActor;
        private GargoyleBrain subscribedBrain;
        public bool IsActive { get; private set; }
        public Vector2 EndPoint { get; private set; }
        public Vector2 Origin { get; private set; }
        public event Action<long> Countered;
        public void Initialize(EnemyActor actor, GargoyleTuningSO tuning, GargoyleBrain brain = null)
        {
            Unsubscribe(); this.actor = actor; this.tuning = tuning; this.brain = brain;
            subscribedActor = actor; subscribedBrain = brain;
            if (subscribedActor != null) subscribedActor.Defeated += OnDefeated;
            if (subscribedBrain != null) subscribedBrain.OffenseCancelled += Cancel;
        }
        public void Begin(long castToken, EnemyAttackDefinitionSO value)
        {
            if (value == null || !value.TryCaptureSteps(out var steps)) return;
            var captured = steps.FirstOrDefault(candidate => candidate.Payload.Kind == EnemyAttackPayloadKind.Beam);
            if (captured != null) Begin(castToken, value, captured);
        }
        public void Begin(in EnemyAttackExecutionSnapshot execution)
        {
            if (execution.IsRunning && execution.Phase == EnemyAttackPhase.Active && execution.Kind == EnemyAttackPayloadKind.Beam)
                Begin(execution.ExecutionToken, execution.Definition, execution.Step);
        }
        private void Begin(long castToken, EnemyAttackDefinitionSO value, EnemyAttackStep captured)
        {
            if (IsActive || castToken <= 0 || usedTokens.Contains(castToken) || actor == null || !actor.IsOperational
                || captured.DamageProfile == null || !float.IsFinite(captured.DamageProfile.BaseDamage) || captured.DamageProfile.BaseDamage < 0) return;
            token = castToken; definition = value; step = captured;
            budget = new EnemyCastHitBudget(token, step.Payload.AcceptedHitLimit);
            lastValidDamage = captured.DamageProfile.BaseDamage;
            usedTokens.Add(token); warnedInvalid = warnedDamage = false; IsActive = true;
        }
        public void Sample(Vector2 origin, Vector2 lockedDirection, float activeElapsed)
        {
            if (!CanSample() || !float.IsFinite(activeElapsed) || activeElapsed < 0
                || !Finite(origin) || !Finite(lockedDirection) || lockedDirection.sqrMagnitude == 0) return;
            Refresh();
            if (activeElapsed >= step.ActiveDuration) return;
            Origin = origin;
            var direction = lockedDirection.normalized;
            var payload = step.Payload;
            var length = payload.BeamLength;
            var radius = payload.BeamThickness / 2;
            foreach (var hit in Physics2D.CircleCastAll(origin, radius, direction, length, tuning.EnvironmentLayer))
                if (hit.collider != null && !hit.collider.isTrigger && !hit.collider.transform.IsChildOf(actor.transform))
                    length = Mathf.Min(length, hit.distance);
            EndPoint = origin + direction * length;
            if (length <= 0) return;
            var targets = Physics2D.OverlapCapsuleAll((origin + EndPoint) / 2,
                new Vector2(length + payload.BeamThickness, payload.BeamThickness), CapsuleDirection2D.Horizontal,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg)
                .Select(EnemyPlayerTargeting.CanonicalTarget).Where(target => target != null).Distinct().ToArray();
            foreach (var guard in FindGuards(targets))
            {
                if (!CanSample()) return;
                var interception = guard.TryIntercept(new BeamGuardQuery(token, origin, EndPoint, payload.BeamThickness,
                    origin, activeElapsed < payload.BeamOpeningDuration));
                if (!actor.IsOperational || !IsActive) return;
                if (interception.Kind == WardInterceptionKind.Countered)
                {
                    EndPoint = interception.ClipPosition; Cancel();
                    Countered?.Invoke(token);
                    if (actor.IsOperational) brain?.RequestBeamStagger(token);
                    return;
                }
                if (interception.Kind == WardInterceptionKind.Clipped) EndPoint = interception.ClipPosition;
            }
            foreach (var target in targets)
            {
                if (!CanSample()) return;
                var collider = target.GetComponent<Collider2D>();
                var intersects = collider != null && BeamSegmentGeometry.TryIntersectBounds(origin, EndPoint, collider.bounds, radius, out _);
                if (collider == null)
                    intersects = target.GetComponentsInChildren<Collider2D>().Any(candidate => candidate.enabled
                        && BeamSegmentGeometry.TryIntersectBounds(origin, EndPoint, candidate.bounds, radius, out _));
                if (!intersects || !EnemyPlayerTargeting.IsAvailable(target)) continue;
                var profile = step.DamageProfile;
                if (profile != null && float.IsFinite(profile.BaseDamage) && profile.BaseDamage >= 0)
                { lastValidDamage = profile.BaseDamage; warnedDamage = false; }
                else if (!warnedDamage)
                { warnedDamage = true; Debug.LogWarning("Invalid live beam damage profile; retaining last valid damage.", this); }
                if (lastValidDamage <= 0 || !budget.TryReserve(target)) continue;
                var report = DamageResolver.Resolve(new DamageRequest(new DamageInstance($"gargoyle-beam-{token}", actor.gameObject,
                    profile, new DamageFormulaValues(0, 0, 0, 0, lastValidDamage, 0, 1),
                    attackExecutionId: $"gargoyle-beam-{token}", procPolicy: DamageProcPolicy.None), new[] { target }, origin, direction));
                budget.Complete(target, report.EffectiveHitCount > 0);
            }
        }
        private IEnumerable<PlayerWardRuntime> FindGuards(IEnumerable<GameObject> healthTargets)
        {
            var guards = healthTargets.Select(target => target.GetComponentInParent<PlayerWardRuntime>());
            if (brain != null)
            {
                // A tall/moving guard can cross the beam while its owner's health bounds miss it.
                var targetGuard = brain.Target != null ? brain.Target.GetComponentInParent<PlayerWardRuntime>() : null;
                guards = guards.Append(targetGuard);
            }
            else
            {
                // Standalone payloads have no bound player; discover guards separately from damage overlaps.
                guards = guards.Concat(FindObjectsByType<PlayerWardRuntime>(FindObjectsSortMode.None)
                    .Where(guard => guard.gameObject.scene == actor.gameObject.scene));
            }
            return guards.Where(guard => guard != null && guard.isActiveAndEnabled && EnemyPlayerTargeting.IsAvailable(guard.gameObject))
                .Distinct().ToArray();
        }
        public void Cancel() => IsActive = false;
        private bool CanSample() => IsActive && isActiveAndEnabled && actor != null && actor.IsOperational
            && tuning != null && tuning.EnvironmentLayer.value != 0 && Time.timeScale > 0
            && (brain == null || (brain.CanEmit && brain.CurrentAttack.ExecutionToken == token && brain.CurrentAttack.StepId == step.Id));
        private void Refresh()
        {
            if (definition != null && definition.GetValidationErrors().Count == 0)
            {
                if (!definition.TryReadStep(step.Id, out var current)) { warnedInvalid = false; return; }
                current = current.CopyForKind(EnemyAttackPayloadKind.Beam);
                if (current.GetValidationErrors().Count == 0) { step = current; warnedInvalid = false; return; }
            }
            if (warnedInvalid) return;
            warnedInvalid = true; Debug.LogWarning("Invalid live beam revision; retaining last valid attack data.", this);
        }
        private void OnDefeated(EnemyDamageEvent _) => Cancel();
        private void OnDisable() => Cancel();
        private void OnDestroy() => Unsubscribe();
        private void Unsubscribe()
        {
            if (subscribedActor != null) subscribedActor.Defeated -= OnDefeated;
            if (subscribedBrain != null) subscribedBrain.OffenseCancelled -= Cancel;
            subscribedActor = null; subscribedBrain = null;
        }
        private static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
    }
}
