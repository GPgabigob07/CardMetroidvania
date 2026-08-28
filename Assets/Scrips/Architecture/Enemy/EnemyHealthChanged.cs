namespace TicGame.Architecture
{
    public readonly struct EnemyHealthChanged
    {
        public EnemyHealthChanged(float previous, float current, float maximum)
        {
            Previous = previous;
            Current = current;
            Maximum = maximum;
        }

        public float Previous { get; }
        public float Current { get; }
        public float Maximum { get; }
        public float Normalized => Maximum > 0f ? Current / Maximum : 0f;
    }
}
