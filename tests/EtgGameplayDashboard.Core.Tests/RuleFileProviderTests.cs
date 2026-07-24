// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.IO;

namespace EtgGameplayDashboard.Core.Tests
{
    internal static class RuleFileProviderTests
    {
        public static void ParsesSpecificAliasRule()
        {
            string presetDirectory = Path.Combine(Path.GetTempPath(), "EtgGameplayDashboard.presets.tests." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(presetDirectory);
            string presetPath = Path.Combine(presetDirectory, "preset.specific-alias.json");
            File.WriteAllText(
                presetPath,
                "{\n" +
                "  \"id\": \"specific-alias\",\n" +
                "  \"rules\": [\n" +
                "    {\n" +
                "      \"enabled\": true,\n" +
                "      \"mode\": \"specific\",\n" +
                "      \"category\": \"gun\",\n" +
                "      \"alias\": \"casey_bat\"\n" +
                "    }\n" +
                "  ]\n" +
                "}\n");

            try
            {
                JsonLoadoutRuleFileProvider provider = new JsonLoadoutRuleFileProvider(
                    Path.Combine(presetDirectory, "anchor.json"),
                    presetDirectory);
                provider.ActivePresetName = "specific-alias";
                LoadoutRuleFileLoadResult result = provider.Load();

                AssertEx.Equal(1, result.Definitions.Length, "The preset rule should produce one definition.");
                AssertEx.Equal("casey_bat", result.Definitions[0].SpecificAlias, "The specific alias should be preserved.");
            }
            finally
            {
                if (Directory.Exists(presetDirectory))
                {
                    Directory.Delete(presetDirectory, true);
                }
            }
        }

        public static void ParsesRandomPoolAliasesAlongsideIdsAndNames()
        {
            string presetDirectory = Path.Combine(Path.GetTempPath(), "EtgGameplayDashboard.presets.tests." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(presetDirectory);
            string presetPath = Path.Combine(presetDirectory, "preset.random-pool.json");
            File.WriteAllText(
                presetPath,
                "{\n" +
                "  \"id\": \"random-pool\",\n" +
                "  \"rules\": [\n" +
                "    {\n" +
                "      \"enabled\": true,\n" +
                "      \"mode\": \"random\",\n" +
                "      \"category\": \"gun\",\n" +
                "      \"count\": 1,\n" +
                "      \"poolIds\": [541],\n" +
                "      \"poolAliases\": [\"casey_bat\"],\n" +
                "      \"pool\": [\"Casey\"]\n" +
                "    }\n" +
                "  ]\n" +
                "}\n");

            try
            {
                JsonLoadoutRuleFileProvider provider = new JsonLoadoutRuleFileProvider(
                    Path.Combine(presetDirectory, "anchor.json"),
                    presetDirectory);
                provider.ActivePresetName = "random-pool";
                LoadoutRuleFileLoadResult result = provider.Load();

                AssertEx.Equal(1, result.Definitions.Length, "The random rule should be preserved.");
                AssertEx.SequenceEqual(new[] { 541 }, result.Definitions[0].PoolIds, "The random rule should preserve pickup IDs.");
                AssertEx.SequenceEqual(new[] { "casey_bat" }, result.Definitions[0].PoolAliases, "The random rule should preserve pool aliases.");
                AssertEx.SequenceEqual(new[] { "Casey" }, result.Definitions[0].PoolNames, "The random rule should preserve pool names.");
            }
            finally
            {
                if (Directory.Exists(presetDirectory))
                {
                    Directory.Delete(presetDirectory, true);
                }
            }
        }

        public static void ParsesPresetPickupsFromPresetFile()
        {
            string presetDirectory = Path.Combine(Path.GetTempPath(), "EtgGameplayDashboard.presets.tests." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(presetDirectory);
            string presetPath = Path.Combine(presetDirectory, "preset.with-pickups.json");
            File.WriteAllText(
                presetPath,
                "{\n" +
                "  \"id\": \"with-pickups\",\n" +
                "  \"name\": \"With Pickups\",\n" +
                "  \"rules\": [],\n" +
                "  \"pickups\": [\n" +
                "    { \"type\": \"key\", \"count\": 3 },\n" +
                "    { \"type\": \"armor\", \"count\": 2 },\n" +
                "    { \"type\": \"invalid_type\", \"count\": 9 }\n" +
                "  ]\n" +
                "}\n");

            try
            {
                JsonLoadoutRuleFileProvider provider = new JsonLoadoutRuleFileProvider(Path.Combine(presetDirectory, "anchor.json"), presetDirectory);
                provider.ActivePresetName = "with-pickups";
                LoadoutRuleFileLoadResult result = provider.Load();

                AssertEx.Equal(2, result.ActivePresetPickups.Length, "The preset should preserve only supported preset pickups.");
                AssertEx.Equal("key", result.ActivePresetPickups[0].Type, "The first preset pickup should be preserved.");
                AssertEx.Equal(3, result.ActivePresetPickups[0].Count, "The first preset pickup count should be preserved.");
                AssertEx.Equal("armor", result.ActivePresetPickups[1].Type, "The second preset pickup should be preserved.");
                AssertEx.Equal(2, result.ActivePresetPickups[1].Count, "The second preset pickup count should be preserved.");
            }
            finally
            {
                if (File.Exists(presetPath))
                {
                    File.Delete(presetPath);
                }

                if (Directory.Exists(presetDirectory))
                {
                    Directory.Delete(presetDirectory);
                }
            }
        }

        public static void ParsesRuleFromPresetFileWithJson5Syntax()
        {
            string presetDirectory = Path.Combine(Path.GetTempPath(), "EtgGameplayDashboard.presets.tests." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(presetDirectory);
            string presetPath = Path.Combine(presetDirectory, "preset.with-rule.json");
            File.WriteAllText(
                presetPath,
                "{\n" +
                "  // Preset comment\n" +
                "  \"id\": \"with-rule\",\n" +
                "  \"rules\": [\n" +
                "    { \"enabled\": true, \"mode\": \"specific\", \"category\": \"gun\", \"alias\": \"casey_bat\", },\n" +
                "  ],\n" +
                "}\n");

            try
            {
                JsonLoadoutRuleFileProvider provider = new JsonLoadoutRuleFileProvider(
                    Path.Combine(presetDirectory, "anchor.json"),
                    presetDirectory);
                provider.ActivePresetName = "with-rule";
                LoadoutRuleFileLoadResult result = provider.Load();

                AssertEx.Equal(1, result.Definitions.Length, "The preset rule should produce one definition.");
                AssertEx.Equal("casey_bat", result.Definitions[0].SpecificAlias, "The preset rule alias should be preserved.");
            }
            finally
            {
                if (Directory.Exists(presetDirectory))
                {
                    Directory.Delete(presetDirectory, true);
                }
            }
        }

        public static void MergesDuplicatePresetPickupsFromPresetFile()
        {
            string presetDirectory = Path.Combine(Path.GetTempPath(), "EtgGameplayDashboard.presets.tests." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(presetDirectory);
            string presetPath = Path.Combine(presetDirectory, "preset.with-duplicate-pickups.json");
            File.WriteAllText(
                presetPath,
                "{\n" +
                "  \"id\": \"with-duplicate-pickups\",\n" +
                "  \"name\": \"With Duplicate Pickups\",\n" +
                "  \"rules\": [],\n" +
                "  \"pickups\": [\n" +
                "    { \"type\": \"key\", \"count\": 1 },\n" +
                "    { \"type\": \"casings\", \"count\": 1 },\n" +
                "    { \"type\": \"key\", \"count\": 2 },\n" +
                "    { \"type\": \"casings\", \"count\": 3 }\n" +
                "  ]\n" +
                "}\n");

            try
            {
                JsonLoadoutRuleFileProvider provider = new JsonLoadoutRuleFileProvider(Path.Combine(presetDirectory, "anchor.json"), presetDirectory);
                provider.ActivePresetName = "with-duplicate-pickups";
                LoadoutRuleFileLoadResult result = provider.Load();

                AssertEx.Equal(2, result.ActivePresetPickups.Length, "Duplicate preset pickups should be merged by type.");
                AssertEx.Equal("key", result.ActivePresetPickups[0].Type, "Merged key pickup should keep its type.");
                AssertEx.Equal(3, result.ActivePresetPickups[0].Count, "Merged key pickup should add counts together.");
                AssertEx.Equal("casings", result.ActivePresetPickups[1].Type, "Merged casings pickup should keep its type.");
                AssertEx.Equal(4, result.ActivePresetPickups[1].Count, "Merged casings pickup should add bundle counts together.");
            }
            finally
            {
                if (File.Exists(presetPath))
                {
                    File.Delete(presetPath);
                }

                if (Directory.Exists(presetDirectory))
                {
                    Directory.Delete(presetDirectory);
                }
            }
        }

        public static void MissingAliasFileFallsBackToBuiltInDefaults()
        {
            string filePath = Path.Combine(Path.GetTempPath(), "EtgGameplayDashboard.aliases.tests." + Guid.NewGuid().ToString("N") + ".json");
            JsonPickupAliasFileProvider provider = new JsonPickupAliasFileProvider(filePath);
            AliasLoadResult result = provider.Load(delegate { return true; });

            int pickupId;
            AssertEx.True(result.Registry.TryResolve("casey_bat", out pickupId), "The built-in alias registry should include casey_bat.");
            AssertEx.Equal(541, pickupId, "The built-in alias registry should map casey_bat to 541.");
            AssertEx.True(result.Warnings.Length > 0, "Falling back to built-in aliases should produce a warning.");
        }

        public static void LoadsPresetDirectoryWhenAnchorFileIsMissing()
        {
            string missingPrimaryPath = Path.Combine(Path.GetTempPath(), "EtgGameplayDashboard.rules.tests." + Guid.NewGuid().ToString("N") + ".json");
            string presetDirectory = Path.Combine(Path.GetTempPath(), "EtgGameplayDashboard.presets.tests." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(presetDirectory);
            string presetPath = Path.Combine(presetDirectory, "preset.full-pool.json");
            File.WriteAllText(
                presetPath,
                "{\n" +
                "  \"id\": \"full-pool\",\n" +
                "  \"rules\": [\n" +
                "    {\n" +
                "      \"enabled\": true,\n" +
                "      \"mode\": \"random\",\n" +
                "      \"category\": \"gun\",\n" +
                "      \"count\": 1,\n" +
                "      \"poolIds\": [541, 616]\n" +
                "    },\n" +
                "    {\n" +
                "      \"enabled\": true,\n" +
                "      \"mode\": \"random\",\n" +
                "      \"category\": \"passive\",\n" +
                "      \"count\": 1,\n" +
                "      \"poolIds\": [118]\n" +
                "    }\n" +
                "  ]\n" +
                "}\n");

            try
            {
                JsonLoadoutRuleFileProvider provider = new JsonLoadoutRuleFileProvider(missingPrimaryPath, presetDirectory);
                provider.ActivePresetName = "full-pool";
                LoadoutRuleFileLoadResult result = provider.Load();

                AssertEx.Equal(2, result.Definitions.Length, "The preset directory should provide both rules when the anchor file is missing.");
                AssertEx.Equal(GrantMode.Random, result.Definitions[0].Mode, "The preset rule should preserve random mode.");
                AssertEx.SequenceEqual(new[] { 541, 616 }, result.Definitions[0].PoolIds, "The preset rule should preserve the pool IDs.");
                AssertEx.True(result.Messages.Length > 0, "Loading a preset should produce an informational message.");
            }
            finally
            {
                if (Directory.Exists(presetDirectory))
                {
                    Directory.Delete(presetDirectory, true);
                }
            }
        }

        public static void LoadsPresetDirectoryWhenAnchorFileIsInvalid()
        {
            string invalidPrimaryPath = CreateTempFile("{ invalid json");
            string presetDirectory = Path.Combine(Path.GetTempPath(), "EtgGameplayDashboard.presets.tests." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(presetDirectory);
            string presetPath = Path.Combine(presetDirectory, "preset.active-pool.json");
            File.WriteAllText(
                presetPath,
                "{\n" +
                "  \"id\": \"active-pool\",\n" +
                "  \"rules\": [\n" +
                "    {\n" +
                "      \"enabled\": true,\n" +
                "      \"mode\": \"random\",\n" +
                "      \"category\": \"active\",\n" +
                "      \"count\": 1,\n" +
                "      \"poolIds\": [120]\n" +
                "    }\n" +
                "  ]\n" +
                "}\n");

            try
            {
                JsonLoadoutRuleFileProvider provider = new JsonLoadoutRuleFileProvider(invalidPrimaryPath, presetDirectory);
                provider.ActivePresetName = "active-pool";
                LoadoutRuleFileLoadResult result = provider.Load();

                AssertEx.Equal(1, result.Definitions.Length, "The preset directory should provide a rule when the anchor file is invalid.");
                AssertEx.Equal(PickupCategory.Active, result.Definitions[0].Category, "The preset rule category should be preserved.");
                AssertEx.SequenceEqual(new[] { 120 }, result.Definitions[0].PoolIds, "The preset rule pool IDs should be preserved.");
                AssertEx.True(result.Messages.Length > 0, "Loading a preset should produce an informational message.");
            }
            finally
            {
                File.Delete(invalidPrimaryPath);
                if (Directory.Exists(presetDirectory))
                {
                    Directory.Delete(presetDirectory, true);
                }
            }
        }

        private static string CreateTempFile(string content)
        {
            string filePath = Path.Combine(Path.GetTempPath(), "EtgGameplayDashboard.rules.tests." + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(filePath, content);
            return filePath;
        }
    }
}
