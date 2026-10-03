using UnityEngine;

namespace TicGame.Architecture
{
    public readonly struct EnemyAttackExecutionSnapshot
    {
        public EnemyAttackExecutionSnapshot(long token, EnemyAttackDefinitionSO definition,
            EnemyAttackStep step, EnemyProjectilePattern pattern, EnemyAttackPhase phase,
            float elapsed, float duration, bool releaseDue, bool aimLocked, Vector2 aim)
        {
            ExecutionToken = token;
            Definition = definition;
            Step = step;
            Pattern = pattern;
            Phase = phase;
            Elapsed = elapsed;
            PhaseDuration = duration;
            ReleaseDue = releaseDue;
            IsAimLocked = aimLocked;
            Aim = aim;
        }

        public long ExecutionToken { get; }
        public EnemyAttackDefinitionSO Definition { get; }
        public EnemyAttackStep Step { get; }
        public EnemyProjectilePattern Pattern { get; }
        public string StepId => Step?.Id;
        public EnemyAttackPhase Phase { get; }
        public float Elapsed { get; }
        public float PhaseDuration { get; }
        public bool IsRunning => Step != null && Phase != EnemyAttackPhase.Completed;
        public bool ReleaseDue { get; }
        public bool IsAimLocked { get; }
        public Vector2 Aim { get; }
        public EnemyAttackPayloadKind Kind => Step?.Payload.Kind ?? EnemyAttackPayloadKind.Melee;
    }
}
