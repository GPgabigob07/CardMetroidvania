using System.Collections.Generic;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerResourceWalletTests
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
        public void TrySpend_IsAtomic_AndGainClampsToMaximum()
        {
            var energy = CreateResource();
            var owner = new GameObject("Wallet");
            objectsToDestroy.Add(owner);
            var wallet = owner.AddComponent<PlayerResourceWallet>();
            wallet.ConfigureSingleResource(energy, startingAmount: 10f, maximumAmount: 20f);

            Assert.IsFalse(wallet.TrySpend(new[]
            {
                new ResourceAmount(energy, 11f)
            }));
            Assert.AreEqual(10f, wallet.GetCurrent(energy));

            Assert.IsTrue(wallet.TrySpend(new[]
            {
                new ResourceAmount(energy, 5f)
            }));
            Assert.AreEqual(5f, wallet.GetCurrent(energy));

            wallet.Gain(energy, 100f);
            Assert.AreEqual(20f, wallet.GetCurrent(energy));
        }

        private ResourceDefinitionSO CreateResource()
        {
            var resource = ScriptableObject.CreateInstance<ResourceDefinitionSO>();
            resource.name = "Energy";
            objectsToDestroy.Add(resource);
            return resource;
        }
    }
}
