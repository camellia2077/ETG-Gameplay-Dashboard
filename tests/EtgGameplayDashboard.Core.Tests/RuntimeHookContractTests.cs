// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.IO;

namespace EtgGameplayDashboard.Core.Tests
{
    internal static class RuntimeHookContractTests
    {
        public static void RoomEnteredHookKeepsVanillaParameterName()
        {
            string sourcePath = Path.Combine(
                Path.Combine("src", "EtgGameplayDashboard"),
                Path.Combine("Runtime", "RoomEnemyReplayHooks.cs"));
            string source = File.ReadAllText(sourcePath);
            const string expectedSignature = "OnEnteredPrefix(RoomHandler __instance, PlayerController p)";

            AssertEx.True(
                source.IndexOf(expectedSignature, System.StringComparison.Ordinal) >= 0,
                "RoomHandler.OnEntered Harmony prefix must keep the vanilla parameter name 'p'.");
        }
    }
}
