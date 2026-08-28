using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class CardFeedbackService :
        MonoBehaviour,
        IGameplayModule,
        ICardFeedbackService
    {
        [Header("Events")]
        [Tooltip("Broadcasts transient world-space card feedback events.")]
        [SerializeField] private CardFeedbackEventChannelSO worldFeedbackEvent;

        private readonly Dictionary<string, CardHudEffectViewModel> hudEffects = new();
        private readonly List<string> hudEffectOrder = new();
        private readonly List<CardHudEffectViewModel> snapshot = new();

        public bool IsInitialized { get; private set; }
        public CardFeedbackEventChannelSO WorldFeedbackEvent => worldFeedbackEvent;

        private void OnDisable()
        {
            Shutdown();
        }

        public void Configure(CardFeedbackEventChannelSO channel)
        {
            if (IsInitialized)
            {
                return;
            }

            worldFeedbackEvent = channel;
        }

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            IsInitialized = true;
        }

        public void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            ClearHudEffects();
            IsInitialized = false;
        }

        public void PublishWorldFeedback(CardWorldFeedbackViewModel feedback)
        {
            if (!feedback.IsValid)
            {
                return;
            }

            worldFeedbackEvent?.Raise(feedback);
        }

        public void UpsertHudEffect(CardHudEffectViewModel effect)
        {
            if (!effect.IsValid)
            {
                return;
            }

            if (!hudEffects.ContainsKey(effect.EffectKey))
            {
                hudEffectOrder.Add(effect.EffectKey);
            }

            hudEffects[effect.EffectKey] = effect;
        }

        public void RemoveHudEffect(string effectKey)
        {
            if (string.IsNullOrWhiteSpace(effectKey)
                || !hudEffects.Remove(effectKey))
            {
                return;
            }

            hudEffectOrder.Remove(effectKey);
        }

        public void RemoveHudEffectsFromSource(GameObject sourceObject)
        {
            if (sourceObject == null)
            {
                return;
            }

            for (var index = hudEffectOrder.Count - 1; index >= 0; index--)
            {
                var key = hudEffectOrder[index];
                if (hudEffects.TryGetValue(key, out var effect)
                    && effect.SourceObject == sourceObject)
                {
                    hudEffects.Remove(key);
                    hudEffectOrder.RemoveAt(index);
                }
            }
        }

        public void ClearHudEffects()
        {
            hudEffects.Clear();
            hudEffectOrder.Clear();
            snapshot.Clear();
        }

        public IReadOnlyList<CardHudEffectViewModel> GetHudEffects()
        {
            snapshot.Clear();
            foreach (var key in hudEffectOrder)
            {
                if (hudEffects.TryGetValue(key, out var effect))
                {
                    snapshot.Add(effect);
                }
            }

            return snapshot;
        }
    }
}
