using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class CardTimeSelectionSlotCommandBinding
    {
        [Min(0)]
        [SerializeField] private int slotIndex;

        [SerializeField] private InputActionReference command;

        [Tooltip("Optional fallback action name resolved from the player's action map when no explicit reference is assigned.")]
        [SerializeField] private string actionName;

        [SerializeField] private string displayLabel;

        public int SlotIndex => Mathf.Max(0, slotIndex);
        public InputActionReference Command => command;
        public string ActionName => actionName;
        public string DisplayLabel => string.IsNullOrWhiteSpace(displayLabel)
            ? BuildDefaultLabel()
            : displayLabel;

        public void Configure(
            int index,
            InputActionReference actionReference,
            string fallbackActionName,
            string label)
        {
            slotIndex = Mathf.Max(0, index);
            command = actionReference;
            actionName = fallbackActionName;
            displayLabel = label;
        }

        public InputAction ResolveAction(InputActionMap fallbackMap)
        {
            if (command != null && command.action != null)
            {
                return command.action;
            }

            return !string.IsNullOrWhiteSpace(actionName)
                ? fallbackMap?.FindAction(actionName)
                : null;
        }

        private string BuildDefaultLabel()
        {
            if (command != null && command.action != null)
            {
                return command.action.name;
            }

            return !string.IsNullOrWhiteSpace(actionName)
                ? actionName
                : $"Slot {SlotIndex + 1}";
        }
    }
}
