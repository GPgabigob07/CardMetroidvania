using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public struct CardStackingDefinition
    {
        [SerializeField] private CardStackingKind kind;
        [Min(0)]
        [SerializeField] private int maximumCharges;
        [Min(0)]
        [SerializeField] private int maximumCapacity;
        [Min(0)]
        [SerializeField] private int maximumStacks;

        public CardStackingDefinition(
            CardStackingKind kind,
            int maximumCharges = 0,
            int maximumCapacity = 0,
            int maximumStacks = 0)
        {
            this.kind = kind;
            this.maximumCharges = Mathf.Max(0, maximumCharges);
            this.maximumCapacity = Mathf.Max(0, maximumCapacity);
            this.maximumStacks = Mathf.Max(0, maximumStacks);
        }

        public CardStackingKind Kind => kind;
        public int MaximumCharges => maximumCharges;
        public int MaximumCapacity => maximumCapacity;
        public int MaximumStacks => maximumStacks;
    }
}
