using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class CardTimeTutorialZone : MonoBehaviour
    {
        [Header("Guide")]
        [Tooltip("Teach the controls and feedback without prescribing a combat solution.")]
        [TextArea(8, 18)]
        [SerializeField] private string guide;

        [Tooltip("Optional area-local guide used by standalone test scenes.")]
        [SerializeField] private CardTimeGuideUI localGuide;
        [Tooltip("When enabled, an unbound trigger may create its legacy local guide. Streamed areas should disable this.")]
        [SerializeField] private bool standaloneGuideFallback = true;

        private CardTimeGuideUI boundGuide;

        /// <summary>
        /// Connects this area trigger to the persistent Gameplay guide presentation.
        /// </summary>
        public void BindGuide(CardTimeGuideUI guidePresentation)
        {
            boundGuide = guidePresentation;
            standaloneGuideFallback = false;
        }

        /// <summary>
        /// Marks this trigger as streamed content and prevents accidental local guide creation.
        /// </summary>
        public void ConfigureForStreamedComposition()
        {
            standaloneGuideFallback = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            player.SetCardTimeUnlocked(true);
            ResolveGuide()?.Discover(guide);
        }

        private CardTimeGuideUI ResolveGuide()
        {
            if (boundGuide != null) return boundGuide;
            if (!standaloneGuideFallback)
            {
                Debug.LogError("Card Time tutorial trigger is configured for streamed composition but has no bound Gameplay guide.", this);
                return null;
            }
            if (localGuide != null) return localGuide;
            localGuide = GetComponent<CardTimeGuideUI>();
            if (localGuide == null) localGuide = gameObject.AddComponent<CardTimeGuideUI>();
            return localGuide;
        }
    }
}
