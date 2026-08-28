using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class CardTimeAwarenessTests
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
        public void TransitionEvent_UpdatesObservationWithoutGrantingAuthority()
        {
            owner = new GameObject("Card Time Awareness Test");
            var awareness = owner.AddComponent<CardTimeAwareness>();
            var channel = ScriptableObject.CreateInstance<CardTimeSessionEventChannelSO>();
            var services = new FakeGameplayServices(channel);
            awareness.BindGameplayServices(services);

            channel.Raise(
                new CardTimeSessionTransition(
                    previous: default,
                    current: new CardTimeSessionSnapshot(
                        state: CardTimeSessionState.Active,
                        availableCardTime: PlayerCardTimeState.Chain,
                        sessionCardTime: PlayerCardTimeState.Chain,
                        activeElapsed: 0f,
                        maximumActiveDuration: 5f)));

            Assert.IsTrue(awareness.IsCardTimeActive);
            Assert.AreEqual(PlayerCardTimeState.Chain, awareness.ActiveCardTime);
            Object.DestroyImmediate(channel);
        }

        private sealed class FakeGameplayServices : IGameplayServices
        {
            public FakeGameplayServices(CardTimeSessionEventChannelSO transitions)
            {
                CardTimeTransitions = transitions;
                CardTime = new FakeSession();
            }

            public IGameplayTimeService Time => null;
            public IHitStopService HitStop => null;
            public HitStopRequestEventChannelSO HitStopRequests => null;
            public ICardTimeSession CardTime { get; }
            public CardTimeSessionEventChannelSO CardTimeTransitions { get; }
            public ICardFeedbackService CardFeedback => null;
            public IGameStateService GameState => null;
        }

        private sealed class FakeSession : ICardTimeSession
        {
            public CardTimeSessionSnapshot Current => default;
        }
    }
}
