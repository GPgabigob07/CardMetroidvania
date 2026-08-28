using UnityEngine;

namespace TicGame.Architecture
{
    public static class GameplayServicesBootstrap
    {
        private const string PrefabResourcePath = "Runtime/GameplayServices";
        private const string RootName = "[Gameplay Services]";

        private static GameplayServicesRoot root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            root = null;
            GameplayServicesRoot.ResetAuthority();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateServices()
        {
            if (root != null)
            {
                return;
            }

            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogError(
                    $"Missing gameplay services prefab at Resources/{PrefabResourcePath}.prefab.");
                return;
            }

            var instance = Object.Instantiate(prefab);
            instance.name = RootName;

            root = instance.GetComponent<GameplayServicesRoot>();
            if (root == null)
            {
                Debug.LogError(
                    "The gameplay services prefab does not contain GameplayServicesRoot.",
                    context: instance);
                Object.Destroy(instance);
                return;
            }

            Object.DontDestroyOnLoad(instance);
            if (root.Initialize())
            {
                return;
            }

            Debug.LogError(
                "Gameplay services failed to initialize. The persistent root will be removed.",
                context: instance);
            Object.Destroy(instance);
            root = null;
        }
    }
}
