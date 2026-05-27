using UnityEngine;
using UnityEngine.Events;

namespace TicGame.Architecture
{
    public abstract class EventChannelListener<TPayload, TChannel, TUnityEvent> : MonoBehaviour
        where TChannel : EventChannelSO<TPayload>
        where TUnityEvent : UnityEvent<TPayload>
    {
        [Header("Event Binding")]
        [Tooltip("Event channel listened by this component.")]
        [SerializeField] private TChannel channel;

        [Tooltip("UnityEvent invoked whenever the channel raises a payload.")]
        [SerializeField] private TUnityEvent response;

        /// <summary>
        /// Subscribes this listener to its configured event channel.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (channel != null)
            {
                channel.Raised += OnRaised;
            }
        }

        /// <summary>
        /// Unsubscribes this listener from its configured event channel.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (channel != null)
            {
                channel.Raised -= OnRaised;
            }
        }

        private void OnRaised(TPayload payload)
        {
            response?.Invoke(payload);
        }
    }
}
