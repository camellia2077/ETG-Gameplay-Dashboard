// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RandomLoadout
{
    internal sealed partial class JsonLoadoutRuleFileProvider
    {
        private static LoadoutRuleFileModel ParseRuleFile(string rawJson)
        {
            if (string.IsNullOrEmpty(rawJson))
            {
                return new LoadoutRuleFileModel();
            }

            if (string.IsNullOrEmpty(rawJson.Trim()))
            {
                return new LoadoutRuleFileModel();
            }

            bool hasPresets = Regex.IsMatch(rawJson, GetPropertyPrefixPattern("presets"), RegexOptions.IgnoreCase);
            bool hasRules = Regex.IsMatch(rawJson, GetPropertyPrefixPattern("rules"), RegexOptions.IgnoreCase);
            if (!hasPresets && !hasRules)
            {
                throw new FormatException("Rule file must contain a 'rules' array or a 'presets' array.");
            }

            if (hasPresets)
            {
                return ParsePresetRuleFile(rawJson);
            }

            return new LoadoutRuleFileModel { Rules = ParseRulesFromArrayBody(ExtractPropertyArrayBody(rawJson, "rules")) };
        }

        private static LoadoutRuleFileModel ParsePresetRuleFile(string rawJson)
        {
            List<LoadoutRuleFilePresetModel> presets = new List<LoadoutRuleFilePresetModel>();
            string presetsArrayBody = ExtractPropertyArrayBody(rawJson, "presets");
            List<string> presetBodies = ExtractObjectBodies(presetsArrayBody);
            for (int i = 0; i < presetBodies.Count; i++)
            {
                string presetBody = presetBodies[i];
                string rulesArrayBody = ExtractPropertyArrayBody(presetBody, "rules");
                if (string.IsNullOrEmpty(rulesArrayBody) &&
                    !ContainsArrayProperty(presetBody, "rules") &&
                    !ContainsArrayProperty(presetBody, "pickups"))
                {
                    continue;
                }

                string id = ParseString(presetBody, "id");
                string displayNameKey = ParseString(presetBody, "display_name_key");
                string name = ParseString(presetBody, "name");
                presets.Add(
                    new LoadoutRuleFilePresetModel
                    {
                        Id = StartItemsPresetNames.CreatePresetId(id, name, i + 1),
                        DisplayNameKey = StartItemsPresetNames.NormalizePresetName(displayNameKey),
                        Name = StartItemsPresetNames.NormalizePresetName(name),
                        Rules = ParseRulesFromArrayBody(rulesArrayBody),
                        Pickups = ParsePresetPickups(presetBody),
                    });
            }

            return new LoadoutRuleFileModel { Presets = presets.ToArray() };
        }

        private static LoadoutRuleFileRuleModel[] ParseRulesFromArrayBody(string arrayBody)
        {
            List<LoadoutRuleFileRuleModel> rules = new List<LoadoutRuleFileRuleModel>();
            List<string> ruleBodies = ExtractObjectBodies(arrayBody);
            for (int i = 0; i < ruleBodies.Count; i++)
            {
                string body = ruleBodies[i];
                if (Regex.IsMatch(body, GetPropertyPrefixPattern("mode"), RegexOptions.IgnoreCase))
                {
                    rules.Add(ParseRule(body));
                }
            }

            return rules.ToArray();
        }

        private static LoadoutRuleFileRuleModel ParseRule(string body)
        {
            LoadoutRuleFileRuleModel rule = new LoadoutRuleFileRuleModel();
            rule.Enabled = ParseBool(body, "enabled", true);
            rule.Mode = ParseString(body, "mode");
            rule.Category = ParseString(body, "category");
            rule.Count = ParseInt(body, "count", 1);
            rule.Id = ParseNullableInt(body, "id");
            rule.Alias = ParseString(body, "alias");
            rule.Name = ParseString(body, "name");
            rule.PoolIds = ParseIntArray(body, "poolIds");
            rule.PoolAliases = ParseStringArray(body, "poolAliases");
            rule.Pool = ParseStringArray(body, "pool");
            return rule;
        }

        private static LoadoutRuleFilePickupModel[] ParsePresetPickups(string body)
        {
            List<LoadoutRuleFilePickupModel> pickups = new List<LoadoutRuleFilePickupModel>();
            string pickupArrayBody = ExtractPropertyArrayBody(body, "pickups");
            if (string.IsNullOrEmpty(pickupArrayBody) && !ContainsArrayProperty(body, "pickups"))
            {
                return pickups.ToArray();
            }

            List<string> pickupBodies = ExtractObjectBodies(pickupArrayBody);
            if (pickupBodies.Count == 0)
            {
                string[] pickupTypes = ParseStringArray(body, "pickups");
                for (int i = 0; i < pickupTypes.Length; i++)
                {
                    string normalizedType = StartItemPickupCatalog.NormalizeType(pickupTypes[i]);
                    if (string.IsNullOrEmpty(normalizedType))
                    {
                        continue;
                    }

                    pickups.Add(new LoadoutRuleFilePickupModel { Type = normalizedType, Count = 1 });
                }
            }

            for (int i = 0; i < pickupBodies.Count; i++)
            {
                string pickupBody = pickupBodies[i];
                string normalizedType = StartItemPickupCatalog.NormalizeType(ParseString(pickupBody, "type"));
                if (string.IsNullOrEmpty(normalizedType))
                {
                    continue;
                }

                pickups.Add(
                    new LoadoutRuleFilePickupModel
                    {
                        Type = normalizedType,
                        Count = StartItemPickupCatalog.NormalizeCount(ParseInt(pickupBody, "count", 1)),
                    });
            }

            return StartItemPickupCatalog.MergePickups(pickups.ToArray());
        }

        private static string ExtractPropertyArrayBody(string rawJson, string propertyName)
        {
            Match match = Regex.Match(rawJson, GetPropertyPrefixPattern(propertyName) + "\\[", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return string.Empty;
            }

            int arrayStart = match.Index + match.Length - 1;
            int arrayEnd = FindMatchingClose(rawJson, arrayStart, '[', ']');
            return arrayEnd > arrayStart ? rawJson.Substring(arrayStart + 1, arrayEnd - arrayStart - 1) : string.Empty;
        }

        private static bool ContainsArrayProperty(string rawJson, string propertyName)
        {
            return Regex.IsMatch(rawJson ?? string.Empty, GetPropertyPrefixPattern(propertyName) + "\\[", RegexOptions.IgnoreCase);
        }

        private static List<string> ExtractObjectBodies(string arrayBody)
        {
            List<string> bodies = new List<string>();
            if (string.IsNullOrEmpty(arrayBody))
            {
                return bodies;
            }

            for (int index = 0; index < arrayBody.Length; index++)
            {
                if (arrayBody[index] != '{')
                {
                    continue;
                }

                int objectEnd = FindMatchingClose(arrayBody, index, '{', '}');
                if (objectEnd <= index)
                {
                    continue;
                }

                bodies.Add(arrayBody.Substring(index + 1, objectEnd - index - 1));
                index = objectEnd;
            }

            return bodies;
        }

        private static int FindMatchingClose(string text, int openIndex, char openChar, char closeChar)
        {
            int depth = 0;
            bool inString = false;
            char stringQuote = '\0';
            bool escaped = false;
            for (int index = openIndex; index < text.Length; index++)
            {
                char current = text[index];
                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (current == '\\')
                    {
                        escaped = true;
                    }
                    else if (current == stringQuote)
                    {
                        inString = false;
                    }

                    continue;
                }

                if (current == '"' || current == '\'')
                {
                    inString = true;
                    stringQuote = current;
                    continue;
                }

                if (current == openChar)
                {
                    depth++;
                }
                else if (current == closeChar)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return index;
                    }
                }
            }

            return -1;
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

            string value = match.Groups["dq"].Success
                ? match.Groups["dq"].Value
                : match.Groups["sq"].Value;
            return UnescapeJsonString(value);
        }

        private static bool ParseBool(string body, string propertyName, bool defaultValue)
        {
            Match match = Regex.Match(
                body,
                GetPropertyPrefixPattern(propertyName) + "(?<value>true|false)",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return defaultValue;
            }

            return string.Equals(match.Groups["value"].Value, "true", StringComparison.OrdinalIgnoreCase);
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

        private static int? ParseNullableInt(string body, string propertyName)
        {
            Match match = Regex.Match(
                body,
                GetPropertyPrefixPattern(propertyName) + "(?<value>-?\\d+)",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return null;
            }

            int value;
            return int.TryParse(match.Groups["value"].Value, out value) ? (int?)value : null;
        }

        private static int[] ParseIntArray(string body, string propertyName)
        {
            Match match = Regex.Match(
                body,
                GetPropertyPrefixPattern(propertyName) + "\\[(?<value>[\\s\\S]*?)\\]",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return new int[0];
            }

            MatchCollection itemMatches = Regex.Matches(match.Groups["value"].Value, "-?\\d+");
            List<int> values = new List<int>();
            for (int i = 0; i < itemMatches.Count; i++)
            {
                int value;
                if (int.TryParse(itemMatches[i].Value, out value))
                {
                    values.Add(value);
                }
            }

            return values.ToArray();
        }

        private static string[] ParseStringArray(string body, string propertyName)
        {
            Match match = Regex.Match(
                body,
                GetPropertyPrefixPattern(propertyName) + "\\[(?<value>[\\s\\S]*?)\\]",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return new string[0];
            }

            MatchCollection itemMatches = Regex.Matches(
                match.Groups["value"].Value,
                "(?:\"(?<dq>(?:\\\\.|[^\"])*)\"|'(?<sq>(?:\\\\.|[^'])*)')");
            List<string> values = new List<string>();
            for (int i = 0; i < itemMatches.Count; i++)
            {
                string itemValue = itemMatches[i].Groups["dq"].Success
                    ? itemMatches[i].Groups["dq"].Value
                    : itemMatches[i].Groups["sq"].Value;
                values.Add(UnescapeJsonString(itemValue));
            }

            return values.ToArray();
        }

        private static string UnescapeJsonString(string value)
        {
            return value
                .Replace("\\\"", "\"")
                .Replace("\\'", "'")
                .Replace("\\\\", "\\")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }

        private static string GetPropertyPrefixPattern(string propertyName)
        {
            string escaped = Regex.Escape(propertyName);
            return "(?:\"" + escaped + "\"|'" + escaped + "'|\\b" + escaped + "\\b)\\s*:\\s*";
        }
    }
}
