using System;

namespace TicGame.Architecture
{
    public sealed class PlayerAnimationSnapshotPublisher
    {
        public event Action<PlayerAnimationTransition> Changed;

        public PlayerAnimationSnapshot Current { get; private set; }
        public bool HasCurrent { get; private set; }

        public void Publish(PlayerAnimationSnapshot snapshot)
        {
            var previous = Current;
            var hasPrevious = HasCurrent;
            var hasStructuralChange = !hasPrevious || previous != snapshot;

            Current = snapshot;
            HasCurrent = true;

            if (hasStructuralChange)
            {
                Changed?.Invoke(new PlayerAnimationTransition(
                    previous: previous,
                    current: snapshot,
                    hasPrevious: hasPrevious));
            }
        }
    }
}
