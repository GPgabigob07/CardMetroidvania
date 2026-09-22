using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Areas/Area Definition", fileName = "AreaDefinition")]
    public sealed class AreaDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable identifier used by streamed area state and spawn addresses.")]
        [SerializeField] private string areaId;

        [Header("Scene")]
        [Tooltip("Complete project-relative path to the scene asset. Assign it with the custom Inspector picker.")]
        [SerializeField] private string scenePath;

        [Header("Default Spawn")]
        [Tooltip("Spawn marker used when this area is first entered without a more specific address.")]
        [SerializeField] private string defaultSpawnId;

        public string AreaId => areaId;
        public string ScenePath => scenePath;
        public string DefaultSpawnId => defaultSpawnId;

        /// <summary>
        /// Assigns the authored area identity and validates the required values.
        /// </summary>
        public void Configure(string configuredAreaId, string configuredScenePath, string configuredDefaultSpawnId)
        {
            ValidateIdentifier(configuredAreaId, nameof(configuredAreaId));
            ValidateIdentifier(configuredDefaultSpawnId, nameof(configuredDefaultSpawnId));
            if (string.IsNullOrWhiteSpace(configuredScenePath))
            {
                throw new ArgumentException("A scene path is required.", nameof(configuredScenePath));
            }

            areaId = configuredAreaId;
            scenePath = configuredScenePath;
            defaultSpawnId = configuredDefaultSpawnId;
        }

        /// <summary>
        /// Reports whether this definition can participate in a streamed composition.
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (!TryValidateIdentifier(areaId, nameof(areaId), out error)) return false;
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                error = "Scene path is required.";
                return false;
            }

            return TryValidateIdentifier(defaultSpawnId, nameof(defaultSpawnId), out error);
        }

        private static void ValidateIdentifier(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Identifier cannot be empty or whitespace.", parameterName);
            }
        }

        private static bool TryValidateIdentifier(string value, string fieldName, out string error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                error = $"{fieldName} cannot be empty or whitespace.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
