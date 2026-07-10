// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RandomLoadout
{
    internal sealed partial class JsonLoadoutRuleFileProvider
    {
        private static LoadoutRuleFileModel ParseRuleFile(string rawJson)
        {
            if (string.IsNullOrEmpty(rawJson) || string.IsNullOrEmpty(rawJson.Trim()))
            {
                return new LoadoutRuleFileModel();
            }

            JObject root = ParseObject(rawJson);
            if (root["presets"] is JArray)
            {
                return ParsePresetRuleFile(root);
            }

            if (root["rules"] is JArray)
            {
                return new LoadoutRuleFileModel
                {
                    Rules = ParseRulesFromArray(root["rules"] as JArray),
                };
            }

            throw new FormatException("Rule file must contain a 'rules' array or a 'presets' array.");
        }

        private static LoadoutRuleFileModel ParsePresetRuleFile(JObject root)
        {
            List<LoadoutRuleFilePresetModel> presets = new List<LoadoutRuleFilePresetModel>();
            JArray presetsArray = root["presets"] as JArray;
            if (presetsArray == null)
            {
                return new LoadoutRuleFileModel { Presets = presets.ToArray() };
            }

            int presetIndex = 0;
            foreach (JToken presetToken in presetsArray)
            {
                presetIndex++;
                JObject preset = presetToken as JObject;
                if (preset == null)
                {
                    continue;
                }

                bool hasRules = preset["rules"] is JArray;
                bool hasPickups = preset["pickups"] is JArray;
                if (!hasRules && !hasPickups)
                {
                    continue;
                }

                string id = GetString(preset, "id");
                string name = GetString(preset, "name");
                presets.Add(
                    new LoadoutRuleFilePresetModel
                    {
                        Id = StartItemsPresetNames.CreatePresetId(id, name, presetIndex),
                        DisplayNameKey = StartItemsPresetNames.NormalizePresetName(GetString(preset, "display_name_key")),
                        Name = StartItemsPresetNames.NormalizePresetName(name),
                        Rules = ParseRulesFromArray(preset["rules"] as JArray),
                        Pickups = ParsePresetPickups(preset),
                    });
            }

            return new LoadoutRuleFileModel { Presets = presets.ToArray() };
        }

        private static LoadoutRuleFileRuleModel[] ParseRulesFromArray(JArray rulesArray)
        {
            List<LoadoutRuleFileRuleModel> rules = new List<LoadoutRuleFileRuleModel>();
            if (rulesArray == null)
            {
                return rules.ToArray();
            }

            foreach (JToken ruleToken in rulesArray)
            {
                JObject ruleObject = ruleToken as JObject;
                if (ruleObject == null || ruleObject["mode"] == null || ruleObject["mode"].Type == JTokenType.Null)
                {
                    continue;
                }

                rules.Add(ParseRule(ruleObject));
            }

            return rules.ToArray();
        }

        private static LoadoutRuleFileRuleModel ParseRule(JObject ruleObject)
        {
            return new LoadoutRuleFileRuleModel
            {
                Enabled = GetBool(ruleObject, "enabled", true),
                Mode = GetString(ruleObject, "mode"),
                Category = GetString(ruleObject, "category"),
                Count = GetInt(ruleObject, "count", 1),
                Id = GetNullableInt(ruleObject, "id"),
                Alias = GetString(ruleObject, "alias"),
                Name = GetString(ruleObject, "name"),
                PoolIds = GetIntArray(ruleObject["poolIds"]),
                PoolAliases = GetStringArray(ruleObject["poolAliases"]),
                Pool = GetStringArray(ruleObject["pool"]),
            };
        }

        private static LoadoutRuleFilePickupModel[] ParsePresetPickups(JObject preset)
        {
            List<LoadoutRuleFilePickupModel> pickups = new List<LoadoutRuleFilePickupModel>();
            JArray pickupsArray = preset["pickups"] as JArray;
            if (pickupsArray == null)
            {
                return pickups.ToArray();
            }

            foreach (JToken pickupToken in pickupsArray)
            {
                JObject pickupObject = pickupToken as JObject;
                string type;
                int count;
                if (pickupObject != null)
                {
                    type = GetString(pickupObject, "type");
                    count = GetInt(pickupObject, "count", 1);
                }
                else
                {
                    type = GetString(pickupToken);
                    count = 1;
                }

                string normalizedType = StartItemPickupCatalog.NormalizeType(type);
                if (!string.IsNullOrEmpty(normalizedType))
                {
                    pickups.Add(
                        new LoadoutRuleFilePickupModel
                        {
                            Type = normalizedType,
                            Count = pickupObject == null
                                ? 1
                                : StartItemPickupCatalog.NormalizeCount(count),
                        });
                }
            }

            return StartItemPickupCatalog.MergePickups(pickups.ToArray());
        }

        private static JObject ParseObject(string rawJson)
        {
            return JObject.Parse(rawJson);
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

        private static bool GetBool(JObject objectValue, string propertyName, bool defaultValue)
        {
            JToken value = objectValue != null ? objectValue[propertyName] : null;
            if (value == null || value.Type == JTokenType.Null)
            {
                return defaultValue;
            }

            if (value.Type == JTokenType.Boolean)
            {
                return value.Value<bool>();
            }

            bool parsed;
            return bool.TryParse(GetString(value), out parsed) ? parsed : defaultValue;
        }

        private static int GetInt(JObject objectValue, string propertyName, int defaultValue)
        {
            return GetInt(objectValue != null ? objectValue[propertyName] : null, defaultValue);
        }

        private static int GetInt(JToken value, int defaultValue)
        {
            int parsed;
            return int.TryParse(GetString(value), out parsed) ? parsed : defaultValue;
        }

        private static int? GetNullableInt(JObject objectValue, string propertyName)
        {
            JToken value = objectValue != null ? objectValue[propertyName] : null;
            if (value == null || value.Type == JTokenType.Null)
            {
                return null;
            }

            int parsed;
            return int.TryParse(GetString(value), out parsed) ? (int?)parsed : null;
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

        private static string[] GetStringArray(JToken value)
        {
            JArray array = value as JArray;
            if (array == null)
            {
                return new string[0];
            }

            List<string> values = new List<string>();
            foreach (JToken item in array)
            {
                string parsed = GetString(item);
                if (!string.IsNullOrEmpty(parsed))
                {
                    values.Add(parsed);
                }
            }

            return values.ToArray();
        }
    }
}
