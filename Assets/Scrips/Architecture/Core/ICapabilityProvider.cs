namespace TicGame.Architecture
{
    /// <summary>
    /// Exposes unlocked abilities and capability tags without requiring callers to know the owning component.
    /// </summary>
    public interface ICapabilityProvider
    {
        /// <summary>
        /// Returns whether the exact ability definition is currently available.
        /// </summary>
        bool HasAbility(AbilityDefinitionSO ability);

        /// <summary>
        /// Returns whether any unlocked ability provides the requested capability tag.
        /// </summary>
        bool HasCapability(GameplayTagSO capabilityTag);

        /// <summary>
        /// Returns whether all required capability tags are currently provided.
        /// </summary>
        bool HasAllCapabilities(GameplayTagSet requiredTags);
    }
}
