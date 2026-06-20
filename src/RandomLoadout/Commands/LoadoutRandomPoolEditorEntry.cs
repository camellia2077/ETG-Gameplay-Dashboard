namespace RandomLoadout
{
    internal sealed class LoadoutRandomPoolEditorEntry
    {
        public LoadoutRandomPoolEditorEntry(int poolIndex, int pickupId, string primaryText, string secondaryText)
        {
            PoolIndex = poolIndex;
            PickupId = pickupId;
            PrimaryText = primaryText ?? string.Empty;
            SecondaryText = secondaryText ?? string.Empty;
        }

        public int PoolIndex { get; private set; }

        public int PickupId { get; private set; }

        public string PrimaryText { get; private set; }

        public string SecondaryText { get; private set; }
    }
}
