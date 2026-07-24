// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace EtgGameplayDashboard
{
    internal sealed class BossRushEncounter
    {
        public BossRushEncounter(string floorKey, string loadLevelToken)
        {
            FloorKey = floorKey ?? string.Empty;
            LoadLevelToken = loadLevelToken ?? string.Empty;
            SceneName = EtgFloorSceneResolver.ResolveNormalizedSceneName(LoadLevelToken);
        }

        public string FloorKey { get; private set; }

        public string LoadLevelToken { get; private set; }

        public string SceneName { get; private set; }
    }
}
