// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RandomLoadout
{
    internal static class KeyboardAimAssistHooks
    {
        private static KeyboardAimAssistService s_service;

        public static void Configure(KeyboardAimAssistService service)
        {
            s_service = service;
        }

        public static void ClearConfiguration()
        {
            if (s_service != null)
            {
                s_service.Reset();
            }

            s_service = null;
        }

        public static void Install(Harmony harmony, ManualLogSource logger)
        {
            if (harmony == null)
            {
                return;
            }

            MethodInfo target = AccessTools.Method(typeof(PlayerController), "DetermineAimPointInWorld");
            MethodInfo postfix = AccessTools.Method(typeof(KeyboardAimAssistHooks), "DetermineAimPointInWorldPostfix");
            if (target == null || postfix == null)
            {
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Init("Keyboard aim assist hook skipped: PlayerController.DetermineAimPointInWorld was not found."));
                }

                return;
            }

            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Init("Keyboard aim assist hook ready: PlayerController.DetermineAimPointInWorld."));
            }
        }

        private static void DetermineAimPointInWorldPostfix(PlayerController __instance, ref Vector3 __result)
        {
            if (s_service == null)
            {
                return;
            }

            s_service.TryApplyAssist(__instance, ref __result);
        }
    }
}
