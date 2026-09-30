using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TicGame.Architecture.Tests
{
    public sealed class RecoveryCardCommitTests
    {
        private readonly List<Object> objects = new();
        [TearDown] public void Cleanup() { foreach (var obj in objects) Object.DestroyImmediate(obj); objects.Clear(); }
        private T Asset<T>() where T : ScriptableObject { var value = ScriptableObject.CreateInstance<T>(); objects.Add(value); return value; }

        [TestCase(140, 3f, 0f, 2f, 15f, 2)]
        [TestCase(150, 2f, 95f, 5f, 5f, 2)]
        [TestCase(160, 2f, 0f, 4f, 0f, 1)]
        public void RecoveryUsesAtomicQuote(int operation, float hp, float energyAmount, float expectedHp, float expectedEnergy, int expectedStock)
        {
            var type = typeof(PlayerResourceWallet).Assembly.GetType("TicGame.Architecture.PlayerRecoveryController");
            Assert.That(type, Is.Not.Null);
            var go = new GameObject("player"); objects.Add(go);
            var energy = Asset<ResourceDefinitionSO>();
            var wallet = go.AddComponent<PlayerResourceWallet>(); wallet.ConfigureSingleResource(energy, energyAmount, 250);
            var health = go.AddComponent<SimpleHealth>(); health.Initialize(); health.TrySpendNonlethal(5 - hp);
            var effect = Asset<CardEffectDefinitionSO>();
            effect.Configure(null, null, new[] { new CardOperationDefinition((CardOperationKind)operation) }, null,
                new[] { new CardLifetimeDefinition(CardLifetimeKind.Immediate) }, new CardStackingDefinition(CardStackingKind.RejectIfActive));
            var card = Asset<CardDefinitionSO>(); card.Configure("recovery", "Recovery", "", PlayerCardTimeState.Neutral, null, effect);
            if (operation == 160) card.ConfigureConsumption(CardConsumptionPolicy.ConsumeOnSuccess);
            var traversal = Asset<CardDefinitionSO>(); traversal.Configure("jump", "Jump", "", PlayerCardTimeState.Neutral, new[] { new ResourceAmount(energy, 15) }, null);
            var profile = Asset<PlayerCardInventoryProfileSO>(); profile.TryAddOwnedCard(card, 2); profile.TryEquip(card); profile.TryAddOwnedCard(traversal); profile.TryEquip(traversal);
            var inventory = go.AddComponent<PlayerCardInventoryRuntime>(); inventory.Initialize(profile);
            var recovery = go.AddComponent(type);
            type.GetMethod("Configure").Invoke(recovery, new object[] { Asset<PlayerRecoveryTuningSO>(), energy, wallet, health, inventory });
            var args = new object[] { card, null, default(RecoveryCardQuote) };
            Assert.That(type.GetMethod("TryQuote").Invoke(recovery, args), Is.True);
            var observed = false;
            health.Changed += _ =>
            {
                observed = true;
                Assert.That(wallet.GetCurrent(energy), Is.EqualTo(expectedEnergy));
                Assert.That(inventory.GetCount(card.Id), Is.EqualTo(expectedStock));
                Assert.That(type.GetMethod("TryApply").Invoke(recovery, new[] { (object)card, args[2] }), Is.False);
            };
            Assert.That(type.GetMethod("TryApply").Invoke(recovery, new[] { (object)card, args[2] }), Is.True);
            Assert.That(observed, Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(expectedHp));
            Assert.That(profile.OwnedCards[0].Count, Is.EqualTo(2));
        }
    }
}
