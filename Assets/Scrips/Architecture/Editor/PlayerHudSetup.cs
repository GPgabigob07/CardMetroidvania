using System.Collections.Generic;
using System.Linq;
using TicGame.Architecture;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TicGame.Architecture.EditorTools
{
    public static class PlayerHudSetup
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string HudRootName = "[Player HUD]";
        private const string EnergyResourcePath =
            "Assets/Data/Resources/Resource_Energy.asset";
        private const string HealthChevronPath =
            "Assets/Art/UI/HUD/health-chevron.png";
        private const string EnergySegmentPath =
            "Assets/Art/UI/HUD/energy-segment.png";
        private const string EnergyBoltPath =
            "Assets/Art/UI/HUD/energy-bolt.png";
        private const string CardFramePath =
            "Assets/Art/UI/HUD/card-frame.png";
        private const string CardInventoryFolder = "Assets/Data/Cards/Inventory";
        private const string CardSelectionUiConfigPath =
            CardInventoryFolder + "/TestCardTimeSelectionUiConfig.asset";
        private const string ControlSchemeProfilePath =
            CardInventoryFolder + "/TestCardTimeControlSchemeProfile.asset";
        private const string InputDisplayMapperPath =
            CardInventoryFolder + "/TestCardTimeInputDisplayMapper.asset";

        private static readonly Color Crimson =
            new(r: 0.91f, g: 0.12f, b: 0.08f, a: 1f);
        private static readonly Color Cyan =
            new(r: 0.05f, g: 0.82f, b: 0.95f, a: 1f);
        private static readonly Color PaleCyan =
            new(r: 0.7f, g: 0.94f, b: 0.98f, a: 1f);

        [MenuItem("TIC/Setup/Create Or Update Sample Scene HUD")]
        public static void CreateOrUpdateSampleSceneHud()
        {
            var scene = OpenTargetScene();
            if (!scene.IsValid())
            {
                return;
            }

            ImportHudSprites();
            var healthChevron = LoadSprite(HealthChevronPath);
            var energySegment = LoadSprite(EnergySegmentPath);
            var energyBolt = LoadSprite(EnergyBoltPath);
            var cardFrame = LoadSprite(CardFramePath);
            if (healthChevron == null
                || energySegment == null
                || energyBolt == null
                || cardFrame == null)
            {
                Debug.LogError(
                    "HUD setup could not load one or more SVG Sprite assets.");
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

            var wallet = player.GetComponent<PlayerResourceWallet>();
            var energy = AssetDatabase.LoadAssetAtPath<ResourceDefinitionSO>(
                EnergyResourcePath);
            var selectionConfig = CreateOrLoadCardSelectionUiConfig();
            var controlSchemeProfile =
                AssetDatabase.LoadAssetAtPath<CardTimeControlSchemeProfileSO>(
                    ControlSchemeProfilePath);
            var inputDisplayMapper = CreateOrLoadInputDisplayMapper();
            if (wallet == null || energy == null)
            {
                Debug.LogError(
                    "HUD setup requires PlayerResourceWallet and Resource_Energy.");
                return;
            }

            var health = player.GetComponent<SimpleHealth>()
                ?? Undo.AddComponent<SimpleHealth>(player.gameObject);
            RemoveExistingHud(scene);

            var root = CreateCanvasRoot(scene);
            var playerHud = root.AddComponent<PlayerHudUI>();
            var topLeft = CreateRect(
                name: "Top Left",
                parent: root.transform,
                anchor: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f),
                position: new Vector2(32f, -32f),
                size: new Vector2(250f, 60f));

            var chevrons = CreateHealthChevrons(topLeft, healthChevron);
            var energyElements = CreateEnergyDisplay(
                topLeft,
                energySegment,
                energyBolt);
            playerHud.Configure(
                healthSource: health,
                wallet: wallet,
                energy: energy,
                chevrons: chevrons,
                segments: energyElements.Segments,
                value: energyElements.Value);
            var cardFeedbackHud = CreateCardFeedbackHud(topLeft, cardFrame);
            var worldFeedback = root.AddComponent<CardWorldFeedbackPresenter>();

            var cardTimeRoot = CreateRect(
                name: "Card Time",
                parent: root.transform,
                anchor: new Vector2(1f, 0f),
                pivot: new Vector2(1f, 0f),
                position: new Vector2(-32f, 32f),
                size: new Vector2(68f, 72f));
            var cardGraphics = CreateCardTimeCards(cardTimeRoot, cardFrame);
            var cardTimeInput = CreateCardTimeInputIndicator(cardTimeRoot);
            var cardTimeHud = cardTimeRoot.gameObject.AddComponent<CardTimeHudUI>();
            cardTimeHud.Configure(
                cardGraphics,
                cardTimeInput.Label,
                cardTimeInput.Background,
                controlSchemeProfile,
                inputDisplayMapper);

            var selectionHud = CreateCardSelectionHud(
                root.transform,
                wallet,
                energy,
                selectionConfig,
                controlSchemeProfile,
                inputDisplayMapper,
                cardFrame);
            var serializedPlayer = new SerializedObject(player);
            serializedPlayer.FindProperty("cardSelectionHud").objectReferenceValue = selectionHud;
            serializedPlayer.FindProperty("cardSelectionUiConfig").objectReferenceValue =
                selectionConfig;
            serializedPlayer.FindProperty("cardControlSchemeProfile").objectReferenceValue =
                controlSchemeProfile;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(health);
            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(playerHud);
            EditorUtility.SetDirty(cardFeedbackHud);
            EditorUtility.SetDirty(worldFeedback);
            EditorUtility.SetDirty(cardTimeHud);
            EditorUtility.SetDirty(selectionHud);
            EditorUtility.SetDirty(selectionConfig);
            EditorUtility.SetDirty(inputDisplayMapper);
            if (controlSchemeProfile != null)
            {
                EditorUtility.SetDirty(controlSchemeProfile);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
            Debug.Log("Created compact player HUD in SampleScene.");
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

        private static void ImportHudSprites()
        {
            ConfigureSpriteImporter(HealthChevronPath, pixelsPerUnit: 64f);
            ConfigureSpriteImporter(EnergySegmentPath, pixelsPerUnit: 32f);
            ConfigureSpriteImporter(EnergyBoltPath, pixelsPerUnit: 48f);
            ConfigureSpriteImporter(CardFramePath, pixelsPerUnit: 104f);
        }

        private static void ConfigureSpriteImporter(string path, float pixelsPerUnit)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void RemoveExistingHud(Scene scene)
        {
            var existing = scene
                .GetRootGameObjects()
                .FirstOrDefault(root => root.name == HudRootName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }
        }

        private static GameObject CreateCanvasRoot(Scene scene)
        {
            var root = new GameObject(
                HudRootName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Create Player HUD");

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return root;
        }

        private static List<Image> CreateHealthChevrons(
            RectTransform parent,
            Sprite sprite)
        {
            var images = new List<Image>();
            for (var index = 0; index < 5; index++)
            {
                var rect = CreateRect(
                    name: $"Health {index + 1}",
                    parent: parent,
                    anchor: new Vector2(0f, 1f),
                    pivot: new Vector2(0f, 1f),
                    position: new Vector2(index * 26f, 0f),
                    size: new Vector2(22f, 16f));
                images.Add(CreateImage(rect, sprite, Crimson));
            }

            return images;
        }

        private static EnergyElements CreateEnergyDisplay(
            RectTransform parent,
            Sprite segmentSprite,
            Sprite boltSprite)
        {
            var boltRect = CreateRect(
                name: "Energy Icon",
                parent: parent,
                anchor: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f),
                position: new Vector2(0f, -26f),
                size: new Vector2(11f, 18f));
            CreateImage(boltRect, boltSprite, Cyan);

            var segments = new List<Image>();
            for (var index = 0; index < 30; index++)
            {
                var rect = CreateRect(
                    name: $"Energy {index + 1:00}",
                    parent: parent,
                    anchor: new Vector2(0f, 1f),
                    pivot: new Vector2(0f, 1f),
                    position: new Vector2(17f + index * 6f, -28f),
                    size: new Vector2(4f, 14f));
                segments.Add(CreateImage(rect, segmentSprite, Cyan));
            }

            var valueRect = CreateRect(
                name: "Energy Value",
                parent: parent,
                anchor: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f),
                position: new Vector2(202f, -29f),
                size: new Vector2(46f, 18f));
            var value = valueRect.gameObject.AddComponent<Text>();
            value.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            value.fontSize = 16;
            value.fontStyle = FontStyle.Bold;
            value.alignment = TextAnchor.MiddleLeft;
            value.color = PaleCyan;
            value.text = "0";
            value.raycastTarget = false;
            return new EnergyElements(segments, value);
        }

        private static List<Graphic> CreateCardTimeCards(
            RectTransform parent,
            Sprite sprite)
        {
            var cards = new List<Graphic>();
            var positions = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(-12f, 8f),
                new Vector2(-24f, 16f)
            };
            var rotations = new[] { 0f, -7f, -14f };

            for (var index = 2; index >= 0; index--)
            {
                var rect = CreateRect(
                    name: $"Card {index + 1}",
                    parent: parent,
                    anchor: new Vector2(1f, 0f),
                    pivot: new Vector2(1f, 0f),
                    position: positions[index],
                    size: new Vector2(34f, 50f));
                rect.localEulerAngles = new Vector3(0f, 0f, rotations[index]);
                var image = CreateImage(rect, sprite, Cyan);
                cards.Insert(0, image);

                var tierRect = CreateRect(
                    name: "Tier",
                    parent: rect,
                    anchor: new Vector2(0.5f, 0.5f),
                    pivot: new Vector2(0.5f, 0.5f),
                    position: Vector2.zero,
                    size: new Vector2(24f, 20f));
                var tier = tierRect.gameObject.AddComponent<Text>();
                tier.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                tier.fontSize = 12;
                tier.fontStyle = FontStyle.Bold;
                tier.alignment = TextAnchor.MiddleCenter;
                tier.color = PaleCyan;
                tier.text = new string('I', index + 1);
                tier.raycastTarget = false;
            }

            return cards;
        }

        private static InputIndicatorElements CreateCardTimeInputIndicator(
            RectTransform parent)
        {
            var badge = CreateRect(
                name: "Input Badge",
                parent: parent,
                anchor: new Vector2(1f, 0f),
                pivot: new Vector2(1f, 0f),
                position: new Vector2(0f, -30f),
                size: new Vector2(76f, 24f));
            var background = CreateImage(badge, null, Color.white);
            background.raycastTarget = false;
            var label = CreateText(
                name: "Input Label",
                parent: badge,
                position: Vector2.zero,
                size: new Vector2(72f, 22f),
                fontSize: 13,
                alignment: TextAnchor.MiddleCenter,
                color: Color.black);
            badge.gameObject.SetActive(false);
            return new InputIndicatorElements(background, label);
        }

        private static CardHudEffectIndicatorPresenter CreateCardFeedbackHud(
            RectTransform parent,
            Sprite cardFrame)
        {
            var root = CreateRect(
                name: "Card Effects",
                parent: parent,
                anchor: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f),
                position: new Vector2(0f, -58f),
                size: new Vector2(250f, 46f));
            var presenter = root.gameObject.AddComponent<CardHudEffectIndicatorPresenter>();
            var indicators = new List<CardHudEffectIndicatorUI>();
            for (var index = 0; index < 8; index++)
            {
                var slot = CreateRect(
                    name: $"Card Effect {index + 1}",
                    parent: root,
                    anchor: new Vector2(0f, 1f),
                    pivot: new Vector2(0f, 1f),
                    position: new Vector2(index * 30f, 0f),
                    size: new Vector2(26f, 40f));
                var group = slot.gameObject.AddComponent<CanvasGroup>();
                group.interactable = false;
                group.blocksRaycasts = false;

                var icon = CreateImage(slot, cardFrame, PaleCyan);
                icon.raycastTarget = false;
                var label = CreateText(
                    name: "Value",
                    parent: slot,
                    position: new Vector2(0f, 1f),
                    size: new Vector2(26f, 14f),
                    fontSize: 10,
                    alignment: TextAnchor.LowerCenter,
                    color: PaleCyan);
                var indicator = slot.gameObject.AddComponent<CardHudEffectIndicatorUI>();
                indicator.Configure(icon, label, group);
                indicator.SetVisible(false);
                indicators.Add(indicator);
            }

            presenter.Configure(indicators);
            return presenter;
        }

        private static CardTimeSelectionHudUI CreateCardSelectionHud(
            Transform parent,
            PlayerResourceWallet wallet,
            ResourceDefinitionSO energy,
            CardTimeSelectionUiConfigSO selectionConfig,
            CardTimeControlSchemeProfileSO controlSchemeProfile,
            CardTimeInputDisplayMapperSO inputDisplayMapper,
            Sprite cardFrame)
        {
            var backdrop = CreateRect(
                name: "Card Selection Backdrop",
                parent: parent,
                anchor: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f),
                position: Vector2.zero,
                size: new Vector2(2400f, 1400f));
            var backdropImage = CreateImage(
                backdrop,
                sprite: null,
                color: new Color(0f, 0f, 0f, 0.58f));
            backdropImage.raycastTarget = false;
            var backdropGroup = backdrop.gameObject.AddComponent<CanvasGroup>();
            backdropGroup.alpha = 0f;
            backdropGroup.interactable = false;
            backdropGroup.blocksRaycasts = false;

            var root = CreateRect(
                name: "Card Selection",
                parent: parent,
                anchor: new Vector2(0.5f, 0f),
                pivot: new Vector2(0.5f, 0f),
                position: new Vector2(0f, 38f),
                size: new Vector2(1110f, 208f));
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var category = CreateText(
                name: "Category",
                parent: root,
                position: new Vector2(-510f, 168f),
                size: new Vector2(210f, 24f),
                fontSize: 18,
                alignment: TextAnchor.MiddleLeft,
                color: PaleCyan);
            var energyValue = CreateText(
                name: "Energy",
                parent: root,
                position: new Vector2(330f, 168f),
                size: new Vector2(220f, 30f),
                fontSize: 22,
                alignment: TextAnchor.MiddleRight,
                color: Cyan);

            var slots = new List<GameObject>();
            var slotViews = new List<CardTimeSelectionSlotUI>();
            var labels = new List<Text>();
            var frames = new List<Graphic>();
            for (var index = 0; index < 8; index++)
            {
                var slot = CreateRect(
                    name: $"Selection Card {index + 1}",
                    parent: root,
                    anchor: new Vector2(0.5f, 0f),
                    pivot: new Vector2(0.5f, 0f),
                    position: new Vector2(-462f + index * 132f, 0f),
                    size: new Vector2(112f, 154f));
                slots.Add(slot.gameObject);
                var frame = CreateImage(slot, cardFrame, Cyan);
                frames.Add(frame);
                var cardName = CreateText(
                    name: "Name",
                    parent: slot,
                    position: new Vector2(0f, 24f),
                    size: new Vector2(98f, 68f),
                    fontSize: 13,
                    alignment: TextAnchor.MiddleCenter,
                    color: PaleCyan);
                var commandBadge = CreateRect(
                    name: "Command Badge",
                    parent: slot,
                    anchor: new Vector2(0.5f, 0f),
                    pivot: new Vector2(0.5f, 0f),
                    position: new Vector2(0f, 118f),
                    size: new Vector2(64f, 24f));
                var commandBackground = CreateImage(commandBadge, null, Color.white);
                commandBackground.raycastTarget = false;
                var commandName = CreateText(
                    name: "Command",
                    parent: commandBadge,
                    position: Vector2.zero,
                    size: new Vector2(60f, 22f),
                    fontSize: 14,
                    alignment: TextAnchor.MiddleCenter,
                    color: Color.black);
                labels.Add(cardName);
                var slotView = slot.gameObject.AddComponent<CardTimeSelectionSlotUI>();
                slotView.Configure(cardName, commandName, frame, commandBackground);
                slotViews.Add(slotView);
            }

            var hud = root.gameObject.AddComponent<CardTimeSelectionHudUI>();
            hud.Configure(
                wallet,
                energy,
                selectionConfig,
                controlSchemeProfile,
                inputDisplayMapper,
                schemeId: string.Empty,
                group,
                backdropGroup,
                category,
                energyValue,
                slots,
                slotViews,
                labels,
                frames);
            return hud;
        }

        private static CardTimeInputDisplayMapperSO CreateOrLoadInputDisplayMapper()
        {
            EnsureFolder(CardInventoryFolder);
            var mapper =
                AssetDatabase.LoadAssetAtPath<CardTimeInputDisplayMapperSO>(
                    InputDisplayMapperPath);
            if (mapper == null)
            {
                mapper = ScriptableObject.CreateInstance<CardTimeInputDisplayMapperSO>();
                mapper.name = "TestCardTimeInputDisplayMapper";
                mapper.ConfigurePrototypeDefaults();
                AssetDatabase.CreateAsset(mapper, InputDisplayMapperPath);
            }
            else if (mapper.Bindings.Count == 0)
            {
                mapper.ConfigurePrototypeDefaults();
            }

            EditorUtility.SetDirty(mapper);
            AssetDatabase.SaveAssets();
            return mapper;
        }

        private static CardTimeSelectionUiConfigSO CreateOrLoadCardSelectionUiConfig()
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

            if (config.Categories.Count == 0)
            {
                config.ConfigurePrototypeDefaults();
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
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

        private static Text CreateText(
            string name,
            RectTransform parent,
            Vector2 position,
            Vector2 size,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            var rect = CreateRect(
                name: name,
                parent: parent,
                anchor: new Vector2(0.5f, 0f),
                pivot: new Vector2(0.5f, 0f),
                position: position,
                size: size);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            var owner = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(owner, $"Create {name}");
            var rect = owner.GetComponent<RectTransform>();
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image CreateImage(
            RectTransform rect,
            Sprite sprite,
            Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private readonly struct EnergyElements
        {
            public EnergyElements(List<Image> segments, Text value)
            {
                Segments = segments;
                Value = value;
            }

            public List<Image> Segments { get; }
            public Text Value { get; }
        }

        private readonly struct InputIndicatorElements
        {
            public InputIndicatorElements(Graphic background, Text label)
            {
                Background = background;
                Label = label;
            }

            public Graphic Background { get; }
            public Text Label { get; }
        }
    }
}
