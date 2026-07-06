// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Linq;

namespace RandomLoadout.Core
{
    public sealed class LoadoutRuleConfig
    {
        private LoadoutRuleConfig(
            PickupCategory category,
            GrantMode mode,
            int count,
            IEnumerable<int> poolIds,
            IEnumerable<LoadoutPoolEntryConfig> poolEntries,
            int specificPickupId)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            Category = category;
            Mode = mode;
            Count = count;
            PoolIds = poolIds != null ? poolIds.ToArray() : new int[0];
            PoolEntries = poolEntries != null ? poolEntries.ToArray() : CreatePoolEntries(category, PoolIds);
            SpecificPickupId = specificPickupId;
        }

        public PickupCategory Category { get; private set; }

        public GrantMode Mode { get; private set; }

        public int Count { get; private set; }

        public int[] PoolIds { get; private set; }

        public LoadoutPoolEntryConfig[] PoolEntries { get; private set; }

        public int SpecificPickupId { get; private set; }

        public static LoadoutRuleConfig CreateRandom(PickupCategory category, int count, IEnumerable<int> poolIds)
        {
            if (poolIds == null)
            {
                throw new ArgumentNullException("poolIds");
            }

            return new LoadoutRuleConfig(category, GrantMode.Random, count, poolIds, null, 0);
        }

        public static LoadoutRuleConfig CreateRandom(PickupCategory category, int count, IEnumerable<LoadoutPoolEntryConfig> poolEntries)
        {
            if (poolEntries == null)
            {
                throw new ArgumentNullException("poolEntries");
            }

            return new LoadoutRuleConfig(category, GrantMode.Random, count, null, poolEntries, 0);
        }

        public static LoadoutRuleConfig CreateSpecific(PickupCategory category, int pickupId)
        {
            return new LoadoutRuleConfig(category, GrantMode.Specific, 1, null, null, pickupId);
        }

        private static LoadoutPoolEntryConfig[] CreatePoolEntries(PickupCategory category, int[] poolIds)
        {
            if (poolIds == null || poolIds.Length == 0)
            {
                return new LoadoutPoolEntryConfig[0];
            }

            LoadoutPoolEntryConfig[] entries = new LoadoutPoolEntryConfig[poolIds.Length];
            for (int i = 0; i < poolIds.Length; i++)
            {
                entries[i] = new LoadoutPoolEntryConfig(category, poolIds[i]);
            }

            return entries;
        }
    }
}
