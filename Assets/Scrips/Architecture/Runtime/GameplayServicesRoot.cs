using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture
{
    public sealed class GameplayServicesRoot : MonoBehaviour, IGameplayServices
    {
        [Header("Modules")]
        [Tooltip("Persistent modules initialized in list order and shut down in reverse order.")]
        [SerializeField] private MonoBehaviour[] moduleComponents;

        private static GameplayServicesRoot authoritativeRoot;

        private readonly List<IGameplayModule> modules = new();
        private readonly HashSet<MonoBehaviour> boundConsumers = new();
        private bool ownsAuthority;

        public bool IsInitialized { get; private set; }
        public IGameplayTimeService Time { get; private set; }
        public IHitStopService HitStop { get; private set; }
        public HitStopRequestEventChannelSO HitStopRequests { get; private set; }
        public ICardTimeSession CardTime { get; private set; }
        public CardTimeSessionEventChannelSO CardTimeTransitions { get; private set; }
        public ICardFeedbackService CardFeedback { get; private set; }
        public IGameStateService GameState { get; private set; }
        private ICardTimeSessionService cardTimeSources;

        private void Awake()
        {
            if (!TryClaimAuthority())
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (!ownsAuthority)
            {
                return;
            }

            Shutdown();
            authoritativeRoot = null;
            ownsAuthority = false;
        }

        public bool Initialize()
        {
            if (IsInitialized)
            {
                return true;
            }

            if (!ownsAuthority)
            {
                return false;
            }

            if (!TryResolveModules())
            {
                return false;
            }

            for (var index = 0; index < modules.Count; index++)
            {
                var module = modules[index];
                module.Initialize();
                if (module.IsInitialized)
                {
                    continue;
                }

                Debug.LogError(
                    message: $"Gameplay Services module '{module.GetType().Name}' failed to initialize.",
                    context: this);
                ShutdownModules(lastInitializedIndex: index - 1);
                return false;
            }

            SceneManager.sceneLoaded += HandleSceneLoaded;
            IsInitialized = true;

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isLoaded)
            {
                BindScene(activeScene);
            }

            return true;
        }

        public void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;

            ShutdownModules(lastInitializedIndex: modules.Count - 1);

            boundConsumers.Clear();
            IsInitialized = false;
        }

        public void ConfigureModules(params MonoBehaviour[] components)
        {
            if (IsInitialized)
            {
                return;
            }

            moduleComponents = components;
        }

        public void BindScene(Scene scene)
        {
            if (!IsInitialized || !scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            boundConsumers.RemoveWhere(behaviour => behaviour == null);

            foreach (var rootObject in scene.GetRootGameObjects())
            {
                var behaviours = rootObject.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
                foreach (var behaviour in behaviours)
                {
                    if (behaviour is not IGameplayServicesConsumer consumer
                        || behaviour == this
                        || !boundConsumers.Add(behaviour))
                    {
                        continue;
                    }

                    consumer.BindGameplayServices(services: this);

                    if (behaviour is IPlayerCardTimeSourceConsumer playerSourceConsumer)
                    {
                        playerSourceConsumer.BindPlayerCardTimeSource(
                            source: cardTimeSources.RegisterPlayerSource(owner: behaviour));
                    }
                }
            }
        }

        internal static void ResetAuthority()
        {
            authoritativeRoot = null;
        }

        private bool TryClaimAuthority()
        {
            if (authoritativeRoot != null && authoritativeRoot != this)
            {
                return false;
            }

            authoritativeRoot = this;
            ownsAuthority = true;
            return true;
        }

        private bool TryResolveModules()
        {
            modules.Clear();
            Time = null;
            HitStop = null;
            HitStopRequests = null;
            CardTime = null;
            cardTimeSources = null;
            CardTimeTransitions = null;
            CardFeedback = null;
            GameState = null;

            if (moduleComponents == null || moduleComponents.Length == 0)
            {
                Debug.LogError("Gameplay Services has no configured modules.", context: this);
                return false;
            }

            foreach (var component in moduleComponents)
            {
                if (component is not IGameplayModule module)
                {
                    Debug.LogError(
                        message: $"Gameplay Services module '{component?.name ?? "Missing"}' does not implement IGameplayModule.",
                        context: this);
                    modules.Clear();
                    return false;
                }

                modules.Add(module);
                if (component is IGameplayTimeService gameplayTime)
                {
                    Time = gameplayTime;
                }

                if (component is HitStopService hitStop)
                {
                    HitStop = hitStop;
                    HitStopRequests = hitStop.RequestEvent;
                }

                if (component is CardTimeSessionController cardTime)
                {
                    CardTime = cardTime;
                    cardTimeSources = cardTime;
                    CardTimeTransitions = cardTime.TransitionEvent;
                }

                if (component is ICardFeedbackService cardFeedback)
                {
                    CardFeedback = cardFeedback;
                }

                if (component is IGameStateService gameState)
                {
                    GameState = gameState;
                }
            }

            if (Time == null
                || HitStop == null
                || HitStopRequests == null
                || CardTime == null
                || cardTimeSources == null
                || CardTimeTransitions == null
                || GameState == null)
            {
                Debug.LogError(
                    "Gameplay Services requires time, hitstop, Card Time, events, and game-state services.",
                    context: this);
                modules.Clear();
                return false;
            }

            return true;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BindScene(scene);
        }

        private void ShutdownModules(int lastInitializedIndex)
        {
            for (var index = lastInitializedIndex; index >= 0; index--)
            {
                modules[index].Shutdown();
            }
        }
    }
}
