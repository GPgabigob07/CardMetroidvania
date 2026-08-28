namespace TicGame.Architecture
{
    public readonly struct EnemyDamageEvent
    {
        public EnemyDamageEvent(DamageContext context, DamageResult result)
        {
            Context = context;
            Result = result;
        }

        public DamageContext Context { get; }
        public DamageResult Result { get; }
    }
}
