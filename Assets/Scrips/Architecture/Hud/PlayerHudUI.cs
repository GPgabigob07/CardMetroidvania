using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TicGame.Architecture
{
    public sealed class PlayerHudUI : MonoBehaviour
    {
        [Header("Sources")]
        [Tooltip("Player health observed by the health chevrons.")]
        [SerializeField] private SimpleHealth health;

        [Tooltip("Player resource wallet observed by the energy display.")]
        [SerializeField] private PlayerResourceWallet resourceWallet;

        [Tooltip("Resource represented by the energy display.")]
        [SerializeField] private ResourceDefinitionSO energyResource;

        [Header("Health")]
        [Tooltip("Exactly five chevron images in left-to-right order.")]
        [SerializeField] private List<Image> healthChevrons = new();

        [SerializeField] private Color filledHealthColor =
            new(r: 0.91f, g: 0.12f, b: 0.08f, a: 1f);

        [SerializeField] private Color emptyHealthColor =
            new(r: 0.15f, g: 0.04f, b: 0.04f, a: 0.7f);

        [Header("Energy")]
        [Tooltip("Exactly 30 energy segment images in left-to-right order.")]
        [SerializeField] private List<Image> energySegments = new();

        [SerializeField] private Text energyValue;

        [SerializeField] private Color filledEnergyColor =
            new(r: 0.05f, g: 0.82f, b: 0.95f, a: 1f);

        [SerializeField] private Color emptyEnergyColor =
            new(r: 0.02f, g: 0.13f, b: 0.17f, a: 0.9f);

        private void OnEnable()
        {
            if (health != null)
            {
                health.Changed += HandleHealthChanged;
            }

            if (resourceWallet != null)
            {
                resourceWallet.Changed += HandleResourceChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Changed -= HandleHealthChanged;
            }

            if (resourceWallet != null)
            {
                resourceWallet.Changed -= HandleResourceChanged;
            }
        }

        public void Configure(
            SimpleHealth healthSource,
            PlayerResourceWallet wallet,
            ResourceDefinitionSO energy,
            IReadOnlyList<Image> chevrons,
            IReadOnlyList<Image> segments,
            Text value)
        {
            health = healthSource;
            resourceWallet = wallet;
            energyResource = energy;
            healthChevrons = new List<Image>(chevrons);
            energySegments = new List<Image>(segments);
            energyValue = value;
        }

        public void Refresh()
        {
            RefreshHealth(health != null ? health.CurrentHealth : 0f);
            RefreshEnergy(
                resourceWallet != null
                    ? resourceWallet.GetCurrent(energyResource)
                    : 0f,
                resourceWallet != null
                    ? resourceWallet.GetMaximum(energyResource)
                    : 0f);
        }

        private void HandleHealthChanged(SimpleHealthChanged change)
        {
            RefreshHealth(change.Current);
        }

        private void HandleResourceChanged(
            ResourceDefinitionSO resource,
            float previous,
            float current)
        {
            if (resource != energyResource)
            {
                return;
            }

            RefreshEnergy(
                current: current,
                maximum: resourceWallet.GetMaximum(energyResource));
        }

        private void RefreshHealth(float current)
        {
            var filledCount = HudValueMath.GetHealthPipCount(
                current: current,
                capacity: healthChevrons.Count);
            for (var index = 0; index < healthChevrons.Count; index++)
            {
                if (healthChevrons[index] != null)
                {
                    healthChevrons[index].color =
                        index < filledCount ? filledHealthColor : emptyHealthColor;
                }
            }
        }

        private void RefreshEnergy(float current, float maximum)
        {
            var filledCount = HudValueMath.GetFilledSegmentCount(
                current: current,
                maximum: maximum,
                segmentCount: energySegments.Count);
            for (var index = 0; index < energySegments.Count; index++)
            {
                if (energySegments[index] != null)
                {
                    energySegments[index].color =
                        index < filledCount ? filledEnergyColor : emptyEnergyColor;
                }
            }

            if (energyValue != null)
            {
                energyValue.text = HudValueMath.FormatWholeResource(current);
            }
        }
    }
}
