using UnityEngine;
using UnityEngine.Events;

namespace TicGame.Architecture
{
    public abstract class EventChannelListener<TPayload, TChannel, TUnityEvent> : MonoBehaviour
        where TChannel : EventChannelSO<TPayload>
        where TUnityEvent : UnityEvent<TPayload>
    {
        [Header(header: "Event Binding")]
        [Tooltip(tooltip: "Event channel listened by this component.")]
        [SerializeField] private TChannel channel;

        [Tooltip(tooltip: "UnityEvent invoked whenever the channel raises a payload.")]
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
            response?.Invoke(arg0: payload);
        }
    }
}
