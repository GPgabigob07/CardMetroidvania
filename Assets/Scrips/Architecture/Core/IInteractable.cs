namespace TicGame.Architecture
{
    /// <summary>
    /// Defines an object that can be queried and activated through an interaction context.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Returns whether the interaction can be executed for the supplied context.
        /// </summary>
        bool CanInteract(in InteractionContext context);

        /// <summary>
        /// Executes the interaction for the supplied context.
        /// </summary>
        void Interact(in InteractionContext context);
    }
}
