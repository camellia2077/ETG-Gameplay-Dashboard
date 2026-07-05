using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RandomLoadout
{
    internal sealed class JsonPickupGameplayProvider
    {
        private readonly string _englishFilePath;
        private readonly string _simplifiedChineseWorkFilePath;

        public JsonPickupGameplayProvider(string englishFilePath, string simplifiedChineseWorkFilePath)
        {
            _englishFilePath = englishFilePath;
            _simplifiedChineseWorkFilePath = simplifiedChineseWorkFilePath;
        }

        public PickupGameplayRegistry Load(out string message, out string warning)
        {
            message = string.Empty;
            warning = string.Empty;

            if (!File.Exists(_englishFilePath))
            {
                warning =
                    "Pickup gameplay info file was not found at '" + _englishFilePath + "'. " +
                    "Deploy the repository default catalog files again if you want gameplay-focused nearby pickup info.";
                return PickupGameplayRegistry.Empty;
            }

            try
            {
                string englishRawJson = File.ReadAllText(_englishFilePath, Encoding.UTF8);
                List<PickupGameplayEntry> englishEntries = ParseEntries(englishRawJson, null);
                Dictionary<int, PickupGameplayEntry> mergedEntries = BuildEntryDictionary(englishEntries);

                bool loadedSimplifiedChineseWorkFile = false;
                if (!string.IsNullOrEmpty(_simplifiedChineseWorkFilePath) && File.Exists(_simplifiedChineseWorkFilePath))
                {
                    string simplifiedChineseRawJson = File.ReadAllText(_simplifiedChineseWorkFilePath, Encoding.UTF8);
                    List<PickupGameplayEntry> localizedEntries = ParseEntries(simplifiedChineseRawJson, mergedEntries);
                    mergedEntries = MergeLocalizedEntries(mergedEntries, localizedEntries);
                    loadedSimplifiedChineseWorkFile = true;
                }

                PickupGameplayRegistry registry = new PickupGameplayRegistry(ToEntryArray(mergedEntries));
                message = "Loaded pickup gameplay info from '" + _englishFilePath + "' (" + registry.Count + " entries).";
                if (loadedSimplifiedChineseWorkFile)
                {
                    message += " Merged Simplified Chinese gameplay info from '" + _simplifiedChineseWorkFilePath + "'.";
                }

                return registry;
            }
            catch (Exception exception)
            {
                warning = "Failed to parse pickup gameplay info file '" + _englishFilePath + "': " + exception.Message;
                return PickupGameplayRegistry.Empty;
            }
        }

        public PickupInfoTermsRegistry LoadTerms(out string message, out string warning)
        {
            message = string.Empty;
            warning = string.Empty;

            if (string.IsNullOrEmpty(_simplifiedChineseWorkFilePath) || !File.Exists(_simplifiedChineseWorkFilePath))
            {
                warning =
                    "Pickup gameplay Simplified Chinese work file was not found at '" +
                    _simplifiedChineseWorkFilePath +
                    "'. Nearby pickup gameplay terms will use built-in fallbacks.";
                return PickupInfoTermsRegistry.Empty;
            }

            try
            {
                string rawJson = File.ReadAllText(_simplifiedChineseWorkFilePath, Encoding.UTF8);
                PickupInfoTermsTable simplifiedChinese = ParseTermsTable(rawJson);
                PickupInfoTermsRegistry registry = new PickupInfoTermsRegistry(PickupInfoTermsTable.Empty, simplifiedChinese);
                message = "Loaded pickup gameplay terms from '" + _simplifiedChineseWorkFilePath + "'.";
                return registry;
            }
            catch (Exception exception)
            {
                warning = "Failed to parse pickup gameplay terms from '" + _simplifiedChineseWorkFilePath + "': " + exception.Message;
                return PickupInfoTermsRegistry.Empty;
            }
        }

        private static Dictionary<int, PickupGameplayEntry> BuildEntryDictionary(List<PickupGameplayEntry> entries)
        {
            Dictionary<int, PickupGameplayEntry> mergedEntries = new Dictionary<int, PickupGameplayEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                PickupGameplayEntry entry = entries[i];
                if (entry == null || mergedEntries.ContainsKey(entry.PickupId))
                {
                    continue;
                }

                mergedEntries.Add(entry.PickupId, entry);
            }

            return mergedEntries;
        }

        private static Dictionary<int, PickupGameplayEntry> MergeLocalizedEntries(
            Dictionary<int, PickupGameplayEntry> baseEntries,
            List<PickupGameplayEntry> localizedEntries)
        {
            Dictionary<int, PickupGameplayEntry> merged = new Dictionary<int, PickupGameplayEntry>(baseEntries);
            for (int i = 0; i < localizedEntries.Count; i++)
            {
                PickupGameplayEntry localizedEntry = localizedEntries[i];
                if (localizedEntry == null || localizedEntry.PickupId <= 0)
                {
                    continue;
                }

                PickupGameplayEntry baseEntry;
                if (!merged.TryGetValue(localizedEntry.PickupId, out baseEntry))
                {
                    merged[localizedEntry.PickupId] = localizedEntry;
                    continue;
                }

                merged[localizedEntry.PickupId] = new PickupGameplayEntry(
                    baseEntry.PickupId,
                    baseEntry.EnglishDisplayName,
                    baseEntry.WikiKey,
                    baseEntry.Quality,
                    baseEntry.PickupType,
                    baseEntry.StatGroups,
                    baseEntry.Unlock,
                    baseEntry.EnglishGameplaySummary,
                    baseEntry.EnglishEffectHighlights,
                    baseEntry.EnglishSynergyHighlights,
                    baseEntry.EnglishUsageNotes,
                    GetMergedValue(localizedEntry.ChineseDisplayName, baseEntry.ChineseDisplayName),
                    GetMergedValue(localizedEntry.ChineseGameplaySummary, baseEntry.ChineseGameplaySummary),
                    GetMergedValue(localizedEntry.ChineseEffectHighlights, baseEntry.ChineseEffectHighlights),
                    GetMergedValue(localizedEntry.ChineseSynergyHighlights, baseEntry.ChineseSynergyHighlights),
                    GetMergedValue(localizedEntry.ChineseUsageNotes, baseEntry.ChineseUsageNotes));
            }

            return merged;
        }

        private static string GetMergedValue(string localizedValue, string baseValue)
        {
            return !string.IsNullOrEmpty(localizedValue) ? localizedValue : baseValue;
        }

        private static PickupGameplayEntry[] ToEntryArray(Dictionary<int, PickupGameplayEntry> entriesByPickupId)
        {
            List<PickupGameplayEntry> entries = new List<PickupGameplayEntry>(entriesByPickupId.Values);
            entries.Sort(CompareEntries);
            return entries.ToArray();
        }

        private static int CompareEntries(PickupGameplayEntry left, PickupGameplayEntry right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            return left.PickupId.CompareTo(right.PickupId);
        }

        private static List<PickupGameplayEntry> ParseEntries(
            string rawJson,
            Dictionary<int, PickupGameplayEntry> englishEntriesByPickupId)
        {
            List<PickupGameplayEntry> entries = new List<PickupGameplayEntry>();
            if (string.IsNullOrEmpty(rawJson))
            {
                return entries;
            }

            string entriesArrayBody = ExtractArrayBody(rawJson, "entries");
            if (string.IsNullOrEmpty(entriesArrayBody))
            {
                return entries;
            }

            List<string> objectBodies = ExtractTopLevelObjectBodies(entriesArrayBody);
            for (int i = 0; i < objectBodies.Count; i++)
            {
                string body = objectBodies[i];
                int pickupId = ParseInt(body, "pickupId", -1);
                if (pickupId <= 0)
                {
                    continue;
                }

                PickupGameplayEntry englishEntry = null;
                if (englishEntriesByPickupId != null)
                {
                    englishEntriesByPickupId.TryGetValue(pickupId, out englishEntry);
                }

                string englishDisplayName = englishEntry != null ? englishEntry.EnglishDisplayName : ParseString(body, "englishDisplayName");
                string englishGameplaySummary = englishEntry != null ? englishEntry.EnglishGameplaySummary : ParseString(body, "englishGameplaySummary");
                bool hasEnglishContent = !string.IsNullOrEmpty(englishDisplayName) || !string.IsNullOrEmpty(englishGameplaySummary);
                bool hasChineseContent =
                    !string.IsNullOrEmpty(ParseString(body, "chineseDisplayName")) ||
                    !string.IsNullOrEmpty(ParseString(body, "chineseGameplaySummary")) ||
                    !string.IsNullOrEmpty(ParseString(body, "chineseEffectHighlights")) ||
                    !string.IsNullOrEmpty(ParseString(body, "chineseSynergyHighlights")) ||
                    !string.IsNullOrEmpty(ParseString(body, "chineseUsageNotes"));

                if (englishEntry == null && !hasEnglishContent)
                {
                    continue;
                }

                if (englishEntry != null && !hasChineseContent)
                {
                    continue;
                }

                entries.Add(
                    new PickupGameplayEntry(
                        pickupId,
                        englishEntry != null ? englishEntry.EnglishDisplayName : englishDisplayName,
                        englishEntry != null ? englishEntry.WikiKey : ParseString(body, "wikiKey"),
                        englishEntry != null ? englishEntry.Quality : ParseString(body, "quality"),
                        englishEntry != null ? englishEntry.PickupType : ParseString(body, "pickupType"),
                        englishEntry != null ? englishEntry.StatGroups : ParseStatGroups(body),
                        englishEntry != null ? englishEntry.Unlock : ParseString(body, "unlock"),
                        englishEntry != null ? englishEntry.EnglishGameplaySummary : englishGameplaySummary,
                        englishEntry != null ? englishEntry.EnglishEffectHighlights : ParseString(body, "englishEffectHighlights"),
                        englishEntry != null ? englishEntry.EnglishSynergyHighlights : ParseString(body, "englishSynergyHighlights"),
                        englishEntry != null ? englishEntry.EnglishUsageNotes : ParseString(body, "englishUsageNotes"),
                        ParseString(body, "chineseDisplayName"),
                        ParseString(body, "chineseGameplaySummary"),
                        ParseString(body, "chineseEffectHighlights"),
                        ParseString(body, "chineseSynergyHighlights"),
                        ParseString(body, "chineseUsageNotes")));
            }

            return entries;
        }

        private static PickupGameplayStatGroup[] ParseStatGroups(string body)
        {
            string statGroupsBody = ExtractArrayBody(body, "statGroups");
            if (string.IsNullOrEmpty(statGroupsBody))
            {
                return new PickupGameplayStatGroup[0];
            }

            List<string> groupBodies = ExtractTopLevelObjectBodies(statGroupsBody);
            List<PickupGameplayStatGroup> groups = new List<PickupGameplayStatGroup>();
            for (int i = 0; i < groupBodies.Count; i++)
            {
                string groupBody = groupBodies[i];
                string statsArrayBody = ExtractArrayBody(groupBody, "stats");
                List<PickupGameplayStatEntry> stats = new List<PickupGameplayStatEntry>();
                if (!string.IsNullOrEmpty(statsArrayBody))
                {
                    List<string> statBodies = ExtractTopLevelObjectBodies(statsArrayBody);
                    for (int j = 0; j < statBodies.Count; j++)
                    {
                        string statBody = statBodies[j];
                        string labelKey = ParseString(statBody, "labelKey");
                        string value = ParseString(statBody, "value");
                        if (string.IsNullOrEmpty(labelKey) || string.IsNullOrEmpty(value))
                        {
                            continue;
                        }

                        stats.Add(new PickupGameplayStatEntry(labelKey, value));
                    }
                }

                if (stats.Count == 0)
                {
                    continue;
                }

                groups.Add(new PickupGameplayStatGroup(ParseString(groupBody, "groupKey"), stats.ToArray()));
            }

            return groups.ToArray();
        }

        private static PickupInfoTermsTable ParseTermsTable(string rawJson)
        {
            return new PickupInfoTermsTable(
                ParseFlatStringObject(ExtractObjectBody(rawJson, "sectionLabels")),
                ParseFlatStringObject(ExtractObjectBody(rawJson, "statLabels")),
                ParseFlatStringObject(ExtractObjectBody(rawJson, "valueMappings")));
        }

        private static Dictionary<string, string> ParseFlatStringObject(string rawJson)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(rawJson))
            {
                return values;
            }

            MatchCollection matches = Regex.Matches(
                rawJson,
                "(?:\"(?<dqk>(?:\\\\.|[^\"])*)\"|'(?<sqk>(?:\\\\.|[^'])*)'|(?<bare>[A-Za-z0-9_.-]+))\\s*:\\s*(?:\"(?<dqv>(?:\\\\.|[^\"])*)\"|'(?<sqv>(?:\\\\.|[^'])*)')",
                RegexOptions.Singleline);
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                string key = GetGroupValue(match, "dqk", "sqk", "bare");
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                values[key] = UnescapeJsonString(GetGroupValue(match, "dqv", "sqv"));
            }

            return values;
        }

        private static string ParseString(string body, string propertyName)
        {
            Match match = Regex.Match(
                body,
                GetPropertyPrefixPattern(propertyName) + "(?:\"(?<dq>(?:\\\\.|[^\"])*)\"|'(?<sq>(?:\\\\.|[^'])*)')",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return string.Empty;
            }

            string value = match.Groups["dq"].Success ? match.Groups["dq"].Value : match.Groups["sq"].Value;
            return UnescapeJsonString(value);
        }

        private static int ParseInt(string body, string propertyName, int defaultValue)
        {
            Match match = Regex.Match(
                body,
                GetPropertyPrefixPattern(propertyName) + "(?<value>-?\\d+)",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return defaultValue;
            }

            int value;
            return int.TryParse(match.Groups["value"].Value, out value) ? value : defaultValue;
        }

        private static string GetPropertyPrefixPattern(string propertyName)
        {
            string escaped = Regex.Escape(propertyName);
            return "(?:\"" + escaped + "\"|'"+ escaped + "'|\\b" + escaped + "\\b)\\s*:\\s*";
        }

        private static string ExtractArrayBody(string text, string propertyName)
        {
            Match match = Regex.Match(text, GetPropertyPrefixPattern(propertyName) + "\\[", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return string.Empty;
            }

            int arrayStartIndex = match.Index + match.Length - 1;
            return ExtractBracketBody(text, arrayStartIndex, '[', ']');
        }

        private static string ExtractObjectBody(string text, string propertyName)
        {
            Match match = Regex.Match(text, GetPropertyPrefixPattern(propertyName) + "\\{", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return string.Empty;
            }

            int objectStartIndex = match.Index + match.Length - 1;
            return ExtractBracketBody(text, objectStartIndex, '{', '}');
        }

        private static string ExtractBracketBody(string text, int startIndex, char openChar, char closeChar)
        {
            if (string.IsNullOrEmpty(text) || startIndex < 0 || startIndex >= text.Length || text[startIndex] != openChar)
            {
                return string.Empty;
            }

            int depth = 0;
            bool inString = false;
            char stringDelimiter = '\0';
            for (int i = startIndex; i < text.Length; i++)
            {
                char current = text[i];
                if (inString)
                {
                    if (current == '\\')
                    {
                        i++;
                        continue;
                    }

                    if (current == stringDelimiter)
                    {
                        inString = false;
                    }

                    continue;
                }

                if (current == '"' || current == '\'')
                {
                    inString = true;
                    stringDelimiter = current;
                    continue;
                }

                if (current == openChar)
                {
                    depth++;
                    continue;
                }

                if (current == closeChar)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return text.Substring(startIndex + 1, i - startIndex - 1);
                    }
                }
            }

            return string.Empty;
        }

        private static List<string> ExtractTopLevelObjectBodies(string arrayBody)
        {
            List<string> objectBodies = new List<string>();
            if (string.IsNullOrEmpty(arrayBody))
            {
                return objectBodies;
            }

            bool inString = false;
            char stringDelimiter = '\0';
            int depth = 0;
            int objectStartIndex = -1;
            for (int i = 0; i < arrayBody.Length; i++)
            {
                char current = arrayBody[i];
                if (inString)
                {
                    if (current == '\\')
                    {
                        i++;
                        continue;
                    }

                    if (current == stringDelimiter)
                    {
                        inString = false;
                    }

                    continue;
                }

                if (current == '"' || current == '\'')
                {
                    inString = true;
                    stringDelimiter = current;
                    continue;
                }

                if (current == '{')
                {
                    if (depth == 0)
                    {
                        objectStartIndex = i;
                    }

                    depth++;
                    continue;
                }

                if (current == '}')
                {
                    depth--;
                    if (depth == 0 && objectStartIndex >= 0)
                    {
                        objectBodies.Add(arrayBody.Substring(objectStartIndex + 1, i - objectStartIndex - 1));
                        objectStartIndex = -1;
                    }
                }
            }

            return objectBodies;
        }

        private static string GetGroupValue(Match match, params string[] groupNames)
        {
            if (match == null || groupNames == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < groupNames.Length; i++)
            {
                Group group = match.Groups[groupNames[i]];
                if (group != null && group.Success)
                {
                    return group.Value;
                }
            }

            return string.Empty;
        }

        private static string UnescapeJsonString(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\\"", "\"")
                .Replace("\\'", "'")
                .Replace("\\\\", "\\")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }
    }
}
