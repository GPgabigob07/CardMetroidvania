using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture
{
    [DefaultExecutionOrder(-1100)]
    public sealed class PlaytestSessionController : MonoBehaviour
    {
        public const string TitleScene = "Assets/Scenes/MainMenu.unity";
        public const string GameplayScene = "Assets/Scenes/Gameplay.unity";
        public static PlaytestSessionController Instance { get; private set; }

        private readonly PlaytestTransitionGate gate = new();
        private AsyncOperation pendingLoad;
        private IDisposable suspension;
        private PlaytestPauseLease transitionPause;
        private bool waitingForRelease;
        private int releaseFrame;
        private bool returnFailed;
        private GameplaySceneRoot root;
        private GameplayServicesRoot services;

        public bool IsTransitioning => gate.IsBusy;
        public string LastError { get; private set; }
        public bool IsTitle => SceneManager.GetActiveScene().path == TitleScene;
        public GameplaySceneRoot Gameplay => root;
        public GameplayServicesRoot Services => services;
        public bool IsInputBlocked => IsTitle || IsTransitioning || LastError != null || waitingForRelease;
        public event Action Changed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += SceneLoaded;
            RefreshScene();
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode) => RefreshScene();

        private void RefreshScene()
        {
            root = FindFirstObjectByType<GameplaySceneRoot>();
            services = FindFirstObjectByType<GameplayServicesRoot>();
            if (IsTitle) services?.GameState?.RequestState(GameState.MainMenu);
            Changed?.Invoke();
        }

        private void Update()
        {
            if (waitingForRelease && Time.frameCount > releaseFrame && !PlaytestPauseController.AnySubmitHeld())
                waitingForRelease = false;
            if (root != null)
            {
                root.Player?.SetMenuInputSuppressed(IsInputBlocked || PlaytestPauseController.IsGamePaused);
                if (!IsTransitioning && LastError == null && root.LastError != null)
                {
                    LastError = root.LastError;
                    Changed?.Invoke();
                }
            }
        }

        public void RequireInputRelease()
        {
            waitingForRelease = true;
            releaseFrame = Time.frameCount;
        }

        public Task<bool> StartPlaytestAsync() => RunTransitionAsync(returnToTitle: false);
        public Task<bool> ReturnToTitleAsync() => RunTransitionAsync(returnToTitle: true);
        public Task<bool> RetryAsync() => RunTransitionAsync(returnFailed);

        private async Task<bool> RunTransitionAsync(bool returnToTitle)
        {
            var token = gate.TryBegin();
            if (token == 0) return false;
            LastError = null;
            returnFailed = returnToTitle;
            RequireInputRelease();
            Changed?.Invoke();
            try
            {
                RefreshScene();
                GetComponent<PlaytestPauseController>().Resume();
                root?.Player?.SetMenuInputSuppressed(true);
                services?.GameState?.RequestState(GameState.LoadGame);
                // Retain this freeze after a failed return until it is safe to finish unloading.
                if (returnToTitle && transitionPause == null && services?.Time != null)
                    transitionPause = new PlaytestPauseLease(services.Time);
                if (returnToTitle) root?.CancelSession();
                suspension ??= SceneStreamingService.SuspendDirectionalRequests();
                if (pendingLoad != null) await WaitUntilAsync(() => pendingLoad.isDone, "Scene loading did not finish.");
                var streamingIdle = SceneStreamingService.WaitForIdleAsync();
                await WaitUntilAsync(() => streamingIdle.IsCompleted,
                    "Area streaming did not finish. Retry, or quit and restart the playtest.");

                if (returnToTitle || root == null)
                {
                    var path = returnToTitle ? TitleScene : GameplayScene;
                    if (!Application.CanStreamedLevelBeLoaded(path))
                        throw new InvalidOperationException($"Required scene is missing from the build: {path}");
                    pendingLoad = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
                    if (pendingLoad == null) throw new InvalidOperationException("Unity could not start the scene load.");
                    await WaitUntilAsync(() => pendingLoad.isDone, "Scene loading did not finish.");
                    pendingLoad = null;
                    RefreshScene();
                }

                if (returnToTitle)
                {
                    transitionPause?.Dispose();
                    transitionPause = null;
                    if (services == null) throw new InvalidOperationException("Gameplay services are unavailable.");
                    services.Shutdown();
                    if (!services.Initialize()) throw new InvalidOperationException("Gameplay services could not reset.");
                    services.GameState.RequestState(GameState.MainMenu);
                }
                else
                {
                    if (root == null) throw new InvalidOperationException("The Gameplay scene has no startup root.");
                    if (root.LastError != null)
                    {
                        var retry = root.RetryStartupAsync();
                        await WaitUntilAsync(() => retry.IsCompleted, "Startup retry did not finish.");
                        await retry;
                    }
                    await WaitUntilAsync(() => root != null && (root.IsReady || root.LastError != null),
                        "The starting area did not become ready.");
                    if (root == null || !root.IsReady)
                        throw new InvalidOperationException(root?.LastError ?? "Gameplay startup was interrupted.");
                    services.GameState.RequestState(GameState.Gameplay);
                }
                return true;
            }
            catch (Exception error)
            {
                LastError = error.Message;
                Debug.LogError($"Playtest transition: {error}");
                return false;
            }
            finally
            {
                // Keep directional requests suspended after a failure; retry reuses this lease.
                if (LastError == null) { suspension?.Dispose(); suspension = null; }
                gate.Complete(token);
                RequireInputRelease();
                Changed?.Invoke();
            }
        }

        private static async Task WaitUntilAsync(Func<bool> predicate, string error)
        {
            var deadline = Time.realtimeSinceStartupAsDouble + 15;
            while (!predicate())
            {
                if (Time.realtimeSinceStartupAsDouble >= deadline) throw new TimeoutException(error);
                await Task.Yield();
            }
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= SceneLoaded;
            transitionPause?.Dispose();
            suspension?.Dispose();
            Instance = null;
        }
    }
}
