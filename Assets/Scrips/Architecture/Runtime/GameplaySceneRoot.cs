using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    public sealed class GameplaySceneRoot : MonoBehaviour
    {
        [Header("Gameplay Composition")]
        [Tooltip("The sole player kept alive while areas stream around it.")]
        [SerializeField] private PlayerController player;

        [Tooltip("Scoped physics and input hold owned by the player.")]
        [SerializeField] private PlayerWorldHold playerHold;

        [Tooltip("Persistent Card Time guide presentation owned by Gameplay.")]
        [SerializeField] private CardTimeGuideUI guide;

        [Tooltip("Coordinates initial area readiness and explicit retries.")]
        [SerializeField] private GameplayAreaCoordinator coordinator;

        [Header("Areas")]
        [Tooltip("All streamed areas that Gameplay can resolve during this run.")]
        [SerializeField] private AreaDefinition[] areaDefinitions;

        [Header("Initial Spawn")]
        [Tooltip("Stable identifier of the area loaded when Gameplay starts.")]
        [SerializeField] private string initialAreaId;

        [Tooltip("Stable marker identifier used in the initial area.")]
        [SerializeField] private string initialSpawnId;

        private static GameplaySceneRoot authoritativeRoot;
        private IDisposable sceneProtection;
        private IDisposable startupHold;
        private bool ownsAuthority;
        private string startupError;
        private bool hasInitialAddress;
        private SpawnAddress initialAddress;

        public RunProgress Progress { get; private set; }
        public string LastError => startupError ?? coordinator?.LastError;

        private void Awake()
        {
            if (authoritativeRoot != null && authoritativeRoot != this)
            {
                Debug.LogError("Gameplay already has an active GameplaySceneRoot. The duplicate was not started.", this);
                enabled = false;
                return;
            }

            authoritativeRoot = this;
            ownsAuthority = true;
        }

        private void Start()
        {
            if (!ownsAuthority)
            {
                return;
            }

            var gameplayScene = gameObject.scene;
            if (gameplayScene.IsValid() && gameplayScene.isLoaded)
            {
                SceneManager.SetActiveScene(gameplayScene);
                if (!string.IsNullOrWhiteSpace(gameplayScene.path))
                {
                    sceneProtection = SceneStreamingService.ProtectScene(gameplayScene.path);
                }
            }

            EnsurePlayerHeld();
            _ = BeginStartupAsync();
        }

        private async Task BeginStartupAsync()
        {
            coordinator ??= GetComponent<GameplayAreaCoordinator>();
            if (coordinator == null || player == null)
            {
                startupError = "GameplaySceneRoot requires a coordinator and PlayerController.";
                Debug.LogError(startupError, this);
                return;
            }

            if (!TryCreateInitialAddress(out var initialAddress, out var error))
            {
                startupError = error;
                Debug.LogError(error, this);
                return;
            }

            InitializeRunProgress(initialAddress);
            var services = FindFirstObjectByType<GameplayServicesRoot>();
            coordinator.Configure(player, playerHold, guide, areaDefinitions, Progress, services);
            if (services != null && services.IsInitialized)
            {
                services.BindScene(gameObject.scene);
            }

            startupError = null;
            var started = await coordinator.StartAtAsync(initialAddress);
            if (!ownsAuthority)
            {
                return;
            }

            if (started)
            {
                startupHold?.Dispose();
                startupHold = null;
                return;
            }

            startupError = coordinator.LastError;
        }

        private void OnDestroy()
        {
            if (!ownsAuthority)
            {
                return;
            }

            coordinator?.CancelSession();
            startupHold?.Dispose();
            startupHold = null;
            sceneProtection?.Dispose();
            sceneProtection = null;
            authoritativeRoot = null;
            ownsAuthority = false;
        }

        [ContextMenu("Retry Gameplay Startup")]
        private void RetryStartup()
        {
            EnsurePlayerHeld();
            _ = BeginStartupAsync();
        }

        private void EnsurePlayerHeld()
        {
            if (startupHold != null || player == null)
            {
                return;
            }

            playerHold ??= player.GetComponent<PlayerWorldHold>();
            playerHold ??= player.gameObject.AddComponent<PlayerWorldHold>();
            startupHold = playerHold.Acquire();
        }

        private void InitializeRunProgress(SpawnAddress initialAddress)
        {
            Progress ??= new RunProgress(initialAddress);
            guide?.Bind(Progress);
        }

        private bool TryCreateInitialAddress(out SpawnAddress address, out string error)
        {
            if (hasInitialAddress)
            {
                address = initialAddress;
                error = null;
                return true;
            }

            try
            {
                var areaId = initialAreaId;
                var spawnId = initialSpawnId;
                GameplayStartupOverride.TryConsume(out var overrideAreaId, out var overrideSpawnId);
                if (!string.IsNullOrWhiteSpace(overrideAreaId) && !string.IsNullOrWhiteSpace(overrideSpawnId))
                {
                    areaId = overrideAreaId;
                    spawnId = overrideSpawnId;
                }

                address = new SpawnAddress(areaId, spawnId);
                initialAddress = address;
                hasInitialAddress = true;
                error = null;
                return true;
            }
            catch (ArgumentException)
            {
                address = default;
                error = "GameplaySceneRoot requires non-empty initial area and spawn identifiers.";
                return false;
            }
        }
    }
}
