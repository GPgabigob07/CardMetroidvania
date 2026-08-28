namespace TicGame.Architecture
{
    public interface IRandomRollSource
    {
        /// <summary>
        /// Returns a normalized random value in the range zero through one.
        /// </summary>
        float NextNormalized();
    }
}
