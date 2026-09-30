using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class RecoveryPreparedCommitTests
    {
        private readonly List<Object> objects = new();
        private GameObject go;
        private ResourceDefinitionSO energy;
        private PlayerResourceWallet wallet;
        private SimpleHealth health;
        private PlayerCardRuntime runtime;
        private PlayerCardInventoryRuntime inventory;
        private PlayerRecoveryController recovery;
        private CardDefinitionSO card;
        private CardCatalogSO catalog;
        private T Asset<T>() where T : ScriptableObject { var obj = ScriptableObject.CreateInstance<T>(); objects.Add(obj); return obj; }
        [TearDown] public void Cleanup() { foreach (var obj in objects) Object.DestroyImmediate(obj); objects.Clear(); }
        private void Setup(CardOperationKind kind, float hp, float amount)
        {
            go = new GameObject("player"); objects.Add(go);
            energy = Asset<ResourceDefinitionSO>();
            wallet = go.AddComponent<PlayerResourceWallet>(); wallet.ConfigureSingleResource(energy, amount, 250);
            health = go.AddComponent<SimpleHealth>(); health.Initialize(); if (hp < 5) health.TrySpendNonlethal(5 - hp);
            var effect = Asset<CardEffectDefinitionSO>();
            effect.Configure(null, null, new[] { new CardOperationDefinition(kind) }, null,
                new[] { new CardLifetimeDefinition(CardLifetimeKind.Immediate) }, new CardStackingDefinition(CardStackingKind.RejectIfActive));
            card = Asset<CardDefinitionSO>(); card.Configure("test", "Test", "", PlayerCardTimeState.Neutral, null, effect);
            if (kind == CardOperationKind.Heal) card.ConfigureConsumption(CardConsumptionPolicy.ConsumeOnSuccess);
            var traversal = Asset<CardDefinitionSO>(); traversal.Configure("jump", "Jump", "", PlayerCardTimeState.Neutral, new[] { new ResourceAmount(energy, 15) }, null);
            var profile = Asset<PlayerCardInventoryProfileSO>(); profile.TryAddOwnedCard(card, 2); profile.TryEquip(card); profile.TryAddOwnedCard(traversal); profile.TryEquip(traversal);
            inventory = go.AddComponent<PlayerCardInventoryRuntime>(); inventory.Initialize(profile);
            recovery = go.AddComponent<PlayerRecoveryController>(); recovery.Configure(Asset<PlayerRecoveryTuningSO>(), energy, wallet, health, inventory);
            runtime = go.AddComponent<PlayerCardRuntime>(); runtime.Configure(wallet, go.AddComponent<PlayerCombatEffects>(), go.AddComponent<PlayerExtraJumpRuntime>());
            runtime.EquipCard(card);
            catalog = Asset<CardCatalogSO>(); catalog.Configure(new[] { card });
        }
        private CardReadinessResult Prepare()
        {
            Assert.That(CardTimeSelectionTransaction.TryCreate(PlayerCardTimeState.Neutral, 1, new[] { card.Id }, catalog, out var selection), Is.True);
            return runtime.TryPrepare(card, selection, new PlayerCardCommitSnapshot(PlayerCardTimeState.Neutral, null, true,
                health.CurrentHealth, health.MaximumHealth, new[] { new PlayerCardResourceSnapshot(energy, wallet.GetCurrent(energy), wallet.GetMaximum(energy)) }));
        }
        [TestCase(CardOperationKind.SacrificeHealthForEnergy, 3, 0, 2, 15)]
        [TestCase(CardOperationKind.ConvertEnergyToHealth, 2, 95, 5, 5)]
        [TestCase(CardOperationKind.Heal, 2, 0, 4, 0)]
        public void SelectionPrepareApplyIsSingleUse(CardOperationKind kind, float hp, float amount, float finalHp, float finalEnergy)
        {
            Setup(kind, hp, amount);
            var ready = Prepare(); Assert.That(ready.Succeeded, Is.True, ready.Failure.ToString());
            health.Changed += _ => Assert.That(ready.Commit.TryApply(), Is.False, "observer reentry");
            Assert.That(ready.Commit.TryApply(), Is.True);
            Assert.That(ready.Commit.TryApply(), Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(finalHp));
            Assert.That(wallet.GetCurrent(energy), Is.EqualTo(finalEnergy));
            Assert.That(inventory.GetCount(card.Id), Is.EqualTo(kind == CardOperationKind.Heal ? 1 : 2));
        }
        [TestCase(CardOperationKind.SacrificeHealthForEnergy, 1, 0, CardCommitFailure.InsufficientHealth)]
        [TestCase(CardOperationKind.SacrificeHealthForEnergy, 3, 240, CardCommitFailure.InsufficientEnergyCapacity)]
        [TestCase(CardOperationKind.ConvertEnergyToHealth, 3, 29, CardCommitFailure.InsufficientLiveResources)]
        [TestCase(CardOperationKind.Heal, 5, 0, CardCommitFailure.FullHealth)]
        public void UnavailableCardsDoNotPay(CardOperationKind kind, float hp, float amount, CardCommitFailure failure)
        {
            Setup(kind, hp, amount);
            var ready = Prepare(); Assert.That(ready.Succeeded, Is.False); Assert.That(ready.Failure, Is.EqualTo(failure));
            Assert.That(health.CurrentHealth, Is.EqualTo(hp)); Assert.That(wallet.GetCurrent(energy), Is.EqualTo(amount));
            Assert.That(inventory.GetCount(card.Id), Is.EqualTo(2));
        }
        [Test] public void ChangingHealingResultInvalidatesQuoteWithoutPayment()
        {
            Setup(CardOperationKind.ConvertEnergyToHealth, 2, 95);
            var ready = Prepare(); Assert.That(ready.Succeeded, Is.True);
            health.Heal(1);
            Assert.That(ready.Commit.TryApply(), Is.False);
            Assert.That(ready.Commit.Failure, Is.EqualTo(CardCommitFailure.StaleRecoveryQuote));
            Assert.That(wallet.GetCurrent(energy), Is.EqualTo(95));
        }
        [Test] public void ConsumableStockPersistsAtZeroWithoutRemovingSlot()
        {
            Setup(CardOperationKind.Heal, 1, 0);
            Assert.That(Prepare().Commit.TryApply(), Is.True);
            Assert.That(Prepare().Commit.TryApply(), Is.True);
            health.TrySpendNonlethal(1);
            Assert.That(Prepare().Failure, Is.EqualTo(CardCommitFailure.DepletedStock));
            Assert.That(inventory.GetEquippedCardIds(PlayerCardTimeState.Neutral)[0], Is.EqualTo(card.Id));
            health.Initialize(); go.SetActive(false); go.SetActive(true);
            Assert.That(inventory.GetCount(card.Id), Is.Zero);
        }
        [Test] public void LegacyCommitUsesSameExchange()
        {
            Setup(CardOperationKind.SacrificeHealthForEnergy, 3, 0);
            Assert.That(runtime.Commit(PlayerCardTimeState.Neutral, null, true), Is.True);
            Assert.That(wallet.GetCurrent(energy), Is.EqualTo(15)); Assert.That(health.CurrentHealth, Is.EqualTo(2));
        }

        [Test]
        public void CancelledSessionCannotDonatePreparedCommitToNextSession()
        {
            Setup(CardOperationKind.Heal, 2, 0);
            var session = new PlayerCardTimeRuntime();
            session.PublishAvailability(new CardTimeOpportunity(PlayerCardTimeState.Neutral, 1));
            Assert.That(session.RequestActivation(), Is.EqualTo(CardTimeActivationRequestResult.Activated));
            var prepared = Prepare();
            Assert.That(prepared.Commit.SessionId, Is.EqualTo(session.Current.ActiveSessionId));
            Assert.That(session.Cancel(), Is.True);
            session.PublishAvailability(new CardTimeOpportunity(PlayerCardTimeState.Neutral, 2));
            Assert.That(session.RequestActivation(), Is.EqualTo(CardTimeActivationRequestResult.Activated));
            Assert.That(session.TryCommit(prepared.Commit), Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(2));
            Assert.That(inventory.GetCount(card.Id), Is.EqualTo(2));
            Assert.That(session.Current.IsActive, Is.True);
        }

        [Test] public void UnusedQuoteAndFailedSpendDoNotConsumeOrResetDelay()
        {
            Setup(CardOperationKind.Heal, 2, 0);
            Assert.That(Prepare().Succeeded, Is.True);
            Assert.That(inventory.GetCount(card.Id), Is.EqualTo(2));
            recovery.Tick(2, true);
            Assert.That(wallet.TrySpend(new[] { new ResourceAmount(energy, 1) }), Is.False);
            recovery.Tick(2, true);
            Assert.That(wallet.GetCurrent(energy), Is.EqualTo(5));
            wallet.TrySpend(new[] { new ResourceAmount(energy, 1) });
            recovery.Tick(2, true);
            Assert.That(wallet.GetCurrent(energy), Is.EqualTo(4));
        }

        [Test] public void DiscountedExchangeIsRejectedAndDeadPlayerCannotHeal()
        {
            Setup(CardOperationKind.ConvertEnergyToHealth, 2, 95);
            var snapshot = new PlayerCardCommitSnapshot(PlayerCardTimeState.Neutral, null, false, 2, 5,
                new[] { new PlayerCardResourceSnapshot(energy, 95, 250) }, new[] { new ResourceAmount(energy, 1) });
            Assert.That(recovery.TryQuote(card, snapshot, out _), Is.False);
            Assert.That(recovery.Failure, Is.EqualTo(CardCommitFailure.UnsupportedEffect));
            health.ApplyDamage(new DamageContext(go, go, null, 10, Vector2.zero, Vector2.zero));
            Assert.That(health.Heal(10), Is.Zero);
            recovery.Tick(100, true);
            Assert.That(wallet.GetCurrent(energy), Is.EqualTo(95));
            Assert.That(Prepare().Succeeded, Is.False);
        }

        [Test] public void TwoInventoriesKeepIndependentConsumableCounts()
        {
            Setup(CardOperationKind.Heal, 2, 0);
            var profile = Asset<PlayerCardInventoryProfileSO>(); profile.TryAddOwnedCard(card, 2); profile.TryEquip(card);
            var other = new GameObject("other inventory"); objects.Add(other);
            var otherInventory = other.AddComponent<PlayerCardInventoryRuntime>(); otherInventory.Initialize(profile);
            Assert.That(Prepare().Commit.TryApply(), Is.True);
            Assert.That(otherInventory.GetCount(card.Id), Is.EqualTo(2));
            Assert.That(profile.OwnedCards[0].Count, Is.EqualTo(2));
        }

        [TestCase(CardOperationKind.Heal, 5, 0, "HP full")]
        [TestCase(CardOperationKind.SacrificeHealthForEnergy, 1, 0, "Must keep 1 HP")]
        [TestCase(CardOperationKind.SacrificeHealthForEnergy, 3, 240, "Needs 15 energy space")]
        [TestCase(CardOperationKind.ConvertEnergyToHealth, 3, 29, "Needs 30 energy")]
        public void PresentationExplainsUnavailability(CardOperationKind kind, float hp, float amount, string text)
        {
            Setup(kind, hp, amount);
            StringAssert.Contains(text, recovery.GetPresentation(card));
        }
    }
}
