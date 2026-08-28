namespace TicGame.Architecture
{
    public readonly struct SimpleHealthChanged
    {
        public SimpleHealthChanged(float previous, float current, float maximum)
        {
            Previous = previous;
            Current = current;
            Maximum = maximum;
        }

        public float Previous { get; }
        public float Current { get; }
        public float Maximum { get; }
    }
}
