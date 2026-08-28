namespace TicGame.Architecture
{
    public interface IPlayerAttackExecution
    {
        /// <summary>
        /// Gets the stable identity of this individual attack execution.
        /// </summary>
        string ExecutionId { get; }
    }
}
