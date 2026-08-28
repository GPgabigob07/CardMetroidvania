namespace TicGame.Architecture
{
    public interface IPlayerAnimationMapper
    {
        /// <summary>
        /// Resolves one presentation command from a gameplay snapshot transition.
        /// </summary>
        PlayerAnimationCommand Map(in PlayerAnimationTransition transition);
    }
}
