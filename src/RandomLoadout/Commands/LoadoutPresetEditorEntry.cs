// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace RandomLoadout
{
    internal sealed class LoadoutPresetEditorEntry
    {
        public LoadoutPresetEditorEntry(string id, string displayName, bool isActive, int ruleCount, int specificCount, int randomCount, int pickupCount)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            IsActive = isActive;
            RuleCount = ruleCount;
            SpecificCount = specificCount;
            RandomCount = randomCount;
            PickupCount = pickupCount;
        }

        public string Id { get; private set; }

        public string DisplayName { get; private set; }

        public bool IsActive { get; private set; }

        public int RuleCount { get; private set; }

        public int SpecificCount { get; private set; }

        public int RandomCount { get; private set; }

        public int PickupCount { get; private set; }
    }
}
