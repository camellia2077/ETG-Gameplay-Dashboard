// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace EtgGameplayDashboard
{
    internal sealed class LoadoutPresetPreviewRow
    {
        public LoadoutPresetPreviewRow(string labelKey, int[] pickupIds)
        {
            LabelKey = labelKey ?? string.Empty;
            PickupIds = pickupIds ?? new int[0];
        }

        public string LabelKey { get; private set; }

        public int[] PickupIds { get; private set; }
    }
}
