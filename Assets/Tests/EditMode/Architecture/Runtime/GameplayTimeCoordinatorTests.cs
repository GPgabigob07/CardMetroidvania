using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class GameplayTimeCoordinatorTests
    {
        private GameObject owner;
        private float originalTimeScale;
        private float originalFixedDeltaTime;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            originalFixedDeltaTime = Time.fixedDeltaTime;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        [TearDown]
        public void TearDown()
        {
            if (owner != null)
            {
                Object.DestroyImmediate(owner);
            }

            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }

        [Test]
        public void RemovingHitStop_WhileCardTimeRemains_RestoresCardTimeScale()
        {
            owner = new GameObject("Gameplay Time Test");
            var coordinator = owner.AddComponent<GameplayTimeCoordinator>();
            var cardTimeOwner = new object();
            var hitStopOwner = new object();
            coordinator.Initialize();
            coordinator.SetModifier(
                cardTimeOwner,
                new GameplayTimeModifier(GameplayTimeModifierKind.CardTime, 0.1f));
            coordinator.SetModifier(
                hitStopOwner,
                new GameplayTimeModifier(GameplayTimeModifierKind.HitStop, 0f));

            coordinator.RemoveModifier(hitStopOwner);

            Assert.AreEqual(0.1f, Time.timeScale);
            Assert.AreEqual(0.002f, Time.fixedDeltaTime, 0.0001f);
        }

        [Test]
        public void RemovingCardTime_DuringHitStop_KeepsTimeStopped()
        {
            owner = new GameObject("Gameplay Time Test");
            var coordinator = owner.AddComponent<GameplayTimeCoordinator>();
            var cardTimeOwner = new object();
            var hitStopOwner = new object();
            coordinator.Initialize();
            coordinator.SetModifier(
                cardTimeOwner,
                new GameplayTimeModifier(GameplayTimeModifierKind.CardTime, 0.1f));
            coordinator.SetModifier(
                hitStopOwner,
                new GameplayTimeModifier(GameplayTimeModifierKind.HitStop, 0f));

            coordinator.RemoveModifier(cardTimeOwner);

            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual(0.02f, Time.fixedDeltaTime);
        }

        [Test]
        public void Shutdown_RestoresCapturedBaseline()
        {
            owner = new GameObject("Gameplay Time Test");
            var coordinator = owner.AddComponent<GameplayTimeCoordinator>();
            coordinator.Initialize();
            coordinator.SetModifier(
                new object(),
                new GameplayTimeModifier(GameplayTimeModifierKind.HitStop, 0f));

            coordinator.Shutdown();

            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(0.02f, Time.fixedDeltaTime);
        }
    }
}
