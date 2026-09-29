using System;

namespace TicGame.Architecture
{
    /// <summary>Stops time without mutating physics state or other time owners.</summary>
    public sealed class PlaytestPauseLease : IDisposable
    {
        private IGameplayTimeService time;

        public PlaytestPauseLease(IGameplayTimeService time)
        {
            this.time = time ?? throw new ArgumentNullException(nameof(time));
            time.SetModifier(this, new GameplayTimeModifier(GameplayTimeModifierKind.Pause, 0f));
        }

        public void Dispose()
        {
            time?.RemoveModifier(this);
            time = null;
        }
    }
}
