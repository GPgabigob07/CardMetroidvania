namespace TicGame.Architecture
{
    public readonly struct RecoveryCardQuote
    {
        public RecoveryCardQuote(float basis, float spent = 0, float gained = 0, float healthSpent = 0,
            float restored = 0, int copies = 0, int revision = 0, int transactionRevision = 0)
        {
            Basis = basis; EnergySpent = spent; EnergyGained = gained; HealthSpent = healthSpent;
            HealthRestored = restored; CopiesConsumed = copies; InventoryRevision = revision;
            TransactionRevision = transactionRevision;
        }
        public float Basis { get; }
        public float EnergySpent { get; }
        public float EnergyGained { get; }
        public float HealthSpent { get; }
        public float HealthRestored { get; }
        public int CopiesConsumed { get; }
        public int InventoryRevision { get; }
        public int TransactionRevision { get; }
        public bool HasEffect => EnergyGained > 0 || HealthRestored > 0;
    }
}
