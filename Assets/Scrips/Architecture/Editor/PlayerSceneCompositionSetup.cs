using System.Linq;
using TicGame.Architecture;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture.EditorTools
{
    public static class PlayerSceneCompositionSetup
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string ReviewRootName = "[Review Tools]";
        private const string ResourceFolder = "Assets/Data/Resources";
        private const string EnergyResourcePath = ResourceFolder + "/Resource_Energy.asset";
        private const string CardInventoryFolder = "Assets/Data/Cards/Inventory";
        private const string CardCatalogPath = CardInventoryFolder + "/TestCardCatalog.asset";
        private const string CardSelectionUiConfigPath =
            CardInventoryFolder + "/TestCardTimeSelectionUiConfig.asset";
        private const string ControlSchemeProfilePath =
            CardInventoryFolder + "/TestCardTimeControlSchemeProfile.asset";

        [MenuItem("TIC/Setup/Update Sample Scene Player Composition")]
        public static void UpdateSampleScenePlayerComposition()
        {
            var scene = OpenTargetScene();
            if (!scene.IsValid())
            {
                return;
            }

            var player = scene
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PlayerController>(true))
                .FirstOrDefault();
            if (player == null)
            {
                Debug.LogError($"No PlayerController exists in {ScenePath}.");
                return;
            }

            var hitDetector = GetOrAddComponent<PlayerAttackHitDetector2D>(player.gameObject);
            var cardTimePresenter =
                GetOrAddComponent<PlayerCardTimeDebugPresenter>(player.gameObject);
            var cardSelectionInput =
                GetOrAddComponent<PlayerCardSelectionInput>(player.gameObject);
            var wallet = GetOrAddComponent<PlayerResourceWallet>(player.gameObject);
            var combatEffects = GetOrAddComponent<PlayerCombatEffects>(player.gameObject);
            var cardRuntime = GetOrAddComponent<PlayerCardRuntime>(player.gameObject);
            var cardSnapshotSource =
                GetOrAddComponent<PlayerCardCommitSnapshotSource>(player.gameObject);
            var extraJump = GetOrAddComponent<PlayerExtraJumpRuntime>(player.gameObject);
            var health = GetOrAddComponent<SimpleHealth>(player.gameObject);
            var respawn = GetOrAddComponent<PlayerDeathRespawn>(player.gameObject);
            var cardDebug = GetOrAddComponent<PlayerCardDebugPresenter>(player.gameObject);
            var motor = player.GetComponent<PlayerMotor2D>();
            var energy = CreateOrLoadEnergyResource();
            var cardLoadout = PrototypeCardAssetSetup.CreateOrUpdateAssets();
            var cardCatalog = CreateOrUpdateCardCatalog(cardLoadout);
            var cardInventory = CardInventoryProfileSetup.CreateOrUpdateTestInventory();
            var cardSelectionUiConfig = CreateOrUpdateCardSelectionUiConfig();
            var controlSchemeProfile = CreateOrUpdateControlSchemeProfile();

            wallet.ConfigureSingleResource(
                energy,
                startingAmount: 30f,
                maximumAmount: 100f);
            combatEffects.ConfigureResources(wallet, energy);
            cardSnapshotSource.Configure(wallet, health, new[] { energy });
            extraJump.ConfigureAbility(cardLoadout.ExtraJumpAbility);
            cardRuntime.Configure(wallet, combatEffects, extraJump);
            cardRuntime.ConfigureCardDefinitions(
                cardLoadout.Neutral,
                cardLoadout.Chain,
                cardLoadout.Finisher);
            cardDebug.Configure(wallet, energy, combatEffects, cardRuntime, extraJump);
            respawn.Configure(health, player, motor);
            ConfigurePlayerVisualRoot(motor, player.transform);
            ConfigurePlayerReferences(
                player,
                hitDetector,
                cardTimePresenter,
                cardSelectionInput,
                wallet,
                combatEffects,
                cardRuntime,
                cardCatalog,
                cardInventory,
                cardSelectionUiConfig,
                controlSchemeProfile,
                cardSnapshotSource,
                extraJump);
            ConfigureReviewTools(scene, player.transform.position);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Updated explicit player and review-tool composition in SampleScene.");
        }

        private static Scene OpenTargetScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == ScenePath)
            {
                return activeScene;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void ConfigurePlayerReferences(
            PlayerController player,
            PlayerAttackHitDetector2D hitDetector,
            PlayerCardTimeDebugPresenter cardTimePresenter,
            PlayerCardSelectionInput cardSelectionInput,
            PlayerResourceWallet wallet,
            PlayerCombatEffects combatEffects,
            PlayerCardRuntime cardRuntime,
            CardCatalogSO cardCatalog,
            PlayerCardInventoryProfileSO cardInventory,
            CardTimeSelectionUiConfigSO cardSelectionUiConfig,
            CardTimeControlSchemeProfileSO controlSchemeProfile,
            PlayerCardCommitSnapshotSource cardSnapshotSource,
            PlayerExtraJumpRuntime extraJump)
        {
            var serializedPlayer = new SerializedObject(player);
            serializedPlayer.FindProperty("attackHitDetector").objectReferenceValue = hitDetector;
            serializedPlayer.FindProperty("cardTimePresenter").objectReferenceValue =
                cardTimePresenter;
            serializedPlayer.FindProperty("cardSelectionInput").objectReferenceValue =
                cardSelectionInput;
            serializedPlayer.FindProperty("resourceWallet").objectReferenceValue = wallet;
            serializedPlayer.FindProperty("combatEffects").objectReferenceValue = combatEffects;
            serializedPlayer.FindProperty("cardRuntime").objectReferenceValue = cardRuntime;
            serializedPlayer.FindProperty("cardCatalog").objectReferenceValue = cardCatalog;
            serializedPlayer.FindProperty("cardInventoryProfile").objectReferenceValue =
                cardInventory;
            serializedPlayer.FindProperty("cardSelectionUiConfig").objectReferenceValue =
                cardSelectionUiConfig;
            serializedPlayer.FindProperty("cardControlSchemeProfile").objectReferenceValue =
                controlSchemeProfile;
            serializedPlayer.FindProperty("selectedCardControlSchemeId").stringValue =
                string.Empty;
            serializedPlayer.FindProperty("cardSnapshotSource").objectReferenceValue =
                cardSnapshotSource;
            serializedPlayer.FindProperty("extraJumpRuntime").objectReferenceValue = extraJump;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();
            var serializedSelectionInput = new SerializedObject(cardSelectionInput);
            serializedSelectionInput.FindProperty("selectionUiConfig").objectReferenceValue =
                cardSelectionUiConfig;
            serializedSelectionInput.FindProperty("controlSchemeProfile").objectReferenceValue =
                controlSchemeProfile;
            serializedSelectionInput.FindProperty("selectedSchemeId").stringValue = string.Empty;
            serializedSelectionInput.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(cardSelectionInput);
            EditorUtility.SetDirty(wallet);
            EditorUtility.SetDirty(combatEffects);
            EditorUtility.SetDirty(cardRuntime);
            EditorUtility.SetDirty(cardCatalog);
            EditorUtility.SetDirty(cardInventory);
            EditorUtility.SetDirty(cardSelectionUiConfig);
            EditorUtility.SetDirty(controlSchemeProfile);
            EditorUtility.SetDirty(cardSnapshotSource);
            EditorUtility.SetDirty(extraJump);
            var health = player.GetComponent<SimpleHealth>();
            if (health != null)
            {
                EditorUtility.SetDirty(health);
            }

            var respawn = player.GetComponent<PlayerDeathRespawn>();
            if (respawn != null)
            {
                EditorUtility.SetDirty(respawn);
            }

            var cardDebug = player.GetComponent<PlayerCardDebugPresenter>();
            if (cardDebug != null)
            {
                EditorUtility.SetDirty(cardDebug);
            }
        }

        private static void ConfigurePlayerVisualRoot(PlayerMotor2D motor, Transform playerRoot)
        {
            if (motor == null)
            {
                return;
            }

            var spriteRenderer = playerRoot.GetComponentsInChildren<SpriteRenderer>(true)
                .FirstOrDefault(renderer => renderer.transform != playerRoot);
            if (spriteRenderer == null)
            {
                Debug.LogWarning(
                    "PlayerMotor2D visualRoot was not assigned automatically. Assign the player's child visual transform in the PlayerMotor2D Presentation section.");
                return;
            }

            motor.SetVisualRoot(spriteRenderer.transform);
            EditorUtility.SetDirty(motor);
        }

        private static void ConfigureReviewTools(Scene scene, Vector3 playerPosition)
        {
            var reviewRoot = scene
                .GetRootGameObjects()
                .FirstOrDefault(root => root.name == ReviewRootName);
            if (reviewRoot == null)
            {
                reviewRoot = new GameObject(name: ReviewRootName);
                SceneManager.MoveGameObjectToScene(reviewRoot, scene);
            }

            reviewRoot.transform.position = playerPosition;
            GetOrAddComponent<TrainingDummyReviewBootstrap>(reviewRoot);
            EditorUtility.SetDirty(reviewRoot);
        }

        private static T GetOrAddComponent<T>(GameObject owner) where T : Component
        {
            var component = owner.GetComponent<T>();
            return component != null ? component : owner.AddComponent<T>();
        }

        private static CardCatalogSO CreateOrUpdateCardCatalog(
            PrototypeCardAssetSetup.PrototypeCardLoadout cardLoadout)
        {
            EnsureFolder(CardInventoryFolder);
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalogSO>(CardCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CardCatalogSO>();
                catalog.name = "TestCardCatalog";
                AssetDatabase.CreateAsset(catalog, CardCatalogPath);
            }

            catalog.Configure(new[]
            {
                cardLoadout.Neutral,
                cardLoadout.Chain,
                cardLoadout.Finisher
            });
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static CardTimeSelectionUiConfigSO CreateOrUpdateCardSelectionUiConfig()
        {
            EnsureFolder(CardInventoryFolder);
            var config =
                AssetDatabase.LoadAssetAtPath<CardTimeSelectionUiConfigSO>(
                    CardSelectionUiConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CardTimeSelectionUiConfigSO>();
                config.name = "TestCardTimeSelectionUiConfig";
                AssetDatabase.CreateAsset(config, CardSelectionUiConfigPath);
            }

            config.ConfigurePrototypeDefaults();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static CardTimeControlSchemeProfileSO CreateOrUpdateControlSchemeProfile()
        {
            EnsureFolder(CardInventoryFolder);
            var wasd = CreateOrUpdateControlScheme(
                "KeyboardWASD",
                "Keyboard WASD",
                CardTimeControlDeviceFamily.KeyboardMouse,
                "WASD movement with nearby left-hand card slot commands.",
                new[]
                {
                    ("CardSlotKeyboardWasd1", "Q", "<Keyboard>/q"),
                    ("CardSlotKeyboardWasd2", "E", "<Keyboard>/e"),
                    ("CardSlotKeyboardWasd3", "R", "<Keyboard>/r"),
                    ("CardSlotKeyboardWasd4", "F", "<Keyboard>/f"),
                    ("CardSlotKeyboardWasd5", "Z", "<Keyboard>/z"),
                    ("CardSlotKeyboardWasd6", "X", "<Keyboard>/x"),
                    ("CardSlotKeyboardWasd7", "C", "<Keyboard>/c"),
                    ("CardSlotKeyboardWasd8", "V", "<Keyboard>/v")
                });
            var arrows = CreateOrUpdateControlScheme(
                "KeyboardArrows",
                "Keyboard Arrows",
                CardTimeControlDeviceFamily.KeyboardMouse,
                "Arrow movement with left-hand grid card slot commands.",
                new[]
                {
                    ("CardSlotKeyboardArrows1", "Q", "<Keyboard>/q"),
                    ("CardSlotKeyboardArrows2", "W", "<Keyboard>/w"),
                    ("CardSlotKeyboardArrows3", "E", "<Keyboard>/e"),
                    ("CardSlotKeyboardArrows4", "R", "<Keyboard>/r"),
                    ("CardSlotKeyboardArrows5", "A", "<Keyboard>/a"),
                    ("CardSlotKeyboardArrows6", "S", "<Keyboard>/s"),
                    ("CardSlotKeyboardArrows7", "D", "<Keyboard>/d"),
                    ("CardSlotKeyboardArrows8", "F", "<Keyboard>/f")
                });
            var gamepad = CreateOrUpdateControlScheme(
                "GamepadDefault",
                "Gamepad Default",
                CardTimeControlDeviceFamily.Gamepad,
                "Shoulders and triggers first, then face buttons.",
                new[]
                {
                    ("CardSlotGamepad1", "LB", "<Gamepad>/leftShoulder"),
                    ("CardSlotGamepad2", "RB", "<Gamepad>/rightShoulder"),
                    ("CardSlotGamepad3", "LT", "<Gamepad>/leftTrigger"),
                    ("CardSlotGamepad4", "RT", "<Gamepad>/rightTrigger"),
                    ("CardSlotGamepad5", "X", "<Gamepad>/buttonWest"),
                    ("CardSlotGamepad6", "Y", "<Gamepad>/buttonNorth"),
                    ("CardSlotGamepad7", "A", "<Gamepad>/buttonSouth"),
                    ("CardSlotGamepad8", "B", "<Gamepad>/buttonEast")
                });

            var profile =
                AssetDatabase.LoadAssetAtPath<CardTimeControlSchemeProfileSO>(
                    ControlSchemeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CardTimeControlSchemeProfileSO>();
                profile.name = "TestCardTimeControlSchemeProfile";
                AssetDatabase.CreateAsset(profile, ControlSchemeProfilePath);
            }

            profile.Configure(wasd, new[] { wasd, arrows, gamepad });
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static CardTimeControlSchemeSO CreateOrUpdateControlScheme(
            string schemeId,
            string displayName,
            CardTimeControlDeviceFamily deviceFamily,
            string description,
            (string Action, string Label, string Path)[] slots)
        {
            var path = $"{CardInventoryFolder}/CardTimeControlScheme_{schemeId}.asset";
            var scheme = AssetDatabase.LoadAssetAtPath<CardTimeControlSchemeSO>(path);
            if (scheme == null)
            {
                scheme = ScriptableObject.CreateInstance<CardTimeControlSchemeSO>();
                scheme.name = $"CardTimeControlScheme_{schemeId}";
                AssetDatabase.CreateAsset(scheme, path);
            }

            scheme.Configure(
                schemeId,
                displayName,
                deviceFamily,
                description,
                new[]
                {
                    CreateControlLayout(PlayerCardTimeState.Neutral, 8, slots),
                    CreateControlLayout(PlayerCardTimeState.Chain, 6, slots),
                    CreateControlLayout(PlayerCardTimeState.Finisher, 4, slots)
                });
            EditorUtility.SetDirty(scheme);
            return scheme;
        }

        private static CardTimeControlSchemeCategoryLayout CreateControlLayout(
            PlayerCardTimeState category,
            int slotCount,
            (string Action, string Label, string Path)[] slots)
        {
            var bindings = new CardTimeControlSchemeSlotBinding[slotCount];
            for (var index = 0; index < slotCount; index++)
            {
                var binding = new CardTimeControlSchemeSlotBinding();
                binding.Configure(index, slots[index].Action, slots[index].Label, slots[index].Path);
                bindings[index] = binding;
            }

            var layout = new CardTimeControlSchemeCategoryLayout();
            layout.Configure(category, slotCount, bindings);
            return layout;
        }

        private static ResourceDefinitionSO CreateOrLoadEnergyResource()
        {
            EnsureFolder(ResourceFolder);
            var resource =
                AssetDatabase.LoadAssetAtPath<ResourceDefinitionSO>(EnergyResourcePath);
            if (resource != null)
            {
                return resource;
            }

            resource = ScriptableObject.CreateInstance<ResourceDefinitionSO>();
            resource.name = "Energy";
            AssetDatabase.CreateAsset(resource, EnergyResourcePath);
            AssetDatabase.SaveAssets();
            return resource;
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
