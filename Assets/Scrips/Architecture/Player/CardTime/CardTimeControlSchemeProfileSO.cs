using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(
        menuName = "TIC/Cards/Card Time Control Scheme Profile",
        fileName = "CardTimeControlSchemeProfile_")]
    public sealed class CardTimeControlSchemeProfileSO : ScriptableObject
    {
        [SerializeField] private CardTimeControlSchemeSO defaultScheme;
        [SerializeField] private List<CardTimeControlSchemeSO> schemes = new();

        public CardTimeControlSchemeSO DefaultScheme => defaultScheme;
        public IReadOnlyList<CardTimeControlSchemeSO> Schemes => schemes;

        public void Configure(
            CardTimeControlSchemeSO fallbackScheme,
            IEnumerable<CardTimeControlSchemeSO> availableSchemes)
        {
            defaultScheme = fallbackScheme;
            schemes = availableSchemes != null
                ? new List<CardTimeControlSchemeSO>(availableSchemes)
                : new List<CardTimeControlSchemeSO>();
        }

        public CardTimeControlSchemeSO ResolveScheme(string schemeId)
        {
            if (!string.IsNullOrWhiteSpace(schemeId)
                && TryGetScheme(schemeId, out var scheme))
            {
                return scheme;
            }

            return defaultScheme;
        }

        public bool TryGetScheme(string schemeId, out CardTimeControlSchemeSO scheme)
        {
            foreach (var candidate in schemes)
            {
                if (candidate != null && candidate.SchemeId == schemeId)
                {
                    scheme = candidate;
                    return true;
                }
            }

            scheme = null;
            return false;
        }
    }
}
