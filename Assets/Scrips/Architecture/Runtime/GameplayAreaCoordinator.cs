using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    public sealed class GameplayAreaCoordinator : MonoBehaviour
    {
        [Header("Startup")]
        [Tooltip("Maximum time to wait for the persistent services to bind the player after the scene is available.")]
        [SerializeField, Min(0.1f)] private float serviceReadyTimeoutSeconds = 5f;

        private PlayerController player;
        private PlayerWorldHold playerHold;
        private CardTimeGuideUI guide;
        private AreaDefinition[] areaDefinitions;
        private RunProgress progress;
        private GameplayServicesRoot servicesRoot;
        private IDisposable startupHold;
        private IDisposable respawnHold;
        private SpawnAddress lastAddress;
        private AreaSpawnPoint resolvedRespawnSpawn;
        private Task<bool> respawnTask;
        private int respawnTaskGeneration = -1;
        private int sessionGeneration;
        private bool hasLastAddress;

        public string LastError { get; private set; }
        public bool IsReady { get; private set; }

        /// <summary>
        /// Supplies the Gameplay composition used by this session coordinator.
        /// </summary>
        public void Configure(
            PlayerController configuredPlayer,
            PlayerWorldHold configuredPlayerHold,
            CardTimeGuideUI configuredGuide,
            AreaDefinition[] configuredAreas,
            RunProgress configuredProgress,
            GameplayServicesRoot configuredServicesRoot)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            sessionGeneration++;
            InvalidateRespawnOperation();
            ReleaseRespawnHold();
            player = configuredPlayer;
            playerHold = configuredPlayerHold;
            guide = configuredGuide;
            areaDefinitions = configuredAreas;
            progress = configuredProgress;
            servicesRoot = configuredServicesRoot;
            IsReady = false;
            LastError = null;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            player?.GetComponent<PlayerDeathRespawn>()?.BindCoordinator(this);
        }

        /// <summary>
        /// Loads and binds an area before placing the player at its unique spawn marker.
        /// </summary>
        public async Task<bool> StartAtAsync(SpawnAddress address)
        {
            var generation = ++sessionGeneration;
            IsReady = false;
            EnsurePlayerHeld();
            try
            {
                address.ThrowIfInvalid();
            }
            catch (ArgumentException)
            {
                return Fail(generation, "Startup requires non-empty area and spawn identifiers.");
            }

            lastAddress = address;
            hasLastAddress = true;

            if (!TryGetArea(address.AreaId, out var area, out var areaError))
            {
                return Fail(generation, areaError);
            }

            var request = await SceneStreamingService.RequestAsync(area.ScenePath, loaded: true);
            if (!IsCurrent(generation))
            {
                return false;
            }

            if (request.Outcome != SceneRequestOutcome.Succeeded)
            {
                return Fail(generation, request.Error ?? $"Area '{address.AreaId}' could not load ({request.Outcome}).");
            }

            var scene = SceneManager.GetSceneByPath(area.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return Fail(generation, $"Area '{address.AreaId}' reported success but its scene is not loaded.");
            }

            if (!await WaitForPlayerServicesAsync(generation, scene))
            {
                return false;
            }

            if (!IsCurrent(generation))
            {
                return false;
            }

            BindLoadedArea(scene, area);
            if (!TryFindSpawn(scene, address, out var spawn, out var spawnError))
            {
                return Fail(generation, spawnError);
            }

            player.ResetTransientState();
            player.transform.SetPositionAndRotation(spawn.AuthoredTransform.position, spawn.AuthoredTransform.rotation);
            player.GetComponent<Rigidbody2D>()?.Sleep();
            LastError = null;
            IsReady = true;
            ReleaseStartupHold();
            return true;
        }

        /// <summary>
        /// Retries the most recent startup address after a load, binding, or marker failure.
        /// </summary>
        public Task<bool> RetryAsync()
        {
            if (!hasLastAddress)
            {
                LastError = "No startup address is available to retry.";
                return Task.FromResult(false);
            }

            return StartAtAsync(lastAddress);
        }

        /// <summary>
        /// Reconciles loaded areas to the current respawn address before returning the player to its marker.
        /// </summary>
        public Task<bool> RespawnAsync()
        {
            if (respawnTask != null && respawnTaskGeneration == sessionGeneration)
            {
                return respawnTask;
            }

            var generation = ++sessionGeneration;
            var completion = new TaskCompletionSource<bool>();
            respawnTask = completion.Task;
            respawnTaskGeneration = generation;
            CompleteRespawnAsync(completion, generation);
            return completion.Task;
        }

        /// <summary>
        /// Invalidates pending asynchronous work when the owning gameplay session ends.
        /// </summary>
        public void CancelSession()
        {
            sessionGeneration++;
            InvalidateRespawnOperation();
            IsReady = false;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            player?.GetComponent<PlayerDeathRespawn>()?.BindCoordinator(null);
            ReleaseRespawnHold();
        }

        private void OnDestroy()
        {
            CancelSession();
        }

        private async void CompleteRespawnAsync(TaskCompletionSource<bool> completion, int generation)
        {
            try
            {
                completion.TrySetResult(await RunRespawnAsync(generation));
            }
            catch (Exception error)
            {
                LastError = $"Respawn failed: {error.Message}";
                IsReady = false;
                Debug.LogError(LastError, this);
                completion.TrySetResult(false);
            }
            finally
            {
                if (respawnTask == completion.Task && respawnTaskGeneration == generation)
                {
                    respawnTask = null;
                    respawnTaskGeneration = -1;
                }
            }
        }

        private async Task<bool> RunRespawnAsync(int generation)
        {
            IsReady = false;
            if (player == null || progress == null)
            {
                return Fail(generation, "Respawn requires a configured player and run progress.");
            }

            var address = progress.Respawn;
            if (!TryGetArea(address.AreaId, out var destination, out var areaError))
            {
                return Fail(generation, areaError);
            }

            resolvedRespawnSpawn = null;
            var sequence = new RespawnSequence(
                hold: () => EnsureRespawnHeld(),
                suspendTriggers: SceneStreamingService.SuspendDirectionalRequests,
                settleRequests: async () =>
                {
                    await SceneStreamingService.WaitForIdleAsync();
                    return IsCurrent(generation);
                },
                unloadOtherAreas: () => UnloadOtherAreasAsync(generation, destination),
                loadDestination: () => LoadRespawnAreaAsync(generation, destination),
                restoreProgress: () => RestoreRespawnProgressAsync(generation, destination),
                resolveSpawn: () => ResolveRespawnSpawn(generation, destination, address),
                teleport: TeleportToResolvedRespawn,
                restoreHealth: RestoreRespawnHealth,
                resetCrossings: ResetDirectionalCrossings,
                releaseHold: ReleaseRespawnHold);

            var respawned = await sequence.RunAsync();
            if (!respawned)
            {
                return false;
            }

            LastError = null;
            IsReady = true;
            return true;
        }

        private async Task<bool> UnloadOtherAreasAsync(int generation, AreaDefinition destination)
        {
            if (areaDefinitions == null)
            {
                return true;
            }

            foreach (var area in areaDefinitions)
            {
                if (area == null || area.ScenePath == destination.ScenePath || !SceneStreamingService.IsLoaded(area.ScenePath))
                {
                    continue;
                }

                var result = await SceneStreamingService.RequestAsync(area.ScenePath, loaded: false);
                if (!IsCurrent(generation))
                {
                    return false;
                }

                if (result.Outcome != SceneRequestOutcome.Succeeded)
                {
                    LastError = result.Error ?? $"Area '{area.AreaId}' could not unload before respawn ({result.Outcome}).";
                    Debug.LogError(LastError, this);
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> LoadRespawnAreaAsync(int generation, AreaDefinition destination)
        {
            var result = await SceneStreamingService.RequestAsync(destination.ScenePath, loaded: true);
            if (!IsCurrent(generation))
            {
                return false;
            }

            if (result.Outcome == SceneRequestOutcome.Succeeded)
            {
                return true;
            }

            LastError = result.Error ?? $"Area '{destination.AreaId}' could not load for respawn ({result.Outcome}).";
            Debug.LogError(LastError, this);
            return false;
        }

        private Task<bool> RestoreRespawnProgressAsync(int generation, AreaDefinition destination)
        {
            if (!IsCurrent(generation))
            {
                return Task.FromResult(false);
            }

            var scene = SceneManager.GetSceneByPath(destination.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return Task.FromResult(Fail(generation, $"Area '{destination.AreaId}' is not loaded for respawn progress restoration."));
            }

            BindKnownServices(scene);
            BindLoadedArea(scene, destination);
            return Task.FromResult(true);
        }

        private Task<bool> ResolveRespawnSpawn(int generation, AreaDefinition destination, SpawnAddress address)
        {
            if (!IsCurrent(generation))
            {
                return Task.FromResult(false);
            }

            var scene = SceneManager.GetSceneByPath(destination.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return Task.FromResult(Fail(generation, $"Area '{destination.AreaId}' is not loaded for respawn."));
            }

            if (!TryFindSpawn(scene, address, out resolvedRespawnSpawn, out var error))
            {
                return Task.FromResult(Fail(generation, error));
            }

            return Task.FromResult(true);
        }

        private void TeleportToResolvedRespawn()
        {
            player.ResetTransientState();
            player.transform.SetPositionAndRotation(
                resolvedRespawnSpawn.AuthoredTransform.position,
                resolvedRespawnSpawn.AuthoredTransform.rotation);
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.Sleep();
            }
        }

        private void RestoreRespawnHealth()
        {
            player.GetComponent<PlayerDeathRespawn>()?.RestoreHealthAfterCoordinatedRespawn();
        }

        private static void ResetDirectionalCrossings()
        {
            foreach (var trigger in FindObjectsByType<DirectionalSceneTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                trigger.ResetCrossing();
            }
        }

        private async Task<bool> WaitForPlayerServicesAsync(int generation, Scene areaScene)
        {
            if (player == null)
            {
                return Fail(generation, "Gameplay has no configured PlayerController.");
            }

            if (player.IsGameplayServicesReady)
            {
                BindKnownServices(areaScene);
                return true;
            }

            var ready = new TaskCompletionSource<bool>();
            void HandleReady() => ready.TrySetResult(true);
            player.GameplayServicesReady += HandleReady;
            try
            {
                BindKnownServices(areaScene);
                if (player.IsGameplayServicesReady)
                {
                    return true;
                }

                var timeout = Task.Delay(TimeSpan.FromSeconds(serviceReadyTimeoutSeconds));
                var completed = await Task.WhenAny(ready.Task, timeout);
                if (!IsCurrent(generation))
                {
                    return false;
                }

                if (completed == ready.Task && player.IsGameplayServicesReady)
                {
                    return true;
                }

                return Fail(generation, "Gameplay services did not bind the player before startup timed out.");
            }
            finally
            {
                player.GameplayServicesReady -= HandleReady;
            }
        }

        private void BindKnownServices(Scene areaScene)
        {
            if (servicesRoot == null)
            {
                servicesRoot = FindFirstObjectByType<GameplayServicesRoot>();
            }

            if (servicesRoot == null || !servicesRoot.IsInitialized)
            {
                return;
            }

            servicesRoot.BindScene(player.gameObject.scene);
            servicesRoot.BindScene(areaScene);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (progress == null || !TryGetAreaByPath(scene.path, out var area))
            {
                return;
            }

            BindKnownServices(scene);
            BindLoadedArea(scene, area);
        }

        private void BindLoadedArea(Scene scene, AreaDefinition area)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var gate in root.GetComponentsInChildren<CardMeleeGate>(includeInactive: true))
                {
                    gate.BindProgress(progress, area.AreaId);
                }

                foreach (var zone in root.GetComponentsInChildren<CardTimeTutorialZone>(includeInactive: true))
                {
                    zone.ConfigureForStreamedComposition();
                    zone.BindGuide(guide);
                }
            }
        }

        private bool TryGetArea(string areaId, out AreaDefinition area, out string error)
        {
            area = null;
            error = null;
            if (areaDefinitions == null)
            {
                error = "Gameplay has no configured area definitions.";
                return false;
            }

            foreach (var definition in areaDefinitions)
            {
                if (definition == null || definition.AreaId != areaId)
                {
                    continue;
                }

                if (area != null)
                {
                    error = $"Gameplay has duplicate definitions for area '{areaId}'.";
                    return false;
                }

                if (!definition.TryValidate(out error))
                {
                    return false;
                }

                area = definition;
            }

            if (area == null)
            {
                error = $"Gameplay has no definition for area '{areaId}'.";
                return false;
            }

            return true;
        }

        private bool TryGetAreaByPath(string scenePath, out AreaDefinition area)
        {
            area = null;
            if (areaDefinitions == null)
            {
                return false;
            }

            foreach (var definition in areaDefinitions)
            {
                if (definition != null && definition.ScenePath == scenePath)
                {
                    area = definition;
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindSpawn(Scene scene, SpawnAddress address, out AreaSpawnPoint spawn, out string error)
        {
            spawn = null;
            var matches = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var candidate in root.GetComponentsInChildren<AreaSpawnPoint>(includeInactive: true))
                {
                    if (candidate.SpawnId != address.SpawnId)
                    {
                        continue;
                    }

                    spawn = candidate;
                    matches++;
                }
            }

            if (matches == 1)
            {
                error = null;
                return true;
            }

            spawn = null;
            error = matches == 0
                ? $"Area '{address.AreaId}' has no spawn marker '{address.SpawnId}'."
                : $"Area '{address.AreaId}' has {matches} spawn markers named '{address.SpawnId}'; exactly one is required.";
            return false;
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

        private void ReleaseStartupHold()
        {
            startupHold?.Dispose();
            startupHold = null;
        }

        private void EnsureRespawnHeld()
        {
            if (respawnHold != null || player == null)
            {
                return;
            }

            playerHold ??= player.GetComponent<PlayerWorldHold>();
            playerHold ??= player.gameObject.AddComponent<PlayerWorldHold>();
            respawnHold = playerHold.Acquire();
            ReleaseStartupHold();
        }

        private void ReleaseRespawnHold()
        {
            respawnHold?.Dispose();
            respawnHold = null;
        }

        private void InvalidateRespawnOperation()
        {
            respawnTask = null;
            respawnTaskGeneration = -1;
        }

        private bool IsCurrent(int generation) => generation == sessionGeneration;

        private bool Fail(int generation, string error)
        {
            if (!IsCurrent(generation))
            {
                return false;
            }

            LastError = error;
            IsReady = false;
            Debug.LogError(error, this);
            return false;
        }

        /// <summary>
        /// Runs the respawn ordering independently from Unity scene APIs so its failure boundaries are testable.
        /// </summary>
        public sealed class RespawnSequence
        {
            private readonly Action hold;
            private readonly Func<IDisposable> suspendTriggers;
            private readonly Func<Task<bool>> settleRequests;
            private readonly Func<Task<bool>> unloadOtherAreas;
            private readonly Func<Task<bool>> loadDestination;
            private readonly Func<Task<bool>> restoreProgress;
            private readonly Func<Task<bool>> resolveSpawn;
            private readonly Action teleport;
            private readonly Action restoreHealth;
            private readonly Action resetCrossings;
            private readonly Action releaseHold;

            public RespawnSequence(
                Action hold,
                Func<IDisposable> suspendTriggers,
                Func<Task<bool>> settleRequests,
                Func<Task<bool>> unloadOtherAreas,
                Func<Task<bool>> loadDestination,
                Func<Task<bool>> restoreProgress,
                Func<Task<bool>> resolveSpawn,
                Action teleport,
                Action restoreHealth,
                Action resetCrossings,
                Action releaseHold)
            {
                this.hold = hold ?? throw new ArgumentNullException(nameof(hold));
                this.suspendTriggers = suspendTriggers ?? throw new ArgumentNullException(nameof(suspendTriggers));
                this.settleRequests = settleRequests ?? throw new ArgumentNullException(nameof(settleRequests));
                this.unloadOtherAreas = unloadOtherAreas ?? throw new ArgumentNullException(nameof(unloadOtherAreas));
                this.loadDestination = loadDestination ?? throw new ArgumentNullException(nameof(loadDestination));
                this.restoreProgress = restoreProgress ?? throw new ArgumentNullException(nameof(restoreProgress));
                this.resolveSpawn = resolveSpawn ?? throw new ArgumentNullException(nameof(resolveSpawn));
                this.teleport = teleport ?? throw new ArgumentNullException(nameof(teleport));
                this.restoreHealth = restoreHealth ?? throw new ArgumentNullException(nameof(restoreHealth));
                this.resetCrossings = resetCrossings ?? throw new ArgumentNullException(nameof(resetCrossings));
                this.releaseHold = releaseHold ?? throw new ArgumentNullException(nameof(releaseHold));
            }

            /// <summary>
            /// Runs the ordered respawn operation and retains the hold when an asynchronous world step fails.
            /// </summary>
            public async Task<bool> RunAsync()
            {
                hold();
                using (suspendTriggers())
                {
                    if (!await settleRequests()
                        || !await unloadOtherAreas()
                        || !await loadDestination()
                        || !await restoreProgress()
                        || !await resolveSpawn())
                    {
                        return false;
                    }

                    teleport();
                    restoreHealth();
                    resetCrossings();
                }

                releaseHold();
                return true;
            }
        }
    }
}
