using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerCombatEffectsTests
    {
        private readonly List<Object> objectsToDestroy = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in objectsToDestroy)
            {
                Object.DestroyImmediate(instance);
            }

            objectsToDestroy.Clear();
        }

        [Test]
        public void Overcharge_ResolvesLinkedSupplementalDamage_WithoutHitProcs()
        {
            var energy = CreateResource();
            var source = CreateObject("Player");
            var wallet = source.AddComponent<PlayerResourceWallet>();
            wallet.ConfigureSingleResource(energy, startingAmount: 0f, maximumAmount: 100f);
            var effects = source.AddComponent<PlayerCombatEffects>();
            effects.ConfigureResources(wallet, energy);
            effects.SetRandomRollSource(new FixedRollSource(0f));
            effects.BeginAttack("attack-1");
            effects.ArmSupplementalDamage("attack-1", "overcharge", totalMultiplier: 3f);

            var target = CreateObject("Target");
            var health = target.AddComponent<EnemyHealth>();
            health.Initialize(maximumHealth: 10f);
            var primary = effects.BuildPrimaryDamageInstance(
                "primary",
                "attack-1",
                attack: 2f,
                strikePercent: 1f,
                knockbackForce: 0f,
                maxTargets: 1);

            var report = DamageResolver.Resolve(new DamageRequest(
                primary,
                new[] { target },
                Vector2.zero,
                Vector2.right));

            Assert.AreEqual(2f, report.TotalAppliedAmount);
            Assert.NotNull(effects.LastSupplementalReport);
            Assert.IsTrue(effects.LastSupplementalReport.IsSupplemental);
            Assert.AreEqual("primary", effects.LastSupplementalReport.Instance.Provenance.ParentInstanceId);
            Assert.AreEqual(4f, effects.LastSupplementalReport.TotalAppliedAmount);
            Assert.IsFalse(effects.LastSupplementalReport.Allows(DamageProcPolicy.AdvanceChain));
            Assert.AreEqual(4f, health.CurrentHealth);
            Assert.AreEqual(1f, wallet.GetCurrent(energy));
        }

        [Test]
        public void Chain_IncrementsOnHits_AndResetsOnCompletedMiss()
        {
            var source = CreateObject("Player");
            var effects = source.AddComponent<PlayerCombatEffects>();
            effects.AddChainCapacity(5, damagePercentPerIncrement: 0.1f);
            effects.BeginAttack("hit");
            var target = CreateObject("Target");
            var health = target.AddComponent<EnemyHealth>();
            health.Initialize(maximumHealth: 20f);

            DamageResolver.Resolve(new DamageRequest(
                effects.BuildPrimaryDamageInstance(
                    "hit-instance",
                    "hit",
                    attack: 2f,
                    strikePercent: 1f,
                    knockbackForce: 0f,
                    maxTargets: 1),
                new[] { target },
                Vector2.zero,
                Vector2.right));

            Assert.AreEqual(1, effects.ChainIncrements);
            effects.CompleteAttack("hit");
            effects.BeginAttack("miss");
            effects.CompleteAttack("miss");
            Assert.AreEqual(0, effects.ChainIncrements);
            Assert.AreEqual(5, effects.ChainCapacity);
        }

        [Test]
        public void Overcharge_UsesConfiguredDamageProfileForItsArmedPrimaryAttack()
        {
            var source = CreateObject("Player");
            var effects = source.AddComponent<PlayerCombatEffects>();
            var tag = ScriptableObject.CreateInstance<GameplayTagSO>();
            objectsToDestroy.Add(tag);
            var tags = new GameplayTagSet();
            SetField(tags, "tags", new List<GameplayTagSO> { tag });
            var profile = ScriptableObject.CreateInstance<DamageProfileSO>();
            objectsToDestroy.Add(profile);
            SetField(profile, "damageTags", tags);
            effects.ConfigureSupplementalDamageProfile(profile);
            effects.BeginAttack("overcharge");
            effects.ArmSupplementalDamage("overcharge", "overcharge", totalMultiplier: 2f);

            var instance = effects.BuildPrimaryDamageInstance(
                "overcharge-primary",
                "overcharge",
                attack: 1f,
                strikePercent: 1f,
                knockbackForce: 0f,
                maxTargets: 1);

            Assert.AreSame(profile, instance.Profile);
            Assert.IsTrue(instance.Tags.Contains(tag));
        }

        [Test]
        public void EnergyGainCharges_ReportHudAndWorldFeedback()
        {
            var energy = CreateResource();
            var source = CreateObject("Player");
            var wallet = source.AddComponent<PlayerResourceWallet>();
            wallet.ConfigureSingleResource(energy, startingAmount: 0f, maximumAmount: 100f);
            var feedback = CreateFeedbackService();
            var card = CreateCard("card.energy");
            var effects = source.AddComponent<PlayerCombatEffects>();
            effects.ConfigureResources(wallet, energy);
            effects.SetRandomRollSource(new FixedRollSource(0f));
            effects.BindGameplayServices(new FakeGameplayServices(feedback));

            var worldFeedback = new List<CardWorldFeedbackViewModel>();
            feedback.WorldFeedbackEvent.Raised += worldFeedback.Add;

            effects.AddEnergyGainCharges(1, multiplier: 2f, card: card);

            Assert.AreEqual(1, effects.EnergyGainCharges);
            Assert.AreEqual(1, feedback.GetHudEffects().Count);
            Assert.AreEqual("1", feedback.GetHudEffects()[0].DisplayText);
            Assert.AreEqual(CardFeedbackKind.Activated, worldFeedback[0].Kind);

            effects.BeginAttack("hit");
            var target = CreateObject("Target");
            var health = target.AddComponent<EnemyHealth>();
            health.Initialize(maximumHealth: 10f);

            DamageResolver.Resolve(new DamageRequest(
                effects.BuildPrimaryDamageInstance(
                    "hit-instance",
                    "hit",
                    attack: 1f,
                    strikePercent: 1f,
                    knockbackForce: 0f,
                    maxTargets: 1),
                new[] { target },
                new Vector2(3f, 4f),
                Vector2.right));

            Assert.AreEqual(0, effects.EnergyGainCharges);
            Assert.AreEqual(0, feedback.GetHudEffects().Count);
            Assert.AreEqual(CardFeedbackKind.Triggered, worldFeedback[1].Kind);
            Assert.AreEqual(CardFeedbackAnchor.HitPoint, worldFeedback[1].Anchor);
            Assert.IsTrue(worldFeedback[1].HasExplicitWorldPosition);
            Assert.AreEqual(2f, wallet.GetCurrent(energy));
        }

        [Test]
        public void ExtraJumpReportsGrantConsumeAndClearFeedback()
        {
            var source = CreateObject("Player");
            var feedback = CreateFeedbackService();
            var card = CreateCard("card.jump");
            var extraJump = source.AddComponent<PlayerExtraJumpRuntime>();
            extraJump.BindGameplayServices(new FakeGameplayServices(feedback));

            var worldFeedback = new List<CardWorldFeedbackViewModel>();
            feedback.WorldFeedbackEvent.Raised += worldFeedback.Add;

            Assert.IsTrue(extraJump.TryGrant(card: card));
            Assert.AreEqual(1, feedback.GetHudEffects().Count);
            Assert.AreEqual("1", feedback.GetHudEffects()[0].DisplayText);
            Assert.AreEqual(CardFeedbackKind.Activated, worldFeedback[0].Kind);

            Assert.IsTrue(extraJump.TryConsume());
            Assert.AreEqual(0, feedback.GetHudEffects().Count);
            Assert.AreEqual(CardFeedbackKind.Triggered, worldFeedback[1].Kind);

            Assert.IsTrue(extraJump.TryGrant(card: card));
            extraJump.Clear();

            Assert.AreEqual(0, feedback.GetHudEffects().Count);
            Assert.AreEqual(CardFeedbackKind.Cleared, worldFeedback[3].Kind);
        }

        private GameObject CreateObject(string name)
        {
            var instance = new GameObject(name);
            objectsToDestroy.Add(instance);
            return instance;
        }

        private ResourceDefinitionSO CreateResource()
        {
            var resource = ScriptableObject.CreateInstance<ResourceDefinitionSO>();
            resource.name = "Energy";
            objectsToDestroy.Add(resource);
            return resource;
        }

        private CardDefinitionSO CreateCard(string id)
        {
            var card = ScriptableObject.CreateInstance<CardDefinitionSO>();
            card.Configure(
                stableId: id,
                nameForDisplay: id,
                cardDescription: "",
                cardTimeCategory: PlayerCardTimeState.Neutral,
                costs: null,
                effectDefinition: null);
            objectsToDestroy.Add(card);
            return card;
        }

        private CardFeedbackService CreateFeedbackService()
        {
            var owner = CreateObject("Feedback");
            var channel = ScriptableObject.CreateInstance<CardFeedbackEventChannelSO>();
            objectsToDestroy.Add(channel);
            var service = owner.AddComponent<CardFeedbackService>();
            service.Configure(channel);
            service.Initialize();
            return service;
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private sealed class FixedRollSource : IRandomRollSource
        {
            private readonly float value;

            public FixedRollSource(float value)
            {
                this.value = value;
            }

            public float NextNormalized()
            {
                return value;
            }
        }

        private sealed class FakeGameplayServices : IGameplayServices
        {
            public FakeGameplayServices(ICardFeedbackService cardFeedback)
            {
                CardFeedback = cardFeedback;
            }

            public IGameplayTimeService Time => null;
            public IHitStopService HitStop => null;
            public HitStopRequestEventChannelSO HitStopRequests => null;
            public ICardTimeSession CardTime => null;
            public CardTimeSessionEventChannelSO CardTimeTransitions => null;
            public ICardFeedbackService CardFeedback { get; }
            public IGameStateService GameState => null;
        }
    }
}
