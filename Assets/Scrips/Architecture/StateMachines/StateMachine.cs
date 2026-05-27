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
                throw new ArgumentNullException(nameof(state));
            }

            states[state.Id] = state;
        }

        public bool ContainsState(TStateId id)
        {
            return states.ContainsKey(id);
        }

        public bool TryChangeState(TStateId id)
        {
            if (!states.TryGetValue(id, out IState<TStateId> nextState))
            {
                return false;
            }

            TStateId previousId = CurrentStateId;
            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentState.Enter();
            StateChanged?.Invoke(previousId, id);
            return true;
        }

        public void Tick(float deltaTime)
        {
            CurrentState?.Tick(deltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            CurrentState?.FixedTick(fixedDeltaTime);
        }
    }
}

