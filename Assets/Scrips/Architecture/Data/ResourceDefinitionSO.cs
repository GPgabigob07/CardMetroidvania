using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Architecture/Resource Definition", fileName = "Resource_")]
    public sealed class ResourceDefinitionSO : ScriptableObject, IIdentified
    {
        [Header("Identity")]
        [Tooltip("Stable resource id used by cards, saves, and debug tooling.")]
        [SerializeField] private string id;

        [Tooltip("Human-readable resource name.")]
        [SerializeField] private string displayName;

        public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
    }
}
