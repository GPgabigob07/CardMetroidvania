namespace TicGame.Architecture
{
    public interface IPlayerAttackHitConfirmation
    {
        /// <summary>
        /// Gets whether the current attack has confirmed accepted damage.
        /// </summary>
        bool HasConfirmedHit { get; }

        /// <summary>
        /// Records accepted damage for the current attack.
        /// </summary>
        void ConfirmHit();
    }
}
