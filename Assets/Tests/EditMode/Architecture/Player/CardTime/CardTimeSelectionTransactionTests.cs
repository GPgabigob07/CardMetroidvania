using System.Collections.Generic;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class CardTimeSelectionTransactionTests
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
        public void TryCreate_RejectsInvalidSession()
        {
            var catalog = CreateAsset<CardCatalogSO>("Catalog");

            Assert.IsFalse(CardTimeSelectionTransaction.TryCreate(
                PlayerCardTimeState.None,
                sessionId: 1,
                candidateIds: null,
                catalog,
                out _));
            Assert.IsFalse(CardTimeSelectionTransaction.TryCreate(
                PlayerCardTimeState.Neutral,
                sessionId: 0,
                candidateIds: null,
                catalog,
                out _));
        }

        [Test]
        public void TryCreate_ResolvesOnlyMatchingCategoryCandidates()
        {
            var neutral = CreateCard("neutral.card", PlayerCardTimeState.Neutral);
            var chain = CreateCard("chain.card", PlayerCardTimeState.Chain);
            var catalog = CreateAsset<CardCatalogSO>("Catalog");
            catalog.Configure(new[] { neutral, chain });

            Assert.IsTrue(CardTimeSelectionTransaction.TryCreate(
                PlayerCardTimeState.Neutral,
                sessionId: 12,
                candidateIds: new[] { "missing.card", "chain.card", "neutral.card" },
                catalog,
                out var transaction));

            Assert.IsTrue(transaction.Current.HasSelection);
            Assert.AreEqual(1, transaction.Current.Candidates.Count);
            Assert.AreSame(neutral, transaction.Current.SelectedCard);
        }

        [Test]
        public void Selection_MovesAndClampsAtEdges()
        {
            var first = CreateCard("neutral.first", PlayerCardTimeState.Neutral);
            var second = CreateCard("neutral.second", PlayerCardTimeState.Neutral);
            var catalog = CreateAsset<CardCatalogSO>("Catalog");
            catalog.Configure(new[] { first, second });
            CardTimeSelectionTransaction.TryCreate(
                PlayerCardTimeState.Neutral,
                sessionId: 3,
                candidateIds: new[] { first.Id, second.Id },
                catalog,
                out var transaction);

            Assert.AreSame(first, transaction.Current.SelectedCard);
            Assert.IsTrue(transaction.MoveSelection(1));
            Assert.AreSame(second, transaction.Current.SelectedCard);
            Assert.IsFalse(transaction.MoveSelection(1));
            Assert.AreSame(second, transaction.Current.SelectedCard);
            Assert.IsTrue(transaction.MoveSelection(-1));
            Assert.AreSame(first, transaction.Current.SelectedCard);
            Assert.IsFalse(transaction.MoveSelection(-1));
        }

        [Test]
        public void Snapshot_RetainsSelectionOutsideUiObjects()
        {
            var card = CreateCard("neutral.card", PlayerCardTimeState.Neutral);
            var catalog = CreateAsset<CardCatalogSO>("Catalog");
            catalog.Configure(new[] { card });
            CardTimeSelectionTransaction.TryCreate(
                PlayerCardTimeState.Neutral,
                sessionId: 7,
                candidateIds: new[] { card.Id },
                catalog,
                out var transaction);

            var snapshot = transaction.Current;

            Assert.IsTrue(snapshot.HasSelection);
            Assert.AreSame(card, snapshot.SelectedCard);
            Assert.AreEqual(7, snapshot.SessionId);
        }

        [Test]
        public void Dispose_InvalidatesSelection()
        {
            var card = CreateCard("neutral.card", PlayerCardTimeState.Neutral);
            var catalog = CreateAsset<CardCatalogSO>("Catalog");
            catalog.Configure(new[] { card });
            CardTimeSelectionTransaction.TryCreate(
                PlayerCardTimeState.Neutral,
                sessionId: 5,
                candidateIds: new[] { card.Id },
                catalog,
                out var transaction);

            transaction.Dispose();

            Assert.IsFalse(transaction.Current.IsValid);
            Assert.IsFalse(transaction.TryGetSelectedCard(out var selected));
            Assert.IsNull(selected);
        }

        private CardDefinitionSO CreateCard(string id, PlayerCardTimeState category)
        {
            var card = CreateAsset<CardDefinitionSO>(id);
            var effect = CreateAsset<CardEffectDefinitionSO>($"{id}.effect");
            var status = CreateAsset<CardStatusDefinitionSO>($"{id}.status");
            effect.Configure(
                status,
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
                effect);
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
