using System.Collections.Generic;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerCardCommitSnapshotTests
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
        public void Capture_FreezesVolatileEnergyAndHealth()
        {
            var energy = CreateAsset<ResourceDefinitionSO>("Energy");
            var player = new GameObject("Player");
            objectsToDestroy.Add(player);
            var wallet = player.AddComponent<PlayerResourceWallet>();
            wallet.ConfigureSingleResource(energy, startingAmount: 20f, maximumAmount: 30f);
            var health = player.AddComponent<SimpleHealth>();
            health.Initialize();
            var source = player.AddComponent<PlayerCardCommitSnapshotSource>();
            source.Configure(wallet, health, new[] { energy });

            var snapshot = source.Capture(
                PlayerCardTimeState.Chain,
                attackExecutionId: "attack",
                isAirborne: true);
            wallet.TrySpend(new[] { new ResourceAmount(energy, 10f) });
            health.ApplyDamage(new DamageContext(
                source: player,
                target: player,
                profile: null,
                amount: 2f,
                hitPoint: Vector2.zero,
                direction: Vector2.zero));

            Assert.AreEqual(20f, snapshot.GetCurrent(energy));
            Assert.AreEqual(30f, snapshot.GetMaximum(energy));
            Assert.AreEqual(5f, snapshot.CurrentHealth);
            Assert.AreEqual(5f, snapshot.MaximumHealth);
            Assert.IsTrue(snapshot.IsAirborne);
            Assert.AreEqual("attack", snapshot.AttackExecutionId);
        }

        [Test]
        public void CostDeltas_AdjustSnapshotAffordabilityWithoutChangingWallet()
        {
            var energy = CreateAsset<ResourceDefinitionSO>("Energy");
            var walletOwner = new GameObject("Wallet");
            objectsToDestroy.Add(walletOwner);
            var wallet = walletOwner.AddComponent<PlayerResourceWallet>();
            wallet.ConfigureSingleResource(energy, startingAmount: 10f, maximumAmount: 10f);
            var snapshot = new PlayerCardCommitSnapshot(
                PlayerCardTimeState.Neutral,
                attackExecutionId: null,
                isAirborne: false,
                currentHealth: 5f,
                maximumHealth: 5f,
                resourceSnapshots: new[]
                {
                    new PlayerCardResourceSnapshot(energy, wallet.GetCurrent(energy), wallet.GetMaximum(energy))
                });

            var adjusted = snapshot.WithResourceCostDelta(new ResourceAmount(energy, 3f));

            Assert.IsTrue(snapshot.CanSpend(new[] { new ResourceAmount(energy, 8f) }));
            Assert.IsFalse(adjusted.CanSpend(new[] { new ResourceAmount(energy, 8f) }));
            Assert.AreEqual(10f, wallet.GetCurrent(energy));
            Assert.AreEqual(11f, adjusted.BuildAdjustedCosts(
                new[] { new ResourceAmount(energy, 8f) })[0].Amount);
        }

        private T CreateAsset<T>(string name) where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            instance.name = name;
            objectsToDestroy.Add(instance);
            return instance;
        }
    }
}
