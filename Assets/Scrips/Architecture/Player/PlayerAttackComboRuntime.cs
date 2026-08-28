using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerAttackComboRuntime
    {
        private PlayerActionState bufferedState;
        private PlayerActionState postRecoveryFollowUpState;
        private float postRecoveryGraceRemaining;
        private float restartCooldownRemaining;
        private bool isWaitingForRestart;
        private bool cardTimeConsumed;

        public bool HasBufferedFollowUp => bufferedState != PlayerActionState.None;
        public bool HasPostRecoveryFollowUp => postRecoveryFollowUpState != PlayerActionState.None
            && postRecoveryGraceRemaining > 0f;
        public bool CanRestartSequence => restartCooldownRemaining <= 0f;
        public PlayerCardTimeState CurrentCardTime { get; private set; } =
            PlayerCardTimeState.Neutral;
        public PlayerCardTimeState AvailableCardTime => cardTimeConsumed
            ? PlayerCardTimeState.None
            : CurrentCardTime;

        public void Tick(float deltaTime)
        {
            postRecoveryGraceRemaining = Mathf.Max(
                a: 0f,
                b: postRecoveryGraceRemaining - deltaTime);
            restartCooldownRemaining = Mathf.Max(
                a: 0f,
                b: restartCooldownRemaining - deltaTime);

            if (postRecoveryGraceRemaining <= 0f)
            {
                postRecoveryFollowUpState = PlayerActionState.None;
            }

            if (isWaitingForRestart && restartCooldownRemaining <= 0f)
            {
                isWaitingForRestart = false;
                SetCardTime(PlayerCardTimeState.Neutral);
            }
        }

        public bool TryBuffer(
            PlayerActionState currentState,
            IPlayerChainBufferSource chainBufferSource)
        {
            if (HasBufferedFollowUp
                || chainBufferSource == null
                || !chainBufferSource.CanBufferFollowUp)
            {
                return false;
            }

            var nextState = PlayerAttackSequence.GetNext(current: currentState);
            if (nextState == PlayerActionState.None)
            {
                return false;
            }

            bufferedState = nextState;
            return true;
        }

        public bool TryConsume(
            IPlayerChainBufferSource chainBufferSource,
            out PlayerActionState followUpState)
        {
            if (!HasBufferedFollowUp
                || chainBufferSource == null
                || !chainBufferSource.CanCommitFollowUp)
            {
                followUpState = PlayerActionState.None;
                return false;
            }

            followUpState = bufferedState;
            bufferedState = PlayerActionState.None;
            return true;
        }

        public void NotifyAttackCompleted(
            PlayerActionState completedState,
            float postRecoveryBufferGraceDuration,
            float sequenceRestartCooldown)
        {
            var nextState = bufferedState != PlayerActionState.None
                ? bufferedState
                : PlayerAttackSequence.GetNext(current: completedState);

            bufferedState = PlayerActionState.None;
            postRecoveryFollowUpState = nextState;
            postRecoveryGraceRemaining = nextState == PlayerActionState.None
                ? 0f
                : Mathf.Max(a: 0f, b: postRecoveryBufferGraceDuration);
            restartCooldownRemaining = Mathf.Max(a: 0f, b: sequenceRestartCooldown);
            isWaitingForRestart = true;
        }

        public bool TryResolveIdleAttack(out PlayerActionState attackState)
        {
            if (HasPostRecoveryFollowUp)
            {
                attackState = postRecoveryFollowUpState;
                ResetTiming();
                return true;
            }

            if (CanRestartSequence)
            {
                attackState = PlayerActionState.Attack1;
                ResetTiming();
                return true;
            }

            attackState = PlayerActionState.None;
            return false;
        }

        public void NotifyAttackStarted(PlayerActionState state)
        {
            ResetTiming();
            isWaitingForRestart = false;
            SetCardTime(PlayerAttackSequence.GetCardTime(state));
        }

        public void ConsumeCardTime()
        {
            cardTimeConsumed = true;
        }

        public void RestoreNeutralCardTime()
        {
            CurrentCardTime = PlayerCardTimeState.Neutral;
            cardTimeConsumed = false;
        }

        public void Clear()
        {
            bufferedState = PlayerActionState.None;
            ResetTiming();
            isWaitingForRestart = false;
            cardTimeConsumed = false;
            SetCardTime(PlayerCardTimeState.Neutral);
        }

        private void ResetTiming()
        {
            postRecoveryFollowUpState = PlayerActionState.None;
            postRecoveryGraceRemaining = 0f;
            restartCooldownRemaining = 0f;
        }

        private void SetCardTime(PlayerCardTimeState next)
        {
            if (CurrentCardTime != next)
            {
                cardTimeConsumed = false;
            }

            CurrentCardTime = next;
        }
    }
}
