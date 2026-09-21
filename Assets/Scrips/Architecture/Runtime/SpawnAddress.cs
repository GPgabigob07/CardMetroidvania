using System;

namespace TicGame.Architecture
{
    public readonly struct SpawnAddress : IEquatable<SpawnAddress>
    {
        private readonly string areaId;
        private readonly string spawnId;

        public SpawnAddress(string areaId, string spawnId)
        {
            ValidateIdentifier(areaId, nameof(areaId));
            ValidateIdentifier(spawnId, nameof(spawnId));
            this.areaId = areaId;
            this.spawnId = spawnId;
        }

        public string AreaId => areaId;
        public string SpawnId => spawnId;
        public bool IsValid => !string.IsNullOrWhiteSpace(areaId) && !string.IsNullOrWhiteSpace(spawnId);

        public bool Equals(SpawnAddress other)
        {
            return string.Equals(areaId, other.areaId, StringComparison.Ordinal)
                && string.Equals(spawnId, other.spawnId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is SpawnAddress other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((areaId != null ? StringComparer.Ordinal.GetHashCode(areaId) : 0) * 397)
                    ^ (spawnId != null ? StringComparer.Ordinal.GetHashCode(spawnId) : 0);
            }
        }

        public override string ToString() => $"{areaId}/{spawnId}";

        public static bool operator ==(SpawnAddress left, SpawnAddress right) => left.Equals(right);
        public static bool operator !=(SpawnAddress left, SpawnAddress right) => !left.Equals(right);

        internal void ThrowIfInvalid()
        {
            ValidateIdentifier(areaId, nameof(areaId));
            ValidateIdentifier(spawnId, nameof(spawnId));
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
