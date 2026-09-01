using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class EnemyPoiseTests
    {
        private GameObject owner;
        private EnemyPoise poise;

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("Enemy Poise Test");
            poise = owner.AddComponent<EnemyPoise>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(owner);
        }

        [Test]
        public void Tick_RegeneratesPoiseWithoutExceedingMaximum()
        {
            poise.Initialize(30f, 0.33f);
            poise.ApplyPoiseDamage(12f);
            poise.Tick(10f);

            Assert.AreEqual(21.3f, poise.CurrentPoise, 0.001f);
            poise.Tick(100f);
            Assert.AreEqual(30f, poise.CurrentPoise, 0.001f);
        }

        [Test]
        public void ApplyPoiseDamage_ReachingZero_RaisesDepletedOnce()
        {
            var count = 0;
            poise.Depleted += () => count++;
            poise.Initialize(30f, 0.33f);

            poise.ApplyPoiseDamage(30f);
            poise.ApplyPoiseDamage(1f);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Restore_FromDepleted_ClearsLatchAndAllowsSecondDepletionEvent()
        {
            var count = 0;
            poise.Depleted += () => count++;
            poise.Initialize(10f, 0f);

            poise.ApplyPoiseDamage(10f);
            poise.Restore(2f);
            poise.ApplyPoiseDamage(2f);

            Assert.AreEqual(2, count);
        }

        [Test]
        public void ApplyPoiseDamage_ClampsToAvailablePoiseAndRejectsNegativeAmount()
        {
            poise.Initialize(10f, 0f);

            Assert.AreEqual(0f, poise.ApplyPoiseDamage(-4f));
            Assert.AreEqual(10f, poise.ApplyPoiseDamage(50f));
            Assert.AreEqual(0f, poise.CurrentPoise);
        }
    }
}
