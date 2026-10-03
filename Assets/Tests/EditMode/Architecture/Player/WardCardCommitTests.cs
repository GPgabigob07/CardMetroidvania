using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class WardCardCommitTests
    {
        private readonly List<Object> objects = new();
        private GameObject player;
        private PlayerCardRuntime cards;
        private PlayerWardRuntime ward;
        private PlayerResourceWallet wallet;
        private ResourceDefinitionSO energy;
        private WardDefinitionSO definition;
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
            definition = Track(ScriptableObject.CreateInstance<WardDefinitionSO>());
            ward = player.AddComponent<PlayerWardRuntime>(); ward.Initialize(definition);
            cards = player.AddComponent<PlayerCardRuntime>(); cards.Configure(wallet, combat, extra);
            effect = Track(ScriptableObject.CreateInstance<CardEffectDefinitionSO>());
            effect.Configure(null, null, new[] { new CardOperationDefinition(CardOperationKind.ArmDirectionalWard, ward: definition) },
                null, new[] { new CardLifetimeDefinition(CardLifetimeKind.Immediate) }, new CardStackingDefinition(CardStackingKind.RejectIfActive));
            card = Track(ScriptableObject.CreateInstance<CardDefinitionSO>()); ConfigureCost(20); cards.EquipCard(card);
        }
        [TearDown]
        public void TearDown() { Time.timeScale = 1; selection?.Dispose(); for (var i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]); objects.Clear(); }
        private T Track<T>(T value) where T : Object { objects.Add(value); return value; }
        private void ConfigureCost(float amount) => card.Configure("card.neutral.ward", "Ward", "", PlayerCardTimeState.Neutral, new[] { new ResourceAmount(energy, amount) }, effect);
        private CardReadinessResult Prepare(PlayerCardTimeState category = PlayerCardTimeState.Neutral, float snapshotEnergy = 100)
        {
            selection?.Dispose(); var catalog = Track(ScriptableObject.CreateInstance<CardCatalogSO>()); catalog.Configure(new[] { card });
            CardTimeSelectionTransaction.TryCreate(PlayerCardTimeState.Neutral, 1, new[] { card.Id }, catalog, out selection);
            var snapshot = new PlayerCardCommitSnapshot(category, null, true, 5, 5, new[] { new PlayerCardResourceSnapshot(energy, snapshotEnergy, 100) });
            return cards.TryPrepare(card, selection, snapshot);
        }

        [Test]
        public void PreparedWardPaysOnce_AndAlreadyAppliedCannotRearm()
        {
            var result = Prepare(); Assert.IsTrue(result.Succeeded); Assert.IsTrue(result.Commit.TryApply());
            Assert.AreEqual(80, wallet.GetCurrent(energy)); Assert.IsTrue(ward.IsActive);
            ward.Clear(); Assert.IsFalse(result.Commit.TryApply()); Assert.AreEqual(CardCommitFailure.AlreadyApplied, result.Commit.Failure);
            Assert.AreEqual(80, wallet.GetCurrent(energy)); Assert.IsFalse(ward.IsActive);
        }

        [Test]
        public void ReentrantWalletNotificationSeesCoherentGuard_AndCannotPayAgain()
        {
            var first = Prepare(); var second = cards.TryPrepare(card, selection, ((PreparedCardCommit)first.Commit).Snapshot);
            Assert.IsTrue(first.Succeeded); Assert.IsTrue(second.Succeeded);
            var notifications = 0; var armedAtNotification = false; var nested = true;
            wallet.Changed += (_, _, _) => { notifications++; armedAtNotification = ward.IsActive; nested = second.Commit.TryApply(); };
            Assert.IsTrue(first.Commit.TryApply()); Assert.IsTrue(armedAtNotification); Assert.IsFalse(nested);
            Assert.AreEqual(1, notifications); Assert.AreEqual(80, wallet.GetCurrent(energy));
        }

        [TestCase(true)] [TestCase(false)]
        public void CostOrConfigurationEditRejectsStaleQuoteWithoutPayment_ThenFreshPreparationSucceeds(bool editCost)
        {
            var prepared = Prepare(); Assert.IsTrue(prepared.Succeeded);
            if (editCost) ConfigureCost(15); else JsonUtility.FromJsonOverwrite("{\"duration\":1}", definition);
            Assert.IsFalse(prepared.Commit.TryApply()); Assert.AreEqual(100, wallet.GetCurrent(energy)); Assert.IsFalse(ward.IsActive);
            var fresh = Prepare(); Assert.IsTrue(fresh.Succeeded); Assert.IsTrue(fresh.Commit.TryApply());
            Assert.AreEqual(editCost ? 85 : 80, wallet.GetCurrent(energy));
        }

        [Test]
        public void ActiveGuardAndInsufficientSnapshotOrLiveEnergyRejectWithoutSideEffects()
        {
            Assert.IsFalse(Prepare(snapshotEnergy: 19).Succeeded);
            var prepared = Prepare(); Assert.IsTrue(prepared.Succeeded);
            wallet.TrySpend(new[] { new ResourceAmount(energy, 90) }); Assert.IsFalse(prepared.Commit.TryApply()); Assert.IsFalse(ward.IsActive);
            wallet.Gain(energy, 90); ward.Arm(1); Assert.IsFalse(Prepare().Succeeded); Assert.AreEqual(100, wallet.GetCurrent(energy));
        }

        [Test]
        public void CancelledSelectionWrongCategoryAndDeadPlayerCannotPayOrArm()
        {
            Assert.IsFalse(Prepare(PlayerCardTimeState.Chain).Succeeded);
            var prepared = Prepare(); Assert.IsTrue(prepared.Succeeded); selection.Dispose(); Assert.IsFalse(prepared.Commit.TryApply());
            prepared = Prepare(); Assert.IsTrue(prepared.Succeeded);
            player.GetComponent<SimpleHealth>().ApplyDamage(new DamageContext(null, player, null, 5, Vector2.zero, Vector2.right));
            Assert.IsFalse(prepared.Commit.TryApply()); Assert.AreEqual(100, wallet.GetCurrent(energy)); Assert.IsFalse(ward.IsActive);
        }

        [Test]
        public void LegacyDirectCommitUsesTheSameWardTransactionAndRejectsActiveGuard()
        {
            Assert.IsTrue(cards.Commit(PlayerCardTimeState.Neutral, null, true)); Assert.AreEqual(80, wallet.GetCurrent(energy));
            Assert.IsFalse(cards.Commit(PlayerCardTimeState.Neutral, null, true)); Assert.AreEqual(80, wallet.GetCurrent(energy));
        }

        [Test]
        public void EquippedWardCostEditsChangeRecoveryBasisWithoutChangingWallet()
        {
            ConfigureCost(15);
            var profile = Track(ScriptableObject.CreateInstance<PlayerCardInventoryProfileSO>()); profile.TryAddOwnedCard(card, 1); profile.TryEquip(card);
            var inventory = player.AddComponent<PlayerCardInventoryRuntime>(); inventory.Initialize(profile);
            var tuning = Track(ScriptableObject.CreateInstance<PlayerRecoveryTuningSO>());
            var recovery = player.AddComponent<PlayerRecoveryController>(); recovery.Configure(tuning, energy, wallet, player.GetComponent<SimpleHealth>(), inventory);
            Assert.AreEqual(15, recovery.NeutralCostBasis); Assert.AreEqual(22.5f, recovery.PassiveEnergyCeiling);
            Assert.AreEqual(30, recovery.EnergyPerHealth); Assert.AreEqual(15, recovery.SacrificeEnergyGain);
            ConfigureCost(20); Assert.AreEqual(20, recovery.NeutralCostBasis); Assert.AreEqual(30, recovery.PassiveEnergyCeiling);
            Assert.AreEqual(40, recovery.EnergyPerHealth); Assert.AreEqual(20, recovery.SacrificeEnergyGain); Assert.AreEqual(100, wallet.GetCurrent(energy));
        }

        [Test]
        public void RemovedEffectAfterPreparationRejectsWithoutThrowingOrSpending()
        {
            var prepared = Prepare(); Assert.IsTrue(prepared.Succeeded);
            card.Configure(card.Id, "Ward", "", PlayerCardTimeState.Neutral, card.FixedCosts, null);
            Assert.IsFalse(prepared.Commit.TryApply()); Assert.AreEqual(100, wallet.GetCurrent(energy)); Assert.IsFalse(ward.IsActive);
        }

        [Test]
        public void GuardClearedDuringArmNotificationStillCannotReenterPayment()
        {
            var first = Prepare(); var second = cards.TryPrepare(card, selection, first.Commit.Snapshot);
            var nested = true; ward.Armed += () => { ward.Clear(); nested = second.Commit.TryApply(); };
            Assert.IsTrue(first.Commit.TryApply()); Assert.IsFalse(nested); Assert.AreEqual(80, wallet.GetCurrent(energy));
        }

        [Test]
        public void PreparedCommitRejectsHeldOrPausedPlayerBeforeAnyPayment()
        {
            var prepared = Prepare(); Assert.IsTrue(prepared.Succeeded);
            Time.timeScale = 0; Assert.IsFalse(prepared.Commit.TryApply()); Time.timeScale = 1;
            using (player.AddComponent<PlayerWorldHold>().Acquire()) Assert.IsFalse(prepared.Commit.TryApply());
            Assert.AreEqual(100, wallet.GetCurrent(energy)); Assert.IsFalse(ward.IsActive);
            Assert.IsTrue(prepared.Commit.TryApply());
        }
    }
}
