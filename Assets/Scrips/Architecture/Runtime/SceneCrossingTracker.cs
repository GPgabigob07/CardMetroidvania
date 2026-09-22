namespace TicGame.Architecture
{
    public enum SceneTriggerSide { A, B }
    public enum SceneFlowAction { None, Load, Unload }

    public sealed class SceneCrossingTracker
    {
        private SceneTriggerSide? lastSide;

        public SceneFlowAction Enter(SceneTriggerSide side, SceneFlowAction aToB, SceneFlowAction bToA)
        {
            var previous = lastSide;
            lastSide = side;
            if (!previous.HasValue || previous.Value == side) return SceneFlowAction.None;
            return side == SceneTriggerSide.B ? aToB : bToA;
        }
    }
}
