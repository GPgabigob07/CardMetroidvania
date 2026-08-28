using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class CardTimeInputDisplayBinding
    {
        [SerializeField] private CardTimeControlDeviceFamily deviceFamily;
        [Tooltip("Stable Input System action name. Leave empty to match by default control path only.")]
        [SerializeField] private string actionName;
        [Tooltip("Optional default control path from the scheme binding, such as <Gamepad>/leftShoulder.")]
        [SerializeField] private string controlPath;
        [SerializeField] private string displayLabel;

        public CardTimeControlDeviceFamily DeviceFamily => deviceFamily;
        public string ActionName => actionName;
        public string ControlPath => controlPath;
        public string DisplayLabel => displayLabel;

        public bool Matches(
            CardTimeControlDeviceFamily family,
            string action,
            string path)
        {
            return deviceFamily == family
                   && (string.IsNullOrWhiteSpace(actionName) || actionName == action)
                   && (string.IsNullOrWhiteSpace(controlPath) || controlPath == path);
        }

        public void Configure(
            CardTimeControlDeviceFamily family,
            string action,
            string path,
            string label)
        {
            deviceFamily = family;
            actionName = action;
            controlPath = path;
            displayLabel = label;
        }
    }
}
