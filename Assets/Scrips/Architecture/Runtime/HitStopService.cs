using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class HitStopService :
        MonoBehaviour,
        IGameplayModule,
        IHitStopService
    {
        [Header("Events")]
        [Tooltip("Receives accepted damage hitstop requests from combat systems.")]
        [SerializeField] private HitStopRequestEventChannelSO requestEvent;

        private IGameplayTimeService timeService;
        private float remaining;

        public bool IsInitialized { get; private set; }
        public bool IsActive => remaining > 0f;
        public HitStopRequestEventChannelSO RequestEvent => requestEvent;

        private void OnDisable()
        {
            Shutdown();
        }

        private void Update()
        {
            if (!IsInitialized || remaining <= 0f)
            {
                return;
            }

            remaining = Mathf.Max(a: 0f, b: remaining - Time.unscaledDeltaTime);
            if (remaining <= 0f)
            {
                timeService.RemoveModifier(owner: this);
            }
        }

        public void Configure(
            IGameplayTimeService gameplayTime,
            HitStopRequestEventChannelSO channel)
        {
            if (IsInitialized)
            {
                return;
            }

            timeService = gameplayTime;
            requestEvent = channel;
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

            if (timeService == null || requestEvent == null)
            {
                Debug.LogError(
                    "Hit Stop Service requires gameplay time and a request event channel.",
                    context: this);
                return;
            }

            requestEvent.Raised += Request;
            remaining = 0f;
            IsInitialized = true;
        }

        public void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            requestEvent.Raised -= Request;
            remaining = 0f;
            timeService.RemoveModifier(owner: this);
            IsInitialized = false;
        }

        public void Request(HitStopRequest request)
        {
            if (!IsInitialized || request.Duration <= 0f)
            {
                return;
            }

            remaining = Mathf.Max(a: remaining, b: request.Duration);
            timeService.SetModifier(
                owner: this,
                modifier: new GameplayTimeModifier(
                    kind: GameplayTimeModifierKind.HitStop,
                    requestedScale: 0f));
        }
    }
}
