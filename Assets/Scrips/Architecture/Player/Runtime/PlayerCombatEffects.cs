using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerCombatEffects :
        MonoBehaviour,
        IDamageProvider,
        IDamageListener,
        IPoiseDamageSource,
        IGameplayServicesConsumer
    {
        private const int MaximumSupplementalDepth = 1;
        private const string ChainFeedbackId = "chain";
        private const string EnergyGainFeedbackId = "energy-gain";
        private const string KnockbackFeedbackId = "knockback";
        private const string SupplementalFeedbackId = "supplemental";
        private const string PoiseFeedbackId = "poise";
        private const string ReachFeedbackId = "reach";

        [Header("Resources")]
        [Tooltip("Wallet that receives Energy from hit rolls and enemy defeats.")]
        [SerializeField] private PlayerResourceWallet wallet;

        [Tooltip("Energy resource used by prototype combat rewards.")]
        [SerializeField] private ResourceDefinitionSO energyResource;

        [Range(0f, 1f)]
        [Tooltip("Base chance to gain Energy from an effective primary attack request.")]
        [SerializeField] private float hitEnergyChance = 0.3f;

        [Min(0f)]
        [Tooltip("Energy granted when the hit-gain roll succeeds.")]
        [SerializeField] private float hitEnergyAmount = 1f;

        [Header("Chain Damage")]
        [Min(0f)]
        [Tooltip("Final damage percentage added by each earned Chain increment.")]
        [SerializeField] private float chainDamagePercentPerIncrement = 0.1f;

        [Header("Supplemental Damage")]
        [Tooltip("Optional profile used by card-armed primary attacks and their linked overcharge damage.")]
        [SerializeField] private DamageProfileSO supplementalDamageProfile;

        private readonly Dictionary<string, AttackOutcome> attackOutcomes = new();
        private ChainDamageModifier chainModifier;
        private IRandomRollSource randomRollSource = new UnityRandomRollSource();
        private ArmedSupplementalDamage armedSupplemental;
        private int chainIncrements;
        private int chainCapacity;
        private int energyGainCharges;
        private int knockbackCharges;
        private int remainingPoiseHits;
        private int reachIncrements;
        private int reachLimit;
        private float reachPercentPerHit;
        private bool growingReachActive;
        private float energyGainMultiplier = 1f;
        private float knockbackMultiplier = 1f;
        private float basePoiseDamage;
        private float poiseMultiplier = 1f;
        private ICardFeedbackService cardFeedback;
        private CardDefinitionSO chainCard;
        private CardDefinitionSO energyGainCard;
        private CardDefinitionSO knockbackCard;
        private CardDefinitionSO poiseCard;
        private CardDefinitionSO reachCard;

        private void Awake()
        {
            chainModifier = new ChainDamageModifier(this);
            EligiblePrimaryHitsResolved += HandleGrowingReachHits;
            PrimaryAttackMissed += HandleGrowingReachMiss;
        }

        public float AttackValue => 1f;
        public GameplayTagSet OffensiveTags => null;
        public int ChainIncrements => chainIncrements;
        public int ChainCapacity => chainCapacity;
        public int EnergyGainCharges => energyGainCharges;
        public int KnockbackCharges => knockbackCharges;
        public int RemainingPoiseHits => remainingPoiseHits;
        public bool CanArmPoiseHits => remainingPoiseHits <= 0;
        public bool CanArmGrowingReach => !growingReachActive;
        public float PrimaryReachMultiplier => 1f + reachIncrements * reachPercentPerHit;
        public DamageResolutionReport LastSupplementalReport { get; private set; }
        public event Action<int> EligiblePrimaryHitsResolved;
        public event Action PrimaryAttackMissed;

        public void BindGameplayServices(IGameplayServices services)
        {
            cardFeedback = services?.CardFeedback;
        }

        public IEnumerable<IDamageModifier> GetDamageModifiers()
        {
            chainModifier ??= new ChainDamageModifier(this);
            yield return chainModifier;
        }

        public void OnDamageResolved(DamageResolutionReport report)
        {
            if (report == null)
            {
                return;
            }

            RecordAttackOutcome(report);
            RecordEligiblePrimaryHits(report);
            ApplyKnockback(report);
            GrantDefeatRewards(report);
            ResolveHitEnergy(report);
            AdvanceChain(report);
            ResolveSupplementalDamage(report);
        }

        public void OnDamageDealt(in DamageContext context, in DamageResult result)
        {
            if (remainingPoiseHits <= 0
                || context.PoiseDamage <= 0f
                || !result.Accepted
                || result.AppliedAmount <= 0f
                || context.Target == null
                || context.Target.GetComponentInParent<EnemyActor>() == null)
            {
                return;
            }

            remainingPoiseHits--;
            PublishWorldFeedback(
                poiseCard,
                CardFeedbackKind.Triggered,
                CardFeedbackAnchor.HitPoint,
                context.HitPoint);
            RefreshChargeHud(PoiseFeedbackId, poiseCard, remainingPoiseHits);
        }

        public void OnDamageReceived(in DamageContext context, in DamageResult result)
        {
        }

        public void OnDamageResolutionComplete(DamageResolutionReport report)
        {
        }

        public DamageInstance BuildPrimaryDamageInstance(
            string instanceId,
            string attackExecutionId,
            float attack,
            float strikePercent,
            float knockbackForce,
            int maxTargets)
        {
            var appliedKnockback = knockbackCharges > 0
                ? knockbackForce * knockbackMultiplier
                : knockbackForce;
            var profile = armedSupplemental.IsArmed
                && armedSupplemental.AttackExecutionId == attackExecutionId
                ? supplementalDamageProfile
                : null;
            var poiseDamage = armedSupplemental.IsArmed
                && armedSupplemental.AttackExecutionId == attackExecutionId
                ? armedSupplemental.PoiseDamage
                : 0f;
            return new DamageInstance(
                instanceId: instanceId,
                sourceObject: gameObject,
                profile: profile,
                formula: new DamageFormulaValues(
                    attack: attack,
                    strikePercent: strikePercent,
                    strikeBonusPercent: 0f,
                    attackBuffPercent: 0f,
                    flatDamage: 0f,
                    finalDamagePercent: 0f,
                    critValue: 1f),
                maxTargets: maxTargets,
                attackExecutionId: attackExecutionId,
                knockbackForce: appliedKnockback,
                procPolicy: DamageProcPolicy.PrimaryAttack,
                poiseDamage: poiseDamage,
                isCardEnhancedMelee: (knockbackCharges > 0 && knockbackMultiplier > 1f)
                    || (chainIncrements > 0 && chainDamagePercentPerIncrement > 0f)
                    || remainingPoiseHits > 0
                    || growingReachActive
                    || (armedSupplemental.IsArmed
                        && armedSupplemental.AttackExecutionId == attackExecutionId
                        && armedSupplemental.TotalMultiplier > 1f));
        }

        public void BeginAttack(string executionId)
        {
            if (!string.IsNullOrWhiteSpace(executionId))
            {
                attackOutcomes[executionId] = default;
            }
        }

        public void CancelAttack(string executionId)
        {
            if (string.IsNullOrWhiteSpace(executionId))
            {
                return;
            }

            attackOutcomes.Remove(executionId);
            if (armedSupplemental.IsArmed
                && armedSupplemental.AttackExecutionId == executionId)
            {
                cardFeedback?.RemoveHudEffect(BuildFeedbackKey(SupplementalFeedbackId));
                armedSupplemental = default;
            }
        }

        public void CompleteAttack(string executionId)
        {
            if (string.IsNullOrWhiteSpace(executionId)
                || !attackOutcomes.Remove(executionId, out var outcome))
            {
                return;
            }

            if (outcome.EligiblePrimaryHitCount == 0)
            {
                PrimaryAttackMissed?.Invoke();
            }

            if (outcome.EffectiveHitCount == 0)
            {
                if (chainIncrements > 0)
                {
                    PublishWorldFeedback(chainCard, CardFeedbackKind.Failed);
                }

                chainIncrements = 0;
                RefreshChainHud();
            }

            if (armedSupplemental.IsArmed
                && armedSupplemental.AttackExecutionId == executionId)
            {
                PublishWorldFeedback(armedSupplemental.Card, CardFeedbackKind.Failed);
                cardFeedback?.RemoveHudEffect(BuildFeedbackKey(SupplementalFeedbackId));
                armedSupplemental = default;
            }
        }

        public void AddChainCapacity(
            int amount,
            float damagePercentPerIncrement,
            CardDefinitionSO card = null)
        {
            chainCapacity = Mathf.Max(0, chainCapacity + Mathf.Max(0, amount));
            chainDamagePercentPerIncrement = Mathf.Max(
                0f,
                damagePercentPerIncrement);
            chainCard = card != null ? card : chainCard;
            RefreshChainHud();
            PublishWorldFeedback(chainCard, CardFeedbackKind.Activated);
        }

        public void AddEnergyGainCharges(
            int amount,
            float multiplier,
            CardDefinitionSO card = null)
        {
            energyGainCharges = Mathf.Max(0, energyGainCharges + Mathf.Max(0, amount));
            energyGainMultiplier = Mathf.Max(1f, multiplier);
            energyGainCard = card != null ? card : energyGainCard;
            RefreshChargeHud(
                EnergyGainFeedbackId,
                energyGainCard,
                energyGainCharges);
            PublishWorldFeedback(energyGainCard, CardFeedbackKind.Activated);
        }

        public void AddKnockbackCharges(
            int amount,
            float multiplier,
            CardDefinitionSO card = null)
        {
            knockbackCharges = Mathf.Max(0, knockbackCharges + Mathf.Max(0, amount));
            knockbackMultiplier = Mathf.Max(1f, multiplier);
            knockbackCard = card != null ? card : knockbackCard;
            RefreshChargeHud(
                KnockbackFeedbackId,
                knockbackCard,
                knockbackCharges);
            PublishWorldFeedback(knockbackCard, CardFeedbackKind.Activated);
        }

        public void ArmSupplementalDamage(
            string attackExecutionId,
            string effectId,
            float totalMultiplier,
            CardDefinitionSO card = null,
            float poiseDamage = 0f)
        {
            armedSupplemental = new ArmedSupplementalDamage(
                attackExecutionId,
                effectId,
                Mathf.Max(1f, totalMultiplier),
                card,
                Mathf.Max(0f, poiseDamage));
            cardFeedback?.UpsertHudEffect(new CardHudEffectViewModel(
                effectKey: BuildFeedbackKey(SupplementalFeedbackId),
                sourceObject: gameObject,
                card: card,
                displayText: "armed"));
            PublishWorldFeedback(card, CardFeedbackKind.Activated);
        }

        public void ArmPoiseHits(
            int hits,
            float basePoise,
            float multiplier,
            CardDefinitionSO card = null)
        {
            remainingPoiseHits = Mathf.Max(0, hits);
            basePoiseDamage = Mathf.Max(0f, basePoise);
            poiseMultiplier = Mathf.Max(0f, multiplier);
            poiseCard = card != null ? card : poiseCard;
            RefreshChargeHud(PoiseFeedbackId, poiseCard, remainingPoiseHits);
            PublishWorldFeedback(poiseCard, CardFeedbackKind.Activated);
        }

        public void ArmGrowingReach(
            float percentPerHit,
            int maxIncrements,
            CardDefinitionSO card = null)
        {
            reachPercentPerHit = Mathf.Max(0f, percentPerHit);
            reachLimit = Mathf.Max(0, maxIncrements);
            reachIncrements = 0;
            growingReachActive = true;
            reachCard = card;
            RefreshReachHud();
            PublishWorldFeedback(reachCard, CardFeedbackKind.Activated);
        }

        public void ClearGrowingReach()
        {
            growingReachActive = false;
            reachIncrements = 0;
            reachLimit = 0;
            reachPercentPerHit = 0f;
            cardFeedback?.RemoveHudEffect(BuildFeedbackKey(ReachFeedbackId));
            reachCard = null;
        }

        private void HandleGrowingReachHits(int eligibleHitCount)
        {
            if (!growingReachActive || eligibleHitCount <= 0)
            {
                return;
            }

            var previous = reachIncrements;
            reachIncrements = Mathf.Min(reachLimit, reachIncrements + eligibleHitCount);
            if (reachIncrements == previous)
            {
                return;
            }

            RefreshReachHud();
            PublishWorldFeedback(reachCard, CardFeedbackKind.Triggered);
        }

        private void HandleGrowingReachMiss()
        {
            if (!growingReachActive)
            {
                return;
            }

            PublishWorldFeedback(reachCard, CardFeedbackKind.Failed);
            ClearGrowingReach();
        }

        private void RefreshReachHud()
        {
            var key = BuildFeedbackKey(ReachFeedbackId);
            if (!growingReachActive)
            {
                cardFeedback?.RemoveHudEffect(key);
                return;
            }

            cardFeedback?.UpsertHudEffect(new CardHudEffectViewModel(
                effectKey: key,
                sourceObject: gameObject,
                card: reachCard,
                displayText: $"+{reachIncrements * reachPercentPerHit:P0}"));
        }

        public void ClearPoiseHits()
        {
            remainingPoiseHits = 0;
            basePoiseDamage = 0f;
            poiseMultiplier = 1f;
            cardFeedback?.RemoveHudEffect(BuildFeedbackKey(PoiseFeedbackId));
        }

        public float GetPoiseDamage(in DamageInstance instance, GameObject target) =>
            remainingPoiseHits > 0
            && instance.Provenance.OriginKind == DamageOriginKind.Primary
            && (instance.ProcPolicy & DamageProcPolicy.ConfirmAttackHit) != 0
            && target != null
            && target.GetComponentInParent<EnemyActor>() != null
                ? basePoiseDamage * poiseMultiplier
                : 0f;

        public void SetRandomRollSource(IRandomRollSource source)
        {
            randomRollSource = source ?? new UnityRandomRollSource();
        }

        public void ConfigureResources(
            PlayerResourceWallet resourceWallet,
            ResourceDefinitionSO energy)
        {
            wallet = resourceWallet;
            energyResource = energy;
        }

        public void ConfigureSupplementalDamageProfile(DamageProfileSO profile)
        {
            supplementalDamageProfile = profile;
        }

        private void RecordAttackOutcome(DamageResolutionReport report)
        {
            var executionId = report.Instance.AttackExecutionId;
            if (string.IsNullOrWhiteSpace(executionId)
                || !report.Allows(DamageProcPolicy.ConfirmAttackHit)
                || !attackOutcomes.TryGetValue(executionId, out var outcome))
            {
                return;
            }

            outcome.EffectiveHitCount += report.EffectiveHitCount;
            attackOutcomes[executionId] = outcome;
        }

        private void RecordEligiblePrimaryHits(DamageResolutionReport report)
        {
            if (!report.IsPrimary
                || !report.Allows(DamageProcPolicy.ConfirmAttackHit))
            {
                return;
            }

            var eligibleHitCount = report.TargetResults.Count(
                targetResult => IsEligibleEnemyHit(targetResult));
            if (eligibleHitCount <= 0)
            {
                return;
            }

            var executionId = report.Instance.AttackExecutionId;
            if (!string.IsNullOrWhiteSpace(executionId)
                && attackOutcomes.TryGetValue(executionId, out var outcome))
            {
                outcome.EligiblePrimaryHitCount += eligibleHitCount;
                attackOutcomes[executionId] = outcome;
            }

            EligiblePrimaryHitsResolved?.Invoke(eligibleHitCount);
        }

        private void ApplyKnockback(DamageResolutionReport report)
        {
            if (report.EffectiveHitCount <= 0
                || !report.Allows(DamageProcPolicy.ApplyKnockback)
                || report.Instance.KnockbackForce <= 0f)
            {
                return;
            }

            foreach (var targetResult in report.TargetResults)
            {
                if (!IsEffective(targetResult))
                {
                    continue;
                }

                var receiver = targetResult.Context.Target
                    ?.GetComponentsInParent<MonoBehaviour>(includeInactive: false)
                    .OfType<IKnockbackReceiver>()
                    .FirstOrDefault();
                receiver?.ApplyKnockback(
                    direction: targetResult.Context.Direction,
                    force: report.Instance.KnockbackForce);
            }

            if (knockbackCharges > 0)
            {
                PublishWorldFeedback(
                    knockbackCard,
                    CardFeedbackKind.Triggered,
                    CardFeedbackAnchor.HitPoint,
                    TryGetFirstEffectiveHitPoint(report));
                knockbackCharges--;
                RefreshChargeHud(
                    KnockbackFeedbackId,
                    knockbackCard,
                    knockbackCharges);
            }
        }

        private void ResolveHitEnergy(DamageResolutionReport report)
        {
            if (report.EffectiveHitCount <= 0
                || !report.Allows(DamageProcPolicy.RollHitResourceGain))
            {
                return;
            }

            var hadCharges = energyGainCharges > 0;
            var multiplier = hadCharges ? energyGainMultiplier : 1f;
            var succeeded = randomRollSource.NextNormalized() < hitEnergyChance;
            if (succeeded)
            {
                wallet?.Gain(energyResource, hitEnergyAmount * multiplier);
            }

            if (hadCharges)
            {
                PublishWorldFeedback(
                    energyGainCard,
                    succeeded ? CardFeedbackKind.Triggered : CardFeedbackKind.Failed,
                    succeeded ? CardFeedbackAnchor.HitPoint : CardFeedbackAnchor.SourceHead,
                    succeeded ? TryGetFirstEffectiveHitPoint(report) : null);
                energyGainCharges--;
                RefreshChargeHud(
                    EnergyGainFeedbackId,
                    energyGainCard,
                    energyGainCharges);
            }
        }

        private void AdvanceChain(DamageResolutionReport report)
        {
            if (report.EffectiveHitCount > 0
                && report.Allows(DamageProcPolicy.AdvanceChain)
                && chainCapacity > 0)
            {
                chainIncrements = Mathf.Min(chainCapacity, chainIncrements + 1);
                RefreshChainHud();
                PublishWorldFeedback(
                    chainCard,
                    CardFeedbackKind.Triggered,
                    CardFeedbackAnchor.HitPoint,
                    TryGetFirstEffectiveHitPoint(report));
            }
        }

        private void GrantDefeatRewards(DamageResolutionReport report)
        {
            if (!report.Allows(DamageProcPolicy.GrantKillRewards))
            {
                return;
            }

            foreach (var targetResult in report.TargetResults)
            {
                if (!IsEffective(targetResult) || !targetResult.Result.Killed)
                {
                    continue;
                }

                var actor = targetResult.Context.Target?.GetComponentInParent<EnemyActor>();
                if (actor?.Definition != null)
                {
                    wallet?.Gain(energyResource, actor.Definition.DefeatEnergyReward);
                }
            }
        }

        private void ResolveSupplementalDamage(DamageResolutionReport report)
        {
            if (!armedSupplemental.IsArmed
                || !report.IsPrimary
                || report.Instance.AttackExecutionId != armedSupplemental.AttackExecutionId)
            {
                return;
            }

            var armed = armedSupplemental;
            armedSupplemental = default;
            cardFeedback?.RemoveHudEffect(BuildFeedbackKey(SupplementalFeedbackId));

            for (var targetIndex = 0; targetIndex < report.TargetResults.Count; targetIndex++)
            {
                var targetResult = report.TargetResults[targetIndex];
                if (!IsEffective(targetResult) || targetResult.Result.Killed)
                {
                    continue;
                }

                var amount = targetResult.Formula.EligibleBaseDamage
                    * Mathf.Max(0f, armed.TotalMultiplier - 1f);
                if (amount <= 0f)
                {
                    continue;
                }

                var instanceId =
                    $"{report.Instance.InstanceId}-supplemental-{armed.EffectId}-{targetIndex}";
                var provenance = DamageProvenance.Supplemental(
                    parentInstanceId: report.Instance.InstanceId,
                    rootInstanceId: report.Instance.Provenance.RootInstanceId,
                    effectId: armed.EffectId,
                    chainDepth: report.Instance.Provenance.ChainDepth + 1);
                if (provenance.ChainDepth > MaximumSupplementalDepth)
                {
                    continue;
                }

                var instance = new DamageInstance(
                    instanceId: instanceId,
                    sourceObject: gameObject,
                    profile: supplementalDamageProfile,
                    formula: new DamageFormulaValues(
                        attack: amount,
                        strikePercent: 1f,
                        strikeBonusPercent: 0f,
                        attackBuffPercent: 0f,
                        flatDamage: 0f,
                        finalDamagePercent: 0f,
                        critValue: 1f),
                    attackExecutionId: report.Instance.AttackExecutionId,
                    provenance: provenance,
                    procPolicy: DamageProcPolicy.SupplementalDefault,
                    poiseDamage: armed.PoiseDamage);
                var request = new DamageRequest(
                    instance: instance,
                    candidateTargets: new[] { targetResult.Context.Target },
                    hitPoint: targetResult.Context.HitPoint,
                    direction: targetResult.Context.Direction);
                LastSupplementalReport = DamageResolver.Resolve(request);
                if (LastSupplementalReport.TotalAppliedAmount > 0f)
                {
                    PublishWorldFeedback(
                        armed.Card,
                        CardFeedbackKind.Triggered,
                        CardFeedbackAnchor.HitPoint,
                        targetResult.Context.HitPoint);
                }
            }
        }

        private void RefreshChainHud()
        {
            if (chainCapacity <= 0)
            {
                cardFeedback?.RemoveHudEffect(BuildFeedbackKey(ChainFeedbackId));
                return;
            }

            cardFeedback?.UpsertHudEffect(new CardHudEffectViewModel(
                effectKey: BuildFeedbackKey(ChainFeedbackId),
                sourceObject: gameObject,
                card: chainCard,
                displayText: chainIncrements > 0
                    ? $"x{chainIncrements}"
                    : $"+{chainCapacity}",
                visualState: chainIncrements > 0
                    ? CardHudEffectVisualState.Active
                    : CardHudEffectVisualState.Inactive));
        }

        private void RefreshChargeHud(
            string feedbackId,
            CardDefinitionSO card,
            int charges)
        {
            var key = BuildFeedbackKey(feedbackId);
            if (charges <= 0)
            {
                cardFeedback?.RemoveHudEffect(key);
                return;
            }

            cardFeedback?.UpsertHudEffect(new CardHudEffectViewModel(
                effectKey: key,
                sourceObject: gameObject,
                card: card,
                displayText: charges.ToString()));
        }

        private void PublishWorldFeedback(
            CardDefinitionSO card,
            CardFeedbackKind kind,
            CardFeedbackAnchor anchor = CardFeedbackAnchor.SourceHead,
            Vector3? worldPosition = null)
        {
            if (card == null)
            {
                return;
            }

            cardFeedback?.PublishWorldFeedback(new CardWorldFeedbackViewModel(
                card: card,
                sourceObject: gameObject,
                kind: kind,
                anchor: anchor,
                worldPosition: worldPosition));
        }

        private string BuildFeedbackKey(string feedbackId)
        {
            return $"{GetInstanceID()}:{feedbackId}";
        }

        private static Vector3? TryGetFirstEffectiveHitPoint(DamageResolutionReport report)
        {
            foreach (var targetResult in report.TargetResults)
            {
                if (IsEffective(targetResult))
                {
                    return targetResult.Context.HitPoint;
                }
            }

            return null;
        }

        private static bool IsEffective(in DamageTargetResult targetResult)
        {
            return targetResult.Result.Accepted && targetResult.Result.AppliedAmount > 0f;
        }

        private static bool IsEligibleEnemyHit(in DamageTargetResult targetResult)
        {
            return targetResult.Result.Accepted
                && targetResult.Result.AppliedAmount > 0f
                && targetResult.Context.Target != null
                && targetResult.Context.Target.GetComponentInParent<EnemyActor>() != null;
        }

        private struct AttackOutcome
        {
            public int EffectiveHitCount;
            public int EligiblePrimaryHitCount;
        }

        private readonly struct ArmedSupplementalDamage
        {
            public ArmedSupplementalDamage(
                string attackExecutionId,
                string effectId,
                float totalMultiplier,
                CardDefinitionSO card,
                float poiseDamage)
            {
                AttackExecutionId = attackExecutionId;
                EffectId = effectId;
                TotalMultiplier = totalMultiplier;
                Card = card;
                PoiseDamage = poiseDamage;
            }

            public string AttackExecutionId { get; }
            public string EffectId { get; }
            public float TotalMultiplier { get; }
            public CardDefinitionSO Card { get; }
            public float PoiseDamage { get; }
            public bool IsArmed => !string.IsNullOrWhiteSpace(AttackExecutionId);
        }

        private sealed class ChainDamageModifier : IDamageModifier
        {
            private readonly PlayerCombatEffects owner;

            public ChainDamageModifier(PlayerCombatEffects owner)
            {
                this.owner = owner;
            }

            public int Priority => 0;
            public DamageModifierPhase Phase => DamageModifierPhase.PreTargetResolve;

            public bool AppliesTo(in DamageModifierContext context)
            {
                return context.Instance.Provenance.OriginKind == DamageOriginKind.Primary
                    && owner.chainIncrements > 0;
            }

            public void Modify(
                ref DamageFormulaValues values,
                in DamageModifierContext context)
            {
                values.FinalDamagePercent += owner.chainIncrements
                    * owner.chainDamagePercentPerIncrement;
            }

            public void OnDamageResolved(DamageResolutionReport report)
            {
            }
        }
    }
}
