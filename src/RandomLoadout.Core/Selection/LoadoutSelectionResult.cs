using System;
using System.Collections.Generic;
using System.Linq;

namespace RandomLoadout.Core
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
