// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed class RandomPoolSelectionStateProvider
    {
        private readonly string _filePath;

        public RandomPoolSelectionStateProvider(string filePath)
        {
            _filePath = filePath;
        }

        public RandomPoolSelectionState[] Load(string presetId)
        {
            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
            {
                return new RandomPoolSelectionState[0];
            }

            string rawJson = Json5TextNormalizer.Normalize(File.ReadAllText(_filePath, Encoding.UTF8));
            RandomPoolSelectionStateFileModel fileModel = ParseFile(rawJson);
            RandomPoolSelectionStatePresetModel preset = FindPreset(fileModel, presetId);
            return preset != null && preset.RandomPools != null
                ? preset.RandomPools
                : new RandomPoolSelectionState[0];
        }

        public void Save(string presetId, RandomPoolSelectionState[] states)
        {
            RandomPoolSelectionStateFileModel fileModel = File.Exists(_filePath)
                ? ParseFile(Json5TextNormalizer.Normalize(File.ReadAllText(_filePath, Encoding.UTF8)))
                : new RandomPoolSelectionStateFileModel();

            string normalizedPresetId = NormalizePresetId(presetId);
            RandomPoolSelectionStatePresetModel preset = FindPreset(fileModel, normalizedPresetId);
            if (preset == null)
            {
                List<RandomPoolSelectionStatePresetModel> presets = new List<RandomPoolSelectionStatePresetModel>(fileModel.Presets ?? new RandomPoolSelectionStatePresetModel[0]);
                preset = new RandomPoolSelectionStatePresetModel { Id = normalizedPresetId };
                presets.Add(preset);
                fileModel.Presets = presets.ToArray();
            }

            preset.RandomPools = states ?? new RandomPoolSelectionState[0];

            string directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_filePath, SerializeFile(fileModel), Encoding.UTF8);
        }

        private static RandomPoolSelectionStatePresetModel FindPreset(RandomPoolSelectionStateFileModel fileModel, string presetId)
        {
            if (fileModel == null || fileModel.Presets == null)
            {
                return null;
            }

            string normalizedPresetId = NormalizePresetId(presetId);
            for (int i = 0; i < fileModel.Presets.Length; i++)
            {
                RandomPoolSelectionStatePresetModel preset = fileModel.Presets[i];
                if (preset != null && string.Equals(NormalizePresetId(preset.Id), normalizedPresetId, StringComparison.OrdinalIgnoreCase))
                {
                    return preset;
                }
            }

            return null;
        }

        private static string NormalizePresetId(string presetId)
        {
            return StartItemsPresetNames.NormalizePresetId(presetId);
        }

        private static RandomPoolSelectionStateFileModel ParseFile(string rawJson)
        {
            RandomPoolSelectionStateFileModel fileModel = new RandomPoolSelectionStateFileModel();
            string presetsArrayBody = ExtractPropertyArrayBody(rawJson, "presets");
            List<string> presetBodies = ExtractObjectBodies(presetsArrayBody);
            List<RandomPoolSelectionStatePresetModel> presets = new List<RandomPoolSelectionStatePresetModel>();
            for (int i = 0; i < presetBodies.Count; i++)
            {
                string presetBody = presetBodies[i];
                string randomPoolsArrayBody = ExtractPropertyArrayBody(presetBody, "randomPools");
                presets.Add(
                    new RandomPoolSelectionStatePresetModel
                    {
                        Id = StartItemsPresetNames.CreatePresetId(ParseString(presetBody, "id"), ParseString(presetBody, "name"), i + 1),
                        RandomPools = ParseRandomPools(randomPoolsArrayBody),
                    });
            }

            fileModel.Presets = presets.ToArray();
            return fileModel;
        }

        private static RandomPoolSelectionState[] ParseRandomPools(string arrayBody)
        {
            List<string> poolBodies = ExtractObjectBodies(arrayBody);
            List<RandomPoolSelectionState> states = new List<RandomPoolSelectionState>();
            for (int i = 0; i < poolBodies.Count; i++)
            {
                string poolBody = poolBodies[i];
                states.Add(
                    new RandomPoolSelectionState(
                        ParseInt(poolBody, "ruleIndex", -1),
                        ParseString(poolBody, "poolSignature"),
                        ParseIntArray(poolBody, "shuffledPickupIds"),
                        ParseInt(poolBody, "nextIndex", 0)));
            }

            return states.ToArray();
        }

        private static string SerializeFile(RandomPoolSelectionStateFileModel fileModel)
        {
            StringBuilder builder = new StringBuilder();
            RandomPoolSelectionStatePresetModel[] presets = fileModel != null && fileModel.Presets != null
                ? fileModel.Presets
                : new RandomPoolSelectionStatePresetModel[0];

            builder.AppendLine("{");
            builder.AppendLine("  presets: [");
            for (int i = 0; i < presets.Length; i++)
            {
                AppendPreset(builder, presets[i] ?? new RandomPoolSelectionStatePresetModel());
                builder.AppendLine(i < presets.Length - 1 ? "," : string.Empty);
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AppendPreset(StringBuilder builder, RandomPoolSelectionStatePresetModel preset)
        {
            RandomPoolSelectionState[] randomPools = preset != null && preset.RandomPools != null
                ? preset.RandomPools
                : new RandomPoolSelectionState[0];

            builder.AppendLine("    {");
            builder.AppendLine("      id: \"" + EscapeJsonString(NormalizePresetId(preset != null ? preset.Id : string.Empty)) + "\",");
            builder.AppendLine("      randomPools: [");
            for (int i = 0; i < randomPools.Length; i++)
            {
                AppendRandomPool(builder, randomPools[i], "        ");
                builder.AppendLine(i < randomPools.Length - 1 ? "," : string.Empty);
            }

            builder.AppendLine("      ]");
            builder.Append("    }");
        }

        private static void AppendRandomPool(StringBuilder builder, RandomPoolSelectionState state, string indent)
        {
            RandomPoolSelectionState normalizedState = state ?? new RandomPoolSelectionState(-1, string.Empty, null, 0);
            builder.AppendLine(indent + "{");
            builder.AppendLine(indent + "  ruleIndex: " + normalizedState.RuleIndex.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine(indent + "  poolSignature: \"" + EscapeJsonString(normalizedState.PoolSignature) + "\",");
            builder.AppendLine(indent + "  shuffledPickupIds: " + FormatIntArray(normalizedState.ShuffledPickupIds) + ",");
            builder.AppendLine(indent + "  nextIndex: " + normalizedState.NextIndex.ToString(CultureInfo.InvariantCulture));
            builder.Append(indent + "}");
        }

        private static string ExtractPropertyArrayBody(string rawJson, string propertyName)
        {
            Match match = Regex.Match(rawJson ?? string.Empty, GetPropertyPrefixPattern(propertyName) + "\\[", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return string.Empty;
            }

            int arrayStart = match.Index + match.Length - 1;
            int arrayEnd = FindMatchingClose(rawJson, arrayStart, '[', ']');
            return arrayEnd > arrayStart ? rawJson.Substring(arrayStart + 1, arrayEnd - arrayStart - 1) : string.Empty;
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
                body ?? string.Empty,
                GetPropertyPrefixPattern(propertyName) + "(?:\"(?<dq>(?:\\\\.|[^\"])*)\"|'(?<sq>(?:\\\\.|[^'])*)')",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return string.Empty;
            }

            return UnescapeJsonString(match.Groups["dq"].Success ? match.Groups["dq"].Value : match.Groups["sq"].Value);
        }

        private static int ParseInt(string body, string propertyName, int defaultValue)
        {
            Match match = Regex.Match(body ?? string.Empty, GetPropertyPrefixPattern(propertyName) + "(?<value>-?\\d+)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return defaultValue;
            }

            int value;
            return int.TryParse(match.Groups["value"].Value, out value) ? value : defaultValue;
        }

        private static int[] ParseIntArray(string body, string propertyName)
        {
            Match match = Regex.Match(body ?? string.Empty, GetPropertyPrefixPattern(propertyName) + "\\[(?<value>[\\s\\S]*?)\\]", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return new int[0];
            }

            MatchCollection itemMatches = Regex.Matches(match.Groups["value"].Value, "-?\\d+");
            int[] values = new int[itemMatches.Count];
            for (int i = 0; i < itemMatches.Count; i++)
            {
                int value;
                values[i] = int.TryParse(itemMatches[i].Value, out value) ? value : 0;
            }

            return values;
        }

        private static string GetPropertyPrefixPattern(string propertyName)
        {
            return "(?:^|[,\\{\\s])(?:\"" + Regex.Escape(propertyName) + "\"|" + Regex.Escape(propertyName) + ")\\s*:\\s*";
        }

        private static string FormatIntArray(int[] values)
        {
            if (values == null || values.Length == 0)
            {
                return "[]";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("[");
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(values[i].ToString(CultureInfo.InvariantCulture));
            }

            builder.Append("]");
            return builder.ToString();
        }

        private static string EscapeJsonString(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private static string UnescapeJsonString(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\\"", "\"")
                .Replace("\\'", "'")
                .Replace("\\\\", "\\")
                .Replace("\\r", "\r")
                .Replace("\\n", "\n")
                .Replace("\\t", "\t");
        }

        private sealed class RandomPoolSelectionStateFileModel
        {
            public RandomPoolSelectionStateFileModel()
            {
                Presets = new RandomPoolSelectionStatePresetModel[0];
            }

            public RandomPoolSelectionStatePresetModel[] Presets { get; set; }
        }

        private sealed class RandomPoolSelectionStatePresetModel
        {
            public RandomPoolSelectionStatePresetModel()
            {
                RandomPools = new RandomPoolSelectionState[0];
            }

            public string Id { get; set; }

            public RandomPoolSelectionState[] RandomPools { get; set; }
        }
    }
}
