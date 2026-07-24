// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections.Generic;

namespace EtgGameplayDashboard
{
    internal sealed class PickupGameplayRegistry
    {
        public static readonly PickupGameplayRegistry Empty = new PickupGameplayRegistry(new PickupGameplayEntry[0]);

        private readonly Dictionary<int, PickupGameplayEntry> _entriesByPickupId;

        public PickupGameplayRegistry(PickupGameplayEntry[] entries)
        {
            Entries = entries ?? new PickupGameplayEntry[0];
            _entriesByPickupId = new Dictionary<int, PickupGameplayEntry>();
            for (int i = 0; i < Entries.Length; i++)
            {
                PickupGameplayEntry entry = Entries[i];
                if (entry == null || _entriesByPickupId.ContainsKey(entry.PickupId))
                {
                    continue;
                }

                _entriesByPickupId.Add(entry.PickupId, entry);
            }
        }

        public PickupGameplayEntry[] Entries { get; private set; }

        public int Count
        {
            get { return Entries.Length; }
        }

        public bool TryGetEntry(int pickupId, out PickupGameplayEntry entry)
        {
            return _entriesByPickupId.TryGetValue(pickupId, out entry);
        }
    }
}
