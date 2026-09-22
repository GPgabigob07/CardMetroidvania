using System;
using System.Collections.Generic;

namespace TicGame.Architecture
{
    public sealed class RunProgress
    {
        private readonly HashSet<(string AreaId, string GateId)> openedGates = new();
        private SpawnAddress respawn;

        public RunProgress(SpawnAddress initialSpawn)
        {
            initialSpawn.ThrowIfInvalid();
            respawn = initialSpawn;
        }

        public SpawnAddress Respawn => respawn;
        public bool GuideDiscovered { get; private set; }

        /// <summary>
        /// Checks whether the gate key has been opened during this run.
        /// </summary>
        public bool IsGateOpen(string areaId, string gateId)
        {
            ValidateIdentifier(areaId, nameof(areaId));
            ValidateIdentifier(gateId, nameof(gateId));
            return openedGates.Contains((areaId, gateId));
        }

        /// <summary>
        /// Records an opened gate for this run; repeating the same key is harmless.
        /// </summary>
        public void OpenGate(string areaId, string gateId)
        {
            ValidateIdentifier(areaId, nameof(areaId));
            ValidateIdentifier(gateId, nameof(gateId));
            openedGates.Add((areaId, gateId));
        }

        /// <summary>
        /// Marks the shared guide as discovered for this run.
        /// </summary>
        public void DiscoverGuide() => GuideDiscovered = true;

        /// <summary>
        /// Updates the respawn address while retaining all other run progress.
        /// </summary>
        public void SetRespawn(SpawnAddress nextRespawn)
        {
            nextRespawn.ThrowIfInvalid();
            respawn = nextRespawn;
        }

        private static void ValidateIdentifier(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Identifier cannot be empty or whitespace.", parameterName);
            }
        }
    }
}
