using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    [DefaultExecutionOrder(100)]
    public sealed class PlayerAttackHitDetector2D :
        MonoBehaviour,
        IGameplayServicesConsumer
    {
        [Header(header: "Hit Shape")]
        [Tooltip(tooltip: "Local center of the attack box when facing right.")]
        [SerializeField] private Vector2 localOffset = new Vector2(x: 1.1f, y: 0.15f);

        [Tooltip(tooltip: "World-space size of the attack overlap box.")]
        [SerializeField] private Vector2 size = new Vector2(x: 1.8f, y: 1.5f);

        [Tooltip(tooltip: "Layers considered by the attack overlap.")]
        [SerializeField] private LayerMask targetLayers = ~0;

        [Header(header: "Damage")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Prototype base damage applied by each basic attack.")]
        [SerializeField] private float baseDamage = 1f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Prototype outgoing knockback requested by each basic attack.")]
        [SerializeField] private float baseKnockbackForce = 1f;

        private readonly HashSet<MonoBehaviour> hitTargets = new();
        private PlayerController playerController;
        private PlayerCombatEffects combatEffects;
        private PlayerActionState trackedAttack;
        private HitStopRequestEventChannelSO hitStopRequests;

        public void Initialize(
            PlayerController controller,
            PlayerCombatEffects effects = null)
        {
            playerController = controller;
            combatEffects = effects != null ? effects : controller?.GetComponent<PlayerCombatEffects>();
        }

        public void BindGameplayServices(IGameplayServices services)
        {
            hitStopRequests = services?.HitStopRequests;
        }

        private void Update()
        {
            if (playerController?.ActionRunner == null)
            {
                return;
            }

            var currentState = playerController.ActionRunner.CurrentState;
            if (currentState != trackedAttack)
            {
                trackedAttack = IsAttack(currentState) ? currentState : PlayerActionState.None;
                hitTargets.Clear();
            }

            if (trackedAttack == PlayerActionState.None
                || playerController.ActionRunner.CurrentAction
                    is not IPlayerActionAnimationSource actionSource
                || actionSource.AnimationPhase != PlayerActionPhase.Execution)
            {
                return;
            }

            ResolveHits();
        }

        private void ResolveHits()
        {
            var facing = playerController.Context.FacingDirection;
            GetHitBoxGeometry(facing, out var center, out var querySize);
            var colliders = Physics2D.OverlapBoxAll(
                point: center,
                size: querySize,
                angle: 0f,
                layerMask: targetLayers);

            var candidates = new List<HitCandidate>();
            var candidateIndexByOwner = new Dictionary<MonoBehaviour, int>();
            foreach (var collider in colliders)
            {
                var damageable = collider
                    .GetComponentsInParent<MonoBehaviour>(includeInactive: false)
                    .FirstOrDefault(component => component is IDamageable);
                if (damageable == null
                    || damageable.transform.IsChildOf(transform))
                {
                    continue;
                }

                var actor = collider.GetComponentInParent<EnemyActor>();
                var region = collider.GetComponentInParent<EnemyHurtboxRegion>();
                if (actor != null && region == null)
                {
                    // The Golem's root EnemyHealth must not bypass its armor policy.
                    damageable = actor.GetComponent<GolemChargerDamagePolicy>()
                        ?? damageable;
                }

                var owner = actor != null ? (MonoBehaviour)actor : damageable;
                if (hitTargets.Contains(owner))
                {
                    continue;
                }

                var priority = region != null
                    ? region.Region == EnemyHurtboxRegionType.HeadWeakPoint ? 2 : 1
                    : 0;
                var candidate = new HitCandidate(owner, damageable, collider, priority);
                if (candidateIndexByOwner.TryGetValue(owner, out var index))
                {
                    if (priority > candidates[index].Priority)
                    {
                        candidates[index] = candidate;
                    }

                    continue;
                }

                candidateIndexByOwner.Add(owner, candidates.Count);
                candidates.Add(candidate);
            }

            var newTargets = new List<MonoBehaviour>(candidates.Count);
            var firstHitPoint = center;
            foreach (var candidate in candidates)
            {
                if (newTargets.Count == 0)
                {
                    firstHitPoint = candidate.Collider.ClosestPoint(center);
                }

                hitTargets.Add(candidate.Owner);
                newTargets.Add(candidate.Damageable);
            }

            if (newTargets.Count == 0)
            {
                return;
            }

            var executionId = (playerController.ActionRunner.CurrentAction
                as IPlayerAttackExecution)?.ExecutionId;
            var instanceId = $"{trackedAttack}-{executionId}-{Time.frameCount}";
            var instance = combatEffects != null
                ? combatEffects.BuildPrimaryDamageInstance(
                    instanceId,
                    executionId,
                    baseDamage,
                    strikePercent: 1f,
                    knockbackForce: baseKnockbackForce,
                    maxTargets: newTargets.Count)
                : new DamageInstance(
                    instanceId: instanceId,
                    sourceObject: gameObject,
                    profile: null,
                    formula: new DamageFormulaValues(
                        attack: baseDamage,
                        strikePercent: 1f,
                        strikeBonusPercent: 0f,
                        attackBuffPercent: 0f,
                        flatDamage: 0f,
                        finalDamagePercent: 0f,
                        critValue: 1f),
                    maxTargets: newTargets.Count,
                    attackExecutionId: executionId,
                    knockbackForce: baseKnockbackForce);
            var request = new DamageRequest(
                instance: instance,
                candidateTargets: newTargets.Select(target => target.gameObject).ToArray(),
                hitPoint: firstHitPoint,
                direction: Vector2.right * facing,
                targetLimit: newTargets.Count);

            var report = DamageResolver.Resolve(request: request);
            if (report.EffectiveHitCount <= 0)
            {
                return;
            }

            if (report.Allows(DamageProcPolicy.ConfirmAttackHit)
                && playerController.ActionRunner.CurrentAction
                    is IPlayerAttackHitConfirmation hitConfirmation)
            {
                hitConfirmation.ConfirmHit();
            }

            if (report.Allows(DamageProcPolicy.RequestHitStop))
            {
                hitStopRequests?.Raise(
                    payload: new HitStopRequest(
                        duration: report.RequestedHitStopSeconds,
                        sourceObject: gameObject,
                        damageInstanceId: report.Instance.InstanceId));
            }
        }

        private void OnDrawGizmosSelected()
        {
            var facing = playerController?.Context?.FacingDirection ?? 1;
            GetHitBoxGeometry(facing, out var center, out var querySize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center: center, size: querySize);
        }

        private void GetHitBoxGeometry(
            int facing,
            out Vector2 center,
            out Vector2 querySize)
        {
            var rear = localOffset.x - size.x * 0.5f;
            var front = localOffset.x + size.x * 0.5f;
            var effects = combatEffects != null
                ? combatEffects
                : GetComponent<PlayerCombatEffects>();
            front *= effects != null ? effects.PrimaryReachMultiplier : 1f;
            var width = Mathf.Max(0f, front - rear);
            center = (Vector2)transform.position
                + new Vector2((front + rear) * 0.5f * facing, localOffset.y);
            querySize = new Vector2(width, size.y);
        }

        private readonly struct HitCandidate
        {
            public HitCandidate(
                MonoBehaviour owner,
                MonoBehaviour damageable,
                Collider2D collider,
                int priority)
            {
                Owner = owner;
                Damageable = damageable;
                Collider = collider;
                Priority = priority;
            }

            public MonoBehaviour Owner { get; }
            public MonoBehaviour Damageable { get; }
            public Collider2D Collider { get; }
            public int Priority { get; }
        }

        private static bool IsAttack(PlayerActionState state)
        {
            return state == PlayerActionState.Attack1
                || state == PlayerActionState.Attack2
                || state == PlayerActionState.Attack3;
        }
    }
}
