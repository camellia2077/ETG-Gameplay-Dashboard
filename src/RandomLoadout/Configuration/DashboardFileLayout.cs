// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.IO;

namespace RandomLoadout
{
    internal static class DashboardFileLayout
    {
        public const string RulesFileName = "ETG-Gameplay-Dashboard.rules.json5";
        public const string AliasFileName = "ETG-Gameplay-Dashboard.aliases.json5";
        public const string EnglishLocalizationFileName = "ETG-Gameplay-Dashboard.localization.en.json5";
        public const string SimplifiedChineseLocalizationFileName = "ETG-Gameplay-Dashboard.localization.zh-CN.json5";
        public const string PresetsDirectoryName = "presets";
        public const string PickupGameplayFileName = "RandomLoadout.pickup-gameplay.json";
        public const string PickupInfoTermsFileName = "RandomLoadout.pickup-info-terms.json";
        public const string BossNamesFileName = "RandomLoadout.boss-names.json";

        public static string GetRulesFilePath(string configDirectory)
        {
            return Path.Combine(configDirectory ?? string.Empty, RulesFileName);
        }

        public static string GetAliasFilePath(string configDirectory)
        {
            return Path.Combine(configDirectory ?? string.Empty, AliasFileName);
        }

        public static string GetEnglishLocalizationFilePath(string configDirectory)
        {
            return Path.Combine(configDirectory ?? string.Empty, EnglishLocalizationFileName);
        }

        public static string GetSimplifiedChineseLocalizationFilePath(string configDirectory)
        {
            return Path.Combine(configDirectory ?? string.Empty, SimplifiedChineseLocalizationFileName);
        }

        public static string GetPresetsDirectoryPath(string configDirectory)
        {
            return Path.Combine(configDirectory ?? string.Empty, PresetsDirectoryName);
        }

        public static string GetPickupGameplayFilePath(string configDirectory)
        {
            return Path.Combine(configDirectory ?? string.Empty, PickupGameplayFileName);
        }

        public static string GetPickupInfoTermsFilePath(string configDirectory)
        {
            return Path.Combine(configDirectory ?? string.Empty, PickupInfoTermsFileName);
        }

        public static string GetBossNamesFilePath(string configDirectory)
        {
            return Path.Combine(configDirectory ?? string.Empty, BossNamesFileName);
        }

        public static string GetPresetFilePath(string configDirectory, string fileName)
        {
            return Path.Combine(GetPresetsDirectoryPath(configDirectory), fileName ?? string.Empty);
        }
    }
}
