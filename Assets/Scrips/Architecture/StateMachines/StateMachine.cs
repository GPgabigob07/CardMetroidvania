using System;
using System.Collections.Generic;

namespace TicGame.Architecture
{
    public sealed class StateMachine<TStateId> where TStateId : struct, Enum
    {
        private readonly Dictionary<TStateId, IState<TStateId>> states = new Dictionary<TStateId, IState<TStateId>>();

        public IState<TStateId> CurrentState { get; private set; }
        public TStateId CurrentStateId => CurrentState?.Id ?? default;
        public bool HasState => CurrentState != null;

        public event Action<TStateId, TStateId> StateChanged;

        public void AddState(IState<TStateId> state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(paramName: nameof(state));
            }

            states[key: state.Id] = state;
        }

        public void AddState<TOwner>(IOwnedState<TStateId, TOwner> state, TOwner owner)
        {
            if (state == null)
            {
                throw new ArgumentNullException(paramName: nameof(state));
            }

            states[key: state.Id] = new OwnedStateAdapter<TOwner>(state, owner);
        }

        public bool ContainsState(TStateId id)
        {
            return states.ContainsKey(key: id);
        }

        public bool TryChangeState(TStateId id, bool restart = false)
        {
            if (!states.TryGetValue(key: id, value: out IState<TStateId> nextState))
            {
                return false;
            }

            if (!restart && CurrentState != null && EqualityComparer<TStateId>.Default.Equals(CurrentState.Id, id))
            {
                return true;
            }

            TStateId previousId = CurrentStateId;
            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentState.Enter();
            StateChanged?.Invoke(arg1: previousId, arg2: id);
            return true;
        }

        public void Tick(float deltaTime)
        {
            CurrentState?.Tick(deltaTime: deltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            CurrentState?.FixedTick(fixedDeltaTime: fixedDeltaTime);
        }

        public void ForwardAnimationEvent(string eventName)
        {
            if (CurrentState is IAnimationAwareState animationAwareState)
            {
                animationAwareState.OnAnimationEvent(eventName);
            }
        }

        public void ForwardAnimationFinished()
        {
            if (CurrentState is IAnimationAwareState animationAwareState)
            {
                animationAwareState.OnAnimationFinished();
            }
        }

        private sealed class OwnedStateAdapter<TOwner> : IState<TStateId>, IAnimationAwareState
        {
            private readonly IOwnedState<TStateId, TOwner> state;
            private readonly TOwner owner;

            public OwnedStateAdapter(IOwnedState<TStateId, TOwner> state, TOwner owner)
            {
                this.state = state;
                this.owner = owner;
            }

            public TStateId Id => state.Id;

            public void Enter()
            {
                state.Enter(owner);
            }

            public void Tick(float deltaTime)
            {
                state.Tick(deltaTime);
            }

            public void FixedTick(float fixedDeltaTime)
            {
                state.FixedTick(fixedDeltaTime);
            }

            public void Exit()
            {
                state.Exit();
            }

            public void OnAnimationEvent(string eventName)
            {
                state.OnAnimationEvent(eventName);
            }

            public void OnAnimationFinished()
            {
                state.OnAnimationFinished();
            }
        }
    }
}
