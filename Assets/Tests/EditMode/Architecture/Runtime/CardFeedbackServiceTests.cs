using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class CardFeedbackServiceTests
    {
        [Test]
        public void PublishWorldFeedback_RaisesConfiguredEvent()
        {
            var owner = new GameObject("Feedback");
            var service = owner.AddComponent<CardFeedbackService>();
            var channel = ScriptableObject.CreateInstance<CardFeedbackEventChannelSO>();
            var card = ScriptableObject.CreateInstance<CardDefinitionSO>();
            card.Configure(
                stableId: "card.feedback",
                nameForDisplay: "Feedback",
                cardDescription: "",
                cardTimeCategory: PlayerCardTimeState.Neutral,
                costs: null,
                effectDefinition: null);

            CardWorldFeedbackViewModel raised = default;
            channel.Raised += feedback => raised = feedback;

            service.Configure(channel);
            service.Initialize();
            service.PublishWorldFeedback(new CardWorldFeedbackViewModel(
                card: card,
                sourceObject: owner,
                kind: CardFeedbackKind.Activated));

            Assert.That(raised.Card, Is.EqualTo(card));
            Assert.That(raised.SourceObject, Is.EqualTo(owner));
            Assert.That(raised.Kind, Is.EqualTo(CardFeedbackKind.Activated));

            Object.DestroyImmediate(card);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(owner);
        }

        [Test]
        public void UpsertHudEffect_ReplacesModelAndPreservesOrder()
        {
            var owner = new GameObject("Feedback");
            var service = owner.AddComponent<CardFeedbackService>();
            service.Initialize();

            service.UpsertHudEffect(new CardHudEffectViewModel(
                effectKey: "first",
                sourceObject: owner,
                card: null,
                displayText: "1"));
            service.UpsertHudEffect(new CardHudEffectViewModel(
                effectKey: "second",
                sourceObject: owner,
                card: null,
                displayText: "2"));
            service.UpsertHudEffect(new CardHudEffectViewModel(
                effectKey: "first",
                sourceObject: owner,
                card: null,
                displayText: "3"));

            var effects = service.GetHudEffects();
            Assert.That(effects.Count, Is.EqualTo(2));
            Assert.That(effects[0].EffectKey, Is.EqualTo("first"));
            Assert.That(effects[0].DisplayText, Is.EqualTo("3"));
            Assert.That(effects[1].EffectKey, Is.EqualTo("second"));

            Object.DestroyImmediate(owner);
        }

        [Test]
        public void RemoveHudEffectsFromSource_RemovesOnlyMatchingSource()
        {
            var owner = new GameObject("Feedback");
            var other = new GameObject("Other");
            var service = owner.AddComponent<CardFeedbackService>();
            service.Initialize();

            service.UpsertHudEffect(new CardHudEffectViewModel("owner", owner, null));
            service.UpsertHudEffect(new CardHudEffectViewModel("other", other, null));

            service.RemoveHudEffectsFromSource(owner);

            var effects = service.GetHudEffects();
            Assert.That(effects.Count, Is.EqualTo(1));
            Assert.That(effects[0].EffectKey, Is.EqualTo("other"));

            Object.DestroyImmediate(other);
            Object.DestroyImmediate(owner);
        }

        [Test]
        public void MissingEventChannel_DoesNotBreakHudStorageOrWorldPublish()
        {
            var owner = new GameObject("Feedback");
            var service = owner.AddComponent<CardFeedbackService>();
            service.Initialize();

            Assert.DoesNotThrow(() => service.PublishWorldFeedback(
                new CardWorldFeedbackViewModel(
                    card: null,
                    sourceObject: owner,
                    kind: CardFeedbackKind.Failed)));

            service.UpsertHudEffect(new CardHudEffectViewModel("safe", owner, null));

            Assert.That(service.GetHudEffects().Count, Is.EqualTo(1));

            Object.DestroyImmediate(owner);
        }
    }
}
