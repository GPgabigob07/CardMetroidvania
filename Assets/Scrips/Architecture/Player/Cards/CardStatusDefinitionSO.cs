using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(
        menuName = "TIC/Cards/Status Definition",
        fileName = "Status_")]
    public sealed class CardStatusDefinitionSO : ScriptableObject, IIdentified
    {
        [Header("Identity")]
        [Tooltip("Stable status id used for stacking and runtime lookup. Falls back to the asset name.")]
        [SerializeField] private string id;

        [Tooltip("Human-readable status name shown by debug tools. Falls back to the id.")]
        [SerializeField] private string displayName;

        public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;

        public void Configure(string stableId, string nameForDisplay)
        {
            id = stableId;
            displayName = nameForDisplay;
        }
    }
}
