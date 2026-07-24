// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;
using Dungeonator;
using HarmonyLib;

namespace EtgGameplayDashboard
{
    internal static class BossSelectionHooks
    {
        private static ManualLogSource s_logger;
        private static System.Func<bool> s_verboseLoggingEnabledProvider;

        public static void Install(Harmony harmony, ManualLogSource logger, System.Func<bool> verboseLoggingEnabledProvider)
        {
            s_logger = logger;
            s_verboseLoggingEnabledProvider = verboseLoggingEnabledProvider;
            System.Reflection.MethodInfo target = AccessTools.Method(typeof(BossManager), "SelectBossRoom");
            System.Reflection.MethodInfo prefix = AccessTools.Method(typeof(BossSelectionHooks), "SelectBossRoomPrefix");
            System.Reflection.MethodInfo postfix = AccessTools.Method(typeof(BossSelectionHooks), "SelectBossRoomPostfix");
            System.Reflection.MethodInfo clearPerLevelDataTarget = AccessTools.Method(typeof(GameManager), "ClearPerLevelData");
            System.Reflection.MethodInfo clearPerLevelDataPrefix = AccessTools.Method(typeof(BossSelectionHooks), "ClearPerLevelDataPrefix");
            System.Reflection.MethodInfo clearPerLevelDataPostfix = AccessTools.Method(typeof(BossSelectionHooks), "ClearPerLevelDataPostfix");
            if (target == null || prefix == null || postfix == null || clearPerLevelDataTarget == null ||
                clearPerLevelDataPrefix == null || clearPerLevelDataPostfix == null)
            {
                LogWarning("Boss selection diagnostics hook skipped because the vanilla Boss selection members were unavailable.");
                return;
            }

            harmony.Patch(target, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));
            harmony.Patch(
                clearPerLevelDataTarget,
                prefix: new HarmonyMethod(clearPerLevelDataPrefix),
                postfix: new HarmonyMethod(clearPerLevelDataPostfix));
            LogInfo("Boss selection diagnostics hooks ready: BossManager.SelectBossRoom and GameManager.ClearPerLevelData.");
        }

        private static void SelectBossRoomPrefix(out PrototypeDungeonRoom __state)
        {
            __state = BossManager.PriorFloorSelectedBossRoom;
            if (!IsVerboseLoggingEnabled())
            {
                return;
            }

            LogInfo("BossManager.SelectBossRoom prefix. PriorFloorSelectedBossRoom=" + DescribePrototype(__state) + ".");
        }

        private static void SelectBossRoomPostfix(PrototypeDungeonRoom __result, PrototypeDungeonRoom __state)
        {
            if (!IsVerboseLoggingEnabled())
            {
                return;
            }

            GameManager gameManager = GameManager.Instance;
            string generationDungeon = "<unavailable>";
            if ((object)gameManager != null && gameManager.BestGenerationDungeonPrefab != null)
            {
                Dungeon dungeon = gameManager.BestGenerationDungeonPrefab;
                generationDungeon = dungeon.name +
                    (dungeon.tileIndices != null ? ", Tileset=" + dungeon.tileIndices.tilesetId : ", Tileset=<unavailable>");
            }

            LogInfo(
                "BossManager.SelectBossRoom postfix. WasForced=" + (__state != null) +
                ", Requested=" + DescribePrototype(__state) +
                ", Result=" + DescribePrototype(__result) +
                ", CurrentPriorFloorSelectedBossRoom=" + DescribePrototype(BossManager.PriorFloorSelectedBossRoom) +
                ", GenerationDungeon=" + generationDungeon + ".");
        }

        private static void ClearPerLevelDataPrefix(out PrototypeDungeonRoom __state)
        {
            __state = BossManager.PriorFloorSelectedBossRoom;
            if (IsVerboseLoggingEnabled())
            {
                LogInfo("GameManager.ClearPerLevelData prefix. Selection before vanilla clear=" + DescribePrototype(__state) + ".");
            }
        }

        private static void ClearPerLevelDataPostfix(PrototypeDungeonRoom __state)
        {
            if (__state != null)
            {
                BossManager.PriorFloorSelectedBossRoom = __state;
            }

            if (IsVerboseLoggingEnabled())
            {
                LogInfo("GameManager.ClearPerLevelData postfix. Selection restored=" + DescribePrototype(BossManager.PriorFloorSelectedBossRoom) + ".");
            }
        }

        private static bool IsVerboseLoggingEnabled()
        {
            return s_verboseLoggingEnabledProvider != null && s_verboseLoggingEnabledProvider();
        }

        private static string DescribePrototype(PrototypeDungeonRoom prototype)
        {
            if (prototype == null)
            {
                return "<null>";
            }

            return "Name=" + prototype.name +
                   ", GUID=" + prototype.GUID +
                   ", Category=" + prototype.category +
                   ", BossSubcategory=" + prototype.subCategoryBoss;
        }

        private static void LogInfo(string message)
        {
            if (s_logger != null)
            {
                s_logger.LogInfo(EtgGameplayDashboardLog.Command(message));
            }
        }

        private static void LogWarning(string message)
        {
            if (s_logger != null)
            {
                s_logger.LogWarning(EtgGameplayDashboardLog.Command(message));
            }
        }
    }
}
