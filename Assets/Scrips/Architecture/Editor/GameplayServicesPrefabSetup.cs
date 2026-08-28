using TicGame.Architecture;
using UnityEditor;
using UnityEngine;

namespace TicGame.Architecture.EditorTools
{
    public static class GameplayServicesPrefabSetup
    {
        private const string ResourcesFolder = "Assets/Resources";
        private const string RuntimeResourcesFolder = ResourcesFolder + "/Runtime";
        private const string EventFolder = "Assets/Data/Events";
        private const string GameplayEventFolder = EventFolder + "/Gameplay";
        private const string PrefabPath = RuntimeResourcesFolder + "/GameplayServices.prefab";
        private const string GameStateEventPath = GameplayEventFolder + "/Event_GameState.asset";
        private const string HitStopRequestEventPath =
            GameplayEventFolder + "/Event_HitStopRequest.asset";
        private const string CardTimeTransitionEventPath =
            GameplayEventFolder + "/Event_CardTimeSession.asset";
        private const string CardFeedbackEventPath =
            GameplayEventFolder + "/Event_CardFeedback.asset";
        private const string CardTimeConfigPath =
            "Assets/SO/Player/PlayerCardTimeConfig.asset";
        private const string RootName = "[Gameplay Services]";

        [MenuItem("TIC/Setup/Create Or Update Gameplay Services")]
        public static void CreateOrUpdateGameplayServices()
        {
            EnsureFolder(RuntimeResourcesFolder);
            EnsureFolder(GameplayEventFolder);

            var stateChangedEvent = CreateOrLoadGameStateEvent();
            var hitStopRequestEvent = CreateOrLoadHitStopRequestEvent();
            var cardTimeTransitionEvent = CreateOrLoadCardTimeTransitionEvent();
            var cardFeedbackEvent = CreateOrLoadCardFeedbackEvent();
            var cardTimeConfig =
                AssetDatabase.LoadAssetAtPath<PlayerCardTimeConfigSO>(CardTimeConfigPath);
            if (cardTimeConfig == null)
            {
                Debug.LogError($"Missing Card Time configuration at {CardTimeConfigPath}.");
                return;
            }
            var loadedPrefabContents = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            var rootObject = loadedPrefabContents
                ? PrefabUtility.LoadPrefabContents(PrefabPath)
                : new GameObject(name: RootName);

            try
            {
                rootObject.name = RootName;

                var root = GetOrAddComponent<GameplayServicesRoot>(rootObject);
                var gameplayTime = GetOrAddComponent<GameplayTimeCoordinator>(rootObject);
                var cardTime = GetOrAddComponent<CardTimeSessionController>(rootObject);
                var hitStop = GetOrAddComponent<HitStopService>(rootObject);
                var gameState = GetOrAddComponent<GameStateController>(rootObject);
                var cardFeedback = GetOrAddComponent<CardFeedbackService>(rootObject);

                hitStop.Configure(
                    gameplayTime: gameplayTime,
                    channel: hitStopRequestEvent);
                cardTime.Configure(
                    gameplayTime: gameplayTime,
                    config: cardTimeConfig,
                    channel: cardTimeTransitionEvent);
                cardFeedback.Configure(channel: cardFeedbackEvent);

                var serializedGameState = new SerializedObject(gameState);
                serializedGameState.FindProperty("stateChangedEvent").objectReferenceValue =
                    stateChangedEvent;
                serializedGameState.ApplyModifiedPropertiesWithoutUndo();

                root.ConfigureModules(gameplayTime, cardTime, hitStop, gameState, cardFeedback);
                EditorUtility.SetDirty(root);
                EditorUtility.SetDirty(gameplayTime);
                EditorUtility.SetDirty(cardTime);
                EditorUtility.SetDirty(hitStop);
                EditorUtility.SetDirty(gameState);
                EditorUtility.SetDirty(cardFeedback);
                PrefabUtility.SaveAsPrefabAsset(rootObject, PrefabPath);
            }
            finally
            {
                if (loadedPrefabContents)
                {
                    PrefabUtility.UnloadPrefabContents(rootObject);
                }
                else
                {
                    Object.DestroyImmediate(rootObject);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created or updated persistent gameplay services.");
        }

        private static GameStateEventChannelSO CreateOrLoadGameStateEvent()
        {
            var channel = AssetDatabase.LoadAssetAtPath<GameStateEventChannelSO>(GameStateEventPath);
            if (channel != null)
            {
                return channel;
            }

            channel = ScriptableObject.CreateInstance<GameStateEventChannelSO>();
            channel.name = "Game State";
            AssetDatabase.CreateAsset(channel, GameStateEventPath);
            return channel;
        }

        private static HitStopRequestEventChannelSO CreateOrLoadHitStopRequestEvent()
        {
            var channel =
                AssetDatabase.LoadAssetAtPath<HitStopRequestEventChannelSO>(HitStopRequestEventPath);
            if (channel != null)
            {
                return channel;
            }

            channel = ScriptableObject.CreateInstance<HitStopRequestEventChannelSO>();
            channel.name = "Hit Stop Request";
            AssetDatabase.CreateAsset(channel, HitStopRequestEventPath);
            return channel;
        }

        private static CardTimeSessionEventChannelSO CreateOrLoadCardTimeTransitionEvent()
        {
            var channel =
                AssetDatabase.LoadAssetAtPath<CardTimeSessionEventChannelSO>(
                    CardTimeTransitionEventPath);
            if (channel != null)
            {
                return channel;
            }

            channel = ScriptableObject.CreateInstance<CardTimeSessionEventChannelSO>();
            channel.name = "Card Time Session";
            AssetDatabase.CreateAsset(channel, CardTimeTransitionEventPath);
            return channel;
        }

        private static CardFeedbackEventChannelSO CreateOrLoadCardFeedbackEvent()
        {
            var channel =
                AssetDatabase.LoadAssetAtPath<CardFeedbackEventChannelSO>(CardFeedbackEventPath);
            if (channel != null)
            {
                return channel;
            }

            channel = ScriptableObject.CreateInstance<CardFeedbackEventChannelSO>();
            channel.name = "Card Feedback";
            AssetDatabase.CreateAsset(channel, CardFeedbackEventPath);
            return channel;
        }

        private static T GetOrAddComponent<T>(GameObject owner) where T : Component
        {
            var component = owner.GetComponent<T>();
            return component != null ? component : owner.AddComponent<T>();
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }
}
