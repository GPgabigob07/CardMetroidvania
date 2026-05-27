using System;
using UnityEngine;

namespace TicGame.Architecture
{
    public enum AbilityKind
    {
        Movement = 0,
        Combat = 10,
        CardTime = 20,
        Interaction = 30,
        Passive = 40
    }

    [CreateAssetMenu(menuName = "TIC/Architecture/Ability Definition", fileName = "Ability_")]
    public sealed class AbilityDefinitionSO : ScriptableObject, IIdentified
    {
        [Header("Identity")]
        [Tooltip("Stable ability id used by saves, gates and debug tooling. Falls back to the asset name when empty.")]
        [SerializeField] private string id;

        [Tooltip("Human-readable ability name shown in tools and UI. Falls back to id when empty.")]
        [SerializeField] private string displayName;

        [TextArea]
        [Tooltip("Short design description of what this ability enables.")]
        [SerializeField] private string description;

        [Header("Classification")]
        [Tooltip("Broad gameplay role for this ability.")]
        [SerializeField] private AbilityKind kind;

        [Tooltip("Whether this ability starts unlocked without save/progression data.")]
        [SerializeField] private bool unlockedByDefault;

        [Header("Runtime Hooks")]
        [Tooltip("Input action id used by the Input System or input adapter.")]
        [SerializeField] private string inputActionId;

        [Tooltip("Animator trigger name fired when this ability executes.")]
        [SerializeField] private string animationTrigger;

        [Header("Costs")]
        [Min(0f)]
        [Tooltip("Generic resource cost paid when the ability executes.")]
        [SerializeField] private float resourceCost;

        [Min(0f)]
        [Tooltip("Cooldown in seconds before the ability can execute again.")]
        [SerializeField] private float cooldownSeconds;

        [Header("Gating")]
        [Tooltip("Capability tags provided when this ability is unlocked.")]
        [SerializeField] private GameplayTagSet gatingTags = new GameplayTagSet();

        public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
        public string Description => description;
        public AbilityKind Kind => kind;
        public bool UnlockedByDefault => unlockedByDefault;
        public string InputActionId => inputActionId;
        public string AnimationTrigger => animationTrigger;
        public float ResourceCost => resourceCost;
        public float CooldownSeconds => cooldownSeconds;
        public GameplayTagSet GatingTags => gatingTags;
    }

    [Serializable]
    public struct AbilityUnlockPayload
    {
        [SerializeField] private AbilityDefinitionSO ability;
        [SerializeField] private bool unlocked;

        public AbilityUnlockPayload(AbilityDefinitionSO ability, bool unlocked)
        {
            this.ability = ability;
            this.unlocked = unlocked;
        }

        public AbilityDefinitionSO Ability => ability;
        public bool Unlocked => unlocked;
    }
}
