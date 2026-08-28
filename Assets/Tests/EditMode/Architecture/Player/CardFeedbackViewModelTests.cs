using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class CardFeedbackViewModelTests
    {
        [Test]
        public void CardDefinition_IconFallback_IsSafeWhenMissing()
        {
            var card = ScriptableObject.CreateInstance<CardDefinitionSO>();
            card.Configure(
                stableId: "card.test",
                nameForDisplay: "Test Card",
                cardDescription: "",
                cardTimeCategory: PlayerCardTimeState.Neutral,
                costs: null,
                effectDefinition: null);

            Assert.That(card.Icon, Is.Null);

            var hud = new CardHudEffectViewModel(
                effectKey: "card.test.effect",
                sourceObject: null,
                card: card);
            var world = new CardWorldFeedbackViewModel(
                card: card,
                sourceObject: null,
                kind: CardFeedbackKind.Activated);

            Assert.That(hud.Icon, Is.Null);
            Assert.That(world.Icon, Is.Null);
            Assert.That(hud.IsValid, Is.True);
            Assert.That(world.IsValid, Is.True);
        }

        [Test]
        public void HudViewModel_ClampsLifetimeAndKeepsFormattedText()
        {
            var model = new CardHudEffectViewModel(
                effectKey: "chain",
                sourceObject: null,
                card: null,
                displayText: "x3",
                normalizedLifetime: 2f,
                hasLifetime: true,
                visualState: CardHudEffectVisualState.Expiring,
                blinkWhenExpiring: true);

            Assert.That(model.DisplayText, Is.EqualTo("x3"));
            Assert.That(model.NormalizedLifetime, Is.EqualTo(1f));
            Assert.That(model.HasLifetime, Is.True);
            Assert.That(model.VisualState, Is.EqualTo(CardHudEffectVisualState.Expiring));
            Assert.That(model.BlinkWhenExpiring, Is.True);
        }

        [Test]
        public void WorldViewModel_ClampsDisplayDurationAndTracksExplicitPosition()
        {
            var position = new Vector3(1f, 2f, 3f);
            var model = new CardWorldFeedbackViewModel(
                card: null,
                sourceObject: null,
                kind: CardFeedbackKind.Triggered,
                anchor: CardFeedbackAnchor.HitPoint,
                worldPosition: position,
                displaySeconds: -1f);

            Assert.That(model.DisplaySeconds, Is.EqualTo(0f));
            Assert.That(model.HasExplicitWorldPosition, Is.True);
            Assert.That(model.WorldPosition.Value, Is.EqualTo(position));
            Assert.That(model.Anchor, Is.EqualTo(CardFeedbackAnchor.HitPoint));
        }
    }
}
