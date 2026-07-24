// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using Dungeonator;
using HarmonyLib;

namespace EtgGameplayDashboard
{
    internal static class RoomEnemyReplayHooks
    {
        private static RoomEnemyReplayService _service;

        public static void Configure(RoomEnemyReplayService service)
        {
            _service = service;
        }

        public static void Install(Harmony harmony, ManualLogSource logger)
        {
            InstallPrefix(harmony, typeof(RoomHandler), "OnEntered", new[] { typeof(PlayerController) }, "OnEnteredPrefix", logger);
            InstallPostfix(harmony, typeof(RoomHandler), "HandleBossClearReward", Type.EmptyTypes, "HandleBossClearRewardPostfix", logger);
            InstallPostfix(harmony, typeof(RoomHandler), "HandleRoomClearReward", Type.EmptyTypes, "HandleRoomClearRewardPostfix", logger);
            InstallPostfix(harmony, typeof(HealthHaver), "Die", new[] { typeof(UnityEngine.Vector2) }, "HealthHaverDiePostfix", logger);
            InstallPostfix(harmony, typeof(MinimapUIController), "AttemptTeleport", Type.EmptyTypes, "AttemptTeleportPostfix", logger);
            InstallPrefix(harmony, typeof(RoomHandler), "TriggerReinforcementLayer", new[] { typeof(int), typeof(bool), typeof(bool), typeof(int), typeof(int), typeof(bool) }, "TriggerReinforcementLayerPrefix", logger);
            InstallPostfix(harmony, typeof(RoomHandler), "TriggerReinforcementLayer", new[] { typeof(int), typeof(bool), typeof(bool), typeof(int), typeof(int), typeof(bool) }, "TriggerReinforcementLayerPostfix", logger);
            InstallPrefix(harmony, typeof(RoomHandler), "DeregisterEnemy", new[] { typeof(AIActor), typeof(bool) }, "DeregisterEnemyPrefix", logger);
            InstallPostfix(harmony, typeof(RoomHandler), "DeregisterEnemy", new[] { typeof(AIActor), typeof(bool) }, "DeregisterEnemyPostfix", logger);
        }

        private static void InstallPrefix(Harmony harmony, Type type, string targetName, Type[] args, string patchName, ManualLogSource logger)
        {
            Install(harmony, type, targetName, args, patchName, true, logger);
        }

        private static void InstallPostfix(Harmony harmony, Type type, string targetName, Type[] args, string patchName, ManualLogSource logger)
        {
            Install(harmony, type, targetName, args, patchName, false, logger);
        }

        private static void Install(Harmony harmony, Type type, string targetName, Type[] args, string patchName, bool prefix, ManualLogSource logger)
        {
            MethodInfo target = AccessTools.Method(type, targetName, args);
            MethodInfo patch = AccessTools.Method(typeof(RoomEnemyReplayHooks), patchName);
            if (target == null || patch == null)
            {
                if (logger != null)
                {
                    logger.LogWarning(EtgGameplayDashboardLog.Init("Room enemy replay hook skipped: " + type.Name + "." + targetName + "."));
                }

                return;
            }

            harmony.Patch(target, prefix ? new HarmonyMethod(patch) : null, prefix ? null : new HarmonyMethod(patch));
        }

        // IMPORTANT: RoomHandler.OnEntered's real ETG parameter is named "p".
        // Harmony binds ordinary patch arguments by name, so changing this to
        // "player" breaks startup even though the type remains PlayerController.
        // Keep this name aligned with the decompiled Assembly-CSharp signature.
        private static void OnEnteredPrefix(RoomHandler __instance, PlayerController p)
        {
            if (_service != null)
            {
                // OnEntered is the first point at which the room's vanilla-selected initial
                // enemies are available. Capture that result rather than rerolling its template.
                _service.RecordInitialWave(__instance, p);
            }
        }

        private static void HandleBossClearRewardPostfix(RoomHandler __instance)
        {
            if (_service != null)
            {
                _service.NotifyBossClearRewardHandled(__instance);
            }
        }

        private static void HandleRoomClearRewardPostfix(RoomHandler __instance)
        {
            if (_service != null)
            {
                _service.NotifyRoomClearRewardHandled(__instance);
            }
        }

        private static void HealthHaverDiePostfix(HealthHaver __instance)
        {
            if (_service != null)
            {
                _service.NotifyBossDeathStarted(__instance);
            }
        }

        private static void AttemptTeleportPostfix(MinimapUIController __instance, bool __result)
        {
            if (_service != null)
            {
                _service.NotifyMinimapTeleportAttempted(__instance, __result);
            }
        }

        private static void TriggerReinforcementLayerPrefix(RoomHandler __instance, out List<AIActor> __state)
        {
            // The reinforcement method does not expose the spawned actors. Preserve the
            // pre-call set and let the postfix store only the actors vanilla added.
            __state = _service != null ? _service.BeginReinforcementCapture(__instance) : null;
        }

        private static void TriggerReinforcementLayerPostfix(RoomHandler __instance, List<AIActor> __state)
        {
            if (_service != null)
            {
                _service.CompleteReinforcementCapture(__instance, __state);
            }
        }

        private static void DeregisterEnemyPrefix(RoomHandler __instance, AIActor enemy)
        {
            if (_service != null)
            {
                // Insert the next replay wave before vanilla removes the final room-clear
                // enemy; otherwise ETG clears the room and opens doors between waves.
                _service.TrySpawnNextWaveBeforeClear(__instance, enemy);
            }
        }

        private static void DeregisterEnemyPostfix(RoomHandler __instance, AIActor enemy)
        {
            if (_service != null)
            {
                _service.NotifyEnemyDeregistered(__instance, enemy);
            }
        }
    }
}
