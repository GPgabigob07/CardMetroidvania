using System.Collections.Generic;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class CardDefinitionTests
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
        public void Definitions_CanReuseOneStatusWithDifferentAuthoredValues()
        {
            var energy = Create<ResourceDefinitionSO>("Energy");
            var status = Create<CardStatusDefinitionSO>("Knockback");
            status.Configure("status.knockback", "Knockback");
            var firstEffect = CreateEffect(
                status,
                new CardOperationDefinition(
                    CardOperationKind.AddStatusCharges,
                    status: status,
                    amount: 2f));
            var secondEffect = CreateEffect(
                status,
                new CardOperationDefinition(
                    CardOperationKind.AddStatusCharges,
                    status: status,
                    amount: 5f));
            var firstCard = CreateCard("card.knockback.small", energy, firstEffect);
            var secondCard = CreateCard("card.knockback.large", energy, secondEffect);

            Assert.IsEmpty(firstCard.GetValidationErrors());
            Assert.IsEmpty(secondCard.GetValidationErrors());
            Assert.AreSame(firstCard.Effect.Status, secondCard.Effect.Status);
            Assert.AreEqual(2f, firstCard.Effect.CommitOperations[0].Amount);
            Assert.AreEqual(5f, secondCard.Effect.CommitOperations[0].Amount);
        }

        [Test]
        public void Definition_RejectsMissingEffectAndNoneCategory()
        {
            var card = Create<CardDefinitionSO>("InvalidCard");
            card.Configure(
                "card.invalid",
                "Invalid",
                string.Empty,
                PlayerCardTimeState.None,
                costs: null,
                effectDefinition: null);

            var errors = card.GetValidationErrors();

            Assert.That(errors, Has.Some.Contains("category"));
            Assert.That(errors, Has.Some.Contains("effect"));
        }

        [Test]
        public void Effect_RejectsAbilityOperationWithoutAbility()
        {
            var effect = Create<CardEffectDefinitionSO>("InvalidAbilityEffect");
            effect.Configure(
                statusDefinition: null,
                conditions: null,
                operations: new[]
                {
                    new CardOperationDefinition(CardOperationKind.InvokeAbility)
                },
                rules: null,
                lifetimeDefinitions: new[]
                {
                    new CardLifetimeDefinition(CardLifetimeKind.Immediate)
                },
                stackingDefinition: new CardStackingDefinition(
                    CardStackingKind.RejectIfActive));

            Assert.That(
                effect.GetValidationErrors(),
                Has.Some.Contains("Invalid commit operation"));
        }

        [Test]
        public void Effect_AllowsMultipleOperationsAndReactiveRules()
        {
            var energy = Create<ResourceDefinitionSO>("Energy");
            var status = Create<CardStatusDefinitionSO>("EnergyBoost");
            var rule = new CardReactiveRule(
                CardTriggerKind.OnEffectivePrimaryAttackResolved,
                conditions: new[]
                {
                    new CardConditionDefinition(CardConditionKind.HasRemainingCharges)
                },
                operations: new[]
                {
                    new CardOperationDefinition(
                        CardOperationKind.ModifyResourceGain,
                        status: status,
                        resource: energy,
                        multiplier: 2f)
                },
                consumesCharge: true);
            var effect = Create<CardEffectDefinitionSO>("EnergyBoostEffect");
            effect.Configure(
                status,
                conditions: null,
                operations: new[]
                {
                    new CardOperationDefinition(
                        CardOperationKind.AddStatusCharges,
                        status: status,
                        amount: 5f),
                    new CardOperationDefinition(
                        CardOperationKind.GainResource,
                        resource: energy,
                        amount: 1f)
                },
                rules: new[] { rule },
                lifetimeDefinitions: new[]
                {
                    new CardLifetimeDefinition(CardLifetimeKind.UntilChargesExhausted)
                },
                stackingDefinition: new CardStackingDefinition(
                    CardStackingKind.AddCharges,
                    maximumCharges: 10));

            Assert.IsEmpty(effect.GetValidationErrors());
            Assert.AreEqual(2, effect.CommitOperations.Count);
            Assert.AreEqual(1, effect.ReactiveRules.Count);
            Assert.IsTrue(effect.ReactiveRules[0].ConsumesCharge);
        }

        [Test]
        public void EscalatingDamage_CanRepresentCapacityStacksAndMissReset()
        {
            var status = Create<CardStatusDefinitionSO>("EscalatingDamage");
            var effect = Create<CardEffectDefinitionSO>("EscalatingDamageEffect");
            effect.Configure(
                status,
                conditions: null,
                operations: new[]
                {
                    new CardOperationDefinition(
                        CardOperationKind.AddStatusCapacity,
                        status: status,
                        amount: 5f),
                    new CardOperationDefinition(
                        CardOperationKind.ModifyDamage,
                        status: status,
                        multiplier: 0.1f)
                },
                rules: new[]
                {
                    new CardReactiveRule(
                        CardTriggerKind.OnEffectivePrimaryAttackResolved,
                        conditions: null,
                        operations: new[]
                        {
                            new CardOperationDefinition(
                                CardOperationKind.AddStatusStacks,
                                status: status,
                                amount: 1f)
                        }),
                    new CardReactiveRule(
                        CardTriggerKind.OnPrimaryAttackCompleted,
                        conditions: new[]
                        {
                            new CardConditionDefinition(CardConditionKind.WasMiss)
                        },
                        operations: new[]
                        {
                            new CardOperationDefinition(
                                CardOperationKind.ClearStatusStacks,
                                status: status)
                        })
                },
                lifetimeDefinitions: new[]
                {
                    new CardLifetimeDefinition(CardLifetimeKind.UntilPlayerDeath)
                },
                stackingDefinition: new CardStackingDefinition(
                    CardStackingKind.AddCapacity));

            Assert.IsEmpty(effect.GetValidationErrors());
            Assert.AreEqual(
                CardOperationKind.AddStatusStacks,
                effect.ReactiveRules[0].Operations[0].Kind);
        }

        private CardEffectDefinitionSO CreateEffect(
            CardStatusDefinitionSO status,
            CardOperationDefinition operation)
        {
            var effect = Create<CardEffectDefinitionSO>($"{status.name}Effect");
            effect.Configure(
                status,
                conditions: null,
                operations: new[] { operation },
                rules: null,
                lifetimeDefinitions: new[]
                {
                    new CardLifetimeDefinition(CardLifetimeKind.UntilChargesExhausted)
                },
                stackingDefinition: new CardStackingDefinition(
                    CardStackingKind.AddCharges,
                    maximumCharges: 10));
            return effect;
        }

        private CardDefinitionSO CreateCard(
            string id,
            ResourceDefinitionSO energy,
            CardEffectDefinitionSO effect)
        {
            var card = Create<CardDefinitionSO>(id);
            card.Configure(
                id,
                id,
                string.Empty,
                PlayerCardTimeState.Neutral,
                new[] { new ResourceAmount(energy, 5f) },
                effect);
            return card;
        }

        private T Create<T>(string objectName) where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            instance.name = objectName;
            objectsToDestroy.Add(instance);
            return instance;
        }
    }
}
