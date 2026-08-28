using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class GameplayServicesRootTests
    {
        private GameObject rootObject;
        private GameObject consumerObject;
        private HitStopRequestEventChannelSO hitStopRequestEvent;
        private CardTimeSessionEventChannelSO cardTimeTransitionEvent;
        private PlayerCardTimeConfigSO cardTimeConfig;

        [TearDown]
        public void TearDown()
        {
            if (consumerObject != null)
            {
                Object.DestroyImmediate(consumerObject);
            }

            if (rootObject != null)
            {
                Object.DestroyImmediate(rootObject);
            }

            if (hitStopRequestEvent != null)
            {
                Object.DestroyImmediate(hitStopRequestEvent);
            }

            if (cardTimeTransitionEvent != null)
            {
                Object.DestroyImmediate(cardTimeTransitionEvent);
            }

            if (cardTimeConfig != null)
            {
                Object.DestroyImmediate(cardTimeConfig);
            }
        }

        [Test]
        public void Initialize_ConfiguredRoot_InitializesModulesAndBindsSceneConsumers()
        {
            consumerObject = new GameObject("Gameplay Services Consumer");
            var consumer = consumerObject.AddComponent<GameplayServicesConsumerSpy>();

            rootObject = new GameObject("Gameplay Services");
            var root = rootObject.AddComponent<GameplayServicesRoot>();
            ConfigureRoot(root);

            var initialized = root.Initialize();

            Assert.IsTrue(initialized);
            Assert.IsTrue(root.IsInitialized);
            Assert.IsTrue(((IGameplayModule)root.GameState).IsInitialized);
            Assert.AreSame(root, consumer.Services);
            Assert.AreEqual(1, consumer.BindCount);
        }

        [Test]
        public void BindScene_Repeated_DoesNotBindConsumerTwice()
        {
            consumerObject = new GameObject("Gameplay Services Consumer");
            var consumer = consumerObject.AddComponent<GameplayServicesConsumerSpy>();

            rootObject = new GameObject("Gameplay Services");
            var root = rootObject.AddComponent<GameplayServicesRoot>();
            ConfigureRoot(root);
            root.Initialize();

            root.BindScene(consumerObject.scene);

            Assert.AreEqual(1, consumer.BindCount);
        }

        [Test]
        public void Shutdown_ShutsDownModules()
        {
            rootObject = new GameObject("Gameplay Services");
            var root = rootObject.AddComponent<GameplayServicesRoot>();
            var gameState = ConfigureRoot(root);
            root.Initialize();

            root.Shutdown();

            Assert.IsFalse(root.IsInitialized);
            Assert.IsFalse(gameState.IsInitialized);
        }

        private GameStateController ConfigureRoot(GameplayServicesRoot root)
        {
            hitStopRequestEvent = ScriptableObject.CreateInstance<HitStopRequestEventChannelSO>();
            cardTimeTransitionEvent =
                ScriptableObject.CreateInstance<CardTimeSessionEventChannelSO>();
            cardTimeConfig = ScriptableObject.CreateInstance<PlayerCardTimeConfigSO>();
            var gameplayTime = rootObject.AddComponent<GameplayTimeCoordinator>();
            var cardTime = rootObject.AddComponent<CardTimeSessionController>();
            var hitStop = rootObject.AddComponent<HitStopService>();
            var gameState = rootObject.AddComponent<GameStateController>();
            cardTime.Configure(gameplayTime, cardTimeConfig, cardTimeTransitionEvent);
            hitStop.Configure(gameplayTime, hitStopRequestEvent);
            root.ConfigureModules(gameplayTime, cardTime, hitStop, gameState);
            return gameState;
        }
    }
}
