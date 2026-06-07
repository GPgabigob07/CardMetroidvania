using UnityEngine;

namespace TicGame.Architecture
{
    public abstract class EventChannelBaseSO : ScriptableObject
    {
        [Header(header: "Debug")]
        [TextArea]
        [Tooltip(tooltip: "Designer-facing notes describing what this event channel is for.")]
        [SerializeField] private string description;

        public string Description => description;
        public int LastRaisedFrame { get; private set; } = -1;

        protected void MarkRaised()
        {
            LastRaisedFrame = Time.frameCount;
        }
    }
}
