// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace RandomLoadout
{
    internal sealed class JsonPickupAliasFileProvider
    {
        private readonly string _filePath;

        public JsonPickupAliasFileProvider(string filePath)
        {
            _filePath = filePath;
        }

        public AliasLoadResult Load(Func<int, bool> isSupportedPickupId)
        {
            List<string> messages = new List<string>();
            List<string> warnings = new List<string>();
            AliasFileModel fileModel = null;
            bool usedBuiltInDefault = false;

            if (!File.Exists(_filePath))
            {
                warnings.Add(
                    "Pickup alias file was not found at '" + _filePath + "'. " +
                    "This build now expects the repository default config to be deployed into BepInEx\\config. " +
                    "Run deploy_mod.py again, or copy the repository default ETG-Gameplay-Dashboard.aliases.json5 into the game config directory. " +
                    "Falling back to built-in default aliases for this session.");
                usedBuiltInDefault = true;
            }
            else
            {
                try
                {
                    string rawJson = Json5TextNormalizer.Normalize(File.ReadAllText(_filePath, Encoding.UTF8));
                    fileModel = ParseAliasFile(rawJson);
                }
                catch (Exception exception)
                {
                    warnings.Add(
                        "Failed to parse pickup alias file '" + _filePath + "'. Falling back to built-in default aliases. " +
                        exception.Message);
                    usedBuiltInDefault = true;
                }
            }

            if (fileModel == null)
            {
                fileModel = CreateDefaultModel();
            }

            PickupAliasRegistry registry = PickupAliasRegistry.Create(fileModel.Aliases, warnings, isSupportedPickupId);
            if (usedBuiltInDefault)
            {
                messages.Add("Using built-in pickup alias registry (" + registry.Count + " aliases).");
            }
            else
            {
                messages.Add("Loaded pickup alias registry from '" + _filePath + "' (" + registry.Count + " aliases).");
            }

            return new AliasLoadResult(registry, messages.ToArray(), warnings.ToArray());
        }

        private static AliasFileModel ParseAliasFile(string rawJson)
        {
            if (string.IsNullOrEmpty(rawJson))
            {
                return new AliasFileModel();
            }

            JObject root = JObject.Parse(rawJson);
            JArray aliasesArray = root["aliases"] as JArray;
            List<AliasEntryModel> aliases = new List<AliasEntryModel>();
            if (aliasesArray == null)
            {
                return new AliasFileModel { Aliases = aliases.ToArray() };
            }

            foreach (JToken aliasToken in aliasesArray)
            {
                JObject aliasObject = aliasToken as JObject;
                if (aliasObject == null)
                {
                    continue;
                }

                aliases.Add(
                    new AliasEntryModel
                    {
                        Alias = GetString(aliasObject["alias"]),
                        Id = GetInt(aliasObject["id"], 0),
                    });
            }

            return new AliasFileModel { Aliases = aliases.ToArray() };
        }

        private static AliasFileModel CreateDefaultModel()
        {
            return new AliasFileModel
            {
                Aliases = new[]
                {
                    new AliasEntryModel { Alias = "casey_bat", Id = 541 },
                    new AliasEntryModel { Alias = "casey_nail", Id = 616 },
                    new AliasEntryModel { Alias = "eyepatch", Id = 118 },
                },
            };
        }

        private static string GetString(JToken value)
        {
            if (value == null || value.Type == JTokenType.Null)
            {
                return string.Empty;
            }

            return value.Type == JTokenType.String ? value.Value<string>() : value.ToString();
        }

        private static int GetInt(JToken value, int defaultValue)
        {
            int parsed;
            return int.TryParse(GetString(value), out parsed) ? parsed : defaultValue;
        }
    }
}
