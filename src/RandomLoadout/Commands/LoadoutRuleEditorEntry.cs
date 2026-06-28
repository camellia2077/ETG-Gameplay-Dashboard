namespace RandomLoadout
{
    internal sealed class LoadoutRuleEditorEntry
    {
        public LoadoutRuleEditorEntry(int index, string primaryText, string secondaryText, int? pickupId, bool isRandomPool)
            : this(index, primaryText, secondaryText, pickupId, isRandomPool, string.Empty, 1, false)
        {
        }

        public LoadoutRuleEditorEntry(int index, string primaryText, string secondaryText, int? pickupId, bool isRandomPool, string pickupType, int count)
            : this(index, primaryText, secondaryText, pickupId, isRandomPool, pickupType, count, false)
        {
        }

        public LoadoutRuleEditorEntry(int index, string primaryText, string secondaryText, int? pickupId, bool isRandomPool, string pickupType, int count, bool isPresetPickupCollection)
        {
            Index = index;
            PrimaryText = primaryText ?? string.Empty;
            SecondaryText = secondaryText ?? string.Empty;
            PickupId = pickupId;
            IsRandomPool = isRandomPool;
            PickupType = pickupType ?? string.Empty;
            Count = count > 0 ? count : 1;
            IsPresetPickupCollection = isPresetPickupCollection;
        }

        public int Index { get; private set; }

        public string PrimaryText { get; private set; }

        public string SecondaryText { get; private set; }

        public int? PickupId { get; private set; }

        public bool IsRandomPool { get; private set; }

        public string PickupType { get; private set; }

        public int Count { get; private set; }

        public bool IsPresetPickupCollection { get; private set; }
    }
}
