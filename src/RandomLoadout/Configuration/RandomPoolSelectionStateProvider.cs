// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
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
            JObject root = JObject.Parse(rawJson);
            RandomPoolSelectionStateFileModel fileModel = new RandomPoolSelectionStateFileModel();
            List<RandomPoolSelectionStatePresetModel> presets = new List<RandomPoolSelectionStatePresetModel>();
            JArray presetsArray = root["presets"] as JArray;
            if (presetsArray == null)
            {
                fileModel.Presets = presets.ToArray();
                return fileModel;
            }

            int presetIndex = 0;
            foreach (JToken presetToken in presetsArray)
            {
                presetIndex++;
                JObject presetObject = presetToken as JObject;
                if (presetObject == null)
                {
                    continue;
                }

                presets.Add(
                    new RandomPoolSelectionStatePresetModel
                    {
                        Id = StartItemsPresetNames.CreatePresetId(
                            GetString(presetObject, "id"),
                            GetString(presetObject, "name"),
                            presetIndex),
                        RandomPools = ParseRandomPools(presetObject["randomPools"] as JArray),
                    });
            }

            fileModel.Presets = presets.ToArray();
            return fileModel;
        }

        private static RandomPoolSelectionState[] ParseRandomPools(JArray poolsArray)
        {
            List<RandomPoolSelectionState> states = new List<RandomPoolSelectionState>();
            if (poolsArray == null)
            {
                return states.ToArray();
            }

            foreach (JToken poolToken in poolsArray)
            {
                JObject poolObject = poolToken as JObject;
                if (poolObject == null)
                {
                    continue;
                }

                states.Add(
                    new RandomPoolSelectionState(
                        GetInt(poolObject, "ruleIndex", -1),
                        GetString(poolObject, "poolSignature"),
                        GetIntArray(poolObject["shuffledPickupIds"]),
                        GetInt(poolObject, "nextIndex", 0)));
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

        private static string GetString(JObject objectValue, string propertyName)
        {
            return objectValue == null ? string.Empty : GetString(objectValue[propertyName]);
        }

        private static string GetString(JToken value)
        {
            if (value == null || value.Type == JTokenType.Null)
            {
                return string.Empty;
            }

            return value.Type == JTokenType.String ? value.Value<string>() : value.ToString();
        }

        private static int GetInt(JObject objectValue, string propertyName, int defaultValue)
        {
            int parsed;
            return int.TryParse(GetString(objectValue, propertyName), out parsed) ? parsed : defaultValue;
        }

        private static int[] GetIntArray(JToken value)
        {
            JArray array = value as JArray;
            if (array == null)
            {
                return new int[0];
            }

            List<int> values = new List<int>();
            foreach (JToken item in array)
            {
                int parsed;
                if (int.TryParse(GetString(item), out parsed))
                {
                    values.Add(parsed);
                }
            }

            return values.ToArray();
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
