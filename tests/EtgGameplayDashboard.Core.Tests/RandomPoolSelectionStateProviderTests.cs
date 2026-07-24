// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.IO;
using EtgGameplayDashboard.Core;

namespace EtgGameplayDashboard.Core.Tests
{
    internal static class RandomPoolSelectionStateProviderTests
    {
        public static void LoadsJson5RandomPoolState()
        {
            string filePath = Path.Combine(Path.GetTempPath(), "EtgGameplayDashboard.random-pool-state.tests." + Guid.NewGuid().ToString("N") + ".json5");
            File.WriteAllText(
                filePath,
                "{\n" +
                "  // State file comment\n" +
                "  \"presets\": [\n" +
                "    {\n" +
                "      \"id\": \"default\",\n" +
                "      \"randomPools\": [\n" +
                "        { \"ruleIndex\": 2, \"poolSignature\": \"541,616\", \"shuffledPickupIds\": [616, 541], \"nextIndex\": 1, },\n" +
                "      ],\n" +
                "    },\n" +
                "  ],\n" +
                "}\n");

            try
            {
                RandomPoolSelectionStateProvider provider = new RandomPoolSelectionStateProvider(filePath);
                RandomPoolSelectionState[] states = provider.Load("default");

                AssertEx.Equal(1, states.Length, "The random pool state file should load one state.");
                AssertEx.Equal(2, states[0].RuleIndex, "The rule index should be preserved.");
                AssertEx.Equal("541,616", states[0].PoolSignature, "The pool signature should be preserved.");
                AssertEx.SequenceEqual(new[] { 616, 541 }, states[0].ShuffledPickupIds, "The shuffled pickup IDs should be preserved.");
                AssertEx.Equal(1, states[0].NextIndex, "The next index should be preserved.");
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        public static void SavesAndReloadsRandomPoolState()
        {
            string filePath = Path.Combine(Path.GetTempPath(), "EtgGameplayDashboard.random-pool-state.tests." + Guid.NewGuid().ToString("N") + ".json5");
            try
            {
                RandomPoolSelectionStateProvider provider = new RandomPoolSelectionStateProvider(filePath);
                provider.Save(
                    "default",
                    new[]
                    {
                        new RandomPoolSelectionState(4, "118", new[] { 118 }, 1),
                    });

                RandomPoolSelectionState[] states = provider.Load("default");
                AssertEx.Equal(1, states.Length, "The saved random pool state should reload.");
                AssertEx.Equal(4, states[0].RuleIndex, "The saved rule index should reload.");
                AssertEx.SequenceEqual(new[] { 118 }, states[0].ShuffledPickupIds, "The saved pickup IDs should reload.");
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }
    }
}
