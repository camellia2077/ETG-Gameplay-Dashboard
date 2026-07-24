// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Linq;

namespace EtgGameplayDashboard.Core
{
    public sealed class LoadoutSelectionResult
    {
        public LoadoutSelectionResult(int seed, IEnumerable<SelectedPickup> selections, IEnumerable<SelectionWarning> warnings)
            : this(seed, selections, warnings, null)
        {
        }

        public LoadoutSelectionResult(int seed, IEnumerable<SelectedPickup> selections, IEnumerable<SelectionWarning> warnings, IEnumerable<RandomPoolSelectionState> randomPoolStates)
        {
            if (selections == null)
            {
                throw new ArgumentNullException("selections");
            }

            if (warnings == null)
            {
                throw new ArgumentNullException("warnings");
            }

            Seed = seed;
            Selections = selections.ToArray();
            Warnings = warnings.ToArray();
            RandomPoolStates = randomPoolStates != null ? randomPoolStates.ToArray() : new RandomPoolSelectionState[0];
        }

        public int Seed { get; private set; }

        public SelectedPickup[] Selections { get; private set; }

        public SelectionWarning[] Warnings { get; private set; }

        public RandomPoolSelectionState[] RandomPoolStates { get; private set; }
    }
}
