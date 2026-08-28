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
        IGameplayServicesConsumer
    {
        private const int MaximumSupplementalDepth = 1;
        private const string ChainFeedbackId = "chain";
        private const string EnergyGainFeedbackId = "energy-gain";
        private const string KnockbackFeedbackId = "knockback";
        private const string SupplementalFeedbackId = "supplemental";

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
        private float energyGainMultiplier = 1f;
        private float knockbackMultiplier = 1f;
        private ICardFeedbackService cardFeedback;
        private CardDefinitionSO chainCard;
        private CardDefinitionSO energyGainCard;
        private CardDefinitionSO knockbackCard;

        private void Awake()
        {
            chainModifier = new ChainDamageModifier(this);
        }

        public float AttackValue => 1f;
        public GameplayTagSet OffensiveTags => null;
        public int ChainIncrements => chainIncrements;
        public int ChainCapacity => chainCapacity;
        public int EnergyGainCharges => energyGainCharges;
        public int KnockbackCharges => knockbackCharges;
        public DamageResolutionReport LastSupplementalReport { get; private set; }

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
            ApplyKnockback(report);
            GrantDefeatRewards(report);
            ResolveHitEnergy(report);
            AdvanceChain(report);
            ResolveSupplementalDamage(report);
        }

        public void OnDamageDealt(in DamageContext context, in DamageResult result)
        {
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
                procPolicy: DamageProcPolicy.PrimaryAttack);
        }

        public void BeginAttack(string executionId)
        {
            if (!string.IsNullOrWhiteSpace(executionId))
            {
                attackOutcomes[executionId] = default;
            }
        }

        public void CompleteAttack(string executionId)
        {
            if (string.IsNullOrWhiteSpace(executionId)
                || !attackOutcomes.Remove(executionId, out var outcome))
            {
                return;
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
            CardDefinitionSO card = null)
        {
            armedSupplemental = new ArmedSupplementalDamage(
                attackExecutionId,
                effectId,
                Mathf.Max(1f, totalMultiplier),
                card);
            cardFeedback?.UpsertHudEffect(new CardHudEffectViewModel(
                effectKey: BuildFeedbackKey(SupplementalFeedbackId),
                sourceObject: gameObject,
                card: card,
                displayText: "armed"));
            PublishWorldFeedback(card, CardFeedbackKind.Activated);
        }

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
                    procPolicy: DamageProcPolicy.SupplementalDefault);
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

        private struct AttackOutcome
        {
            public int EffectiveHitCount;
        }

        private readonly struct ArmedSupplementalDamage
        {
            public ArmedSupplementalDamage(
                string attackExecutionId,
                string effectId,
                float totalMultiplier,
                CardDefinitionSO card)
            {
                AttackExecutionId = attackExecutionId;
                EffectId = effectId;
                TotalMultiplier = totalMultiplier;
                Card = card;
            }

            public string AttackExecutionId { get; }
            public string EffectId { get; }
            public float TotalMultiplier { get; }
            public CardDefinitionSO Card { get; }
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
