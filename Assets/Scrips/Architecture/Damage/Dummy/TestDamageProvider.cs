using System.Collections.Generic;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class TestDamageProvider : MonoBehaviour, IDamageProvider, IDamageListener
    {
        private readonly List<IDamageModifier> modifiers = new List<IDamageModifier>();

        public float AttackValue { get; set; } = 10f;
        public GameplayTagSet OffensiveTags { get; } = new GameplayTagSet();
        public int DamageDealtNotifications { get; private set; }
        public int DamageResolutionNotifications { get; private set; }
        public DamageResolutionReport LastReport { get; private set; }

        public IEnumerable<IDamageModifier> GetDamageModifiers()
        {
            return modifiers;
        }

        public void AddModifier(IDamageModifier modifier)
        {
            modifiers.Add(modifier);
        }

        public void OnDamageResolved(DamageResolutionReport report)
        {
            LastReport = report;
            DamageResolutionNotifications++;
        }

        public void OnDamageDealt(in DamageContext context, in DamageResult result)
        {
            DamageDealtNotifications++;
        }

        public void OnDamageReceived(in DamageContext context, in DamageResult result)
        {
        }

        public void OnDamageResolutionComplete(DamageResolutionReport report)
        {
        }
    }
}

