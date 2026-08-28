using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace TicGame.Architecture.Tests
{
    public sealed class CardFeedbackPresenterTests
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
        public void HudPresenter_RendersViewModelTextWithoutPlayerCounters()
        {
            var service = CreateFeedbackService();
            service.UpsertHudEffect(new CardHudEffectViewModel(
                effectKey: "chain",
                sourceObject: null,
                card: null,
                displayText: "x3"));
            var presenterObject = CreateObject("Presenter");
            var presenter = presenterObject.AddComponent<CardHudEffectIndicatorPresenter>();
            var indicator = CreateIndicator(out var label);

            presenter.Configure(new[] { indicator });
            presenter.BindGameplayServices(new FakeGameplayServices(service));
            presenter.Refresh();

            Assert.IsTrue(indicator.gameObject.activeSelf);
            Assert.AreEqual("x3", label.text);
        }

        [Test]
        public void WorldPresenter_UsesHitPointWhenProvided()
        {
            var presenterObject = CreateObject("Presenter");
            var presenter = presenterObject.AddComponent<CardWorldFeedbackPresenter>();
            var source = CreateObject("Source");
            source.transform.position = new Vector3(3f, 4f, 0f);
            var hitPoint = new Vector3(9f, 8f, 0f);

            var position = presenter.ResolveWorldPosition(
                new CardWorldFeedbackViewModel(
                    card: null,
                    sourceObject: source,
                    kind: CardFeedbackKind.Triggered,
                    anchor: CardFeedbackAnchor.HitPoint,
                    worldPosition: hitPoint));

            Assert.AreEqual(hitPoint, position);
        }

        [Test]
        public void WorldPresenter_FallsBackToSourceHead()
        {
            var presenterObject = CreateObject("Presenter");
            var presenter = presenterObject.AddComponent<CardWorldFeedbackPresenter>();
            var source = CreateObject("Source");
            source.transform.position = new Vector3(3f, 4f, 0f);

            var position = presenter.ResolveWorldPosition(
                new CardWorldFeedbackViewModel(
                    card: null,
                    sourceObject: source,
                    kind: CardFeedbackKind.Failed,
                    anchor: CardFeedbackAnchor.SourceHead));

            Assert.AreEqual(new Vector3(3f, 5.65f, 0f), position);
        }

        private CardHudEffectIndicatorUI CreateIndicator(out Text label)
        {
            var owner = CreateObject("Indicator");
            var image = owner.AddComponent<Image>();
            label = owner.AddComponent<Text>();
            var group = owner.AddComponent<CanvasGroup>();
            var indicator = owner.AddComponent<CardHudEffectIndicatorUI>();
            indicator.Configure(image, label, group);
            return indicator;
        }

        private CardFeedbackService CreateFeedbackService()
        {
            var owner = CreateObject("Feedback");
            var channel = ScriptableObject.CreateInstance<CardFeedbackEventChannelSO>();
            objectsToDestroy.Add(channel);
            var service = owner.AddComponent<CardFeedbackService>();
            service.Configure(channel);
            service.Initialize();
            return service;
        }

        private GameObject CreateObject(string name)
        {
            var instance = new GameObject(name);
            objectsToDestroy.Add(instance);
            return instance;
        }

        private sealed class FakeGameplayServices : IGameplayServices
        {
            public FakeGameplayServices(ICardFeedbackService cardFeedback)
            {
                CardFeedback = cardFeedback;
            }

            public IGameplayTimeService Time => null;
            public IHitStopService HitStop => null;
            public HitStopRequestEventChannelSO HitStopRequests => null;
            public ICardTimeSession CardTime => null;
            public CardTimeSessionEventChannelSO CardTimeTransitions => null;
            public ICardFeedbackService CardFeedback { get; }
            public IGameStateService GameState => null;
        }
    }
}
