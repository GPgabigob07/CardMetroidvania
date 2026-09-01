using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class BatMachineDamagePolicyTests
    {
        private GameObject root;
        private EnemyHealth health;
        private EnemyPoise poise;
        private BatMachineDamagePolicy policy;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Bat Damage Policy");
            health = root.AddComponent<EnemyHealth>();
            health.Initialize(12f);
            poise = root.AddComponent<EnemyPoise>();
            poise.Initialize(30f, 0f);
            policy = root.AddComponent<BatMachineDamagePolicy>();
            policy.SetDependencies(health, poise, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ApplyDamage_AcceptedHealthHit_ForwardsAuthoredPoiseDamage()
        {
            var result = policy.ApplyDamage(CreateContext(amount: 2f, poiseDamage: 10f));

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(10f, health.CurrentHealth);
            Assert.AreEqual(20f, poise.CurrentPoise);
        }

        [Test]
        public void ApplyDamage_NormalUnmodifiedAttack_DoesNotInventPoiseDamage()
        {
            policy.ApplyDamage(CreateContext(amount: 2f));

            Assert.AreEqual(30f, poise.CurrentPoise);
        }

        [Test]
        public void ApplyDamage_AfterDeath_RejectsWithoutFurtherPoiseDamage()
        {
            policy.ApplyDamage(CreateContext(amount: 12f, poiseDamage: 4f));
            var poiseAfterLethalHit = poise.CurrentPoise;

            var result = policy.ApplyDamage(CreateContext(amount: 1f, poiseDamage: 10f));

            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(poiseAfterLethalHit, poise.CurrentPoise);
        }

        private DamageContext CreateContext(float amount, float poiseDamage = 0f)
        {
            return new DamageContext(
                source: null,
                target: root,
                profile: null,
                amount: amount,
                hitPoint: Vector2.zero,
                direction: Vector2.right,
                poiseDamage: poiseDamage);
        }
    }
}
