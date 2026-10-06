using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class RepelCardCommitTests
    {
        private readonly List<Object> objects = new();
        private GameObject player;
        private PlayerCardRuntime cards;
        private PlayerRepelRuntime repel;
        private PlayerResourceWallet wallet;
        private ResourceDefinitionSO energy;
        private CardDefinitionSO card;
        private CardEffectDefinitionSO effect;
        private CardTimeSelectionTransaction selection;
        [SetUp]
        public void SetUp()
        {
            player = Track(new GameObject("Player")); player.AddComponent<SimpleHealth>().Initialize();
            energy = Track(ScriptableObject.CreateInstance<ResourceDefinitionSO>());
            wallet = player.AddComponent<PlayerResourceWallet>(); wallet.ConfigureSingleResource(energy, 100, 100);
            var combat = player.AddComponent<PlayerCombatEffects>(); combat.ConfigureResources(wallet, energy);
            var extra = player.AddComponent<PlayerExtraJumpRuntime>();
            repel = player.AddComponent<PlayerRepelRuntime>();
            cards = player.AddComponent<PlayerCardRuntime>(); cards.Configure(wallet, combat, extra);
            effect = Track(ScriptableObject.CreateInstance<CardEffectDefinitionSO>());
            effect.Configure(null, null, new[] { new CardOperationDefinition((CardOperationKind)180, amount: 3f) },
                null, new[] { new CardLifetimeDefinition(CardLifetimeKind.Immediate) }, new CardStackingDefinition(CardStackingKind.RejectIfActive));
            card = Track(ScriptableObject.CreateInstance<CardDefinitionSO>()); ConfigureCost(15); cards.EquipCard(card);
        }
        [TearDown]
        public void TearDown() { Time.timeScale = 1; selection?.Dispose(); for (var i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]); objects.Clear(); }
        private T Track<T>(T value) where T : Object { objects.Add(value); return value; }
        private void ConfigureCost(float amount) => card.Configure("repel", "Repel", "", PlayerCardTimeState.Chain, new[] { new ResourceAmount(energy, amount) }, effect);
        private void ConfigureDuration(float duration) => effect.Configure(null, null, new[] { new CardOperationDefinition((CardOperationKind)180, amount: duration) }, null, new[] { new CardLifetimeDefinition(CardLifetimeKind.Immediate) }, new CardStackingDefinition(CardStackingKind.RejectIfActive));
        private CardReadinessResult Prepare(PlayerCardTimeState category = PlayerCardTimeState.Chain, float snapshotEnergy = 100)
        {
            selection?.Dispose(); var catalog = Track(ScriptableObject.CreateInstance<CardCatalogSO>()); catalog.Configure(new[] { card });
            CardTimeSelectionTransaction.TryCreate(PlayerCardTimeState.Chain, 1, new[] { card.Id }, catalog, out selection);
            var snapshot = new PlayerCardCommitSnapshot(category, null, true, 5, 5, new[] { new PlayerCardResourceSnapshot(energy, snapshotEnergy, 100) });
            return cards.TryPrepare(card, selection, snapshot);
        }

        [Test]
        public void PreparedRepelPaysOnce_AndAlreadyAppliedCannotRearm()
        {
            var result = Prepare(); Assert.IsTrue(result.Succeeded); Assert.IsTrue(result.Commit.TryApply());
            Assert.AreEqual(85, wallet.GetCurrent(energy)); Assert.IsTrue(repel.IsActive);
            repel.Clear(); Assert.IsFalse(result.Commit.TryApply()); Assert.AreEqual(CardCommitFailure.AlreadyApplied, result.Commit.Failure);
            Assert.AreEqual(85, wallet.GetCurrent(energy)); Assert.IsFalse(repel.IsActive);
        }

        [Test]
        public void ReentrantWalletNotificationSeesCoherentGuard_AndCannotPayAgain()
        {
            var first = Prepare(); var second = cards.TryPrepare(card, selection, ((PreparedCardCommit)first.Commit).Snapshot);
            Assert.IsTrue(first.Succeeded); Assert.IsTrue(second.Succeeded);
            var notifications = 0; var armedAtNotification = false; var nested = true;
            wallet.Changed += (_, _, _) => { notifications++; armedAtNotification = repel.IsActive; nested = second.Commit.TryApply(); };
            Assert.IsTrue(first.Commit.TryApply()); Assert.IsTrue(armedAtNotification); Assert.IsFalse(nested);
            Assert.AreEqual(1, notifications); Assert.AreEqual(85, wallet.GetCurrent(energy));
        }

        [TestCase(true)] [TestCase(false)]
        public void CostOrConfigurationEditRejectsStaleQuoteWithoutPayment_ThenFreshPreparationSucceeds(bool editCost)
        {
            var prepared = Prepare(); Assert.IsTrue(prepared.Succeeded);
            if (editCost) ConfigureCost(25); else ConfigureDuration(1f);
            Assert.IsFalse(prepared.Commit.TryApply()); Assert.AreEqual(100, wallet.GetCurrent(energy)); Assert.IsFalse(repel.IsActive);
            var fresh = Prepare(); Assert.IsTrue(fresh.Succeeded); Assert.IsTrue(fresh.Commit.TryApply());
            Assert.AreEqual(editCost ? 75 : 85, wallet.GetCurrent(energy));
        }

        [Test]
        public void ActiveGuardAndInsufficientSnapshotOrLiveEnergyRejectWithoutSideEffects()
        {
            Assert.IsFalse(Prepare(snapshotEnergy: 14).Succeeded);
            var prepared = Prepare(); Assert.IsTrue(prepared.Succeeded);
            wallet.TrySpend(new[] { new ResourceAmount(energy, 90) }); Assert.IsFalse(prepared.Commit.TryApply()); Assert.IsFalse(repel.IsActive);
            wallet.Gain(energy, 90); repel.Arm(3); Assert.IsFalse(Prepare().Succeeded); Assert.AreEqual(100, wallet.GetCurrent(energy));
        }

        [Test]
        public void CancelledSelectionWrongCategoryAndDeadPlayerCannotPayOrArm()
        {
            Assert.IsFalse(Prepare(PlayerCardTimeState.Neutral).Succeeded);
            var prepared = Prepare(); Assert.IsTrue(prepared.Succeeded); selection.Dispose(); Assert.IsFalse(prepared.Commit.TryApply());
            prepared = Prepare(); Assert.IsTrue(prepared.Succeeded);
            player.GetComponent<SimpleHealth>().ApplyDamage(new DamageContext(null, player, null, 5, Vector2.zero, Vector2.right));
            Assert.IsFalse(prepared.Commit.TryApply()); Assert.AreEqual(100, wallet.GetCurrent(energy)); Assert.IsFalse(repel.IsActive);
        }

        [Test]
        public void LegacyDirectCommitUsesTheSameRepelTransactionAndRejectsActiveGuard()
        {
            Assert.IsTrue(cards.Commit(PlayerCardTimeState.Chain, null, true)); Assert.AreEqual(85, wallet.GetCurrent(energy));
            Assert.IsFalse(cards.Commit(PlayerCardTimeState.Chain, null, true)); Assert.AreEqual(85, wallet.GetCurrent(energy));
        }

        [Test]
        public void RemovedEffectAfterPreparationRejectsWithoutThrowingOrSpending()
        {
            var prepared = Prepare(); Assert.IsTrue(prepared.Succeeded);
            card.Configure(card.Id, "Repel", "", PlayerCardTimeState.Chain, card.FixedCosts, null);
            Assert.IsFalse(prepared.Commit.TryApply()); Assert.AreEqual(100, wallet.GetCurrent(energy)); Assert.IsFalse(repel.IsActive);
        }

        [Test]
        public void GuardClearedDuringArmNotificationStillCannotReenterPayment()
        {
            var first = Prepare(); var second = cards.TryPrepare(card, selection, first.Commit.Snapshot);
            var nested = true; repel.Armed += () => { repel.Clear(); nested = second.Commit.TryApply(); };
            Assert.IsTrue(first.Commit.TryApply()); Assert.IsFalse(nested); Assert.AreEqual(85, wallet.GetCurrent(energy));
        }

        [Test]
        public void PreparedCommitRejectsHeldOrPausedPlayerBeforeAnyPayment()
        {
            var prepared = Prepare(); Assert.IsTrue(prepared.Succeeded);
            Time.timeScale = 0; Assert.IsFalse(prepared.Commit.TryApply()); Time.timeScale = 1;
            using (player.AddComponent<PlayerWorldHold>().Acquire()) Assert.IsFalse(prepared.Commit.TryApply());
            Assert.AreEqual(100, wallet.GetCurrent(energy)); Assert.IsFalse(repel.IsActive);
            Assert.IsTrue(prepared.Commit.TryApply());
        }

        [Test]
        public void FeedbackShowsRemainingTime_AndExpiryRemovesItOnce()
        {
            var channel = Track(ScriptableObject.CreateInstance<CardFeedbackEventChannelSO>());
            var service = player.AddComponent<CardFeedbackService>(); service.Configure(channel); service.Initialize();
            var servicesRoot = Track(new GameObject("Services")).AddComponent<GameplayServicesRoot>();
            typeof(GameplayServicesRoot).GetProperty("CardFeedback").SetValue(servicesRoot, service);
            repel.BindGameplayServices(servicesRoot); cards.BindGameplayServices(servicesRoot);
            var expiryCount = 0; channel.Raised += feedback => { if (feedback.Kind == CardFeedbackKind.Expired) expiryCount++; };
            Assert.IsTrue(Prepare().Commit.TryApply()); Assert.AreEqual($"{3f:0.0}s", service.GetHudEffects()[0].DisplayText);
            repel.Tick(1); Assert.AreEqual($"{2f:0.0}s", service.GetHudEffects()[0].DisplayText);
            repel.Tick(2); repel.Tick(1); Assert.IsEmpty(service.GetHudEffects()); Assert.AreEqual(1, expiryCount);
        }
    }
}
