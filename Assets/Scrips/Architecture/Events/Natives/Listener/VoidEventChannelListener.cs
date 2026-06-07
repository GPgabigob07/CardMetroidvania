using UnityEngine;
using UnityEngine.Events;

namespace TicGame.Architecture
{
    public sealed class VoidEventChannelListener : MonoBehaviour
    {
        [Header(header: "Event Binding")]
        [Tooltip(tooltip: "Void event channel listened by this component.")]
        [SerializeField] private VoidEventChannelSO channel;

        [Tooltip(tooltip: "UnityEvent invoked whenever the channel is raised.")]
        [SerializeField] private UnityEvent response;

        private void OnEnable()
        {
            if (channel != null)
            {
                channel.Raised += OnRaised;
            }
        }

        private void OnDisable()
        {
            if (channel != null)
            {
                channel.Raised -= OnRaised;
            }
        }

        private void OnRaised()
        {
            response?.Invoke();
        }
    }
}
