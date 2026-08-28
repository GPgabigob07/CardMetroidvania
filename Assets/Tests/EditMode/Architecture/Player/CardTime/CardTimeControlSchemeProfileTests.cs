using System.Collections.Generic;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class CardTimeControlSchemeProfileTests
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
        public void ResolveScheme_UsesStableIdOrFallsBackToDefault()
        {
            var wasd = CreateScheme("KeyboardWASD", "CardSlotKeyboardWasd", "Q");
            var arrows = CreateScheme("KeyboardArrows", "CardSlotKeyboardArrows", "A");
            var profile = CreateAsset<CardTimeControlSchemeProfileSO>("Profile");
            profile.Configure(wasd, new[] { wasd, arrows });

            Assert.AreSame(arrows, profile.ResolveScheme("KeyboardArrows"));
            Assert.AreSame(wasd, profile.ResolveScheme("missing"));
            Assert.AreSame(wasd, profile.ResolveScheme(null));
        }

        [Test]
        public void Scheme_ResolvesSlotCountActionNameAndLabel()
        {
            var scheme = CreateScheme("KeyboardWASD", "CardSlotKeyboardWasd", "Q");

            Assert.AreEqual(8, scheme.GetSlotCount(PlayerCardTimeState.Neutral));
            Assert.AreEqual(6, scheme.GetSlotCount(PlayerCardTimeState.Chain));
            Assert.AreEqual(4, scheme.GetSlotCount(PlayerCardTimeState.Finisher));
            Assert.AreEqual(
                "CardSlotKeyboardWasd3",
                scheme.GetActionName(PlayerCardTimeState.Neutral, 2));
            Assert.AreEqual("Q3", scheme.GetDisplayLabel(PlayerCardTimeState.Neutral, 2));
        }

        private CardTimeControlSchemeSO CreateScheme(
            string id,
            string actionPrefix,
            string labelPrefix)
        {
            var scheme = CreateAsset<CardTimeControlSchemeSO>(id);
            scheme.Configure(
                id,
                id,
                CardTimeControlDeviceFamily.KeyboardMouse,
                string.Empty,
                new[]
                {
                    CreateLayout(PlayerCardTimeState.Neutral, 8, actionPrefix, labelPrefix),
                    CreateLayout(PlayerCardTimeState.Chain, 6, actionPrefix, labelPrefix),
                    CreateLayout(PlayerCardTimeState.Finisher, 4, actionPrefix, labelPrefix)
                });
            return scheme;
        }

        private static CardTimeControlSchemeCategoryLayout CreateLayout(
            PlayerCardTimeState category,
            int slotCount,
            string actionPrefix,
            string labelPrefix)
        {
            var bindings = new List<CardTimeControlSchemeSlotBinding>();
            for (var index = 0; index < slotCount; index++)
            {
                var binding = new CardTimeControlSchemeSlotBinding();
                binding.Configure(
                    index,
                    $"{actionPrefix}{index + 1}",
                    $"{labelPrefix}{index + 1}",
                    $"<Keyboard>/{index + 1}");
                bindings.Add(binding);
            }

            var layout = new CardTimeControlSchemeCategoryLayout();
            layout.Configure(category, slotCount, bindings);
            return layout;
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
