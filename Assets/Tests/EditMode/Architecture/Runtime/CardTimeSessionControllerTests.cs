using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class CardTimeSessionControllerTests
    {
        private GameObject owner;
        private PlayerCardTimeConfigSO config;
        private CardTimeSessionEventChannelSO transitionEvent;
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

            if (config != null)
            {
                Object.DestroyImmediate(config);
            }

            if (transitionEvent != null)
            {
                Object.DestroyImmediate(transitionEvent);
            }

            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }

        [Test]
        public void RegisteredPlayerSource_CanActivateAndCommit()
        {
            var controller = CreateController();
            var source = controller.RegisterPlayerSource(owner);
            source.PublishAvailability(PlayerCardTimeState.Chain);

            var activation = source.RequestActivation();

            Assert.AreEqual(CardTimeActivationRequestResult.Activated, activation);
            Assert.IsTrue(controller.Current.IsActive);
            Assert.AreEqual(0.1f, Time.timeScale, 0.0001f);

            var commitTransitionCount = 0;
            var commitTransition = default(CardTimeSessionTransition);
            transitionEvent.Raised += transition =>
            {
                if (transition.Outcome != CardTimeSessionOutcome.Committed)
                {
                    return;
                }

                commitTransitionCount++;
                commitTransition = transition;
            };

            Assert.IsTrue(source.TryCommit());
            Assert.AreEqual(CardTimeSessionState.Unavailable, controller.Current.State);
            Assert.AreEqual(1, commitTransitionCount);
            Assert.AreEqual(
                CardTimeSessionState.Unavailable,
                commitTransition.Current.State);
            Assert.AreEqual(
                PlayerCardTimeState.None,
                commitTransition.Current.SessionCardTime);
            Assert.AreEqual(0f, commitTransition.Current.ActiveRemaining);
            Assert.AreEqual(
                PlayerCardTimeState.None,
                controller.Current.AvailableCardTime);
            Assert.AreEqual(
                PlayerCardTimeState.None,
                controller.Current.SessionCardTime);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void RegisteredPlayerSource_FailedTransactionKeepsSessionActive()
        {
            var controller = CreateController();
            var source = controller.RegisterPlayerSource(owner);
            source.PublishAvailability(PlayerCardTimeState.Chain);
            source.RequestActivation();
            var transaction = new StubCommitTransaction(shouldApply: false);
            var committedCount = 0;
            transitionEvent.Raised += transition =>
            {
                if (transition.Outcome == CardTimeSessionOutcome.Committed)
                {
                    committedCount++;
                }
            };

            Assert.IsFalse(source.TryCommit(transaction));

            Assert.IsTrue(controller.Current.IsActive);
            Assert.AreEqual(0, committedCount);
            Assert.AreEqual(1, transaction.ApplyCount);
            Assert.AreEqual(0.1f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void CommittedFinisher_DoesNotReopenFromRepeatedSourcePublication()
        {
            var controller = CreateController();
            var source = controller.RegisterPlayerSource(owner);
            source.PublishAvailability(PlayerCardTimeState.Finisher);
            source.RequestActivation();
            Assert.IsTrue(source.TryCommit());

            source.PublishAvailability(PlayerCardTimeState.None);
            source.PublishAvailability(PlayerCardTimeState.Finisher);

            Assert.AreEqual(CardTimeSessionState.Unavailable, controller.Current.State);
            Assert.AreEqual(
                PlayerCardTimeState.None,
                controller.Current.AvailableCardTime);
            Assert.AreEqual(
                CardTimeActivationRequestResult.Rejected,
                source.RequestActivation());
        }

        [Test]
        public void CancelledFinisher_DoesNotReopenFromRepeatedSourcePublication()
        {
            var controller = CreateController();
            var source = controller.RegisterPlayerSource(owner);
            source.PublishAvailability(PlayerCardTimeState.Finisher);
            source.RequestActivation();

            Assert.IsTrue(source.Cancel());
            source.PublishAvailability(PlayerCardTimeState.None);
            source.PublishAvailability(PlayerCardTimeState.Finisher);

            Assert.AreEqual(CardTimeSessionState.Unavailable, controller.Current.State);
            Assert.AreEqual(
                CardTimeActivationRequestResult.Rejected,
                source.RequestActivation());
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void SecondOwner_CannotReceiveMutationAuthority()
        {
            var controller = CreateController();
            var firstOwner = new GameObject("First Player");
            var secondOwner = new GameObject("Second Player");
            try
            {
                Assert.IsNotNull(controller.RegisterPlayerSource(firstOwner));
                Assert.IsNull(controller.RegisterPlayerSource(secondOwner));
            }
            finally
            {
                Object.DestroyImmediate(firstOwner);
                Object.DestroyImmediate(secondOwner);
            }
        }

        [Test]
        public void Unregister_EndsOwnedSessionAndInvalidatesSource()
        {
            var controller = CreateController();
            var source = controller.RegisterPlayerSource(owner);
            source.PublishAvailability(PlayerCardTimeState.Finisher);
            source.RequestActivation();

            source.Unregister();

            Assert.IsFalse(controller.Current.IsActive);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(
                CardTimeActivationRequestResult.Rejected,
                source.RequestActivation());
        }

        private CardTimeSessionController CreateController()
        {
            owner = new GameObject("Card Time Session Test");
            config = ScriptableObject.CreateInstance<PlayerCardTimeConfigSO>();
            transitionEvent = ScriptableObject.CreateInstance<CardTimeSessionEventChannelSO>();
            var time = owner.AddComponent<GameplayTimeCoordinator>();
            var controller = owner.AddComponent<CardTimeSessionController>();
            time.Initialize();
            controller.Configure(time, config, transitionEvent);
            controller.Initialize();
            return controller;
        }

        private sealed class StubCommitTransaction : ICardCommitTransaction
        {
            private readonly bool shouldApply;

            public StubCommitTransaction(bool shouldApply)
            {
                this.shouldApply = shouldApply;
            }

            public CardDefinitionSO Card => null;
            public PlayerCardTimeState Category => PlayerCardTimeState.Chain;
            public bool IsApplied { get; private set; }
            public int ApplyCount { get; private set; }

            public bool TryApply()
            {
                ApplyCount++;
                IsApplied = shouldApply;
                return shouldApply;
            }
        }
    }
}
