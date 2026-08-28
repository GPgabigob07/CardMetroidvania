using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace TicGame.Architecture.Tests
{
    public sealed class CardTimeHudUITests
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
        public void BindGameplayServices_ShowsInputIndicatorWhenCardTimeAvailable()
        {
            var hud = CreateHud(out _, out var label, out var background);
            hud.BindGameplayServices(new FakeGameplayServices(
                new CardTimeSessionSnapshot(
                    state: CardTimeSessionState.Available,
                    availableCardTime: PlayerCardTimeState.Neutral,
                    sessionCardTime: PlayerCardTimeState.None,
                    activeElapsed: 0f,
                    maximumActiveDuration: 0f)));

            Assert.IsTrue(label.gameObject.activeSelf);
            Assert.IsTrue(background.gameObject.activeSelf);
            Assert.AreEqual("J + K", label.text);
            Assert.IsFalse(background.raycastTarget);
        }

        [Test]
        public void BindGameplayServices_UsesGamepadChordLabelForGamepadScheme()
        {
            var hud = CreateHud(out _, out var label, out _);
            var profile = CreateProfile(CardTimeControlDeviceFamily.Gamepad);
            var mapper = ScriptableObject.CreateInstance<CardTimeInputDisplayMapperSO>();
            objectsToDestroy.Add(mapper);
            mapper.ConfigurePrototypeDefaults();
            hud.Configure(
                cardGraphics: new List<Graphic>(),
                actionLabel: label,
                actionBackground: null,
                schemeProfile: profile,
                displayMapper: mapper,
                schemeId: "GamepadDefault");

            hud.BindGameplayServices(new FakeGameplayServices(
                new CardTimeSessionSnapshot(
                    state: CardTimeSessionState.Available,
                    availableCardTime: PlayerCardTimeState.Chain,
                    sessionCardTime: PlayerCardTimeState.None,
                    activeElapsed: 0f,
                    maximumActiveDuration: 0f)));

            Assert.AreEqual("LB + RB", label.text);
        }

        [Test]
        public void BindGameplayServices_HidesInputIndicatorWhenUnavailable()
        {
            var hud = CreateHud(out _, out var label, out var background);
            hud.BindGameplayServices(new FakeGameplayServices(default));

            Assert.IsFalse(label.gameObject.activeSelf);
            Assert.IsFalse(background.gameObject.activeSelf);
        }

        private CardTimeHudUI CreateHud(
            out List<Graphic> cards,
            out Text label,
            out Graphic background)
        {
            var owner = new GameObject("Card Time Hud");
            objectsToDestroy.Add(owner);
            var hud = owner.AddComponent<CardTimeHudUI>();
            cards = new List<Graphic>();
            for (var index = 0; index < 3; index++)
            {
                var card = new GameObject($"Card {index}");
                objectsToDestroy.Add(card);
                card.transform.SetParent(owner.transform);
                cards.Add(card.AddComponent<Image>());
            }

            var labelObject = new GameObject("Label");
            objectsToDestroy.Add(labelObject);
            label = labelObject.AddComponent<Text>();
            var backgroundObject = new GameObject("Background");
            objectsToDestroy.Add(backgroundObject);
            background = backgroundObject.AddComponent<Image>();
            hud.Configure(
                cards,
                label,
                background,
                schemeProfile: null,
                displayMapper: null);
            return hud;
        }

        private CardTimeControlSchemeProfileSO CreateProfile(
            CardTimeControlDeviceFamily family)
        {
            var scheme = ScriptableObject.CreateInstance<CardTimeControlSchemeSO>();
            objectsToDestroy.Add(scheme);
            scheme.Configure(
                stableId: "GamepadDefault",
                nameForDisplay: "Gamepad Default",
                family: family,
                schemeDescription: string.Empty,
                categoryLayouts: null);
            var profile = ScriptableObject.CreateInstance<CardTimeControlSchemeProfileSO>();
            objectsToDestroy.Add(profile);
            profile.Configure(scheme, new[] { scheme });
            return profile;
        }

        private sealed class FakeGameplayServices : IGameplayServices
        {
            private readonly ICardTimeSession session;

            public FakeGameplayServices(CardTimeSessionSnapshot snapshot)
            {
                session = new FakeCardTimeSession(snapshot);
            }

            public IGameplayTimeService Time => null;
            public IHitStopService HitStop => null;
            public HitStopRequestEventChannelSO HitStopRequests => null;
            public ICardTimeSession CardTime => session;
            public CardTimeSessionEventChannelSO CardTimeTransitions => null;
            public ICardFeedbackService CardFeedback => null;
            public IGameStateService GameState => null;
        }

        private sealed class FakeCardTimeSession : ICardTimeSession
        {
            public FakeCardTimeSession(CardTimeSessionSnapshot current)
            {
                Current = current;
            }

            public CardTimeSessionSnapshot Current { get; }
        }
    }
}
