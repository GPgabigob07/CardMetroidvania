using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerEnergyRecoveryTests
    {
        [Test]
        public void PassiveDelaySplitsLargeTicksAndHoldDoesNotCatchUp()
        {
            var type = typeof(PlayerResourceWallet).Assembly.GetType("TicGame.Architecture.PlayerRecoveryController");
            Assert.That(type, Is.Not.Null);
            var go = new GameObject("recovery");
            var energy = ScriptableObject.CreateInstance<ResourceDefinitionSO>();
            var tuning = ScriptableObject.CreateInstance<PlayerRecoveryTuningSO>();
            try
            {
                var wallet = go.AddComponent<PlayerResourceWallet>();
                wallet.ConfigureSingleResource(energy, 0, 250);
                var health = go.AddComponent<SimpleHealth>();
                health.Initialize();
                var inventory = go.AddComponent<PlayerCardInventoryRuntime>();
                var controller = go.AddComponent(type);
                type.GetMethod("Configure").Invoke(controller, new object[] { tuning, energy, wallet, health, inventory });
                var tick = type.GetMethod("Tick");
                tick.Invoke(controller, new object[] { 2f, true });
                Assert.That(wallet.GetCurrent(energy), Is.Zero);
                tick.Invoke(controller, new object[] { 100f, false });
                tick.Invoke(controller, new object[] { 2f, true });
                Assert.That(wallet.GetCurrent(energy), Is.EqualTo(5));
                tick.Invoke(controller, new object[] { 10f, true });
                Assert.That(wallet.GetCurrent(energy), Is.EqualTo(30));
                wallet.Gain(energy, 70);
                tick.Invoke(controller, new object[] { 10f, true });
                Assert.That(wallet.GetCurrent(energy), Is.EqualTo(100));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(energy);
                Object.DestroyImmediate(tuning);
            }
        }
    }
}
