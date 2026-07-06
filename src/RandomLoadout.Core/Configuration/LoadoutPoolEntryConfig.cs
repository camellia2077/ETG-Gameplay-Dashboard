// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;

namespace RandomLoadout.Core
{
    public sealed class LoadoutPoolEntryConfig
    {
        public LoadoutPoolEntryConfig(PickupCategory category, int pickupId)
        {
            if (pickupId <= 0)
            {
                throw new ArgumentOutOfRangeException("pickupId");
            }

            Category = category;
            PickupId = pickupId;
        }

        public PickupCategory Category { get; private set; }

        public int PickupId { get; private set; }
    }
}
