namespace TicGame.Architecture
{
    public readonly struct CardTimeSelectionSlotCommand
    {
        public CardTimeSelectionSlotCommand(
            int slotIndex,
            bool selected)
        {
            SlotIndex = slotIndex;
            Selected = selected;
        }

        public int SlotIndex { get; }
        public bool Selected { get; }
    }
}
