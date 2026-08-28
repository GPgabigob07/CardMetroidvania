namespace TicGame.Architecture
{
    /// <summary>
    /// Provides no-op state hooks and retains the typed owner supplied on entry.
    /// </summary>
    public abstract class OwnedState<TStateId, TOwner> : IOwnedState<TStateId, TOwner>
    {
        /// <summary>
        /// Gets the owner supplied when this state most recently became active.
        /// </summary>
        protected TOwner Owner { get; private set; }

        /// <summary>
        /// Gets the stable identifier used by the owning state machine.
        /// </summary>
        public abstract TStateId Id { get; }

        /// <summary>
        /// Stores the owner and invokes the extensibility hook for state entry.
        /// </summary>
        public void Enter(TOwner owner)
        {
            Owner = owner;
            OnEnter();
        }

        /// <summary>
        /// Provides an optional extension point for variable-rate updates.
        /// </summary>
        public virtual void Tick(float deltaTime)
        {
        }

        /// <summary>
        /// Provides an optional extension point for fixed-rate updates.
        /// </summary>
        public virtual void FixedTick(float fixedDeltaTime)
        {
        }

        /// <summary>
        /// Provides an optional extension point for state exit.
        /// </summary>
        public virtual void Exit()
        {
        }

        /// <summary>
        /// Provides an optional extension point for named animation events.
        /// </summary>
        public virtual void OnAnimationEvent(string eventName)
        {
        }

        /// <summary>
        /// Provides an optional extension point for animation completion.
        /// </summary>
        public virtual void OnAnimationFinished()
        {
        }

        /// <summary>
        /// Runs after the owner is stored and the state becomes active.
        /// </summary>
        protected virtual void OnEnter()
        {
        }
    }
}
