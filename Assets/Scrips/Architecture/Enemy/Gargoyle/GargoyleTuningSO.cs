using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Enemies/Gargoyle Tuning")]
    public sealed class GargoyleTuningSO : ScriptableObject
    {
        [Header("Attack Selection")]
        [Tooltip("Each family occurs once in a shuffled bag. Changes apply at the next refill.")]
        [SerializeField] private GargoyleAttackFamily[] families =
        {
            GargoyleAttackFamily.Wingbreaker,
            GargoyleAttackFamily.Volley,
            GargoyleAttackFamily.Beam
        };

        [Tooltip("Authored projectile-count variants. Each occurs once per volley bag; edits apply on refill.")]
        [SerializeField] private int[] volleyCounts = { 1, 3, 5 };

        public IReadOnlyList<GargoyleAttackFamily> Families => Array.AsReadOnly(families ?? Array.Empty<GargoyleAttackFamily>());
        public IReadOnlyList<int> VolleyCounts => Array.AsReadOnly(volleyCounts ?? Array.Empty<int>());

        public bool TryReadSelectorConfiguration(out GargoyleAttackFamily[] familySnapshot, out int[] volleySnapshot)
        {
            familySnapshot = null;
            volleySnapshot = null;
            if (families == null || families.Length < 2 || families.Distinct().Count() != families.Length
                || families.Any(family => !Enum.IsDefined(typeof(GargoyleAttackFamily), family))
                || volleyCounts == null || volleyCounts.Length < 2
                || volleyCounts.Any(count => count <= 0) || volleyCounts.Distinct().Count() != volleyCounts.Length)
                return false;

            familySnapshot = (GargoyleAttackFamily[])families.Clone();
            volleySnapshot = (int[])volleyCounts.Clone();
            return true;
        }
    }
}
