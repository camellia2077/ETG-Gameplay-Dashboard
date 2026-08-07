// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;

namespace EtgGameplayDashboard
{
    /// <summary>
    /// Compares and formats captured replay waves without owning replay orchestration.
    /// </summary>
    internal static class RoomReplayWaveDiagnostics
    {
        public static int CountSnapshotEnemies(RoomEnemyReplaySnapshot snapshot)
        {
            if (snapshot == null || snapshot.Waves == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < snapshot.Waves.Count; index++)
            {
                if (snapshot.Waves[index] != null)
                {
                    count += snapshot.Waves[index].Count;
                }
            }

            return count;
        }

        public static bool WavesMatch(
            List<RoomEnemyReplayEntry> expectedWave,
            List<RoomEnemyReplayEntry> actualWave)
        {
            if (expectedWave == null || actualWave == null || expectedWave.Count != actualWave.Count)
            {
                return false;
            }

            for (int index = 0; index < expectedWave.Count; index++)
            {
                RoomEnemyReplayEntry expected = expectedWave[index];
                RoomEnemyReplayEntry actual = actualWave[index];
                if (!string.Equals(expected.EnemyGuid, actual.EnemyGuid, StringComparison.Ordinal) ||
                    expected.WorldPosition != actual.WorldPosition ||
                    expected.IgnoreForRoomClear != actual.IgnoreForRoomClear)
                {
                    return false;
                }
            }

            return true;
        }

        public static string DescribeWave(List<RoomEnemyReplayEntry> wave)
        {
            if (wave == null || wave.Count == 0)
            {
                return string.Empty;
            }

            List<string> entries = new List<string>();
            for (int index = 0; index < wave.Count; index++)
            {
                RoomEnemyReplayEntry entry = wave[index];
                entries.Add(
                    entry.EnemyGuid +
                    " Spawn@" + entry.SpawnPosition.x + "," + entry.SpawnPosition.y +
                    " World@" + entry.WorldPosition.x + "," + entry.WorldPosition.y +
                    ":IgnoreForRoomClear=" + entry.IgnoreForRoomClear);
            }

            return string.Join(";", entries.ToArray());
        }

        public static bool SnapshotContainsEnemies(RoomEnemyReplaySnapshot snapshot)
        {
            if (snapshot == null || snapshot.Waves == null)
            {
                return false;
            }

            for (int waveIndex = 0; waveIndex < snapshot.Waves.Count; waveIndex++)
            {
                List<RoomEnemyReplayEntry> wave = snapshot.Waves[waveIndex];
                if (wave != null && wave.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
