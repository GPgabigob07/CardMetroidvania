namespace TicGame.Architecture
{
    public enum SceneRequestOutcome
    {
        Succeeded,
        Superseded,
        Failed
    }

    public sealed class SceneRequestResult
    {
        public SceneRequestOutcome Outcome { get; }
        public string Error { get; }

        public SceneRequestResult(SceneRequestOutcome outcome, string error = null)
        {
            Outcome = outcome;
            Error = error;
        }
    }
}
