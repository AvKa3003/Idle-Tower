namespace IdleTower.UI.Views
{
    public readonly struct OperationOptionDisplay
    {
        public int ModeIndex { get; }
        public string Label { get; }
        public string DetailText { get; }
        public bool IsActive { get; }
        public bool ShowUnlockButton { get; }
        public bool ShowSelectButton { get; }
        public bool Interactable { get; }

        public OperationOptionDisplay(
            int modeIndex,
            string label,
            string detailText,
            bool isActive,
            bool showUnlockButton,
            bool showSelectButton,
            bool interactable)
        {
            ModeIndex = modeIndex;
            Label = label;
            DetailText = detailText;
            IsActive = isActive;
            ShowUnlockButton = showUnlockButton;
            ShowSelectButton = showSelectButton;
            Interactable = interactable;
        }
    }
}
