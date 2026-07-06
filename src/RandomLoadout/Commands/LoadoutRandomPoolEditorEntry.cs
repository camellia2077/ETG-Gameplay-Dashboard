// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

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
