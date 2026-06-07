using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Architecture/Gameplay Tag", fileName = "Tag_")]
    public sealed class GameplayTagSO : ScriptableObject, IIdentified
    {
        [Header(header: "Identity")]
        [Tooltip(tooltip: "Stable id used by saves, gates and debug tooling. Falls back to the asset name when empty.")]
        [SerializeField] private string id;

        [Tooltip(tooltip: "Human-readable name shown in tools and debug UI. Falls back to id when empty.")]
        [SerializeField] private string displayName;

        [Header(header: "Notes")]
        [TextArea]
        [Tooltip(tooltip: "Optional design notes explaining when this tag should be used.")]
        [SerializeField] private string description;

        public string Id => string.IsNullOrWhiteSpace(value: id) ? name : id;
        public string DisplayName => string.IsNullOrWhiteSpace(value: displayName) ? Id : displayName;
        public string Description => description;
    }
}
