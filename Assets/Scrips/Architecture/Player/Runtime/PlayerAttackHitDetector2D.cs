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
            var center = (Vector2)transform.position
                + new Vector2(x: localOffset.x * facing, y: localOffset.y);
            var colliders = Physics2D.OverlapBoxAll(
                point: center,
                size: size,
                angle: 0f,
                layerMask: targetLayers);

            var newTargets = new List<MonoBehaviour>();
            var firstHitPoint = center;
            foreach (var collider in colliders)
            {
                var damageable = collider
                    .GetComponentsInParent<MonoBehaviour>(includeInactive: false)
                    .FirstOrDefault(predicate: component => component is IDamageable);

                if (damageable == null
                    || damageable.transform.IsChildOf(parent: transform)
                    || !hitTargets.Add(item: damageable))
                {
                    continue;
                }

                if (newTargets.Count == 0)
                {
                    firstHitPoint = collider.ClosestPoint(position: center);
                }

                newTargets.Add(damageable);
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
            var center = (Vector2)transform.position
                + new Vector2(x: localOffset.x * facing, y: localOffset.y);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center: center, size: size);
        }

        private static bool IsAttack(PlayerActionState state)
        {
            return state == PlayerActionState.Attack1
                || state == PlayerActionState.Attack2
                || state == PlayerActionState.Attack3;
        }
    }
}
