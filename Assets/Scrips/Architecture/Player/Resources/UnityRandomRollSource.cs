using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class UnityRandomRollSource : IRandomRollSource
    {
        public float NextNormalized()
        {
            return Random.value;
        }
    }
}
