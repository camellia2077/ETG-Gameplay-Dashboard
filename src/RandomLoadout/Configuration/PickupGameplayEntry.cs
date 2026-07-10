// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections.Generic;

namespace RandomLoadout
{
    internal sealed class PickupGameplayEntry
    {
        public PickupGameplayEntry(
            int pickupId,
            string englishDisplayName,
            string chineseDisplayName,
            string wikiKey,
            string quality,
            string type,
            PickupGameplayStatSection[] statSections,
            string englishGameplaySummary,
            string chineseGameplaySummary,
            string[] englishEffects,
            string[] chineseEffects,
            string[] englishSynergies,
            string[] chineseSynergies,
            string[] englishNotes,
            string[] chineseNotes)
        {
            PickupId = pickupId;
            EnglishDisplayName = englishDisplayName ?? string.Empty;
            ChineseDisplayName = chineseDisplayName ?? string.Empty;
            WikiKey = wikiKey ?? string.Empty;
            Quality = quality ?? string.Empty;
            Type = type ?? string.Empty;
            StatSections = statSections ?? new PickupGameplayStatSection[0];
            EnglishGameplaySummary = englishGameplaySummary ?? string.Empty;
            ChineseGameplaySummary = chineseGameplaySummary ?? string.Empty;
            EnglishEffects = englishEffects ?? new string[0];
            ChineseEffects = chineseEffects ?? new string[0];
            EnglishSynergies = englishSynergies ?? new string[0];
            ChineseSynergies = chineseSynergies ?? new string[0];
            EnglishNotes = englishNotes ?? new string[0];
            ChineseNotes = chineseNotes ?? new string[0];
        }

        public int PickupId { get; private set; }

        public string EnglishDisplayName { get; private set; }

        public string ChineseDisplayName { get; private set; }

        public string WikiKey { get; private set; }

        public string Quality { get; private set; }

        public string Type { get; private set; }

        public PickupGameplayStatSection[] StatSections { get; private set; }

        public string EnglishGameplaySummary { get; private set; }

        public string ChineseGameplaySummary { get; private set; }

        public string[] EnglishEffects { get; private set; }

        public string[] ChineseEffects { get; private set; }

        public string[] EnglishSynergies { get; private set; }

        public string[] ChineseSynergies { get; private set; }

        public string[] EnglishNotes { get; private set; }

        public string[] ChineseNotes { get; private set; }
    }

    internal sealed class PickupGameplayStatSection
    {
        public PickupGameplayStatSection(string key, PickupGameplayStatEntry[] stats)
        {
            Key = key ?? string.Empty;
            Stats = stats ?? new PickupGameplayStatEntry[0];
        }

        public string Key { get; private set; }

        public PickupGameplayStatEntry[] Stats { get; private set; }
    }

    internal sealed class PickupGameplayStatEntry
    {
        public PickupGameplayStatEntry(string key, string value)
        {
            Key = key ?? string.Empty;
            Value = value ?? string.Empty;
        }

        public string Key { get; private set; }

        public string Value { get; private set; }
    }
}
