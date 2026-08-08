// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace EtgGameplayDashboard
{
    internal static class SkipChargeHooks
    {
        private static SkipChargeToggleService s_service;

        public static void Configure(SkipChargeToggleService service)
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

            MethodInfo attackTarget = AccessTools.Method(typeof(Gun), "Attack");
            MethodInfo continueTarget = AccessTools.Method(typeof(Gun), "ContinueAttack");
            MethodInfo ceaseTarget = AccessTools.Method(typeof(Gun), "CeaseAttack");
            MethodInfo postfix = AccessTools.Method(typeof(SkipChargeHooks), "AttackPostfix");
            MethodInfo prefix = AccessTools.Method(typeof(SkipChargeHooks), "ChargeActionPrefix");
            if (attackTarget == null || continueTarget == null || ceaseTarget == null || postfix == null || prefix == null)
            {
                if (logger != null)
                {
                    logger.LogWarning(EtgGameplayDashboardLog.Init("Skip charge hook skipped: one or more Gun charge methods were not found."));
                }

                return;
            }

            harmony.Patch(attackTarget, postfix: new HarmonyMethod(postfix));
            harmony.Patch(continueTarget, prefix: new HarmonyMethod(prefix));
            harmony.Patch(ceaseTarget, prefix: new HarmonyMethod(prefix));
            if (logger != null)
            {
                logger.LogInfo(EtgGameplayDashboardLog.Init("Skip charge hook ready: Gun.Attack, Gun.ContinueAttack, Gun.CeaseAttack."));
            }
        }

        private static void AttackPostfix(Gun __instance)
        {
            PrepareGun(__instance);
        }

        private static void ChargeActionPrefix(Gun __instance)
        {
            PrepareGun(__instance);
        }

        private static void PrepareGun(Gun gun)
        {
            if (s_service != null)
            {
                s_service.PrepareGunForSkipCharge(gun);
            }
        }
    }
}
