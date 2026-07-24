// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace EtgGameplayDashboard
{
    internal sealed class JsonPickupGameplayProvider
    {
        private readonly string _gameplayFilePath;
        private readonly string _termsFilePath;

        public JsonPickupGameplayProvider(string gameplayFilePath, string termsFilePath)
        {
            _gameplayFilePath = gameplayFilePath;
            _termsFilePath = termsFilePath;
        }

        public string GameplayFilePath
        {
            get { return _gameplayFilePath; }
        }

        public string TermsFilePath
        {
            get { return _termsFilePath; }
        }

        public PickupGameplayRegistry Load(out string message, out string warning)
        {
            message = string.Empty;
            warning = string.Empty;

            if (!File.Exists(_gameplayFilePath))
            {
                warning =
                    "Pickup gameplay info file was not found at '" + _gameplayFilePath + "'. " +
                    "Deploy the repository default catalog files again if you want gameplay-focused nearby pickup info.";
                return PickupGameplayRegistry.Empty;
            }

            try
            {
                string rawJson = File.ReadAllText(_gameplayFilePath, Encoding.UTF8);
                PickupGameplayEntry[] entries = ParsePickupGameplayEntries(rawJson);
                PickupGameplayRegistry registry = new PickupGameplayRegistry(entries);
                message = "Loaded pickup gameplay info v2 from '" + _gameplayFilePath + "' (" + registry.Count + " entries).";
                return registry;
            }
            catch (Exception exception)
            {
                warning = "Failed to parse pickup gameplay info file '" + _gameplayFilePath + "': " + exception.Message;
                return PickupGameplayRegistry.Empty;
            }
        }

        public PickupInfoTermsRegistry LoadTerms(out string message, out string warning)
        {
            message = string.Empty;
            warning = string.Empty;

            if (string.IsNullOrEmpty(_termsFilePath) || !File.Exists(_termsFilePath))
            {
                warning =
                    "Pickup info terms file was not found at '" +
                    _termsFilePath +
                    "'. Nearby pickup gameplay terms will use built-in fallbacks.";
                return PickupInfoTermsRegistry.Empty;
            }

            try
            {
                string rawJson = File.ReadAllText(_termsFilePath, Encoding.UTF8);
                JObject root = ParseObject(rawJson);
                PickupInfoTermsTable english = ParseTermsTable(root, "en");
                PickupInfoTermsTable simplifiedChinese = ParseTermsTable(root, "zh-CN");
                PickupInfoTermsRegistry registry = new PickupInfoTermsRegistry(english, simplifiedChinese);
                message = "Loaded pickup gameplay terms v2 from '" + _termsFilePath + "'.";
                return registry;
            }
            catch (Exception exception)
            {
                warning = "Failed to parse pickup gameplay terms from '" + _termsFilePath + "': " + exception.Message;
                return PickupInfoTermsRegistry.Empty;
            }
        }

        private static PickupGameplayEntry[] ParsePickupGameplayEntries(string rawJson)
        {
            JObject root = ParseObject(rawJson);
            JObject pickups = root["pickups"] as JObject;
            if (pickups == null)
            {
                return new PickupGameplayEntry[0];
            }

            List<PickupGameplayEntry> entries = new List<PickupGameplayEntry>();
            foreach (JProperty property in pickups.Properties())
            {
                JObject pickup = property.Value as JObject;
                if (pickup == null)
                {
                    continue;
                }

                int pickupId = ParseInt(property.Name, -1);
                if (pickupId < 0)
                {
                    pickupId = ParseInt(pickup["id"], -1);
                }

                if (pickupId < 0)
                {
                    continue;
                }

                JObject names = pickup["names"] as JObject;
                JObject text = pickup["text"] as JObject;
                entries.Add(
                    new PickupGameplayEntry(
                        pickupId,
                        GetString(names, "en"),
                        GetString(names, "zh-CN"),
                        GetString(pickup, "wikiKey"),
                        GetString(pickup, "quality"),
                        GetString(pickup, "type"),
                        ParseStatSections(pickup["statSections"] as JArray),
                        GetLocalizedString(text, "summary", "en"),
                        GetLocalizedString(text, "summary", "zh-CN"),
                        GetLocalizedStringArray(text, "effects", "en"),
                        GetLocalizedStringArray(text, "effects", "zh-CN"),
                        GetLocalizedStringArray(text, "synergies", "en"),
                        GetLocalizedStringArray(text, "synergies", "zh-CN"),
                        GetLocalizedStringArray(text, "notes", "en"),
                        GetLocalizedStringArray(text, "notes", "zh-CN")));
            }

            entries.Sort(CompareEntries);
            return entries.ToArray();
        }

        private static int CompareEntries(PickupGameplayEntry left, PickupGameplayEntry right)
        {
            if (left == null && right == null) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return left.PickupId.CompareTo(right.PickupId);
        }

        private static PickupGameplayStatSection[] ParseStatSections(JArray sectionsArray)
        {
            if (sectionsArray == null) return new PickupGameplayStatSection[0];

            List<PickupGameplayStatSection> sections = new List<PickupGameplayStatSection>();
            foreach (JToken sectionToken in sectionsArray)
            {
                JObject section = sectionToken as JObject;
                if (section == null) continue;

                JArray statsArray = section["stats"] as JArray;
                List<PickupGameplayStatEntry> stats = new List<PickupGameplayStatEntry>();
                if (statsArray != null)
                {
                    foreach (JToken statToken in statsArray)
                    {
                        JObject stat = statToken as JObject;
                        if (stat == null) continue;

                        string key = GetString(stat, "key");
                        JArray partsArray = stat["parts"] as JArray;
                        List<PickupGameplayStatPart> parts = new List<PickupGameplayStatPart>();
                        if (partsArray != null)
                        {
                            foreach (JToken partToken in partsArray)
                            {
                                JObject part = partToken as JObject;
                                if (part == null) continue;

                                string value = GetString(part, "value");
                                string label = GetString(part, "label");
                                if (!string.IsNullOrEmpty(value))
                                {
                                    parts.Add(new PickupGameplayStatPart(value, label));
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(key) && parts.Count > 0)
                        {
                            stats.Add(new PickupGameplayStatEntry(key, parts.ToArray()));
                        }
                    }
                }

                if (stats.Count > 0)
                {
                    sections.Add(new PickupGameplayStatSection(GetString(section, "key"), stats.ToArray()));
                }
            }

            return sections.ToArray();
        }

        private static PickupInfoTermsTable ParseTermsTable(JObject root, string languageCode)
        {
            return new PickupInfoTermsTable(
                ParseLocalizedLookupTable(root["sections"] as JObject, languageCode),
                ParseLocalizedLookupTable(root["stats"] as JObject, languageCode),
                ParseLocalizedLookupTable(root["displayValues"] as JObject, languageCode));
        }

        private static Dictionary<string, string> ParseLocalizedLookupTable(JObject table, string languageCode)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (table == null) return values;

            foreach (JProperty property in table.Properties())
            {
                JObject localized = property.Value as JObject;
                string value = GetString(localized, languageCode);
                if (!string.IsNullOrEmpty(value))
                {
                    values[property.Name] = value;
                }
            }

            return values;
        }

        private static string GetLocalizedString(JObject root, string propertyName, string languageCode)
        {
            return GetString(root != null ? root[propertyName] as JObject : null, languageCode);
        }

        private static string[] GetLocalizedStringArray(JObject root, string propertyName, string languageCode)
        {
            JObject localized = root != null ? root[propertyName] as JObject : null;
            JArray valuesArray = localized != null ? localized[languageCode] as JArray : null;
            if (valuesArray == null) return new string[0];

            List<string> values = new List<string>();
            foreach (JToken valueToken in valuesArray)
            {
                string value = valueToken.Type == JTokenType.String ? valueToken.Value<string>().Trim() : string.Empty;
                if (!string.IsNullOrEmpty(value)) values.Add(value);
            }

            return values.ToArray();
        }

        private static string GetString(JObject objectValue, string propertyName)
        {
            return objectValue == null ? string.Empty : GetString(objectValue[propertyName]);
        }

        private static string GetString(JToken value)
        {
            if (value == null || value.Type == JTokenType.Null) return string.Empty;
            return value.Type == JTokenType.String ? value.Value<string>() : value.ToString();
        }

        private static int ParseInt(JToken value, int defaultValue)
        {
            int parsed;
            return int.TryParse(GetString(value), out parsed) ? parsed : defaultValue;
        }

        private static JObject ParseObject(string rawJson)
        {
            JObject root = JObject.Parse(rawJson);
            return root ?? new JObject();
        }
    }
}
