using UnityEngine;

namespace TicGame.Architecture
{
    public readonly struct BeamGuardQuery
    {
        public BeamGuardQuery(long castToken, Vector2 start, Vector2 end, float thickness, Vector2 emitterPosition, bool isOpening)
        { CastToken = castToken; Start = start; End = end; Thickness = thickness; EmitterPosition = emitterPosition; IsOpening = isOpening; }
        public long CastToken { get; }
        public Vector2 Start { get; }
        public Vector2 End { get; }
        public float Thickness { get; }
        public Vector2 EmitterPosition { get; }
        public bool IsOpening { get; }
    }
}
