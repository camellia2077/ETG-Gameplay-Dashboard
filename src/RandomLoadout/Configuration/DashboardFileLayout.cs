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

        public static string GetPresetFilePath(string configDirectory, string fileName)
        {
            return Path.Combine(GetPresetsDirectoryPath(configDirectory), fileName ?? string.Empty);
        }
    }
}
