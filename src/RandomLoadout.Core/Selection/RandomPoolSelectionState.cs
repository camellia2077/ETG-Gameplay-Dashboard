// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Linq;

namespace RandomLoadout.Core
{
    public sealed class RandomPoolSelectionState
    {
        public RandomPoolSelectionState(int ruleIndex, string poolSignature, IEnumerable<int> shuffledPickupIds, int nextIndex)
        {
            RuleIndex = ruleIndex;
            PoolSignature = poolSignature ?? string.Empty;
            ShuffledPickupIds = shuffledPickupIds != null ? shuffledPickupIds.ToArray() : new int[0];
            NextIndex = Math.Max(0, nextIndex);
        }

        public int RuleIndex { get; private set; }

        public string PoolSignature { get; private set; }

        public int[] ShuffledPickupIds { get; private set; }

        public int NextIndex { get; private set; }
    }
}
