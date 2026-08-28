namespace TicGame.Architecture
{
    public interface IGameplayTimeService
    {
        /// <summary>
        /// Gets the currently resolved gameplay time scale.
        /// </summary>
        float EffectiveTimeScale { get; }

        /// <summary>
        /// Creates or replaces the time modifier owned by the supplied key.
        /// </summary>
        void SetModifier(object owner, GameplayTimeModifier modifier);

        /// <summary>
        /// Removes the time modifier owned by the supplied key.
        /// </summary>
        bool RemoveModifier(object owner);
    }
}
