using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class CardTimeInputDisplayMapperTests
    {
        [Test]
        public void ResolveLabel_UsesGamepadDisplayBindingForActiveScheme()
        {
            var mapper = ScriptableObject.CreateInstance<CardTimeInputDisplayMapperSO>();
            mapper.ConfigurePrototypeDefaults();
            var scheme = CreateScheme(CardTimeControlDeviceFamily.Gamepad);

            var label = mapper.ResolveLabel(
                scheme,
                PlayerCardTimeState.Neutral,
                slotIndex: 1);

            Assert.That(label, Is.EqualTo("RB"));

            Object.DestroyImmediate(mapper);
            Object.DestroyImmediate(scheme);
        }

        [Test]
        public void ResolveLabel_FallsBackToSchemeDisplayLabel()
        {
            var mapper = ScriptableObject.CreateInstance<CardTimeInputDisplayMapperSO>();
            var scheme = CreateScheme(CardTimeControlDeviceFamily.KeyboardMouse);

            var label = mapper.ResolveLabel(
                scheme,
                PlayerCardTimeState.Neutral,
                slotIndex: 0);

            Assert.That(label, Is.EqualTo("Q"));

            Object.DestroyImmediate(mapper);
            Object.DestroyImmediate(scheme);
        }

        private static CardTimeControlSchemeSO CreateScheme(
            CardTimeControlDeviceFamily family)
        {
            var binding = new CardTimeControlSchemeSlotBinding();
            binding.Configure(
                index: 0,
                action: family == CardTimeControlDeviceFamily.Gamepad
                    ? "CardSlotGamepad1"
                    : "CardSlotKeyboardWasd1",
                label: "Q",
                controlPath: string.Empty);
            var binding2 = new CardTimeControlSchemeSlotBinding();
            binding2.Configure(
                index: 1,
                action: family == CardTimeControlDeviceFamily.Gamepad
                    ? "CardSlotGamepad2"
                    : "CardSlotKeyboardWasd2",
                label: "E",
                controlPath: string.Empty);
            var layout = new CardTimeControlSchemeCategoryLayout();
            layout.Configure(
                PlayerCardTimeState.Neutral,
                visibleSlotCount: 2,
                slotBindings: new[] { binding, binding2 });
            var scheme = ScriptableObject.CreateInstance<CardTimeControlSchemeSO>();
            scheme.Configure(
                stableId: family.ToString(),
                nameForDisplay: family.ToString(),
                family: family,
                schemeDescription: string.Empty,
                categoryLayouts: new[] { layout });
            return scheme;
        }
    }
}
