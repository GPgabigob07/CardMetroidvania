using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerCardDebugPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerResourceWallet wallet;
        [SerializeField] private ResourceDefinitionSO energyResource;
        [SerializeField] private PlayerCombatEffects combatEffects;
        [SerializeField] private PlayerCardRuntime cardRuntime;
        [SerializeField] private PlayerExtraJumpRuntime extraJump;

        public void Configure(
            PlayerResourceWallet resourceWallet,
            ResourceDefinitionSO energy,
            PlayerCombatEffects effects,
            PlayerCardRuntime cards,
            PlayerExtraJumpRuntime extraJumpRuntime)
        {
            wallet = resourceWallet;
            energyResource = energy;
            combatEffects = effects;
            cardRuntime = cards;
            extraJump = extraJumpRuntime;
        }

        private void OnGUI()
        {
            if (wallet == null || energyResource == null)
            {
                return;
            }

            var text =
                $"ENERGY {wallet.GetCurrent(energyResource):0}/{wallet.GetMaximum(energyResource):0}\n"
                + $"CARDS N:{GetCardName(cardRuntime?.NeutralCard)}"
                + $" C:{GetCardName(cardRuntime?.ChainCard)}"
                + $" F:{GetCardName(cardRuntime?.FinisherCard)}\n"
                + $"CHAIN {combatEffects?.ChainIncrements ?? 0}/{combatEffects?.ChainCapacity ?? 0}"
                + $"  ENERGY HITS {combatEffects?.EnergyGainCharges ?? 0}"
                + $"  KB HITS {combatEffects?.KnockbackCharges ?? 0}"
                + $"  EXTRA JUMP {extraJump?.Charges ?? 0}";
            GUI.Label(
                position: new Rect(x: 16f, y: 112f, width: 900f, height: 70f),
                text: text);
        }

        private static string GetCardName(CardDefinitionSO definition)
        {
            return definition != null ? definition.DisplayName : "NONE";
        }
    }
}
