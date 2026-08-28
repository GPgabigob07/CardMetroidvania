namespace TicGame.Architecture
{
    public sealed class PlayerAttackAnimationMapper
    {
        public PlayerAnimationState Map(
            in PlayerAnimationSnapshot snapshot
        ) {
            return snapshot.Action switch {
                PlayerActionState.Attack1 => ResolvePhase(
                    phase: snapshot.ActionPhase,
                    reading: PlayerAnimationState.Attack1Reading,
                    execution: PlayerAnimationState.Attack1Execution,
                    recovery: PlayerAnimationState.Attack1Recovery),
                PlayerActionState.Attack2 => ResolvePhase(
                    phase: snapshot.ActionPhase,
                    reading: PlayerAnimationState.Attack2Reading,
                    execution: PlayerAnimationState.Attack2Execution,
                    recovery: PlayerAnimationState.Attack2Recovery),
                PlayerActionState.Attack3 => ResolvePhase(
                    phase: snapshot.ActionPhase,
                    reading: PlayerAnimationState.Attack3Reading,
                    execution: PlayerAnimationState.Attack3Execution,
                    recovery: PlayerAnimationState.Attack3Recovery),
                _ => PlayerAnimationState.Idle
            };
        }

        private static PlayerAnimationState ResolvePhase(
            PlayerActionPhase phase,
            PlayerAnimationState reading,
            PlayerAnimationState execution,
            PlayerAnimationState recovery
        ) {
            switch (phase) {
                case PlayerActionPhase.Execution: return execution;
                case PlayerActionPhase.Recovery: return recovery;
                default: return reading;
            }
        }
    }
}
