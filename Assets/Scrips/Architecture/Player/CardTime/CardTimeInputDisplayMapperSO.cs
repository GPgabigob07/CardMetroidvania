using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(
        menuName = "TIC/Cards/Card Time Input Display Mapper",
        fileName = "CardTimeInputDisplayMapper_")]
    public sealed class CardTimeInputDisplayMapperSO : ScriptableObject
    {
        [SerializeField] private List<CardTimeInputDisplayBinding> bindings = new();

        public IReadOnlyList<CardTimeInputDisplayBinding> Bindings => bindings;

        public string ResolveLabel(
            CardTimeControlSchemeSO scheme,
            PlayerCardTimeState category,
            int slotIndex)
        {
            var slot = scheme != null ? scheme.GetSlot(category, slotIndex) : null;
            return ResolveLabel(scheme, slot, slotIndex);
        }

        public string ResolveLabel(
            CardTimeControlSchemeSO scheme,
            CardTimeControlSchemeSlotBinding slot,
            int slotIndex)
        {
            if (slot != null)
            {
                foreach (var binding in bindings)
                {
                    if (binding != null
                        && binding.Matches(
                            scheme != null
                                ? scheme.DeviceFamily
                                : CardTimeControlDeviceFamily.KeyboardMouse,
                            slot.ActionName,
                            slot.DefaultControlPath)
                        && !string.IsNullOrWhiteSpace(binding.DisplayLabel))
                    {
                        return binding.DisplayLabel;
                    }
                }
            }

            if (slot != null && !string.IsNullOrWhiteSpace(slot.DisplayLabel))
            {
                return slot.DisplayLabel;
            }

            if (slot != null && !string.IsNullOrWhiteSpace(slot.DefaultControlPath))
            {
                return FormatControlPath(slot.DefaultControlPath);
            }

            if (slot != null && !string.IsNullOrWhiteSpace(slot.ActionName))
            {
                return slot.ActionName;
            }

            return (slotIndex + 1).ToString();
        }

        public string ResolveActionLabel(
            CardTimeControlDeviceFamily family,
            string actionName,
            string fallbackLabel,
            string defaultControlPath = "")
        {
            foreach (var binding in bindings)
            {
                if (binding != null
                    && binding.Matches(family, actionName, defaultControlPath)
                    && !string.IsNullOrWhiteSpace(binding.DisplayLabel))
                {
                    return binding.DisplayLabel;
                }
            }

            if (!string.IsNullOrWhiteSpace(fallbackLabel))
            {
                return fallbackLabel;
            }

            if (!string.IsNullOrWhiteSpace(defaultControlPath))
            {
                return FormatControlPath(defaultControlPath);
            }

            return actionName ?? string.Empty;
        }

        public void Configure(IEnumerable<CardTimeInputDisplayBinding> displayBindings)
        {
            bindings = displayBindings != null
                ? new List<CardTimeInputDisplayBinding>(displayBindings)
                : new List<CardTimeInputDisplayBinding>();
        }

        public void ConfigurePrototypeDefaults()
        {
            var defaults = new List<CardTimeInputDisplayBinding>();
            var gamepadLabels = new[]
            {
                "LB",
                "RB",
                "LT",
                "RT",
                "X",
                "Y",
                "A",
                "B"
            };

            var chordBindings = new[]
            {
                (CardTimeControlDeviceFamily.KeyboardMouse, "CardTimeLeft", "J", "<Keyboard>/j"),
                (CardTimeControlDeviceFamily.KeyboardMouse, "CardTimeRight", "K", "<Keyboard>/k"),
                (CardTimeControlDeviceFamily.Gamepad, "CardTimeLeft", "LB", "<Gamepad>/leftShoulder"),
                (CardTimeControlDeviceFamily.Gamepad, "CardTimeRight", "RB", "<Gamepad>/rightShoulder")
            };

            foreach (var (family, action, label, path) in chordBindings)
            {
                var binding = new CardTimeInputDisplayBinding();
                binding.Configure(family, action, path, label);
                defaults.Add(binding);
            }

            for (var index = 0; index < gamepadLabels.Length; index++)
            {
                var binding = new CardTimeInputDisplayBinding();
                binding.Configure(
                    CardTimeControlDeviceFamily.Gamepad,
                    $"CardSlotGamepad{index + 1}",
                    string.Empty,
                    gamepadLabels[index]);
                defaults.Add(binding);
            }

            bindings = defaults;
        }

        private static string FormatControlPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var slash = path.LastIndexOf('/');
            var token = slash >= 0 && slash < path.Length - 1
                ? path[(slash + 1)..]
                : path;
            return token switch
            {
                "leftShoulder" => "LB",
                "rightShoulder" => "RB",
                "leftTrigger" => "LT",
                "rightTrigger" => "RT",
                "buttonSouth" => "A",
                "buttonEast" => "B",
                "buttonWest" => "X",
                "buttonNorth" => "Y",
                _ => token.ToUpperInvariant()
            };
        }
    }
}
