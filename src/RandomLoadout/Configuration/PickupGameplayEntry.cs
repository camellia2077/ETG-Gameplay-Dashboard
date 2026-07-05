using System.Collections.Generic;

namespace RandomLoadout
{
    internal sealed class PickupGameplayEntry
    {
        public PickupGameplayEntry(
            int pickupId,
            string englishDisplayName,
            string wikiKey,
            string quality,
            string pickupType,
            PickupGameplayStatGroup[] statGroups,
            string unlock,
            string englishGameplaySummary,
            string englishEffectHighlights,
            string englishSynergyHighlights,
            string englishUsageNotes,
            string chineseDisplayName,
            string chineseGameplaySummary,
            string chineseEffectHighlights,
            string chineseSynergyHighlights,
            string chineseUsageNotes)
        {
            PickupId = pickupId;
            EnglishDisplayName = englishDisplayName ?? string.Empty;
            WikiKey = wikiKey ?? string.Empty;
            Quality = quality ?? string.Empty;
            PickupType = pickupType ?? string.Empty;
            StatGroups = statGroups ?? new PickupGameplayStatGroup[0];
            Unlock = unlock ?? string.Empty;
            EnglishGameplaySummary = englishGameplaySummary ?? string.Empty;
            EnglishEffectHighlights = englishEffectHighlights ?? string.Empty;
            EnglishSynergyHighlights = englishSynergyHighlights ?? string.Empty;
            EnglishUsageNotes = englishUsageNotes ?? string.Empty;
            ChineseDisplayName = chineseDisplayName ?? string.Empty;
            ChineseGameplaySummary = chineseGameplaySummary ?? string.Empty;
            ChineseEffectHighlights = chineseEffectHighlights ?? string.Empty;
            ChineseSynergyHighlights = chineseSynergyHighlights ?? string.Empty;
            ChineseUsageNotes = chineseUsageNotes ?? string.Empty;
        }

        public int PickupId { get; private set; }

        public string EnglishDisplayName { get; private set; }

        public string WikiKey { get; private set; }

        public string Quality { get; private set; }

        public string PickupType { get; private set; }

        public PickupGameplayStatGroup[] StatGroups { get; private set; }

        public string Unlock { get; private set; }

        public string EnglishGameplaySummary { get; private set; }

        public string EnglishEffectHighlights { get; private set; }

        public string EnglishSynergyHighlights { get; private set; }

        public string EnglishUsageNotes { get; private set; }

        public string ChineseDisplayName { get; private set; }

        public string ChineseGameplaySummary { get; private set; }

        public string ChineseEffectHighlights { get; private set; }

        public string ChineseSynergyHighlights { get; private set; }

        public string ChineseUsageNotes { get; private set; }
    }

    internal sealed class PickupGameplayStatGroup
    {
        public PickupGameplayStatGroup(string groupKey, PickupGameplayStatEntry[] stats)
        {
            GroupKey = groupKey ?? string.Empty;
            Stats = stats ?? new PickupGameplayStatEntry[0];
        }

        public string GroupKey { get; private set; }

        public PickupGameplayStatEntry[] Stats { get; private set; }
    }

    internal sealed class PickupGameplayStatEntry
    {
        public PickupGameplayStatEntry(string labelKey, string value)
        {
            LabelKey = labelKey ?? string.Empty;
            Value = value ?? string.Empty;
        }

        public string LabelKey { get; private set; }

        public string Value { get; private set; }
    }
}
