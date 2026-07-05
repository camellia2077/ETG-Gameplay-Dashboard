using System;
using System.Collections.Generic;

namespace RandomLoadout
{
    internal sealed class PickupInfoTermsRegistry
    {
        public static readonly PickupInfoTermsRegistry Empty = new PickupInfoTermsRegistry(
            new PickupInfoTermsTable(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)),
            new PickupInfoTermsTable(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)));

        public PickupInfoTermsRegistry(PickupInfoTermsTable english, PickupInfoTermsTable simplifiedChinese)
        {
            English = english ?? PickupInfoTermsTable.Empty;
            SimplifiedChinese = simplifiedChinese ?? PickupInfoTermsTable.Empty;
        }

        public PickupInfoTermsTable English { get; private set; }

        public PickupInfoTermsTable SimplifiedChinese { get; private set; }

        public string ResolveSectionLabel(string languageCode, string key, string fallback)
        {
            return ResolveValue(
                languageCode,
                key,
                fallback,
                delegate(PickupInfoTermsTable table, string termKey, string defaultValue)
                {
                    return table.GetSectionLabel(termKey, defaultValue);
                });
        }

        public string ResolveStatLabel(string languageCode, string key, string fallback)
        {
            return ResolveValue(
                languageCode,
                key,
                fallback,
                delegate(PickupInfoTermsTable table, string termKey, string defaultValue)
                {
                    return table.GetStatLabel(termKey, defaultValue);
                });
        }

        public string ResolveDisplayValue(string languageCode, string rawValue, string fallback)
        {
            return ResolveValue(
                languageCode,
                rawValue,
                fallback,
                delegate(PickupInfoTermsTable table, string termKey, string defaultValue)
                {
                    return table.GetValueMapping(termKey, defaultValue);
                });
        }

        private string ResolveValue(
            string languageCode,
            string key,
            string fallback,
            Func<PickupInfoTermsTable, string, string, string> resolveFromTable)
        {
            if (resolveFromTable == null)
            {
                return fallback ?? string.Empty;
            }

            if (string.Equals(languageCode, "zh-CN", StringComparison.OrdinalIgnoreCase))
            {
                string chineseValue = resolveFromTable(SimplifiedChinese, key, string.Empty);
                if (!string.IsNullOrEmpty(chineseValue))
                {
                    return chineseValue;
                }
            }

            string englishValue = resolveFromTable(English, key, string.Empty);
            return !string.IsNullOrEmpty(englishValue) ? englishValue : (fallback ?? string.Empty);
        }
    }

    internal sealed class PickupInfoTermsTable
    {
        public static readonly PickupInfoTermsTable Empty = new PickupInfoTermsTable(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        public PickupInfoTermsTable(
            Dictionary<string, string> sectionLabels,
            Dictionary<string, string> statLabels,
            Dictionary<string, string> valueMappings)
        {
            SectionLabels = sectionLabels ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            StatLabels = statLabels ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ValueMappings = valueMappings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public Dictionary<string, string> SectionLabels { get; private set; }

        public Dictionary<string, string> StatLabels { get; private set; }

        public Dictionary<string, string> ValueMappings { get; private set; }

        public string GetSectionLabel(string key, string fallback)
        {
            return GetValue(SectionLabels, key, fallback);
        }

        public string GetStatLabel(string key, string fallback)
        {
            return GetValue(StatLabels, key, fallback);
        }

        public string GetValueMapping(string key, string fallback)
        {
            return GetValue(ValueMappings, key, fallback);
        }

        private static string GetValue(Dictionary<string, string> values, string key, string fallback)
        {
            if (values == null || string.IsNullOrEmpty(key))
            {
                return fallback ?? string.Empty;
            }

            string resolved;
            return values.TryGetValue(key, out resolved) && !string.IsNullOrEmpty(resolved)
                ? resolved
                : (fallback ?? string.Empty);
        }
    }
}
