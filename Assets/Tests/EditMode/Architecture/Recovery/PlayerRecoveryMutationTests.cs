using System;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerRecoveryMutationTests
    {
        [Test]
        public void HealingIsBoundedAndNonlethalPaymentCannotKill()
        {
            var go = new GameObject("health");
            try
            {
                var health = go.AddComponent<SimpleHealth>();
                health.Initialize();
                var spend = typeof(SimpleHealth).GetMethod("TrySpendNonlethal");
                var heal = typeof(SimpleHealth).GetMethod("Heal");
                Assert.That(spend, Is.Not.Null, "Health requires a non-damage payment API");
                Assert.That(heal, Is.Not.Null);
                Assert.That(spend.Invoke(health, new object[] { 4f, 1f }), Is.True);
                Assert.That(health.CurrentHealth, Is.EqualTo(1f));
                Assert.That(spend.Invoke(health, new object[] { 1f, 1f }), Is.False);
                Assert.That(heal.Invoke(health, new object[] { 9f }), Is.EqualTo(4f));
                Assert.That(heal.Invoke(health, new object[] { float.NaN }), Is.EqualTo(0f));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void RuntimeInventoryInitializationIsIdempotent()
        {
            var type = typeof(PlayerCardRuntime).Assembly.GetType("TicGame.Architecture.PlayerCardInventoryRuntime");
            Assert.That(type, Is.Not.Null, "Run stock must not mutate the profile asset");
            var profile = ScriptableObject.CreateInstance<PlayerCardInventoryProfileSO>();
            var card = ScriptableObject.CreateInstance<CardDefinitionSO>();
            var go = new GameObject("inventory");
            try
            {
                card.Configure("mend", "Mend", "", PlayerCardTimeState.Neutral, null, null);
                profile.TryAddOwnedCard(card, 2);
                profile.TryEquip(card);
                var runtime = go.AddComponent(type);
                type.GetMethod("Initialize").Invoke(runtime, new object[] { profile });
                profile.TryUnequip(card);
                type.GetMethod("Initialize").Invoke(runtime, new object[] { profile });
                Assert.That(type.GetMethod("GetCount").Invoke(runtime, new object[] { "mend" }), Is.EqualTo(2));
                var ids = (System.Collections.Generic.IReadOnlyList<string>)type.GetMethod("GetEquippedCardIds").Invoke(runtime, new object[] { PlayerCardTimeState.Neutral });
                Assert.That(ids, Is.EqualTo(new[] { "mend" }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(card);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }
    }
}
