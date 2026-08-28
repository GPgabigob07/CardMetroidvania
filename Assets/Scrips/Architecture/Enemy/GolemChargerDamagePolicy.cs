using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(EnemyHealth))]
    public sealed class GolemChargerDamagePolicy : MonoBehaviour, IDamageable
    {
        [Header(header: "Dependencies")]
        [Tooltip(tooltip: "Shared health component that receives damage accepted by this policy.")]
        [SerializeField] private EnemyHealth health;

        [Tooltip(tooltip: "Golem state provider that determines armor and interrupt rules.")]
        [SerializeField] private GolemChargerBrain brain;

        [Header(header: "Interrupt Tags")]
        [Tooltip(tooltip: "Damage tag that individually interrupts the windup and combines with Card during charge.")]
        [SerializeField] private GameplayTagSO impactTag;

        [Tooltip(tooltip: "Damage tag that individually interrupts the windup and combines with Impact during charge.")]
        [SerializeField] private GameplayTagSO cardTag;

        [Header(header: "Damage Multipliers")]
        [Range(min: 0f, max: 1f)]
        [Tooltip(tooltip: "Damage multiplier while the golem is idle or patrolling.")]
        [SerializeField] private float idleDamageMultiplier = 0.15f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Damage multiplier during windup, including an interrupting hit.")]
        [SerializeField] private float windupDamageMultiplier = 1f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Damage multiplier for a Card plus Impact hit that interrupts a charge.")]
        [SerializeField] private float chargeInterruptDamageMultiplier = 1f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Damage multiplier for body hits while the golem is interrupted.")]
        [SerializeField] private float interruptedBodyDamageMultiplier = 1.5f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Damage multiplier for head weak-point hits while the golem is interrupted.")]
        [SerializeField] private float interruptedHeadDamageMultiplier = 3f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Damage multiplier during post-charge recovery.")]
        [SerializeField] private float recoveryDamageMultiplier = 1f;

        public EnemyHealth Health => health;

        private void Awake()
        {
            ResolveDependencies();
        }

        public DamageResult ApplyDamage(in DamageContext context)
        {
            return ApplyDamage(context, EnemyHurtboxRegionType.Body);
        }

        public DamageResult ApplyDamage(in DamageContext context, EnemyHurtboxRegionType hitRegion)
        {
            ResolveDependencies();
            if (health == null || brain == null || brain.CurrentState == GolemChargerState.Dead)
            {
                return CreateRejectedResult();
            }

            if (!TryResolveMultiplier(context, hitRegion, out var multiplier))
            {
                return CreateRejectedResult();
            }

            var requestedAmount = context.Amount > 0f
                ? context.Amount
                : context.Profile != null ? context.Profile.BaseDamage : 0f;
            var adjustedContext = new DamageContext(
                source: context.Source,
                target: context.Target,
                profile: context.Profile,
                amount: requestedAmount * multiplier,
                hitPoint: context.HitPoint,
                direction: context.Direction,
                tags: context.Tags);
            return health.ApplyDamage(adjustedContext);
        }

        public void ConfigureForTests(
            EnemyHealth enemyHealth,
            GolemChargerBrain golemBrain,
            GameplayTagSO impact,
            GameplayTagSO card)
        {
            health = enemyHealth;
            brain = golemBrain;
            impactTag = impact;
            cardTag = card;
        }

        public void ConfigureMultipliers(
            float idle,
            float windup,
            float chargeInterrupt,
            float interruptedBody,
            float interruptedHead,
            float recovery)
        {
            idleDamageMultiplier = Mathf.Max(0f, idle);
            windupDamageMultiplier = Mathf.Max(0f, windup);
            chargeInterruptDamageMultiplier = Mathf.Max(0f, chargeInterrupt);
            interruptedBodyDamageMultiplier = Mathf.Max(0f, interruptedBody);
            interruptedHeadDamageMultiplier = Mathf.Max(0f, interruptedHead);
            recoveryDamageMultiplier = Mathf.Max(0f, recovery);
        }

        private bool TryResolveMultiplier(
            in DamageContext context,
            EnemyHurtboxRegionType hitRegion,
            out float multiplier)
        {
            multiplier = 0f;
            switch (brain.CurrentState)
            {
                case GolemChargerState.Idle:
                case GolemChargerState.Patrol:
                    multiplier = idleDamageMultiplier;
                    return true;
                case GolemChargerState.Windup:
                    if (HasImpact(context) || HasCard(context))
                    {
                        brain.TryInterrupt(context);
                    }

                    multiplier = windupDamageMultiplier;
                    return true;
                case GolemChargerState.Charge:
                    if (!HasImpact(context) || !HasCard(context) || !brain.TryInterrupt(context))
                    {
                        return false;
                    }

                    multiplier = chargeInterruptDamageMultiplier;
                    return true;
                case GolemChargerState.Interrupted:
                    multiplier = hitRegion == EnemyHurtboxRegionType.HeadWeakPoint
                        ? interruptedHeadDamageMultiplier
                        : interruptedBodyDamageMultiplier;
                    return true;
                case GolemChargerState.Recovery:
                    multiplier = recoveryDamageMultiplier;
                    return true;
                default:
                    return false;
            }
        }

        private bool HasImpact(in DamageContext context)
        {
            return impactTag != null && context.Tags != null && context.Tags.Contains(impactTag);
        }

        private bool HasCard(in DamageContext context)
        {
            return cardTag != null && context.Tags != null && context.Tags.Contains(cardTag);
        }

        private DamageResult CreateRejectedResult()
        {
            return new DamageResult(
                accepted: false,
                killed: health != null && health.IsDefeated,
                appliedAmount: 0f,
                remainingHealth: health != null ? health.CurrentHealth : 0f,
                hitStopSeconds: 0f);
        }

        private void ResolveDependencies()
        {
            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }

            if (brain == null)
            {
                brain = GetComponent<GolemChargerBrain>();
            }
        }
    }
}
