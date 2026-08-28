using System.Collections.Generic;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerCardInventoryProfileTests
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
        public void TryEquip_RequiresOwnershipAndMatchingCapacity()
        {
            var profile = CreateAsset<PlayerCardInventoryProfileSO>("Inventory");
            profile.EnsureDefaultLoadouts();
            var neutralCards = new List<CardDefinitionSO>();
            for (var index = 0; index < 9; index++)
            {
                neutralCards.Add(CreateCard($"Neutral{index}", PlayerCardTimeState.Neutral));
            }

            Assert.IsFalse(profile.TryEquip(neutralCards[0]));

            foreach (var card in neutralCards)
            {
                Assert.IsTrue(profile.TryAddOwnedCard(card));
            }

            for (var index = 0; index < 8; index++)
            {
                Assert.IsTrue(profile.TryEquip(neutralCards[index]));
            }

            Assert.IsFalse(profile.TryEquip(neutralCards[8]));
            Assert.AreEqual(8, profile.GetEquippedCards(PlayerCardTimeState.Neutral).Count);
        }

        [Test]
        public void ExportSaveData_UsesStableIds()
        {
            var profile = CreateAsset<PlayerCardInventoryProfileSO>("Inventory");
            var neutral = CreateCard("neutral.card", PlayerCardTimeState.Neutral);
            var chain = CreateCard("chain.card", PlayerCardTimeState.Chain);

            profile.EnsureDefaultLoadouts();
            profile.TryAddOwnedCard(neutral);
            profile.TryAddOwnedCard(chain);
            profile.TryEquip(neutral);
            profile.TryEquip(chain);

            var saveData = profile.ExportSaveData();

            CollectionAssert.Contains(saveData.ownedCardIds, "neutral.card");
            CollectionAssert.Contains(saveData.ownedCardIds, "chain.card");
            Assert.AreEqual(
                "neutral.card",
                saveData.loadouts
                    .Find(loadout => loadout.category == PlayerCardTimeState.Neutral)
                    .equippedCardIds[0]);
            Assert.AreEqual(
                "chain.card",
                saveData.loadouts
                    .Find(loadout => loadout.category == PlayerCardTimeState.Chain)
                    .equippedCardIds[0]);
        }

        [Test]
        public void ApplySaveData_RebuildsOwnedCardsAndLoadoutsFromCatalog()
        {
            var profile = CreateAsset<PlayerCardInventoryProfileSO>("Inventory");
            var neutral = CreateCard("neutral.card", PlayerCardTimeState.Neutral);
            var chain = CreateCard("chain.card", PlayerCardTimeState.Chain);
            var finisher = CreateCard("finisher.card", PlayerCardTimeState.Finisher);
            var saveData = new CardInventorySaveData
            {
                ownedCardIds = new List<string> { neutral.Id, chain.Id },
                loadouts = new List<CardLoadoutSaveData>
                {
                    new()
                    {
                        category = PlayerCardTimeState.Neutral,
                        equippedCardIds = new List<string> { neutral.Id }
                    },
                    new()
                    {
                        category = PlayerCardTimeState.Finisher,
                        equippedCardIds = new List<string> { finisher.Id }
                    }
                }
            };

            profile.ApplySaveData(saveData, new[] { neutral, chain, finisher });

            Assert.IsTrue(profile.Owns(neutral));
            Assert.IsTrue(profile.Owns(chain));
            Assert.IsFalse(profile.Owns(finisher));
            CollectionAssert.Contains(
                profile.GetEquippedCards(PlayerCardTimeState.Neutral),
                neutral);
            Assert.AreEqual(0, profile.GetEquippedCards(PlayerCardTimeState.Finisher).Count);
        }

        [Test]
        public void Catalog_ResolvesStableIdsAndRejectsMissingIds()
        {
            var catalog = CreateAsset<CardCatalogSO>("Catalog");
            var neutral = CreateCard("neutral.card", PlayerCardTimeState.Neutral);
            var chain = CreateCard("chain.card", PlayerCardTimeState.Chain);
            catalog.Configure(new[] { neutral, chain });

            Assert.IsTrue(catalog.TryGetCard("neutral.card", out var resolved));
            Assert.AreSame(neutral, resolved);
            Assert.IsFalse(catalog.TryGetCard("missing.card", out resolved));
            Assert.IsNull(resolved);
        }

        [Test]
        public void LoadoutProvider_ReturnsEquippedIdsInPresentationOrder()
        {
            var profile = CreateAsset<PlayerCardInventoryProfileSO>("Inventory");
            var first = CreateCard("neutral.first", PlayerCardTimeState.Neutral);
            var second = CreateCard("neutral.second", PlayerCardTimeState.Neutral);

            profile.EnsureDefaultLoadouts();
            profile.TryAddOwnedCard(first);
            profile.TryAddOwnedCard(second);
            profile.TryEquip(first);
            profile.TryEquip(second);

            CollectionAssert.AreEqual(
                new[] { "neutral.first", "neutral.second" },
                profile.GetEquippedCardIds(PlayerCardTimeState.Neutral));
        }

        private CardDefinitionSO CreateCard(string id, PlayerCardTimeState category)
        {
            var card = CreateAsset<CardDefinitionSO>(id);
            var status = CreateAsset<CardStatusDefinitionSO>($"{id}.status");
            var effect = CreateAsset<CardEffectDefinitionSO>($"{id}.effect");
            effect.Configure(
                statusDefinition: status,
                conditions: null,
                operations: new[]
                {
                    new CardOperationDefinition(
                        CardOperationKind.AddStatusCharges,
                        status: status,
                        amount: 1f)
                },
                rules: null,
                lifetimeDefinitions: new[]
                {
                    new CardLifetimeDefinition(CardLifetimeKind.Immediate)
                },
                stackingDefinition: new CardStackingDefinition(CardStackingKind.AddCharges));
            card.Configure(
                id,
                id,
                string.Empty,
                category,
                costs: null,
                effectDefinition: effect);
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
