using System;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerRecoveryEconomyTests
    {
        [TestCase(15f, 250f, 22.5f)]
        [TestCase(20f, 250f, 30f)]
        [TestCase(15f, 10f, 10f)]
        public void PassiveCeilingUsesEquippedBasisAndWalletMaximum(float basis, float maximum, float expected)
        {
            var type = typeof(PlayerResourceWallet).Assembly.GetType("TicGame.Architecture.PlayerRecoveryEconomy");
            Assert.NotNull(type, "Recovery arithmetic must derive the traversal reserve.");
            var actual = type.GetMethod("GetPassiveCeiling").Invoke(null, new object[] { basis, maximum, 1.5f });
            Assert.AreEqual(expected, actual);
        }

        [TestCase(29f, 2f, 0f, 0f)]
        [TestCase(30f, 4f, 30f, 1f)]
        [TestCase(95f, 2f, 90f, 3f)]
        [TestCase(95f, 4f, 30f, 1f)]
        [TestCase(95f, 5f, 0f, 0f)]
        [TestCase(95f, 0f, 0f, 0f)]
        [TestCase(95f, 4.5f, 30f, .5f)]
        [TestCase(float.NaN, 2f, 0f, 0f)]
        public void HealingPreservesRemainderAndDoesNotChargeForOverheal(float energy, float health, float spent, float healed)
        {
            var type = typeof(PlayerResourceWallet).Assembly.GetType("TicGame.Architecture.PlayerRecoveryEconomy");
            Assert.NotNull(type, "Healing needs whole-chunk arithmetic.");
            var quote = type.GetMethod("QuoteEnergyHealing").Invoke(null, new object[] {energy, health, 5f, 15f, 2f});
            Assert.AreEqual(spent, quote.GetType().GetProperty("EnergySpent").GetValue(quote));
            Assert.AreEqual(healed, quote.GetType().GetProperty("HealthRestored").GetValue(quote));
        }

        [Test]
        public void BasisUsesEquippedNeutralSummedFixedCostsOnly()
        {
            var energy = ScriptableObject.CreateInstance<ResourceDefinitionSO>();
            var profile = ScriptableObject.CreateInstance<PlayerCardInventoryProfileSO>();
            var cards = new CardDefinitionSO[3];
            try
            {
                for (var i = 0; i < 3; i++)
                {
                    cards[i] = ScriptableObject.CreateInstance<CardDefinitionSO>();
                    cards[i].Configure("card" + i, "", "", PlayerCardTimeState.Neutral,
                        i == 1 ? new[] { new ResourceAmount(energy, 5), new ResourceAmount(energy, 10) }
                            : new[] { new ResourceAmount(energy, i == 0 ? 5 : 100) }, null);
                    profile.TryAddOwnedCard(cards[i]);
                    if (i < 2) profile.TryEquip(cards[i]);
                }
                Assert.That(PlayerRecoveryEconomy.ResolveNeutralCost(profile.GetEquippedCards(PlayerCardTimeState.Neutral), energy), Is.EqualTo(15));
                Assert.That(PlayerRecoveryEconomy.ResolveNeutralCost(null, energy), Is.EqualTo(20));
                Assert.That(PlayerRecoveryEconomy.ResolveNeutralCost(null, energy, float.NaN), Is.EqualTo(20));
            }
            finally
            {
                foreach (var card in cards) UnityEngine.Object.DestroyImmediate(card);
                UnityEngine.Object.DestroyImmediate(profile); UnityEngine.Object.DestroyImmediate(energy);
            }
        }
    }
}
