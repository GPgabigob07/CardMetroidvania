using System;

namespace TicGame.Architecture
{
    public sealed class PlayerActionRunner
    {
        public IPlayerAction CurrentAction { get; private set; }
        public PlayerActionState CurrentState => CurrentAction?.State ?? PlayerActionState.None;
        public ILocomotionOverride CurrentLocomotionOverride => CurrentAction as ILocomotionOverride;
        public bool HasAction => CurrentAction != null;

        public bool TryStartAction(PlayerContext context, IPlayerAction action, bool replaceCurrent = false)
        {
            if (action == null)
            {
                throw new ArgumentNullException(paramName: nameof(action));
            }

            if (CurrentAction != null && !replaceCurrent)
            {
                return false;
            }

            CurrentAction?.Exit(context: context);
            CurrentAction = action;
            context.ClearAnimatorActionFrame();
            CurrentAction.Enter(context: context);
            return true;
        }

        public void Tick(PlayerContext context, float deltaTime)
        {
            if (CurrentAction == null)
            {
                return;
            }

            CurrentAction.Tick(context: context, deltaTime: deltaTime);
            ClearIfComplete(context: context);
        }

        public void FixedTick(PlayerContext context, float fixedDeltaTime)
        {
            if (CurrentAction == null)
            {
                return;
            }

            CurrentAction.FixedTick(context: context, fixedDeltaTime: fixedDeltaTime);
            ClearIfComplete(context: context);
        }

        public void Clear(PlayerContext context)
        {
            CurrentAction?.Exit(context: context);
            CurrentAction = null;
            context.ClearAnimatorActionFrame();
        }

        private void ClearIfComplete(PlayerContext context)
        {
            if (CurrentAction != null && CurrentAction.IsComplete)
            {
                Clear(context: context);
            }
        }
    }
}
