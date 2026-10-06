using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class GargoyleDamagePolicy : MonoBehaviour
    {
        [Header("Asset And Actor Bindings")]
        [SerializeField] private EnemyActor actor;
        [SerializeField] private EnemyPoise poise;
        [SerializeField] private GargoyleTuningSO tuning;
        private GargoyleLiveTuning liveTuning;
        private GargoylePresentationValues presentation;
        private bool coreOpen;
        private bool headExposed;
        private bool poiseResistant;
        private bool reportedInvalidPresentation;
        private readonly HashSet<(GameObject source, string execution)> acceptedExecutions = new();
        private EnemyActor subscribedActor;
        public event Action<DamageContext> AcceptedCoreHit;
        public event Action RegionsChanged;
        public EnemyActor Actor => actor;
        public int FacingDirection { get; private set; } = 1;
        public bool CoreOpen => coreOpen;
        public bool HeadExposed => headExposed;

        private void Awake()
        {
            if (actor == null) actor = GetComponent<EnemyActor>();
            if (poise == null) poise = GetComponent<EnemyPoise>();
            if (actor != null && actor.IsInitialized && poise != null && poise.IsInitialized && tuning != null)
                Initialize(actor, poise, tuning);
        }
        private void OnDestroy() => Unsubscribe();
        public void Initialize(EnemyActor actor, EnemyPoise poise, GargoyleTuningSO tuning)
        {
            if (actor == null || !actor.IsInitialized) throw new ArgumentException("An initialized enemy actor is required.", nameof(actor));
            if (poise == null || !poise.IsInitialized) throw new ArgumentException("Initialized enemy poise is required.", nameof(poise));
            Unsubscribe();
            this.actor = actor; this.poise = poise; this.tuning = tuning;
            liveTuning = new GargoyleLiveTuning(tuning);
            if (tuning.Presentation == null || !tuning.Presentation.TryReadPresentation(out presentation))
                throw new ArgumentException("Gargoyle damage regions require a valid presentation asset.", nameof(tuning));
            subscribedActor = actor;
            subscribedActor.Defeated += OnDefeated;
            acceptedExecutions.Clear();
            coreOpen = false; headExposed = false; poiseResistant = false;
            RegionsChanged?.Invoke();
        }
        public void SetResponseState(bool coreOpen, bool headExposed, bool poiseResistant, int facing = 1)
        {
            this.coreOpen = coreOpen; this.headExposed = headExposed; this.poiseResistant = poiseResistant;
            FacingDirection = facing < 0 ? -1 : 1;
            RefreshConfiguration();
        }

        public void ResetDamageIdentity()
        {
            acceptedExecutions.Clear();
            SetResponseState(false, false, false);
        }

        public GargoyleRegionSettings GetRegionSettings(GargoyleRegionKind region) => region switch
        {
            GargoyleRegionKind.Core => presentation.CoreRegion,
            GargoyleRegionKind.Head => presentation.HeadRegion,
            _ => presentation.BodyRegion
        };

        public bool IsRegionEnabled(GargoyleRegionKind region) => actor != null && actor.IsOperational && liveTuning != null
            && (region == GargoyleRegionKind.Body || (region == GargoyleRegionKind.Core && coreOpen)
                || (region == GargoyleRegionKind.Head && headExposed));

        public void RefreshConfiguration()
        {
            if (liveTuning == null) return;
            liveTuning.Refresh();
            if (tuning.Presentation != null && tuning.Presentation.TryReadPresentation(out var current))
            { presentation = current; reportedInvalidPresentation = false; }
            else if (!reportedInvalidPresentation)
            {
                Debug.LogWarning("Invalid Gargoyle presentation tuning; retaining the last valid configuration.", tuning);
                reportedInvalidPresentation = true;
            }
            RegionsChanged?.Invoke();
        }

        public DamageResult ApplyDamage(GargoyleRegionKind region, in DamageContext context)
        {
            if (!IsRegionEnabled(region)) return Reject(actor == null || !actor.IsInitialized ? DamageRejectionReason.InvalidTarget
                : actor.IsDefeated ? DamageRejectionReason.AlreadyDefeated
                : context.Amount <= 0 && (context.Profile == null || context.Profile.BaseDamage <= 0)
                    ? DamageRejectionReason.NonPositiveDamage : DamageRejectionReason.GameplayBlocked);
            RefreshConfiguration();
            var multiplier = region == GargoyleRegionKind.Head && headExposed
                ? liveTuning.Values.ExposedHeadMultiplier : liveTuning.Values.BodyDamageMultiplier;
            var amount = context.Amount > 0 ? context.Amount : context.Profile != null ? context.Profile.BaseDamage : 0;
            amount *= multiplier;
            if (!float.IsFinite(amount) || amount <= 0) return Reject(DamageRejectionReason.NonPositiveDamage);

            var hasPrimaryIdentity = context.Provenance?.OriginKind == DamageOriginKind.Primary
                && !string.IsNullOrWhiteSpace(context.AttackExecutionId);
            var identity = (context.Source, context.AttackExecutionId);
            if (hasPrimaryIdentity && !acceptedExecutions.Add(identity)) return Reject(DamageRejectionReason.DuplicateExecution);

            var adjusted = new DamageContext(context.Source, context.Target, context.Profile, amount,
                context.HitPoint, context.Direction, context.Tags, context.PoiseDamage,
                context.IsCardEnhancedMelee, context.Provenance, context.AttackExecutionId);
            var result = actor.Health.ApplyDamage(adjusted);
            if (!result.Accepted || result.AppliedAmount <= 0)
            {
                if (hasPrimaryIdentity) acceptedExecutions.Remove(identity);
                return result;
            }
            if (actor.IsOperational && !poiseResistant && context.PoiseDamage > 0)
                poise.ApplyPoiseDamage(context.PoiseDamage);
            if (actor.IsOperational && coreOpen && region == GargoyleRegionKind.Core
                && hasPrimaryIdentity && context.IsCardEnhancedMelee && context.PoiseDamage > 0)
                AcceptedCoreHit?.Invoke(adjusted);

            return new DamageResult(result.Accepted, actor.IsDefeated, result.AppliedAmount,
                actor.Health.CurrentHealth, result.HitStopSeconds);
        }

        private DamageResult Reject(DamageRejectionReason reason) => new DamageResult(false, actor != null && actor.IsDefeated, 0,
            actor != null && actor.Health != null ? actor.Health.CurrentHealth : 0, 0, reason);
        private void OnDefeated(EnemyDamageEvent _) { coreOpen = false; headExposed = false; RegionsChanged?.Invoke(); }
        private void Unsubscribe()
        {
            if (subscribedActor != null) subscribedActor.Defeated -= OnDefeated;
            subscribedActor = null;
        }
    }
}
