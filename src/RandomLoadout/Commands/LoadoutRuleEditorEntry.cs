namespace RandomLoadout
{
    internal sealed class LoadoutRuleEditorEntry
    {
        public LoadoutRuleEditorEntry(int index, string primaryText, string secondaryText, int? pickupId, bool isRandomPool)
        {
            Index = index;
            PrimaryText = primaryText ?? string.Empty;
            SecondaryText = secondaryText ?? string.Empty;
            PickupId = pickupId;
            IsRandomPool = isRandomPool;
        }

        public int Index { get; private set; }

        public string PrimaryText { get; private set; }

        public string SecondaryText { get; private set; }

        public int? PickupId { get; private set; }

        public bool IsRandomPool { get; private set; }
    }
}
