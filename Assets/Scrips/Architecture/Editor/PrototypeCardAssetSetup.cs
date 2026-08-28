using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TicGame.Architecture.EditorTools
{
    public static class PrototypeCardAssetSetup
    {
        private const string AbilityFolder = "Assets/Data/Abilities";
        private const string CardFolder = "Assets/Data/Cards";
        private const string StatusFolder = CardFolder + "/Statuses";
        private const string EffectFolder = CardFolder + "/Effects";
        private const string DefinitionFolder = CardFolder + "/Definitions";
        private const string EnergyPath = "Assets/Data/Resources/Resource_Energy.asset";
        private const string ExtraJumpAbilityPath = AbilityFolder + "/Ability_ExtraJump.asset";

        public readonly struct PrototypeCardLoadout
        {
            public PrototypeCardLoadout(
                CardDefinitionSO neutral,
                CardDefinitionSO chain,
                CardDefinitionSO finisher,
                AbilityDefinitionSO extraJumpAbility)
            {
                Neutral = neutral;
                Chain = chain;
                Finisher = finisher;
                ExtraJumpAbility = extraJumpAbility;
            }

            public CardDefinitionSO Neutral { get; }
            public CardDefinitionSO Chain { get; }
            public CardDefinitionSO Finisher { get; }
            public AbilityDefinitionSO ExtraJumpAbility { get; }
        }

        [MenuItem("TIC/Setup/Update Prototype Card Assets")]
        public static void UpdatePrototypeCardAssets()
        {
            CreateOrUpdateAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created or updated prototype card assets.");
        }

        public static PrototypeCardLoadout CreateOrUpdateAssets()
        {
            EnsureFolder(AbilityFolder);
            EnsureFolder(StatusFolder);
            EnsureFolder(EffectFolder);
            EnsureFolder(DefinitionFolder);

            var energy = AssetDatabase.LoadAssetAtPath<ResourceDefinitionSO>(EnergyPath);
            if (energy == null)
            {
                Debug.LogError($"Missing required Energy resource at {EnergyPath}.");
                return default;
            }

            var extraJumpAbility = CreateOrLoad<AbilityDefinitionSO>(
                ExtraJumpAbilityPath,
                "Ability_ExtraJump");
            ConfigureAbility(
                extraJumpAbility,
                id: "ability.extra-jump",
                displayName: "Extra Jump",
                description: "Allows one card-granted jump while airborne.",
                kind: AbilityKind.Movement,
                unlockedByDefault: true);

            var knockbackStatus = CreateStatus(
                "Status_KnockbackBoost",
                "status.knockback-boost",
                "Knockback Boost");
            var escalatingStatus = CreateStatus(
                "Status_EscalatingDamage",
                "status.escalating-damage",
                "Escalating Damage");
            var doubleEnergyStatus = CreateStatus(
                "Status_DoubleHitEnergy",
                "status.double-hit-energy",
                "Double Hit Energy");

            var knockbackEffect = CreateKnockbackEffect(knockbackStatus);
            var escalatingEffect = CreateEscalatingDamageEffect(escalatingStatus);
            var doubleEnergyEffect = CreateDoubleEnergyEffect(doubleEnergyStatus);
            var extraJumpEffect = CreateExtraJumpEffect(extraJumpAbility);
            var overchargeEffect = CreateOverchargeEffect(energy);

            var knockbackCard = CreateCard(
                "Card_Neutral_KnockbackCharges",
                "card.neutral.knockback-charges",
                "Knockback Charges",
                "Doubles outgoing knockback for the next three effective hits.",
                PlayerCardTimeState.Neutral,
                energy,
                cost: 5f,
                knockbackEffect);
            var escalatingCard = CreateCard(
                "Card_Chain_EscalatingDamage",
                "card.chain.escalating-damage",
                "Escalating Damage",
                "Builds damage stacks on effective hits and loses current stacks on a miss.",
                PlayerCardTimeState.Chain,
                energy,
                cost: 15f,
                escalatingEffect);
            CreateCard(
                "Card_Chain_DoubleHitEnergy",
                "card.chain.double-hit-energy",
                "Double Hit Energy",
                "Doubles successful hit Energy gains for the next five effective hits.",
                PlayerCardTimeState.Chain,
                energy,
                cost: 15f,
                doubleEnergyEffect);
            var extraJumpCard = CreateCard(
                "Card_Finisher_ExtraJump",
                "card.finisher.extra-jump",
                "Extra Jump",
                "Invokes the unlocked Extra Jump ability while airborne.",
                PlayerCardTimeState.Finisher,
                energy,
                cost: 5f,
                extraJumpEffect);
            CreateCard(
                "Card_Finisher_BaseDamageOvercharge",
                "card.finisher.base-damage-overcharge",
                "Base Damage Overcharge",
                "Arms attack-scoped supplemental damage using a variable Energy spend.",
                PlayerCardTimeState.Finisher,
                energy,
                cost: 15f,
                overchargeEffect);

            AssetDatabase.SaveAssets();
            return new PrototypeCardLoadout(
                knockbackCard,
                escalatingCard,
                extraJumpCard,
                extraJumpAbility);
        }

        private static CardEffectDefinitionSO CreateKnockbackEffect(
            CardStatusDefinitionSO status)
        {
            var effect = CreateOrLoad<CardEffectDefinitionSO>(
                EffectFolder + "/Effect_KnockbackCharges.asset",
                "Effect_KnockbackCharges");
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
                        conditions: new[]
                        {
                            new CardConditionDefinition(
                                CardConditionKind.HasRemainingCharges)
                        },
                        operations: new[]
                        {
                            new CardOperationDefinition(
                                CardOperationKind.ModifyKnockback,
                                status: status,
                                multiplier: 2f)
                        },
                        consumesCharge: true)
                },
                lifetimeDefinitions: new[]
                {
                    new CardLifetimeDefinition(CardLifetimeKind.UntilChargesExhausted)
                },
                stackingDefinition: new CardStackingDefinition(
                    CardStackingKind.AddCharges));
            EditorUtility.SetDirty(effect);
            return effect;
        }

        private static CardEffectDefinitionSO CreateEscalatingDamageEffect(
            CardStatusDefinitionSO status)
        {
            var effect = CreateOrLoad<CardEffectDefinitionSO>(
                EffectFolder + "/Effect_EscalatingDamage.asset",
                "Effect_EscalatingDamage");
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
                        }),
                    new CardReactiveRule(
                        CardTriggerKind.OnPrimaryAttackCompleted,
                        conditions: new[]
                        {
                            new CardConditionDefinition(CardConditionKind.WasMiss)
                        },
                        operations: new[]
                        {
                            new CardOperationDefinition(
                                CardOperationKind.ClearStatusStacks,
                                status: status)
                        })
                },
                lifetimeDefinitions: new[]
                {
                    new CardLifetimeDefinition(CardLifetimeKind.UntilPlayerDeath),
                    new CardLifetimeDefinition(CardLifetimeKind.UntilSceneTransition)
                },
                stackingDefinition: new CardStackingDefinition(
                    CardStackingKind.AddCapacity));
            EditorUtility.SetDirty(effect);
            return effect;
        }

        private static CardEffectDefinitionSO CreateDoubleEnergyEffect(
            CardStatusDefinitionSO status)
        {
            var effect = CreateOrLoad<CardEffectDefinitionSO>(
                EffectFolder + "/Effect_DoubleHitEnergy.asset",
                "Effect_DoubleHitEnergy");
            effect.Configure(
                status,
                conditions: null,
                operations: new[]
                {
                    new CardOperationDefinition(
                        CardOperationKind.AddStatusCharges,
                        status: status,
                        amount: 5f)
                },
                rules: new[]
                {
                    new CardReactiveRule(
                        CardTriggerKind.OnEffectivePrimaryAttackResolved,
                        conditions: new[]
                        {
                            new CardConditionDefinition(
                                CardConditionKind.HasRemainingCharges)
                        },
                        operations: new[]
                        {
                            new CardOperationDefinition(
                                CardOperationKind.ModifyResourceGain,
                                status: status,
                                multiplier: 2f)
                        },
                        consumesCharge: true)
                },
                lifetimeDefinitions: new[]
                {
                    new CardLifetimeDefinition(CardLifetimeKind.UntilChargesExhausted)
                },
                stackingDefinition: new CardStackingDefinition(
                    CardStackingKind.AddCharges));
            EditorUtility.SetDirty(effect);
            return effect;
        }

        private static CardEffectDefinitionSO CreateExtraJumpEffect(
            AbilityDefinitionSO ability)
        {
            var effect = CreateOrLoad<CardEffectDefinitionSO>(
                EffectFolder + "/Effect_ExtraJump.asset",
                "Effect_ExtraJump");
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
            EditorUtility.SetDirty(effect);
            return effect;
        }

        private static CardEffectDefinitionSO CreateOverchargeEffect(
            ResourceDefinitionSO energy)
        {
            var effect = CreateOrLoad<CardEffectDefinitionSO>(
                EffectFolder + "/Effect_BaseDamageOvercharge.asset",
                "Effect_BaseDamageOvercharge");
            effect.Configure(
                statusDefinition: null,
                conditions: new[]
                {
                    new CardConditionDefinition(CardConditionKind.HasAttackExecution)
                },
                operations: new[]
                {
                    new CardOperationDefinition(
                        CardOperationKind.ArmSupplementalDamage,
                        resource: energy,
                        amount: 10f,
                        multiplier: 0.25f,
                        effectId: "card.finisher.overcharge")
                },
                rules: null,
                lifetimeDefinitions: new[]
                {
                    new CardLifetimeDefinition(
                        CardLifetimeKind.UntilAttackExecutionCompletes)
                },
                stackingDefinition: new CardStackingDefinition(
                    CardStackingKind.RejectIfActive));
            EditorUtility.SetDirty(effect);
            return effect;
        }

        private static CardStatusDefinitionSO CreateStatus(
            string assetName,
            string id,
            string displayName)
        {
            var status = CreateOrLoad<CardStatusDefinitionSO>(
                StatusFolder + "/" + assetName + ".asset",
                assetName);
            status.Configure(id, displayName);
            EditorUtility.SetDirty(status);
            return status;
        }

        private static CardDefinitionSO CreateCard(
            string assetName,
            string id,
            string displayName,
            string description,
            PlayerCardTimeState category,
            ResourceDefinitionSO energy,
            float cost,
            CardEffectDefinitionSO effect)
        {
            var card = CreateOrLoad<CardDefinitionSO>(
                DefinitionFolder + "/" + assetName + ".asset",
                assetName);
            card.Configure(
                id,
                displayName,
                description,
                category,
                new[] { new ResourceAmount(energy, cost) },
                effect);
            EditorUtility.SetDirty(card);
            return card;
        }

        private static T CreateOrLoad<T>(string path, string assetName)
            where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void ConfigureAbility(
            AbilityDefinitionSO ability,
            string id,
            string displayName,
            string description,
            AbilityKind kind,
            bool unlockedByDefault)
        {
            var serialized = new SerializedObject(ability);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("description").stringValue = description;
            serialized.FindProperty("kind").intValue = (int)kind;
            serialized.FindProperty("unlockedByDefault").boolValue = unlockedByDefault;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ability);
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }
}
