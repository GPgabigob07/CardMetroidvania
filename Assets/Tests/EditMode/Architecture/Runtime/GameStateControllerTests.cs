using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class GameStateControllerTests
    {
        private GameObject owner;

        [TearDown]
        public void TearDown()
        {
            if (owner != null)
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void Initialize_SetsBootStateAndMarksModuleInitialized()
        {
            var controller = CreateController();

            controller.Initialize();

            Assert.IsTrue(controller.IsInitialized);
            Assert.AreEqual(GameState.Boot, controller.CurrentState);
        }

        [Test]
        public void RequestState_BeforeInitialization_IsIgnored()
        {
            var controller = CreateController();

            controller.RequestGameplay();

            Assert.IsFalse(controller.IsInitialized);
            Assert.AreEqual(default(GameState), controller.CurrentState);
        }

        [Test]
        public void Shutdown_PreventsFurtherTransitions()
        {
            var controller = CreateController();
            controller.Initialize();
            controller.Shutdown();

            controller.RequestGameplay();

            Assert.IsFalse(controller.IsInitialized);
            Assert.AreEqual(GameState.Boot, controller.CurrentState);
        }

        private GameStateController CreateController()
        {
            owner = new GameObject("Game State Test");
            return owner.AddComponent<GameStateController>();
        }
    }
}
