using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public struct CardLifetimeDefinition
    {
        [SerializeField] private CardLifetimeKind kind;

        public CardLifetimeDefinition(CardLifetimeKind kind)
        {
            this.kind = kind;
        }

        public CardLifetimeKind Kind => kind;
    }
}
