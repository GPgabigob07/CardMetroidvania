using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class CardTimeSessionController :
        MonoBehaviour,
        IGameplayModule,
        ICardTimeSessionService
    {
        [Header("Configuration")]
        [Tooltip("Authoritative Card Time duration, slowdown, and input-leniency tuning.")]
        [SerializeField] private PlayerCardTimeConfigSO configuration;

        [Header("Events")]
        [Tooltip("Broadcasts authoritative Card Time session transitions.")]
        [SerializeField] private CardTimeSessionEventChannelSO transitionEvent;

        private IGameplayTimeService timeService;
        private PlayerCardTimeRuntime runtime;
        private PlayerSourceToken activeSource;

        public bool IsInitialized { get; private set; }
        public CardTimeSessionSnapshot Current =>
            runtime?.Current ?? default;
        public CardTimeSessionEventChannelSO TransitionEvent => transitionEvent;

        private void Update()
        {
            if (IsInitialized)
            {
                runtime.Tick(unscaledDeltaTime: Time.unscaledDeltaTime);
            }
        }

        private void OnDisable()
        {
            Shutdown();
        }

        public void Configure(
            IGameplayTimeService gameplayTime,
            PlayerCardTimeConfigSO config,
            CardTimeSessionEventChannelSO channel)
        {
            if (IsInitialized)
            {
                return;
            }

            timeService = gameplayTime;
            configuration = config;
            transitionEvent = channel;
        }

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            if (timeService == null)
            {
                timeService = GetComponent<GameplayTimeCoordinator>();
            }

            if (timeService == null || configuration == null || transitionEvent == null)
            {
                Debug.LogError(
                    "Card Time Session requires gameplay time, configuration, and a transition event.",
                    context: this);
                return;
            }

            runtime = new PlayerCardTimeRuntime(
                maximumActiveDuration: configuration.MaximumActiveDuration,
                inputBufferDuration: configuration.InputBufferDuration,
                postWindowGraceDuration: configuration.PostWindowGraceDuration);
            runtime.Changed += HandleRuntimeChanged;
            activeSource = null;
            IsInitialized = true;
        }

        public void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            runtime.Changed -= HandleRuntimeChanged;
            timeService.RemoveModifier(owner: this);
            activeSource?.Invalidate();
            activeSource = null;
            runtime = null;
            IsInitialized = false;
        }

        public IPlayerCardTimeSource RegisterPlayerSource(Object owner)
        {
            if (!IsInitialized || owner == null)
            {
                return null;
            }

            if (activeSource != null && activeSource.IsValid)
            {
                return activeSource.Owner == owner ? activeSource : null;
            }

            activeSource = new PlayerSourceToken(service: this, owner: owner);
            return activeSource;
        }

        private void HandleRuntimeChanged(CardTimeSessionTransition transition)
        {
            if (!transition.Previous.IsActive && transition.Current.IsActive)
            {
                timeService.SetModifier(
                    owner: this,
                    modifier: new GameplayTimeModifier(
                        kind: GameplayTimeModifierKind.CardTime,
                        requestedScale: configuration.ActiveTimeScale));
            }
            else if (transition.Previous.IsActive && !transition.Current.IsActive)
            {
                timeService.RemoveModifier(owner: this);
            }

            transitionEvent.Raise(payload: transition);
        }

        private bool IsAuthorized(PlayerSourceToken source)
        {
            return IsInitialized
                && source != null
                && source.IsValid
                && ReferenceEquals(activeSource, source);
        }

        private void PublishAvailability(
            PlayerSourceToken source,
            PlayerCardTimeState state)
        {
            if (IsAuthorized(source))
            {
                runtime.PublishAvailability(cardTimeState: state);
            }
        }

        private CardTimeActivationRequestResult RequestActivation(PlayerSourceToken source)
        {
            return IsAuthorized(source)
                ? runtime.RequestActivation()
                : CardTimeActivationRequestResult.Rejected;
        }

        private bool TryCommit(PlayerSourceToken source)
        {
            return IsAuthorized(source) && runtime.TryCommit();
        }

        private bool TryCommit(
            PlayerSourceToken source,
            ICardCommitTransaction transaction)
        {
            return IsAuthorized(source) && runtime.TryCommit(transaction);
        }

        private bool Cancel(PlayerSourceToken source)
        {
            return IsAuthorized(source) && runtime.Cancel();
        }

        private void Unregister(PlayerSourceToken source)
        {
            if (!IsAuthorized(source))
            {
                return;
            }

            runtime.Cancel();
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.None);
            source.Invalidate();
            activeSource = null;
        }

        private sealed class PlayerSourceToken : IPlayerCardTimeSource
        {
            private CardTimeSessionController service;

            public PlayerSourceToken(
                CardTimeSessionController service,
                Object owner)
            {
                this.service = service;
                Owner = owner;
            }

            public Object Owner { get; }
            public bool IsValid => service != null && Owner != null;
            public PlayerCardTimeConfigSO Configuration => service?.configuration;

            public void PublishAvailability(PlayerCardTimeState state)
            {
                service?.PublishAvailability(source: this, state: state);
            }

            public CardTimeActivationRequestResult RequestActivation()
            {
                return service?.RequestActivation(source: this)
                    ?? CardTimeActivationRequestResult.Rejected;
            }

            public bool TryCommit()
            {
                return service != null && service.TryCommit(source: this);
            }

            public bool TryCommit(ICardCommitTransaction transaction)
            {
                return service != null && service.TryCommit(
                    source: this,
                    transaction: transaction);
            }

            public bool Cancel()
            {
                return service != null && service.Cancel(source: this);
            }

            public void Unregister()
            {
                service?.Unregister(source: this);
            }

            public void Invalidate()
            {
                service = null;
            }
        }
    }
}
