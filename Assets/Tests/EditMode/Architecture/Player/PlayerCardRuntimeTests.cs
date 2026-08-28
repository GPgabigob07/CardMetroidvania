using System.Collections.Generic;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerCardRuntimeTests
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
        public void EquippedDefinitions_SpendEnergyAndInstallEffects()
        {
            var energy = CreateAsset<ResourceDefinitionSO>("Energy");
            var player = new GameObject("Player");
            objectsToDestroy.Add(player);
            var wallet = player.AddComponent<PlayerResourceWallet>();
            wallet.ConfigureSingleResource(energy, startingAmount: 100f, maximumAmount: 100f);
            var effects = player.AddComponent<PlayerCombatEffects>();
            effects.ConfigureResources(wallet, energy);
            var extraJump = player.AddComponent<PlayerExtraJumpRuntime>();
            var extraJumpAbility = CreateAsset<AbilityDefinitionSO>("ExtraJump");
            extraJump.ConfigureAbility(extraJumpAbility);
            var cards = player.AddComponent<PlayerCardRuntime>();
            cards.Configure(wallet, effects, extraJump);
            cards.ConfigureCardDefinitions(
                CreateKnockbackCard(energy),
                CreateEscalatingCard(energy),
                CreateExtraJumpCard(energy, extraJumpAbility));

            Assert.IsTrue(cards.Commit(
                PlayerCardTimeState.Neutral,
                attackExecutionId: null,
                isAirborne: false));
            Assert.AreEqual(3, effects.KnockbackCharges);
            Assert.AreEqual(95f, wallet.GetCurrent(energy));

            Assert.IsTrue(cards.Commit(
                PlayerCardTimeState.Chain,
                attackExecutionId: "chain",
                isAirborne: false));
            Assert.AreEqual(5, effects.ChainCapacity);
            Assert.AreEqual(80f, wallet.GetCurrent(energy));

            Assert.IsTrue(cards.Commit(
                PlayerCardTimeState.Finisher,
                attackExecutionId: "finisher",
                isAirborne: true));
            Assert.AreEqual(1, extraJump.Charges);
            Assert.AreEqual(75f, wallet.GetCurrent(energy));
        }

        [Test]
        public void ExtraJumpCard_RejectsGroundedCommitWithoutSpending()
        {
            var energy = CreateAsset<ResourceDefinitionSO>("Energy");
            var player = new GameObject("Player");
            objectsToDestroy.Add(player);
            var wallet = player.AddComponent<PlayerResourceWallet>();
            wallet.ConfigureSingleResource(energy, startingAmount: 10f, maximumAmount: 10f);
            var effects = player.AddComponent<PlayerCombatEffects>();
            var extraJump = player.AddComponent<PlayerExtraJumpRuntime>();
            var ability = CreateAsset<AbilityDefinitionSO>("ExtraJump");
            extraJump.ConfigureAbility(ability);
            var cards = player.AddComponent<PlayerCardRuntime>();
            cards.Configure(wallet, effects, extraJump);
            cards.EquipCard(
                PlayerCardTimeState.Finisher,
                CreateExtraJumpCard(energy, ability));

            Assert.IsFalse(cards.Commit(
                PlayerCardTimeState.Finisher,
                attackExecutionId: "finisher",
                isAirborne: false));
            Assert.AreEqual(10f, wallet.GetCurrent(energy));
        }

        [Test]
        public void EquipCard_StoresAuthoredLoadoutAndRejectsWrongCategory()
        {
            var player = new GameObject("Player");
            objectsToDestroy.Add(player);
            var runtime = player.AddComponent<PlayerCardRuntime>();
            var energy = CreateAsset<ResourceDefinitionSO>("Energy");
            var neutral = CreateKnockbackCard(energy);
            var chain = CreateEscalatingCard(energy);
            var ability = CreateAsset<AbilityDefinitionSO>("ExtraJump");
            var finisher = CreateExtraJumpCard(energy, ability);

            runtime.ConfigureCardDefinitions(neutral, chain, finisher);

            Assert.AreSame(neutral, runtime.NeutralCard);
            Assert.AreSame(chain, runtime.ChainCard);
            Assert.AreSame(finisher, runtime.FinisherCard);
            Assert.IsFalse(runtime.EquipCard(PlayerCardTimeState.Neutral, chain));
            Assert.IsTrue(runtime.EquipCard(chain));
        }

        [Test]
        public void TryPrepare_SelectedCardDoesNotRequireSerializedSlot()
        {
            var energy = CreateAsset<ResourceDefinitionSO>("Energy");
            var player = new GameObject("Player");
            objectsToDestroy.Add(player);
            var wallet = player.AddComponent<PlayerResourceWallet>();
            wallet.ConfigureSingleResource(energy, startingAmount: 100f, maximumAmount: 100f);
            var effects = player.AddComponent<PlayerCombatEffects>();
            effects.ConfigureResources(wallet, energy);
            var extraJump = player.AddComponent<PlayerExtraJumpRuntime>();
            var cards = player.AddComponent<PlayerCardRuntime>();
            cards.Configure(wallet, effects, extraJump);
            var chain = CreateEscalatingCard(energy);
            var selection = CreateSelection(PlayerCardTimeState.Chain, chain);
            var snapshot = CreateSnapshot(
                PlayerCardTimeState.Chain,
                energy,
                currentEnergy: 100f,
                attackExecutionId: "chain",
                isAirborne: false);

            var result = cards.TryPrepare(chain, selection, snapshot);

            Assert.IsTrue(result.Succeeded);
            Assert.AreSame(chain, result.Card);
            Assert.AreEqual(15f, result.Costs[0].Amount);
            Assert.IsTrue(result.Commit.TryApply());
            Assert.AreEqual(5, effects.ChainCapacity);
        }

        [Test]
        public void TryPrepare_RejectsWrongCategorySelection()
        {
            var energy = CreateAsset<ResourceDefinitionSO>("Energy");
            var player = new GameObject("Player");
            objectsToDestroy.Add(player);
            var wallet = player.AddComponent<PlayerResourceWallet>();
            wallet.ConfigureSingleResource(energy, startingAmount: 100f, maximumAmount: 100f);
            var cards = player.AddComponent<PlayerCardRuntime>();
            cards.Configure(
                wallet,
                player.AddComponent<PlayerCombatEffects>(),
                player.AddComponent<PlayerExtraJumpRuntime>());
            var neutral = CreateKnockbackCard(energy);
            var selection = CreateSelection(PlayerCardTimeState.Neutral, neutral);
            var snapshot = CreateSnapshot(
                PlayerCardTimeState.Chain,
                energy,
                currentEnergy: 100f,
                attackExecutionId: "chain",
                isAirborne: false);

            var result = cards.TryPrepare(neutral, selection, snapshot);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(CardCommitFailure.CategoryMismatch, result.Failure);
        }

        [Test]
        public void TryPrepare_RejectsInsufficientSnapshotEnergyBeforeLiveSpend()
        {
            var energy = CreateAsset<ResourceDefinitionSO>("Energy");
            var player = new GameObject("Player");
            objectsToDestroy.Add(player);
            var wallet = player.AddComponent<PlayerResourceWallet>();
            wallet.ConfigureSingleResource(energy, startingAmount: 100f, maximumAmount: 100f);
            var cards = player.AddComponent<PlayerCardRuntime>();
            cards.Configure(
                wallet,
                player.AddComponent<PlayerCombatEffects>(),
                player.AddComponent<PlayerExtraJumpRuntime>());
            var neutral = CreateKnockbackCard(energy);
            var selection = CreateSelection(PlayerCardTimeState.Neutral, neutral);
            var snapshot = CreateSnapshot(
                PlayerCardTimeState.Neutral,
                energy,
                currentEnergy: 1f,
                attackExecutionId: null,
                isAirborne: false);

            var result = cards.TryPrepare(neutral, selection, snapshot);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(CardCommitFailure.InsufficientSnapshotResources, result.Failure);
            Assert.AreEqual(100f, wallet.GetCurrent(energy));
        }

        [Test]
        public void PreparedCommit_LiveSpendFailureDoesNotApplyEffect()
        {
            var energy = CreateAsset<ResourceDefinitionSO>("Energy");
            var player = new GameObject("Player");
            objectsToDestroy.Add(player);
            var wallet = player.AddComponent<PlayerResourceWallet>();
            wallet.ConfigureSingleResource(energy, startingAmount: 10f, maximumAmount: 10f);
            var effects = player.AddComponent<PlayerCombatEffects>();
            effects.ConfigureResources(wallet, energy);
            var cards = player.AddComponent<PlayerCardRuntime>();
            cards.Configure(wallet, effects, player.AddComponent<PlayerExtraJumpRuntime>());
            var neutral = CreateKnockbackCard(energy);
            var selection = CreateSelection(PlayerCardTimeState.Neutral, neutral);
            var snapshot = CreateSnapshot(
                PlayerCardTimeState.Neutral,
                energy,
                currentEnergy: 10f,
                attackExecutionId: null,
                isAirborne: false);
            var result = cards.TryPrepare(neutral, selection, snapshot);

            wallet.TrySpend(new[] { new ResourceAmount(energy, 10f) });

            Assert.IsFalse(result.Commit.TryApply());
            Assert.AreEqual(CardCommitFailure.InsufficientLiveResources, result.Commit.Failure);
            Assert.AreEqual(0, effects.KnockbackCharges);
        }

        [TestCase(PlayerActionState.Attack1, PlayerCardTimeState.Chain)]
        [TestCase(PlayerActionState.Attack2, PlayerCardTimeState.Chain)]
        [TestCase(PlayerActionState.Attack3, PlayerCardTimeState.Finisher)]
        [TestCase(PlayerActionState.None, PlayerCardTimeState.Neutral)]
        public void ComboStep_OwnsCardTimeForItsWholeAnimation(
            PlayerActionState action,
            PlayerCardTimeState expected)
        {
            Assert.AreEqual(expected, PlayerAttackSequence.GetCardTime(action));
        }

        [Test]
        public void ComboCardTime_PersistsUntilRestartCooldownExpires()
        {
            var combo = new PlayerAttackComboRuntime();
            Assert.AreEqual(PlayerCardTimeState.Neutral, combo.CurrentCardTime);

            combo.NotifyAttackStarted(PlayerActionState.Attack1);
            Assert.AreEqual(PlayerCardTimeState.Chain, combo.CurrentCardTime);
            combo.NotifyAttackCompleted(
                PlayerActionState.Attack1,
                postRecoveryBufferGraceDuration: 0.2f,
                sequenceRestartCooldown: 0.5f);
            combo.Tick(0.49f);
            Assert.AreEqual(PlayerCardTimeState.Chain, combo.CurrentCardTime);
            combo.Tick(0.02f);
            Assert.AreEqual(PlayerCardTimeState.Neutral, combo.CurrentCardTime);

            combo.NotifyAttackStarted(PlayerActionState.Attack3);
            Assert.AreEqual(PlayerCardTimeState.Finisher, combo.CurrentCardTime);
            combo.NotifyAttackCompleted(
                PlayerActionState.Attack3,
                postRecoveryBufferGraceDuration: 0.2f,
                sequenceRestartCooldown: 0.5f);
            combo.Tick(0.49f);
            Assert.AreEqual(PlayerCardTimeState.Finisher, combo.CurrentCardTime);
            combo.Tick(0.02f);
            Assert.AreEqual(PlayerCardTimeState.Neutral, combo.CurrentCardTime);
        }

        [Test]
        public void CommittedFinisher_CannotReopenUntilComboCategoryChanges()
        {
            var combo = new PlayerAttackComboRuntime();
            combo.NotifyAttackStarted(PlayerActionState.Attack3);
            Assert.AreEqual(PlayerCardTimeState.Finisher, combo.AvailableCardTime);

            combo.ConsumeCardTime();

            Assert.AreEqual(PlayerCardTimeState.Finisher, combo.CurrentCardTime);
            Assert.AreEqual(PlayerCardTimeState.None, combo.AvailableCardTime);
            combo.NotifyAttackCompleted(
                PlayerActionState.Attack3,
                postRecoveryBufferGraceDuration: 0.2f,
                sequenceRestartCooldown: 0.5f);
            combo.Tick(0.49f);
            Assert.AreEqual(PlayerCardTimeState.None, combo.AvailableCardTime);

            combo.Tick(0.02f);

            Assert.AreEqual(PlayerCardTimeState.Neutral, combo.AvailableCardTime);
        }

        [Test]
        public void ComboCardTime_RestoresNeutralAfterTimedOutGrounding()
        {
            var combo = new PlayerAttackComboRuntime();
            combo.NotifyAttackStarted(PlayerActionState.Attack3);
            combo.ConsumeCardTime();

            combo.RestoreNeutralCardTime();

            Assert.AreEqual(PlayerCardTimeState.Neutral, combo.CurrentCardTime);
            Assert.AreEqual(PlayerCardTimeState.Neutral, combo.AvailableCardTime);
        }

        private CardDefinitionSO CreateKnockbackCard(ResourceDefinitionSO energy)
        {
            var status = CreateAsset<CardStatusDefinitionSO>("KnockbackStatus");
            var effect = CreateAsset<CardEffectDefinitionSO>("KnockbackEffect");
            effect.Configure(
                status,
                conditions: null,
                operations: new[]
                {
                    new CardOperationDefinition(
                        CardOperationKind.AddStatusCharges,
                        status: status,
                        amount: 3f)
                },
                rules: new[]
                {
                    new CardReactiveRule(
                        CardTriggerKind.OnEffectivePrimaryAttackResolved,
                        conditions: null,
                        operations: new[]
                        {
                            new CardOperationDefinition(
                                CardOperationKind.ModifyKnockback,
                                status: status,
                                multiplier: 2f)
                        })
                },
                lifetimeDefinitions: new[]
                {
                    new CardLifetimeDefinition(CardLifetimeKind.UntilChargesExhausted)
                },
                stackingDefinition: new CardStackingDefinition(
                    CardStackingKind.AddCharges));
            return CreateCard(
                "Knockback",
                PlayerCardTimeState.Neutral,
                energy,
                5f,
                effect);
        }

        private CardDefinitionSO CreateEscalatingCard(ResourceDefinitionSO energy)
        {
            var status = CreateAsset<CardStatusDefinitionSO>("ChainStatus");
            var effect = CreateAsset<CardEffectDefinitionSO>("ChainEffect");
            effect.Configure(
                status,
                conditions: null,
                operations: new[]
                {
                    new CardOperationDefinition(
                        CardOperationKind.AddStatusCapacity,
                        status: status,
                        amount: 5f),
                    new CardOperationDefinition(
                        CardOperationKind.ModifyDamage,
                        status: status,
                        multiplier: 0.1f)
                },
                rules: new[]
                {
                    new CardReactiveRule(
                        CardTriggerKind.OnEffectivePrimaryAttackResolved,
                        conditions: null,
                        operations: new[]
                        {
                            new CardOperationDefinition(
                                CardOperationKind.AddStatusStacks,
                                status: status,
                                amount: 1f)
                        })
                },
                lifetimeDefinitions: new[]
                {
                    new CardLifetimeDefinition(CardLifetimeKind.UntilSceneTransition)
                },
                stackingDefinition: new CardStackingDefinition(
                    CardStackingKind.AddCapacity));
            return CreateCard(
                "Escalating",
                PlayerCardTimeState.Chain,
                energy,
                15f,
                effect);
        }

        private CardDefinitionSO CreateExtraJumpCard(
            ResourceDefinitionSO energy,
            AbilityDefinitionSO ability)
        {
            var effect = CreateAsset<CardEffectDefinitionSO>("ExtraJumpEffect");
            effect.Configure(
                statusDefinition: null,
                conditions: new[]
                {
                    new CardConditionDefinition(CardConditionKind.IsAirborne),
                    new CardConditionDefinition(
                        CardConditionKind.AbilityAvailable,
                        ability: ability),
                    new CardConditionDefinition(
                        CardConditionKind.AbilityUnlocked,
                        ability: ability)
                },
                operations: new[]
                {
                    new CardOperationDefinition(
                        CardOperationKind.InvokeAbility,
                        ability: ability)
                },
                rules: null,
                lifetimeDefinitions: new[]
                {
                    new CardLifetimeDefinition(CardLifetimeKind.Immediate)
                },
                stackingDefinition: new CardStackingDefinition(
                    CardStackingKind.RejectIfActive));
            return CreateCard(
                "ExtraJump",
                PlayerCardTimeState.Finisher,
                energy,
                5f,
                effect);
        }

        private CardDefinitionSO CreateCard(
            string id,
            PlayerCardTimeState category,
            ResourceDefinitionSO energy,
            float cost,
            CardEffectDefinitionSO effect)
        {
            var card = CreateAsset<CardDefinitionSO>(id);
            card.Configure(
                id,
                id,
                string.Empty,
                category,
                new[] { new ResourceAmount(energy, cost) },
                effect);
            return card;
        }

        private CardTimeSelectionTransaction CreateSelection(
            PlayerCardTimeState category,
            CardDefinitionSO card)
        {
            var catalog = CreateAsset<CardCatalogSO>($"{card.Id}.Catalog");
            catalog.Configure(new[] { card });
            CardTimeSelectionTransaction.TryCreate(
                category,
                sessionId: 1,
                candidateIds: new[] { card.Id },
                catalog,
                out var selection);
            return selection;
        }

        private static PlayerCardCommitSnapshot CreateSnapshot(
            PlayerCardTimeState category,
            ResourceDefinitionSO energy,
            float currentEnergy,
            string attackExecutionId,
            bool isAirborne)
        {
            return new PlayerCardCommitSnapshot(
                category,
                attackExecutionId,
                isAirborne,
                currentHealth: 5f,
                maximumHealth: 5f,
                resourceSnapshots: new[]
                {
                    new PlayerCardResourceSnapshot(energy, currentEnergy, 100f)
                });
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
