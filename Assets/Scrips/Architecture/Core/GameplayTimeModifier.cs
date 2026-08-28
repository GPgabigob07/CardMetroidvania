using System;

namespace TicGame.Architecture
{
    public readonly struct GameplayTimeModifier
    {
        public GameplayTimeModifier(
            GameplayTimeModifierKind kind,
            float requestedScale)
        {
            if (requestedScale < 0f
                || requestedScale > 1f
                || float.IsNaN(requestedScale)
                || float.IsInfinity(requestedScale))
            {
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(requestedScale),
                    message: "Gameplay time scale must be between zero and one.");
            }

            Kind = kind;
            RequestedScale = requestedScale;
        }

        public GameplayTimeModifierKind Kind { get; }
        public float RequestedScale { get; }
    }
}
