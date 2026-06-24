using System;

namespace RandomLoadout
{
    internal static class StartItemsPresetNames
    {
        public const string DefaultPresetId = "default";
        public const string CaseySynergiesPresetId = "casey_synergies";
        public const string DefaultPresetDisplayNameKey = "preset.default";
        public const string CaseySynergiesPresetDisplayNameKey = "preset.casey_synergies";
        public const string DefaultPresetFileName = "preset.default.json";
        public const string CaseySynergiesPresetFileName = "preset.casey_synergies.json";

        public static string NormalizePresetId(string presetId)
        {
            string normalized = (presetId ?? string.Empty).Trim();
            return string.IsNullOrEmpty(normalized) ? DefaultPresetId : normalized;
        }

        public static string NormalizePresetName(string presetName)
        {
            return (presetName ?? string.Empty).Trim();
        }

        public static string CreatePresetId(string presetId, string presetName, int fallbackIndex)
        {
            string normalizedId = NormalizePresetName(presetId);
            if (!string.IsNullOrEmpty(normalizedId))
            {
                return normalizedId;
            }

            string normalizedName = NormalizePresetName(presetName);
            if (!string.IsNullOrEmpty(normalizedName))
            {
                return normalizedName;
            }

            return "preset-" + fallbackIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static LoadoutRuleFilePresetModel CreateBuiltInPreset(string presetId, LoadoutRuleFileRuleModel[] rules)
        {
            string normalizedId = NormalizePresetId(presetId);
            return new LoadoutRuleFilePresetModel
            {
                Id = normalizedId,
                DisplayNameKey = GetBuiltInDisplayNameKey(normalizedId),
                Name = string.Empty,
                Rules = rules ?? new LoadoutRuleFileRuleModel[0],
            };
        }

        public static string GetDisplayName(LoadoutRuleFilePresetModel preset)
        {
            return GetDisplayName(
                preset != null ? preset.Id : string.Empty,
                preset != null ? preset.Name : string.Empty,
                preset != null ? preset.DisplayNameKey : string.Empty);
        }

        public static string GetEnglishDisplayName(LoadoutRuleFilePresetModel preset)
        {
            return GetEnglishDisplayName(
                preset != null ? preset.Id : string.Empty,
                preset != null ? preset.Name : string.Empty,
                preset != null ? preset.DisplayNameKey : string.Empty);
        }

        public static string GetDisplayName(string presetId, string presetName, string displayNameKey)
        {
            return GetDisplayNameInternal(presetId, presetName, displayNameKey, false);
        }

        public static string GetEnglishDisplayName(string presetId, string presetName, string displayNameKey)
        {
            return GetDisplayNameInternal(presetId, presetName, displayNameKey, true);
        }

        private static string GetDisplayNameInternal(string presetId, string presetName, string displayNameKey, bool forceEnglish)
        {
            string effectiveDisplayNameKey = !string.IsNullOrEmpty(NormalizePresetName(displayNameKey))
                ? NormalizePresetName(displayNameKey)
                : GetBuiltInDisplayNameKey(NormalizePresetId(presetId));
            string localizedName = GetLocalizedDisplayName(effectiveDisplayNameKey, forceEnglish);
            if (!string.IsNullOrEmpty(localizedName))
            {
                return localizedName;
            }

            string normalizedName = NormalizePresetName(presetName);
            if (!string.IsNullOrEmpty(normalizedName))
            {
                return normalizedName;
            }

            return NormalizePresetId(presetId);
        }

        private static string GetBuiltInDisplayNameKey(string presetId)
        {
            if (string.Equals(presetId, CaseySynergiesPresetId, StringComparison.OrdinalIgnoreCase))
            {
                return CaseySynergiesPresetDisplayNameKey;
            }

            if (string.Equals(presetId, DefaultPresetId, StringComparison.OrdinalIgnoreCase))
            {
                return DefaultPresetDisplayNameKey;
            }

            return string.Empty;
        }

        private static string GetLocalizedDisplayName(string displayNameKey, bool forceEnglish)
        {
            if (string.IsNullOrEmpty(displayNameKey))
            {
                return string.Empty;
            }

            string value = forceEnglish
                ? GuiText.GetEnglish(displayNameKey)
                : GuiText.Get(displayNameKey);
            return string.Equals(value, displayNameKey, StringComparison.Ordinal)
                ? string.Empty
                : value;
        }
    }
}
