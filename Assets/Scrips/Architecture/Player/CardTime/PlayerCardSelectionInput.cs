using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TicGame.Architecture
{
    public sealed class PlayerCardSelectionInput : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private CardTimeSelectionUiConfigSO selectionUiConfig;
        [SerializeField] private CardTimeControlSchemeProfileSO controlSchemeProfile;
        [SerializeField] private string selectedSchemeId;

        [Header("Navigation")]
        [SerializeField] [Range(0.1f, 0.95f)]
        private float axisDeadZone = 0.45f;

        [SerializeField] [Min(0f)]
        private float initialRepeatDelay = 0.22f;

        [SerializeField] [Min(0.01f)]
        private float repeatInterval = 0.12f;

        private CardTimeSelectionTransaction selection;
        private InputActionMap fallbackActionMap;
        private CardTimeControlSchemeSO activeScheme;
        private int heldDirection;
        private float repeatTimer;

        public event Action<CardTimeSelectionSlotCommand> SlotCommanded;

        private void OnEnable()
        {
            SetConfiguredActionsEnabled(true);
        }

        private void OnDisable()
        {
            SetConfiguredActionsEnabled(false);
        }

        public void Configure(
            CardTimeSelectionUiConfigSO config,
            CardTimeControlSchemeProfileSO schemeProfile,
            string schemeId,
            InputActionMap fallbackMap)
        {
            SetConfiguredActionsEnabled(false);
            selectionUiConfig = config;
            controlSchemeProfile = schemeProfile;
            selectedSchemeId = schemeId;
            fallbackActionMap = fallbackMap;
            activeScheme = controlSchemeProfile != null
                ? controlSchemeProfile.ResolveScheme(selectedSchemeId)
                : null;
            SetConfiguredActionsEnabled(isActiveAndEnabled);
        }

        public void BindSelection(CardTimeSelectionTransaction transaction)
        {
            selection = transaction;
            heldDirection = 0;
            repeatTimer = 0f;
        }

        public void ClearSelection(CardTimeSelectionTransaction transaction)
        {
            if (selection != transaction)
            {
                return;
            }

            ClearSelection();
        }

        public void ClearSelection()
        {
            selection = null;
            heldDirection = 0;
            repeatTimer = 0f;
        }

        public bool TickSlotCommands(PlayerCardTimeState category)
        {
            if (selection == null || !selection.IsValid)
            {
                ClearSelection();
                return false;
            }

            foreach (var binding in activeScheme?.GetSlots(category)
                         ?? System.Array.Empty<CardTimeControlSchemeSlotBinding>())
            {
                var action = ResolveAction(binding?.ActionName);
                if (action != null && action.WasPressedThisFrame())
                {
                    TryCommandSlot(binding.SlotIndex);
                    return true;
                }
            }

            return false;
        }

        public bool TryCommandSlot(int slotIndex)
        {
            if (selection == null || !selection.IsValid)
            {
                ClearSelection();
                return false;
            }

            var selected = selection.SelectIndex(slotIndex);
            SlotCommanded?.Invoke(new CardTimeSelectionSlotCommand(slotIndex, selected));
            return selected;
        }

        public bool TickNavigation(Vector2 navigation, float unscaledDeltaTime)
        {
            if (selection == null || !selection.IsValid)
            {
                ClearSelection();
                return false;
            }

            var direction = ResolveDirection(navigation);
            if (direction == 0)
            {
                heldDirection = 0;
                repeatTimer = 0f;
                return false;
            }

            repeatTimer -= Mathf.Max(0f, unscaledDeltaTime);
            var directionChanged = direction != heldDirection;
            if (!directionChanged && repeatTimer > 0f)
            {
                return false;
            }

            heldDirection = direction;
            repeatTimer = directionChanged ? initialRepeatDelay : repeatInterval;
            return selection.MoveSelection(direction);
        }

        private void SetConfiguredActionsEnabled(bool enabled)
        {
            if (activeScheme == null)
            {
                return;
            }

            foreach (var layout in activeScheme.Layouts)
            {
                if (layout == null)
                {
                    continue;
                }

                foreach (var binding in layout.Slots)
                {
                    var action = ResolveAction(binding?.ActionName);
                    if (action == null)
                    {
                        continue;
                    }

                    if (enabled)
                    {
                        action.Enable();
                    }
                    else
                    {
                        action.Disable();
                    }
                }
            }
        }

        private InputAction ResolveAction(string actionName)
        {
            return !string.IsNullOrWhiteSpace(actionName)
                ? fallbackActionMap?.FindAction(actionName)
                : null;
        }

        private int ResolveDirection(Vector2 navigation)
        {
            if (navigation.sqrMagnitude < axisDeadZone * axisDeadZone)
            {
                return 0;
            }

            if (Mathf.Abs(navigation.x) >= Mathf.Abs(navigation.y))
            {
                return navigation.x > 0f ? 1 : -1;
            }

            return navigation.y > 0f ? 1 : -1;
        }
    }
}
