// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.IO;

namespace EtgGameplayDashboard.Core.Tests
{
    internal static class AliasRegistryTests
    {
        public static void AliasLookupIsCaseInsensitive()
        {
            string[] warnings = new string[0];
            PickupAliasRegistry registry = PickupAliasRegistry.Create(
                new[]
                {
                    new AliasEntryModel { Alias = "casey_bat", Id = 541 },
                },
                warnings,
                delegate(int candidatePickupId) { return candidatePickupId == 541; });

            int pickupId;
            AssertEx.True(registry.TryResolve("CASEY_BAT", out pickupId), "The alias registry should resolve aliases case-insensitively.");
            AssertEx.Equal(541, pickupId, "The alias registry should return the configured pickup ID.");
        }

        public static void DuplicateAliasKeepsFirstDefinition()
        {
            System.Collections.Generic.List<string> warnings = new System.Collections.Generic.List<string>();
            PickupAliasRegistry registry = PickupAliasRegistry.Create(
                new[]
                {
                    new AliasEntryModel { Alias = "casey_bat", Id = 541 },
                    new AliasEntryModel { Alias = "casey_bat", Id = 616 },
                },
                warnings,
                delegate { return true; });

            int pickupId;
            AssertEx.True(registry.TryResolve("casey_bat", out pickupId), "The first duplicate alias should remain available.");
            AssertEx.Equal(541, pickupId, "The first alias definition should win.");
            AssertEx.True(warnings.Count == 1, "A duplicate alias should produce one warning.");
        }

        public static void NumericAliasIsRejected()
        {
            System.Collections.Generic.List<string> warnings = new System.Collections.Generic.List<string>();
            PickupAliasRegistry registry = PickupAliasRegistry.Create(
                new[]
                {
                    new AliasEntryModel { Alias = "541", Id = 541 },
                },
                warnings,
                delegate { return true; });

            int pickupId;
            AssertEx.True(!registry.TryResolve("541", out pickupId), "Pure numeric aliases should be rejected.");
            AssertEx.True(warnings.Count == 1, "Rejecting a numeric alias should produce one warning.");
        }

        public static void UnsupportedPickupIdIsRejected()
        {
            System.Collections.Generic.List<string> warnings = new System.Collections.Generic.List<string>();
            PickupAliasRegistry registry = PickupAliasRegistry.Create(
                new[]
                {
                    new AliasEntryModel { Alias = "bad_alias", Id = 9999 },
                },
                warnings,
                delegate(int candidatePickupId) { return candidatePickupId == 541; });

            int pickupId;
            AssertEx.True(!registry.TryResolve("bad_alias", out pickupId), "Aliases with unsupported pickup IDs should be rejected.");
            AssertEx.True(warnings.Count == 1, "Rejecting an unsupported pickup ID should produce one warning.");
        }

        public static void LoadsJson5AliasesWithCommentsAndTrailingCommas()
        {
            string path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(
                    path,
                    "{\n" +
                    "  // Alias comment\n" +
                    "  \"aliases\": [\n" +
                    "    { \"alias\": \"casey_bat\", \"id\": 541 },\n" +
                    "  ],\n" +
                    "}");

                AliasLoadResult result = new JsonPickupAliasFileProvider(path).Load(
                    delegate { return true; });
                int pickupId;
                AssertEx.True(result.Registry.TryResolve("casey_bat", out pickupId), "The JSON5 alias should be loaded.");
                AssertEx.Equal(541, pickupId, "The JSON5 alias should preserve its pickup ID.");
                AssertEx.Equal(0, result.Warnings.Length, "A valid JSON5 alias file should not produce warnings: " + string.Join(" | ", result.Warnings));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
