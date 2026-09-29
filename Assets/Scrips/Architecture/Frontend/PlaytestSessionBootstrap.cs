using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture
{
    public static class PlaytestSessionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (PlaytestSessionController.Instance != null) return;
            if (scene.path != PlaytestSessionController.TitleScene && scene.path != PlaytestSessionController.GameplayScene) return;
            var prefab = Resources.Load<GameObject>("Runtime/PlaytestSession");
            if (prefab == null) { Debug.LogError("Run TIC/Setup/Create Or Update Playtest Frontend before playing."); return; }
            Object.Instantiate(prefab).name = "[Playtest Frontend]";
        }
    }
}
