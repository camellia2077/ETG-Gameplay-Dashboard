// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;

namespace RandomLoadout
{
    internal static class EtgFloorSceneResolver
    {
        private static readonly string RuntimeScenePrefix = new string(new[] { 't', 't', '_' });
        private static readonly string RuntimeSceneRoot = RuntimeScenePrefix.Substring(0, 2);

        private static readonly EtgFloorDefinition[] KnownFloors =
        {
            new EtgFloorDefinition("keep", RuntimeScenePrefix + "castle", "castle", true),
            new EtgFloorDefinition("oubliette", RuntimeScenePrefix + "sewer", "sewer", true),
            new EtgFloorDefinition("proper", RuntimeSceneRoot + "5", "proper", true),
            new EtgFloorDefinition("abbey", RuntimeScenePrefix + "cathedral", "cathedral", true),
            new EtgFloorDefinition("mine", RuntimeScenePrefix + "mines", "mines", true),
            new EtgFloorDefinition("ratden", "ss_resourcefulrat", "resourcefulrat", true),
            new EtgFloorDefinition("hollow", RuntimeScenePrefix + "catacombs", "catacombs", true),
            new EtgFloorDefinition("R&G_Dept", RuntimeScenePrefix + "nakatomi", "nakatomi", true),
            new EtgFloorDefinition("forge", RuntimeScenePrefix + "forge", "forge", true),
            new EtgFloorDefinition("heli", RuntimeScenePrefix + "bullethell", "bullethell", true),
        };

        public static bool TryGetFloor(string commandToken, out EtgFloorDefinition floor)
        {
            string normalizedCommandToken = (commandToken ?? string.Empty).Trim();
            for (int i = 0; i < KnownFloors.Length; i++)
            {
                if (string.Equals(KnownFloors[i].CommandToken, normalizedCommandToken, StringComparison.OrdinalIgnoreCase))
                {
                    floor = KnownFloors[i];
                    return true;
                }
            }

            floor = null;
            return false;
        }

        public static string ResolveLoadSceneName(string commandToken)
        {
            EtgFloorDefinition floor;
            return TryGetFloor(commandToken, out floor) ? floor.LoadSceneName : string.Empty;
        }

        public static string ResolveNormalizedSceneName(string commandToken)
        {
            EtgFloorDefinition floor;
            return TryGetFloor(commandToken, out floor) ? floor.NormalizedSceneName : string.Empty;
        }

        public static string DescribeKnownFloors()
        {
            string[] mappings = new string[KnownFloors.Length];
            for (int i = 0; i < KnownFloors.Length; i++)
            {
                EtgFloorDefinition floor = KnownFloors[i];
                mappings[i] =
                    floor.CommandToken +
                    "->" +
                    floor.LoadSceneName +
                    "/" +
                    floor.NormalizedSceneName +
                    ",Foyer=" +
                    (floor.CanLoadFromFoyer ? "yes" : "no");
            }

            return string.Join(", ", mappings);
        }
    }
}
