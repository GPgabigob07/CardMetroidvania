using System;

namespace TicGame.Architecture
{
    public readonly struct CardTimeOpportunity : IEquatable<CardTimeOpportunity>
    {
        public CardTimeOpportunity(PlayerCardTimeState category, long opportunityId)
        {
            if (category == PlayerCardTimeState.None || opportunityId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(opportunityId),
                    "An offered Card Time opportunity needs a category and a positive ID.");
            }

            Category = category;
            OpportunityId = opportunityId;
        }

        public static CardTimeOpportunity None => default;
        public PlayerCardTimeState Category { get; }
        public long OpportunityId { get; }
        public bool IsValid => Category != PlayerCardTimeState.None && OpportunityId > 0;

        public bool Equals(CardTimeOpportunity other)
        {
            return Category == other.Category && OpportunityId == other.OpportunityId;
        }

        public override bool Equals(object obj)
        {
            return obj is CardTimeOpportunity other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Category * 397) ^ OpportunityId.GetHashCode();
            }
        }

        public static bool operator ==(CardTimeOpportunity left, CardTimeOpportunity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CardTimeOpportunity left, CardTimeOpportunity right)
        {
            return !left.Equals(right);
        }
    }
}
