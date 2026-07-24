// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Text;

namespace EtgGameplayDashboard
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
                    return table.GetSection(termKey, defaultValue);
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
                    return table.GetStat(termKey, defaultValue);
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
                    return table.GetDisplayValue(termKey, defaultValue);
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
            Dictionary<string, string> sections,
            Dictionary<string, string> stats,
            Dictionary<string, string> displayValues)
        {
            Sections = sections ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Stats = stats ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            DisplayValues = displayValues ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public Dictionary<string, string> Sections { get; private set; }

        public Dictionary<string, string> Stats { get; private set; }

        public Dictionary<string, string> DisplayValues { get; private set; }

        public string GetSection(string key, string fallback)
        {
            return GetValue(Sections, key, fallback);
        }

        public string GetStat(string key, string fallback)
        {
            return GetValue(Stats, key, fallback);
        }

        public string GetDisplayValue(string key, string fallback)
        {
            string exactValue;
            if (DisplayValues.TryGetValue(key ?? string.Empty, out exactValue) && !string.IsNullOrEmpty(exactValue))
            {
                return exactValue;
            }

            string localizedFragments = ReplaceDisplayValueFragments(key);
            return !string.IsNullOrEmpty(localizedFragments) ? localizedFragments : (fallback ?? string.Empty);
        }

        private string ReplaceDisplayValueFragments(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue) || DisplayValues == null || DisplayValues.Count == 0)
            {
                return string.Empty;
            }

            List<KeyValuePair<string, string>> candidates = new List<KeyValuePair<string, string>>();
            foreach (KeyValuePair<string, string> pair in DisplayValues)
            {
                if (pair.Key.Length > 1 &&
                    !string.IsNullOrEmpty(pair.Value) &&
                    !string.Equals(pair.Key, pair.Value, StringComparison.Ordinal))
                {
                    candidates.Add(pair);
                }
            }

            candidates.Sort(delegate(KeyValuePair<string, string> left, KeyValuePair<string, string> right)
            {
                return right.Key.Length.CompareTo(left.Key.Length);
            });

            string result = rawValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                KeyValuePair<string, string> candidate = candidates[i];
                result = ReplaceStandaloneFragment(result, candidate.Key, candidate.Value);
            }

            return string.Equals(result, rawValue, StringComparison.Ordinal) ? string.Empty : result;
        }

        private static string ReplaceStandaloneFragment(string value, string fragment, string replacement)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(fragment))
            {
                return value;
            }

            StringBuilder builder = new StringBuilder(value.Length);
            int searchStart = 0;
            int matchIndex;
            while ((matchIndex = value.IndexOf(fragment, searchStart, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                int matchEnd = matchIndex + fragment.Length;
                if (HasStandaloneBoundaries(value, matchIndex, matchEnd, fragment))
                {
                    builder.Append(value, searchStart, matchIndex - searchStart);
                    builder.Append(replacement);
                    searchStart = matchEnd;
                }
                else
                {
                    builder.Append(value, searchStart, matchEnd - searchStart);
                    searchStart = matchEnd;
                }
            }

            if (searchStart == 0)
            {
                return value;
            }

            builder.Append(value, searchStart, value.Length - searchStart);
            return builder.ToString();
        }

        private static bool HasStandaloneBoundaries(string value, int start, int end, string fragment)
        {
            bool beginsWithWordCharacter = IsWordCharacter(fragment[0]);
            bool endsWithWordCharacter = IsWordCharacter(fragment[fragment.Length - 1]);
            if (beginsWithWordCharacter && start > 0 && IsWordCharacter(value[start - 1]))
            {
                return false;
            }

            if (endsWithWordCharacter && end < value.Length && IsWordCharacter(value[end]))
            {
                return false;
            }

            return true;
        }

        private static bool IsWordCharacter(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
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
