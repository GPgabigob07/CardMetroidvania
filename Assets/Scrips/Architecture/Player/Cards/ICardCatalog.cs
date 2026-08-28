namespace TicGame.Architecture
{
    public interface ICardCatalog
    {
        /// <summary>
        /// Resolves a stable card id to its authored card definition.
        /// </summary>
        bool TryGetCard(string id, out CardDefinitionSO card);
    }
}
