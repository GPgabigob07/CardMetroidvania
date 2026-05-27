using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class CapabilitySet : MonoBehaviour, ICapabilityProvider
    {
        [Serializable]
        private sealed class RuntimeAbility
        {
            [Tooltip("Ability definition tracked by this runtime set.")]
            [SerializeField] private AbilityDefinitionSO definition;

            [Tooltip("Forces this ability to start unlocked for prototypes or debug scenes.")]
            [SerializeField] private bool unlockedOverride;

            public AbilityDefinitionSO Definition => definition;
            public bool IsUnlocked { get; private set; }

            public void Initialize()
            {
                IsUnlocked = unlockedOverride || (definition != null && definition.UnlockedByDefault);
            }

            public void SetUnlocked(bool unlocked)
            {
                IsUnlocked = unlocked;
            }
        }

        [Header("Abilities")]
        [Tooltip("Abilities tracked by this provider.")]
        [SerializeField] private List<RuntimeAbility> abilities = new List<RuntimeAbility>();

        [Header("Events")]
        [Tooltip("Raised whenever an ability changes unlocked state.")]
        [SerializeField] private AbilityUnlockEventChannelSO abilityUnlockEvent;

        private void Awake()
        {
            foreach (var ability in abilities)
            {
                ability.Initialize();
            }
        }

        public bool HasAbility(AbilityDefinitionSO ability)
        {
            if (ability == null)
            {
                return false;
            }

            RuntimeAbility runtimeAbility = FindRuntimeAbility(ability);
            return runtimeAbility != null && runtimeAbility.IsUnlocked;
        }

        public bool HasCapability(GameplayTagSO capabilityTag)
        {
            if (capabilityTag == null)
            {
                return false;
            }

            foreach (var runtimeAbility in abilities)
            {
                var definition = runtimeAbility.Definition;
                if (runtimeAbility.IsUnlocked && definition != null && definition.GatingTags.Contains(capabilityTag))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAllCapabilities(GameplayTagSet requiredTags)
        {
            if (requiredTags == null)
            {
                return true;
            }

            foreach (var tag in requiredTags.Tags)
            {
                if (!HasCapability(tag))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TrySetUnlocked(AbilityDefinitionSO ability, bool unlocked)
        {
            RuntimeAbility runtimeAbility = FindRuntimeAbility(ability);
            if (runtimeAbility == null)
            {
                return false;
            }

            runtimeAbility.SetUnlocked(unlocked);
            abilityUnlockEvent?.Raise(new AbilityUnlockPayload(ability, unlocked));
            return true;
        }

        private RuntimeAbility FindRuntimeAbility(AbilityDefinitionSO ability)
        {
            foreach (var abilityEntry in abilities)
            {
                if (abilityEntry.Definition == ability)
                {
                    return abilityEntry;
                }
            }

            return null;
        }
    }
}
