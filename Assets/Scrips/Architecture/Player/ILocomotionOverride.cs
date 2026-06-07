namespace TicGame.Architecture
{
    public interface ILocomotionOverride
    {
        /// <summary>
        /// Modifies or replaces the locomotion frame before it is applied to the body.
        /// </summary>
        void ModifyLocomotionFrame(
            ref LocomotionFrame frame,
            PlayerContext context,
            float fixedDeltaTime);
    }
}
