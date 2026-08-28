using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class CardTimeControlSchemeSlotBinding
    {
        [Min(0)]
        [SerializeField] private int slotIndex;

        [Tooltip("Stable Input System action name resolved from the active player action map.")]
        [SerializeField] private string actionName;

        [SerializeField] private string displayLabel;

        [Tooltip("Documentation/UI hint for the default binding, such as <Keyboard>/q.")]
        [SerializeField] private string defaultControlPath;

        public int SlotIndex => Mathf.Max(0, slotIndex);
        public string ActionName => actionName;
        public string DisplayLabel => string.IsNullOrWhiteSpace(displayLabel)
            ? actionName
            : displayLabel;
        public string DefaultControlPath => defaultControlPath;

        public void Configure(
            int index,
            string action,
            string label,
            string controlPath)
        {
            slotIndex = Mathf.Max(0, index);
            actionName = action;
            displayLabel = label;
            defaultControlPath = controlPath;
        }
    }
}
