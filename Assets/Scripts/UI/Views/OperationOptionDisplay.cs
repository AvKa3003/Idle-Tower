using IdleTower.Rooms.Production;

namespace IdleTower.UI.Views
{
    public readonly struct OperationOptionDisplay
    {
        public ModeId ModeId { get; }
        public string Label { get; }
        public string DetailText { get; }
        public bool IsActive { get; }
        public bool ShowUnlockButton { get; }
        public bool ShowSelectButton { get; }
        public bool Interactable { get; }

        public OperationOptionDisplay(
            ModeId modeId,
            string label,
            string detailText,
            bool isActive,
            bool showUnlockButton,
            bool showSelectButton,
            bool interactable)
        {
            ModeId = modeId;
            Label = label;
            DetailText = detailText;
            IsActive = isActive;
            ShowUnlockButton = showUnlockButton;
            ShowSelectButton = showSelectButton;
            Interactable = interactable;
        }
    }
}
