using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Architecture/Damage Profile", fileName = "Damage_")]
    public sealed class DamageProfileSO : ScriptableObject, IIdentified
    {
        [Header(header: "Identity")]
        [Tooltip(tooltip: "Stable damage profile id used by attacks, logs and debug tooling. Falls back to the asset name when empty.")]
        [SerializeField] private string id;

        [Tooltip(tooltip: "Human-readable damage profile name shown in tools. Falls back to id when empty.")]
        [SerializeField] private string displayName;

        [TextArea]
        [Tooltip(tooltip: "Optional design notes for this damage profile.")]
        [SerializeField] private string description;

        [Header(header: "Tuning")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Fallback damage amount used when a damage context does not provide an explicit amount.")]
        [SerializeField] private float baseDamage = 1f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Hit stop duration requested by this damage profile.")]
        [SerializeField] private float hitStopSeconds;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Knockback force requested by this damage profile.")]
        [SerializeField] private float knockbackForce;

        [Header(header: "Tags")]
        [Tooltip(tooltip: "Tags that describe this damage for resistances, reactions and debug filters.")]
        [SerializeField] private GameplayTagSet damageTags = new GameplayTagSet();

        public string Id => string.IsNullOrWhiteSpace(value: id) ? name : id;
        public string DisplayName => string.IsNullOrWhiteSpace(value: displayName) ? Id : displayName;
        public string Description => description;
        public float BaseDamage => baseDamage;
        public float HitStopSeconds => hitStopSeconds;
        public float KnockbackForce => knockbackForce;
        public GameplayTagSet DamageTags => damageTags;
    }
}
