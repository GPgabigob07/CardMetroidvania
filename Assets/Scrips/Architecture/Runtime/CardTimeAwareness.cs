using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class CardTimeAwareness :
        MonoBehaviour,
        IGameplayServicesConsumer,
        ICardTimeAwareness
    {
        private CardTimeSessionEventChannelSO transitionEvent;
        private bool subscribed;

        public bool IsCardTimeActive { get; private set; }
        public PlayerCardTimeState ActiveCardTime { get; private set; }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void BindGameplayServices(IGameplayServices services)
        {
            Unsubscribe();
            transitionEvent = services?.CardTimeTransitions;
            Apply(snapshot: services?.CardTime.Current ?? default);
            Subscribe();
        }

        private void Subscribe()
        {
            if (!isActiveAndEnabled || subscribed || transitionEvent == null)
            {
                return;
            }

            transitionEvent.Raised += HandleTransition;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || transitionEvent == null)
            {
                return;
            }

            transitionEvent.Raised -= HandleTransition;
            subscribed = false;
        }

        private void HandleTransition(CardTimeSessionTransition transition)
        {
            Apply(snapshot: transition.Current);
        }

        private void Apply(CardTimeSessionSnapshot snapshot)
        {
            IsCardTimeActive = snapshot.IsActive;
            ActiveCardTime = snapshot.IsActive
                ? snapshot.SessionCardTime
                : PlayerCardTimeState.None;
        }
    }
}
