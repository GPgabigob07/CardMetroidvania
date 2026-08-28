using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(
        menuName = "TIC/Cards/Card Definition",
        fileName = "Card_")]
    public sealed class CardDefinitionSO : ScriptableObject, IIdentified
    {
        [Header("Identity")]
        [Tooltip("Stable card id used by saves and debug tooling. Falls back to the asset name.")]
        [SerializeField] private string id;

        [Tooltip("Human-readable card name. Falls back to the id.")]
        [SerializeField] private string displayName;

        [TextArea]
        [SerializeField] private string description;

        [Header("Presentation")]
        [Tooltip("Optional card icon used by selection UI, HUD feedback, and world feedback.")]
        [SerializeField] private Sprite icon;

        [Header("Card Time")]
        [SerializeField] private PlayerCardTimeState category;

        [Header("Cost")]
        [SerializeField] private List<ResourceAmount> fixedCosts = new();

        [Header("Effect")]
        [SerializeField] private CardEffectDefinitionSO effect;

        public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public PlayerCardTimeState Category => category;
        public IReadOnlyList<ResourceAmount> FixedCosts => fixedCosts;
        public CardEffectDefinitionSO Effect => effect;

        public void Configure(
            string stableId,
            string nameForDisplay,
            string cardDescription,
            PlayerCardTimeState cardTimeCategory,
            IEnumerable<ResourceAmount> costs,
            CardEffectDefinitionSO effectDefinition,
            Sprite cardIcon = null)
        {
            id = stableId;
            displayName = nameForDisplay;
            description = cardDescription;
            icon = cardIcon;
            category = cardTimeCategory;
            fixedCosts = costs != null
                ? new List<ResourceAmount>(costs)
                : new List<ResourceAmount>();
            effect = effectDefinition;
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Id))
            {
                errors.Add("Card requires a stable id or asset name.");
            }

            if (category == PlayerCardTimeState.None)
            {
                errors.Add("Card Time category cannot be None.");
            }

            if (effect == null)
            {
                errors.Add("Card requires an effect definition.");
            }
            else
            {
                foreach (var effectError in effect.GetValidationErrors())
                {
                    errors.Add($"Effect: {effectError}");
                }
            }

            if (fixedCosts != null)
            {
                foreach (var cost in fixedCosts)
                {
                    if (cost.Resource == null)
                    {
                        errors.Add("Card costs cannot contain a null resource.");
                    }

                    if (!float.IsFinite(cost.Amount) || cost.Amount < 0f)
                    {
                        errors.Add("Card costs must be finite and non-negative.");
                    }
                }
            }

            return errors;
        }
    }
}
