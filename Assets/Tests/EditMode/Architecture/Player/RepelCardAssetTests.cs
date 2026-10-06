using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class RepelCardAssetTests
    {
        private const string CardPath = "Assets/Data/Cards/Definitions/Card_Chain_Repel.asset";
        private const string ProfilePath = "Assets/Data/Cards/Inventory/RepelTestCardInventory.asset";
        private static void Setup(string method)
        {
            var type = Type.GetType("TicGame.Architecture.EditorTools.RepelCardSetup, TicGame.Architecture.Editor");
            Assert.NotNull(type, "Repel's deterministic asset setup must exist.");
            type.GetMethod(method).Invoke(null, null);
        }

        [Test]
        public void Setup_AuthorsValidCardAndLeavesProductionLoadoutAlone()
        {
            var original = AssetDatabase.LoadAssetAtPath<PlayerCardInventoryProfileSO>("Assets/Data/Cards/Inventory/TestCardInventory.asset");
            Assert.NotNull(original); var before = JsonUtility.ToJson(original);
            Setup("CreateOrUpdateRepelAssets"); Setup("CreateOrUpdateRepelAssets");
            var card = AssetDatabase.LoadAssetAtPath<CardDefinitionSO>(CardPath);
            Assert.NotNull(card); Assert.AreEqual("repel", card.Id); Assert.AreEqual("Repel", card.DisplayName);
            Assert.AreEqual(PlayerCardTimeState.Chain, card.Category); Assert.AreEqual(CardConsumptionPolicy.Reusable, card.ConsumptionPolicy);
            Assert.AreEqual(15, card.FixedCosts.Single().Amount); Assert.AreEqual(3, card.Effect.CommitOperations.Single().Amount);
            Assert.AreEqual((CardOperationKind)180, card.Effect.CommitOperations.Single().Kind);
            Assert.IsEmpty(card.GetValidationErrors()); Assert.AreEqual(before, JsonUtility.ToJson(original));
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalogSO>("Assets/Data/Cards/Inventory/TestCardCatalog.asset");
            Assert.AreEqual(1, catalog.Cards.Count(c => c != null && c.Id == "repel"));
            var profile = AssetDatabase.LoadAssetAtPath<PlayerCardInventoryProfileSO>(ProfilePath);
            Assert.AreEqual(1, profile.GetEquippedCards(PlayerCardTimeState.Chain).Count(c => c.Id == "repel"));
            Assert.IsEmpty(profile.GetValidationErrors());
        }

        [Test]
        public void DescriptionFollowsAuthoredDuration()
        {
            Setup("CreateOrUpdateRepelAssets");
            var card = AssetDatabase.LoadAssetAtPath<CardDefinitionSO>(CardPath);
            var effect = card.Effect; var original = JsonUtility.ToJson(effect);
            try
            {
                effect.Configure(null, null, new[] { new CardOperationDefinition((CardOperationKind)180, amount: 1.5f) },
                    null, new[] { new CardLifetimeDefinition(CardLifetimeKind.Immediate) }, new CardStackingDefinition(CardStackingKind.RejectIfActive));
                StringAssert.Contains($"{1.5f:0.##} seconds", card.Description);
            }
            finally { JsonUtility.FromJsonOverwrite(original, effect); }
        }

        [Test]
        public void SetupTwice_PreservesSceneAndAddsOneCapabilityWithFocusedInventory()
        {
            Setup("CreateOrUpdateRepelPlaytest"); Setup("CreateOrUpdateRepelPlaytest");
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Test_BatRepel.unity");
            Assert.IsTrue(scene.isLoaded);
            var players = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerController>()).ToArray();
            Assert.AreEqual(1, players.Length); Assert.AreEqual(1, players[0].GetComponents<PlayerRepelRuntime>().Length);
            var serialized = new SerializedObject(players[0]);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<PlayerCardInventoryProfileSO>(ProfilePath), serialized.FindProperty("cardInventoryProfile").objectReferenceValue);
            var bats = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BatMachineBrain>()).ToArray();
            Assert.AreEqual(1, bats.Length);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
