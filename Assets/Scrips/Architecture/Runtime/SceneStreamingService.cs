using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture
{
    public static class SceneStreamingService
    {
        private static readonly Dictionary<string, SceneLoadRequest> requests = new(System.StringComparer.Ordinal);
        private static readonly Dictionary<string, int> protectedScenes = new(System.StringComparer.Ordinal);
        private static readonly List<TaskCompletionSource<object>> globalIdleWaiters = new();
        private static int directionalRequestSuspensionCount;

        public static bool DirectionalRequestsAllowed => directionalRequestSuspensionCount == 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            requests.Clear();
            protectedScenes.Clear();
            globalIdleWaiters.Clear();
            directionalRequestSuspensionCount = 0;
        }

        public static SceneLoadRequest GetStatus(string path)
        {
            return !string.IsNullOrEmpty(path) && requests.TryGetValue(path, out var request) ? request : null;
        }

        public static bool IsLoaded(string path) => !string.IsNullOrWhiteSpace(path)
            && SceneManager.GetSceneByPath(path).isLoaded;

        public static void Request(string path, bool loaded)
        {
            if (!loaded && IsSceneProtected(path))
            {
                Debug.LogError($"Cannot unload '{path}': it is protected by the active gameplay session.");
                return;
            }
            RequestAsync(path, loaded);
        }

        public static Task<SceneRequestResult> RequestAsync(string path, bool loaded)
        {
            if (!loaded && IsSceneProtected(path))
            {
                var error = $"Cannot unload '{path}': it is protected by the active gameplay session.";
                return Task.FromResult(new SceneRequestResult(SceneRequestOutcome.Failed, error));
            }
            if (!requests.TryGetValue(path, out var request))
            {
                request = new SceneLoadRequest(
                    () => IsLoaded(path),
                    (load, completed) => Begin(path, load, completed),
                    CompleteGlobalIdleWaiters);
                requests.Add(path, request);
            }
            return request.RequestAsync(loaded);
        }

        public static Task WaitForIdleAsync(string path)
        {
            return !string.IsNullOrEmpty(path) && requests.TryGetValue(path, out var request)
                ? request.WaitForIdleAsync()
                : Task.CompletedTask;
        }

        public static Task WaitForIdleAsync()
        {
            if (!requests.Values.Any(request => request.IsBusy)) return Task.CompletedTask;
            var waiter = new TaskCompletionSource<object>();
            globalIdleWaiters.Add(waiter);
            CompleteGlobalIdleWaiters();
            return waiter.Task;
        }

        public static IDisposable ProtectScene(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A full scene path is required.", nameof(path));
            protectedScenes.TryGetValue(path, out var count);
            protectedScenes[path] = count + 1;
            return new SceneProtection(path);
        }

        /// <summary>
        /// Prevents directional trigger crossings from scheduling new requests until the returned lease is released.
        /// </summary>
        public static IDisposable SuspendDirectionalRequests()
        {
            directionalRequestSuspensionCount++;
            return new DirectionalRequestSuspension();
        }

        private static void Begin(string path, bool load, Action<Exception> completed)
        {
            try
            {
                AsyncOperation operation;
                if (load)
                {
                    if (!Application.CanStreamedLevelBeLoaded(path))
                        throw new InvalidOperationException($"Scene '{path}' is not available. Add it to the enabled Build Profile scene list.");
                    operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
                }
                else
                {
                    if (IsSceneProtected(path))
                        throw new InvalidOperationException($"Cannot unload '{path}': it is protected by the active gameplay session.");
                    var scene = SceneManager.GetSceneByPath(path);
                    if (scene.GetRootGameObjects().Any(root => root.GetComponentInChildren<PlayerController>(true) != null))
                        throw new InvalidOperationException($"Cannot unload '{path}': it owns a PlayerController. Move the player, camera and HUD to a separate loaded gameplay scene first.");
                    if (SceneManager.sceneCount <= 1)
                        throw new InvalidOperationException("Cannot unload the last loaded scene.");
                    if (scene == SceneManager.GetActiveScene())
                    {
                        for (var index = 0; index < SceneManager.sceneCount; index++)
                        {
                            var other = SceneManager.GetSceneAt(index);
                            if (other != scene && other.isLoaded)
                            {
                                SceneManager.SetActiveScene(other);
                                break;
                            }
                        }
                    }
                    operation = SceneManager.UnloadSceneAsync(scene);
                }
                if (operation == null) throw new InvalidOperationException($"Unity did not start the scene operation for '{path}'.");
                operation.completed += _ =>
                {
                    var error = IsLoaded(path) == load ? null
                        : new InvalidOperationException($"Scene '{path}' did not reach the requested loaded state ({load}).");
                    if (error != null) Debug.LogError(error.Message);
                    completed(error);
                };
            }
            catch (Exception error)
            {
                Debug.LogError(error.Message);
                completed(error);
            }
        }

        public static bool IsSceneProtected(string path)
        {
            return !string.IsNullOrEmpty(path) && protectedScenes.ContainsKey(path);
        }

        private static void CompleteGlobalIdleWaiters()
        {
            if (requests.Values.Any(request => request.IsBusy) || globalIdleWaiters.Count == 0) return;
            var waiters = globalIdleWaiters.ToArray();
            globalIdleWaiters.Clear();
            foreach (var waiter in waiters) waiter.TrySetResult(null);
        }

        private sealed class SceneProtection : IDisposable
        {
            private string path;

            public SceneProtection(string protectedPath)
            {
                path = protectedPath;
            }

            public void Dispose()
            {
                if (path == null) return;
                if (protectedScenes.TryGetValue(path, out var count))
                {
                    if (count <= 1) protectedScenes.Remove(path);
                    else protectedScenes[path] = count - 1;
                }
                path = null;
            }
        }

        private sealed class DirectionalRequestSuspension : IDisposable
        {
            private bool isDisposed;

            public void Dispose()
            {
                if (isDisposed)
                {
                    return;
                }

                isDisposed = true;
                directionalRequestSuspensionCount = Math.Max(0, directionalRequestSuspensionCount - 1);
            }
        }
    }
}
