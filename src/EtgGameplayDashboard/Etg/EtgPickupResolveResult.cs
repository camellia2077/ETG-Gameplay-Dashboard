// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using EtgGameplayDashboard.Core;

namespace EtgGameplayDashboard
{
    internal sealed class EtgPickupResolveResult
    {
        public EtgPickupResolveResult(bool succeeded, PickupCategory? category, int pickupId, string pickupLabel, SelectionWarning warning)
        {
            Succeeded = succeeded;
            Category = category;
            PickupId = pickupId;
            PickupLabel = pickupLabel;
            Warning = warning;
        }

        public bool Succeeded { get; private set; }

        public PickupCategory? Category { get; private set; }

        public int PickupId { get; private set; }

        public string PickupLabel { get; private set; }

        public SelectionWarning Warning { get; private set; }
    }
}
