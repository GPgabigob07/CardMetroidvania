namespace TicGame.Architecture
{
    /// <summary>Serializes transitions and rejects completions from an older operation.</summary>
    public sealed class PlaytestTransitionGate
    {
        private int generation;
        public bool IsBusy { get; private set; }

        public int TryBegin()
        {
            if (IsBusy) return 0;
            IsBusy = true;
            return ++generation;
        }

        public bool Complete(int token)
        {
            if (!IsBusy || token != generation) return false;
            IsBusy = false;
            return true;
        }
    }
}
