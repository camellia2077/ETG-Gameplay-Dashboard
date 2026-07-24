// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;

namespace EtgGameplayDashboard
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

    internal static class StartItemPickupCatalog
    {
        public const int CasingsPerBundle = 50;
        private const int BlankRepresentativePickupId = 224;
        public const string KeyType = "key";
        public const string RatKeyType = "rat_key";
        public const string MaxHealthType = "max_health";
        public const string ArmorType = "armor";
        public const string CasingsType = "casings";
        public const string BlankType = "blank";

        public static readonly string[] AllTypes =
        {
            KeyType,
            RatKeyType,
            MaxHealthType,
            ArmorType,
            CasingsType,
            BlankType,
        };

        public static string NormalizeType(string pickupType)
        {
            string normalized = (pickupType ?? string.Empty).Trim().ToLowerInvariant();
            switch (normalized)
            {
                case KeyType:
                    return KeyType;
                case RatKeyType:
                    return RatKeyType;
                case MaxHealthType:
                    return MaxHealthType;
                case ArmorType:
                    return ArmorType;
                case CasingsType:
                    return CasingsType;
                case BlankType:
                    return BlankType;
                default:
                    return string.Empty;
            }
        }

        public static int NormalizeCount(int count)
        {
            return count > 0 ? count : 1;
        }

        public static int GetDisplayCount(string pickupType, int storedCount)
        {
            int normalizedStoredCount = NormalizeCount(storedCount);
            return string.Equals(NormalizeType(pickupType), CasingsType, StringComparison.OrdinalIgnoreCase)
                ? normalizedStoredCount * CasingsPerBundle
                : normalizedStoredCount;
        }

        public static bool TryGetStoredCount(string pickupType, int displayCount, out int storedCount)
        {
            storedCount = 0;
            if (displayCount <= 0)
            {
                return false;
            }

            string normalizedType = NormalizeType(pickupType);
            if (string.IsNullOrEmpty(normalizedType))
            {
                return false;
            }

            if (string.Equals(normalizedType, CasingsType, StringComparison.OrdinalIgnoreCase))
            {
                if (displayCount % CasingsPerBundle != 0)
                {
                    return false;
                }

                storedCount = displayCount / CasingsPerBundle;
                return storedCount > 0;
            }

            storedCount = displayCount;
            return true;
        }

        public static LoadoutRuleFilePickupModel[] MergePickups(LoadoutRuleFilePickupModel[] pickups)
        {
            if (pickups == null || pickups.Length == 0)
            {
                return new LoadoutRuleFilePickupModel[0];
            }

            System.Collections.Generic.List<LoadoutRuleFilePickupModel> mergedPickups = new System.Collections.Generic.List<LoadoutRuleFilePickupModel>();
            System.Collections.Generic.Dictionary<string, int> indexByType = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < pickups.Length; i++)
            {
                string normalizedType = NormalizeType(pickups[i] != null ? pickups[i].Type : string.Empty);
                if (string.IsNullOrEmpty(normalizedType))
                {
                    continue;
                }

                int normalizedCount = NormalizeCount(pickups[i] != null ? pickups[i].Count : 1);
                int existingIndex;
                if (indexByType.TryGetValue(normalizedType, out existingIndex))
                {
                    mergedPickups[existingIndex].Count += normalizedCount;
                    continue;
                }

                indexByType.Add(normalizedType, mergedPickups.Count);
                mergedPickups.Add(
                    new LoadoutRuleFilePickupModel
                    {
                        Type = normalizedType,
                        Count = normalizedCount,
                    });
            }

            return mergedPickups.ToArray();
        }

        public static bool IsSupportedType(string pickupType)
        {
            return !string.IsNullOrEmpty(NormalizeType(pickupType));
        }

        public static int? GetRepresentativePickupId(string pickupType)
        {
            switch (NormalizeType(pickupType))
            {
                case BlankType:
                    return BlankRepresentativePickupId;
                default:
                    return null;
            }
        }

        public static int? GetFirstRepresentativePickupId(LoadoutRuleFilePickupModel[] pickups)
        {
            if (pickups == null || pickups.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < pickups.Length; i++)
            {
                int? representativePickupId = GetRepresentativePickupId(pickups[i] != null ? pickups[i].Type : string.Empty);
                if (representativePickupId.HasValue)
                {
                    return representativePickupId;
                }
            }

            return null;
        }

        public static string GetDisplayName(string pickupType)
        {
            return GetDisplayNameInternal(pickupType, false);
        }

        public static string GetEnglishDisplayName(string pickupType)
        {
            return GetDisplayNameInternal(pickupType, true);
        }

        private static string GetDisplayNameInternal(string pickupType, bool forceEnglish)
        {
            string key;
            string fallback;
            switch (NormalizeType(pickupType))
            {
                case KeyType:
                    key = "gui.loadout_editor.pickup.key";
                    fallback = "Key";
                    break;
                case RatKeyType:
                    key = "gui.loadout_editor.pickup.rat_key";
                    fallback = "Rat Key";
                    break;
                case MaxHealthType:
                    key = "gui.loadout_editor.pickup.max_health";
                    fallback = "Max Health";
                    break;
                case ArmorType:
                    key = "gui.loadout_editor.pickup.armor";
                    fallback = "Armor";
                    break;
                case CasingsType:
                    key = "gui.loadout_editor.pickup.casings";
                    fallback = "Casings";
                    break;
                case BlankType:
                    key = "gui.loadout_editor.pickup.blank";
                    fallback = "Blank";
                    break;
                default:
                    return string.Empty;
            }

            string value = forceEnglish
                ? GuiText.GetEnglish(key)
                : GuiText.Get(key);
            return string.Equals(value, key, StringComparison.Ordinal)
                ? fallback
                : value;
        }
    }
}
