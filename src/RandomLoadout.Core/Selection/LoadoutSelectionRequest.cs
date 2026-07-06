// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Linq;

namespace RandomLoadout.Core
{
    public sealed class LoadoutSelectionRequest
    {
        public LoadoutSelectionRequest(int seed, LoadoutConfig config, IEnumerable<int> ownedPickupIds)
            : this(seed, config, ownedPickupIds, null)
        {
        }

        public LoadoutSelectionRequest(int seed, LoadoutConfig config, IEnumerable<int> ownedPickupIds, IEnumerable<RandomPoolSelectionState> randomPoolStates)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            Seed = seed;
            Config = config;
            OwnedPickupIds = ownedPickupIds != null ? ownedPickupIds.ToArray() : new int[0];
            RandomPoolStates = randomPoolStates != null ? randomPoolStates.ToArray() : new RandomPoolSelectionState[0];
        }

        public int Seed { get; private set; }

        public LoadoutConfig Config { get; private set; }

        public int[] OwnedPickupIds { get; private set; }

        public RandomPoolSelectionState[] RandomPoolStates { get; private set; }
    }
}
