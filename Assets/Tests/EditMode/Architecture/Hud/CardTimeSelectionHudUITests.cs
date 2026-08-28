using System.Collections.Generic;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;
using UnityEngine.UI;

namespace TicGame.Architecture.Tests
{
    public sealed class CardTimeSelectionHudUITests
    {
        private readonly List<Object> objectsToDestroy = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in objectsToDestroy)
            {
                Object.DestroyImmediate(instance);
            }

            objectsToDestroy.Clear();
        }

        [Test]
        public void BindSelection_UsesConfiguredSlotCountAsVisibleCap()
        {
            var hud = CreateHud(out var slots, out _);
            var config = CreateConfig(slotCount: 2);
            var transaction = CreateTransaction(candidateCount: 3);
            hud.Configure(
                resourceWallet: null,
                energy: null,
                config,
                schemeProfile: null,
                schemeId: null,
                group: null,
                category: null,
                energyValue: null,
                slots,
                views: null,
                labels: null,
                frames: null);

            hud.BindSelection(transaction);

            Assert.IsTrue(slots[0].activeSelf);
            Assert.IsTrue(slots[1].activeSelf);
            Assert.IsFalse(slots[2].activeSelf);
        }

        [Test]
        public void PlaySlotAnimation_TargetsIndexedSlotView()
        {
            var hud = CreateHud(out var slots, out var frames);
            var slotViews = new List<CardTimeSelectionSlotUI>();
            for (var index = 0; index < slots.Count; index++)
            {
                var view = slots[index].AddComponent<CardTimeSelectionSlotUI>();
                view.Configure(cardName: null, commandName: null, frameGraphic: frames[index]);
                slotViews.Add(view);
            }

            hud.Configure(
                resourceWallet: null,
                energy: null,
                config: null,
                schemeProfile: null,
                schemeId: null,
                group: null,
                category: null,
                energyValue: null,
                slots,
                slotViews,
                labels: null,
                frames);

            hud.PlaySlotAnimation(1, CardTimeSelectionSlotAnimation.Invalid);

            Assert.AreEqual(1f, frames[1].color.r);
            Assert.Less(frames[0].color.r, 1f);
        }

        [Test]
        public void BindSelection_UsesSchemeLabelsForSlots()
        {
            var hud = CreateHud(out var slots, out var frames);
            var labels = new List<Text>();
            var slotViews = new List<CardTimeSelectionSlotUI>();
            for (var index = 0; index < slots.Count; index++)
            {
                var label = slots[index].AddComponent<Text>();
                labels.Add(label);
                var view = slots[index].AddComponent<CardTimeSelectionSlotUI>();
                view.Configure(cardName: null, commandName: label, frameGraphic: frames[index]);
                slotViews.Add(view);
            }

            var profile = CreateSchemeProfile();
            hud.Configure(
                resourceWallet: null,
                energy: null,
                config: null,
                profile,
                schemeId: "KeyboardWASD",
                group: null,
                category: null,
                energyValue: null,
                slots,
                slotViews,
                labels: null,
                frames);

            hud.BindSelection(CreateTransaction(candidateCount: 2));

            Assert.AreEqual("Q", labels[0].text);
            Assert.AreEqual("E", labels[1].text);
        }

        [Test]
        public void BindSelection_UsesInputDisplayMapperForActiveSchemeLabels()
        {
            var hud = CreateHud(out var slots, out var frames);
            var labels = new List<Text>();
            var slotViews = new List<CardTimeSelectionSlotUI>();
            for (var index = 0; index < slots.Count; index++)
            {
                var label = slots[index].AddComponent<Text>();
                labels.Add(label);
                var view = slots[index].AddComponent<CardTimeSelectionSlotUI>();
                view.Configure(cardName: null, commandName: label, frameGraphic: frames[index]);
                slotViews.Add(view);
            }

            var profile = CreateGamepadSchemeProfile();
            var mapper = CreateAsset<CardTimeInputDisplayMapperSO>("Mapper");
            mapper.ConfigurePrototypeDefaults();
            hud.Configure(
                resourceWallet: null,
                energy: null,
                config: null,
                profile,
                mapper,
                schemeId: "GamepadDefault",
                group: null,
                backdrop: null,
                category: null,
                energyValue: null,
                slots,
                slotViews,
                labels: null,
                frames);

            hud.BindSelection(CreateTransaction(candidateCount: 2));

            Assert.AreEqual("LB", labels[0].text);
            Assert.AreEqual("RB", labels[1].text);
        }

        [Test]
        public void BindSelection_ShowsAndHidesBackdropWithSelection()
        {
            var hud = CreateHud(out var slots, out var frames);
            var backdropOwner = new GameObject("Backdrop");
            objectsToDestroy.Add(backdropOwner);
            var backdrop = backdropOwner.AddComponent<CanvasGroup>();
            hud.Configure(
                resourceWallet: null,
                energy: null,
                config: null,
                schemeProfile: null,
                displayMapper: null,
                schemeId: null,
                group: null,
                backdrop,
                category: null,
                energyValue: null,
                slots,
                views: null,
                labels: null,
                frames);

            hud.BindSelection(CreateTransaction(candidateCount: 1));
            Assert.AreEqual(1f, backdrop.alpha);

            hud.ClearSelection();
            Assert.AreEqual(0f, backdrop.alpha);
            Assert.IsFalse(backdrop.blocksRaycasts);
        }

        [Test]
        public void BindSelection_CreatesFallbackBackdropWhenMissing()
        {
            var hud = CreateHud(out var slots, out var frames);
            hud.Configure(
                resourceWallet: null,
                energy: null,
                config: null,
                schemeProfile: null,
                displayMapper: null,
                schemeId: null,
                group: null,
                backdrop: null,
                category: null,
                energyValue: null,
                slots,
                views: null,
                labels: null,
                frames);

            hud.BindSelection(CreateTransaction(candidateCount: 1));

            var backdrop = hud.transform.parent
                .Find("Card Selection Backdrop")
                ?.GetComponent<CanvasGroup>();
            Assert.NotNull(backdrop);
            Assert.AreEqual(1f, backdrop.alpha);
            Assert.IsFalse(backdrop.blocksRaycasts);
        }

        private CardTimeSelectionHudUI CreateHud(
            out List<GameObject> slots,
            out List<Graphic> frames)
        {
            var parent = new GameObject("Hud Parent");
            objectsToDestroy.Add(parent);
            var owner = new GameObject("Hud");
            owner.transform.SetParent(parent.transform);
            objectsToDestroy.Add(owner);
            var hud = owner.AddComponent<CardTimeSelectionHudUI>();
            slots = new List<GameObject>();
            frames = new List<Graphic>();
            for (var index = 0; index < 3; index++)
            {
                var slot = new GameObject($"Slot {index}");
                objectsToDestroy.Add(slot);
                slots.Add(slot);
                var image = slot.AddComponent<Image>();
                image.color = Color.black;
                frames.Add(image);
            }

            return hud;
        }

        private CardTimeSelectionUiConfigSO CreateConfig(int slotCount)
        {
            var config = CreateAsset<CardTimeSelectionUiConfigSO>("Config");
            var serialized = new UnityEditor.SerializedObject(config);
            var categories = serialized.FindProperty("categories");
            categories.arraySize = 1;
            var category = categories.GetArrayElementAtIndex(0);
            category.FindPropertyRelative("category").intValue = (int)PlayerCardTimeState.Neutral;
            category.FindPropertyRelative("slotCount").intValue = slotCount;
            category.FindPropertyRelative("slotCommands").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private CardTimeControlSchemeProfileSO CreateSchemeProfile()
        {
            var scheme = CreateAsset<CardTimeControlSchemeSO>("KeyboardWASD");
            var bindings = new List<CardTimeControlSchemeSlotBinding>();
            var labels = new[] { "Q", "E" };
            for (var index = 0; index < labels.Length; index++)
            {
                var binding = new CardTimeControlSchemeSlotBinding();
                binding.Configure(index, $"CardSlotKeyboardWasd{index + 1}", labels[index], string.Empty);
                bindings.Add(binding);
            }

            var layout = new CardTimeControlSchemeCategoryLayout();
            layout.Configure(PlayerCardTimeState.Neutral, 2, bindings);
            scheme.Configure(
                "KeyboardWASD",
                "Keyboard WASD",
                CardTimeControlDeviceFamily.KeyboardMouse,
                string.Empty,
                new[] { layout });
            var profile = CreateAsset<CardTimeControlSchemeProfileSO>("Profile");
            profile.Configure(scheme, new[] { scheme });
            return profile;
        }

        private CardTimeControlSchemeProfileSO CreateGamepadSchemeProfile()
        {
            var scheme = CreateAsset<CardTimeControlSchemeSO>("GamepadDefault");
            var bindings = new List<CardTimeControlSchemeSlotBinding>();
            for (var index = 0; index < 2; index++)
            {
                var binding = new CardTimeControlSchemeSlotBinding();
                binding.Configure(index, $"CardSlotGamepad{index + 1}", $"G{index + 1}", string.Empty);
                bindings.Add(binding);
            }

            var layout = new CardTimeControlSchemeCategoryLayout();
            layout.Configure(PlayerCardTimeState.Neutral, 2, bindings);
            scheme.Configure(
                "GamepadDefault",
                "Gamepad Default",
                CardTimeControlDeviceFamily.Gamepad,
                string.Empty,
                new[] { layout });
            var profile = CreateAsset<CardTimeControlSchemeProfileSO>("GamepadProfile");
            profile.Configure(scheme, new[] { scheme });
            return profile;
        }

        private CardTimeSelectionTransaction CreateTransaction(int candidateCount)
        {
            var cards = new List<CardDefinitionSO>();
            for (var index = 0; index < candidateCount; index++)
            {
                cards.Add(CreateCard($"neutral.{index}", PlayerCardTimeState.Neutral));
            }

            var catalog = CreateAsset<CardCatalogSO>("Catalog");
            catalog.Configure(cards);
            CardTimeSelectionTransaction.TryCreate(
                PlayerCardTimeState.Neutral,
                sessionId: 1,
                candidateIds: cards.ConvertAll(card => card.Id),
                catalog,
                out var transaction);
            return transaction;
        }

        private CardDefinitionSO CreateCard(string id, PlayerCardTimeState category)
        {
            var card = CreateAsset<CardDefinitionSO>(id);
            card.Configure(id, id, string.Empty, category, costs: null, effectDefinition: null);
            return card;
        }

        private T CreateAsset<T>(string name) where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            instance.name = name;
            objectsToDestroy.Add(instance);
            return instance;
        }
    }
}
