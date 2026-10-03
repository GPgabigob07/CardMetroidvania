using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class EnemyAttackRunner
    {
        private readonly HashSet<long> usedTokens = new HashSet<long>();
        private EnemyAttackDefinitionSO definition;
        private EnemyAttackStep[] steps;
        private EnemyAttackStep step;
        private EnemyProjectilePattern pattern;
        private EnemyAttackPhase phase = EnemyAttackPhase.Completed;
        private int stepIndex;
        private long token;
        private float elapsed;
        private float surplus;
        private Vector2 aim = Vector2.right;
        private bool aimLocked;
        private bool sampled;
        private bool released;
        private bool warnedInvalid;

        public EnemyAttackExecutionSnapshot Current => new EnemyAttackExecutionSnapshot(token,
            definition, step, pattern, phase, elapsed, Duration, Running && phase == EnemyAttackPhase.Active && !released,
            aimLocked, aim);

        private bool Running => phase != EnemyAttackPhase.Completed;
        private float Duration => !Running ? 0 : phase switch
        {
            EnemyAttackPhase.Windup => step.Payload.Kind == EnemyAttackPayloadKind.Volley ? pattern.WindupDuration : step.WindupDuration,
            EnemyAttackPhase.Active => step.ActiveDuration,
            _ => step.RecoveryDuration
        };

        public bool Begin(EnemyAttackDefinitionSO value, long executionToken, int volleyCount = 0)
        {
            if (Running || executionToken <= 0 || usedTokens.Contains(executionToken)
                || value == null || !value.TryCaptureSteps(out var captured)) return false;
            var selectedPattern = value.FindPattern(volleyCount);
            if (captured.Any(candidate => candidate.Payload.Kind == EnemyAttackPayloadKind.Volley)
                && (selectedPattern == null || selectedPattern.GetValidationErrors().Count != 0
                    || captured.Any(candidate => candidate.Payload.Kind == EnemyAttackPayloadKind.Volley
                        && candidate.Payload.LockedAimDuration > selectedPattern.WindupDuration))) return false;

            usedTokens.Add(executionToken);
            definition = value;
            steps = captured;
            pattern = selectedPattern?.Copy();
            token = executionToken;
            stepIndex = 0;
            step = steps[0];
            phase = EnemyAttackPhase.Windup;
            elapsed = surplus = 0;
            aimLocked = sampled = released = warnedInvalid = false;
            return true;
        }

        public void SetAim(Vector2 direction)
        {
            if (!Running || aimLocked || !float.IsFinite(direction.x) || !float.IsFinite(direction.y)
                || direction.sqrMagnitude == 0) return;
            aim = direction.normalized;
        }

        public void Tick(float scaledDelta)
        {
            if (!float.IsFinite(scaledDelta) || scaledDelta < 0) throw new ArgumentOutOfRangeException(nameof(scaledDelta));
            if (!Running) return;
            Refresh();
            surplus += scaledDelta;
            while (Running)
            {
                // Surplus cannot erase the first actual physics sample of a damaging phase.
                if (phase == EnemyAttackPhase.Active && !sampled) return;
                elapsed += surplus;
                surplus = 0;
                if (phase == EnemyAttackPhase.Windup && elapsed >= Duration - step.Payload.LockedAimDuration)
                    aimLocked = true;
                if (elapsed < Duration) return;
                surplus = elapsed - Duration;
                elapsed = 0;
                switch (phase)
                {
                    case EnemyAttackPhase.Windup:
                        aimLocked = true;
                        phase = EnemyAttackPhase.Active;
                        break;
                    case EnemyAttackPhase.Active:
                        phase = EnemyAttackPhase.Recovery;
                        break;
                    case EnemyAttackPhase.Recovery:
                        if (++stepIndex >= steps.Length)
                        {
                            phase = EnemyAttackPhase.Completed;
                            surplus = 0;
                            return;
                        }
                        step = steps[stepIndex];
                        phase = EnemyAttackPhase.Windup;
                        aimLocked = sampled = released = false;
                        Refresh();
                        break;
                }
            }
        }

        public void ConfirmPhysicsSample(long executionToken, string stepId)
        {
            if (Matches(executionToken, stepId)) sampled = true;
        }

        public bool TryConsumeRelease(long executionToken, string stepId)
        {
            if (!Matches(executionToken, stepId) || released) return false;
            released = true;
            return true;
        }

        public void Cancel()
        {
            phase = EnemyAttackPhase.Completed;
            elapsed = surplus = 0;
            sampled = released = false;
        }

        private bool Matches(long executionToken, string stepId) => Running && phase == EnemyAttackPhase.Active
            && executionToken == token && string.Equals(stepId, step.Id, StringComparison.Ordinal);

        private void Refresh()
        {
            if (definition == null || definition.GetValidationErrors().Count != 0)
            {
                WarnInvalid();
                return;
            }
            // A valid graph replacement applies at Begin; removed IDs retain captured data.
            if (!definition.TryReadStep(step.Id, out var candidate)) { warnedInvalid = false; return; }
            candidate = candidate.CopyForKind(steps[stepIndex].Payload.Kind);
            var candidatePattern = candidate.Payload.Kind == EnemyAttackPayloadKind.Volley
                ? definition.FindPattern(pattern.Count)?.Copy() ?? pattern : pattern;
            if (candidate.GetValidationErrors().Count != 0
                || (candidate.Payload.Kind == EnemyAttackPayloadKind.Volley
                    && (candidatePattern.GetValidationErrors().Count != 0
                        || candidate.Payload.LockedAimDuration > candidatePattern.WindupDuration)))
            {
                WarnInvalid();
                return;
            }
            step = candidate;
            pattern = candidatePattern;
            warnedInvalid = false;
        }

        private void WarnInvalid()
        {
            if (warnedInvalid) return;
            warnedInvalid = true;
            Debug.LogWarning("Invalid live enemy attack revision; retaining last valid execution data.");
        }
    }
}
