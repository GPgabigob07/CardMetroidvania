using System.Collections.Generic;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerCardSelectionInputTests
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
        public void TickNavigation_MovesBoundSelectionAndRepeatsAfterDelay()
        {
            var selector = CreateSelector();
            var transaction = CreateTransaction();
            selector.BindSelection(transaction);

            Assert.AreEqual(0, transaction.Current.SelectedIndex);
            Assert.IsTrue(selector.TickNavigation(Vector2.right, 0f));
            Assert.AreEqual(1, transaction.Current.SelectedIndex);
            Assert.IsFalse(selector.TickNavigation(Vector2.right, 0.01f));
            Assert.AreEqual(1, transaction.Current.SelectedIndex);
            Assert.IsTrue(selector.TickNavigation(Vector2.right, 0.25f));
            Assert.AreEqual(2, transaction.Current.SelectedIndex);
        }

        [Test]
        public void ClearSelection_StopsNavigation()
        {
            var selector = CreateSelector();
            var transaction = CreateTransaction();
            selector.BindSelection(transaction);
            selector.ClearSelection(transaction);

            Assert.IsFalse(selector.TickNavigation(Vector2.right, 1f));
            Assert.AreEqual(0, transaction.Current.SelectedIndex);
        }

        [Test]
        public void TryCommandSlot_SelectsIndexAndEmitsCommand()
        {
            var selector = CreateSelector();
            var transaction = CreateTransaction();
            var commanded = default(CardTimeSelectionSlotCommand);
            selector.BindSelection(transaction);
            selector.SlotCommanded += command => commanded = command;

            Assert.IsTrue(selector.TryCommandSlot(2));

            Assert.AreEqual(2, transaction.Current.SelectedIndex);
            Assert.AreEqual(2, commanded.SlotIndex);
            Assert.IsTrue(commanded.Selected);
        }

        [Test]
        public void TryCommandSlot_InvalidIndexEmitsFailedCommandWithoutThrowing()
        {
            var selector = CreateSelector();
            var transaction = CreateTransaction();
            var commanded = default(CardTimeSelectionSlotCommand);
            selector.BindSelection(transaction);
            selector.SlotCommanded += command => commanded = command;

            Assert.IsFalse(selector.TryCommandSlot(7));

            Assert.AreEqual(0, transaction.Current.SelectedIndex);
            Assert.AreEqual(7, commanded.SlotIndex);
            Assert.IsFalse(commanded.Selected);
        }

        [Test]
        public void TryCommandSlot_IgnoresDisposedSelection()
        {
            var selector = CreateSelector();
            var transaction = CreateTransaction();
            var emitted = false;
            selector.BindSelection(transaction);
            selector.SlotCommanded += _ => emitted = true;
            transaction.Dispose();

            Assert.IsFalse(selector.TryCommandSlot(1));

            Assert.IsFalse(emitted);
        }

        private PlayerCardSelectionInput CreateSelector()
        {
            var owner = new GameObject("Selector");
            objectsToDestroy.Add(owner);
            return owner.AddComponent<PlayerCardSelectionInput>();
        }

        private CardTimeSelectionTransaction CreateTransaction()
        {
            var first = CreateCard("neutral.first", PlayerCardTimeState.Neutral);
            var second = CreateCard("neutral.second", PlayerCardTimeState.Neutral);
            var third = CreateCard("neutral.third", PlayerCardTimeState.Neutral);
            var catalog = CreateAsset<CardCatalogSO>("Catalog");
            catalog.Configure(new[] { first, second, third });

            CardTimeSelectionTransaction.TryCreate(
                PlayerCardTimeState.Neutral,
                sessionId: 11,
                candidateIds: new[] { first.Id, second.Id, third.Id },
                catalog,
                out var transaction);
            return transaction;
        }

        private CardDefinitionSO CreateCard(string id, PlayerCardTimeState category)
        {
            var card = CreateAsset<CardDefinitionSO>(id);
            card.Configure(
                id,
                id,
                string.Empty,
                category,
                costs: null,
                effectDefinition: null);
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
