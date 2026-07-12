// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace RandomLoadout
{
    internal static class PlayerHealthOverrideHooks
    {
        private static PlayerHealthOverrideService s_service;
        private static readonly HashSet<int> GunChangePlayers = new HashSet<int>();
        private static bool s_rebuildingArmorForHeart;

        public static void Configure(PlayerHealthOverrideService service)
        {
            s_service = service;
            GunChangePlayers.Clear();
            s_rebuildingArmorForHeart = false;
        }

        public static void ClearConfiguration()
        {
            s_service = null;
            GunChangePlayers.Clear();
            s_rebuildingArmorForHeart = false;
        }

        public static void Install(Harmony harmony, ManualLogSource logger)
        {
            if (harmony == null)
            {
                return;
            }

            const string hookLabel = "PlayerController.OnGunChanged -> HealthHaver.SetHealthMaximum";
            MethodInfo gunChangedMethod = AccessTools.Method(
                typeof(PlayerController),
                "OnGunChanged",
                new[] { typeof(Gun), typeof(Gun), typeof(Gun), typeof(Gun), typeof(bool) });
            MethodInfo gunChangedPrefix = AccessTools.Method(typeof(PlayerHealthOverrideHooks), "OnGunChangedPrefix");
            MethodInfo gunChangedPostfix = AccessTools.Method(typeof(PlayerHealthOverrideHooks), "OnGunChangedPostfix");
            MethodInfo setHealthMaximumMethod = AccessTools.Method(
                typeof(HealthHaver),
                "SetHealthMaximum",
                new[] { typeof(float), typeof(float?), typeof(bool) });
            MethodInfo setHealthMaximumPrefix = AccessTools.Method(typeof(PlayerHealthOverrideHooks), "SetHealthMaximumPrefix");
            MethodInfo addHeartMethod = AccessTools.Method(typeof(GameUIHeartController), "AddHeart");
            MethodInfo addHeartPrefix = AccessTools.Method(typeof(PlayerHealthOverrideHooks), "AddHeartPrefix");
            MethodInfo addHeartPostfix = AccessTools.Method(typeof(PlayerHealthOverrideHooks), "AddHeartPostfix");
            MethodInfo removeArmorMethod = AccessTools.Method(typeof(GameUIHeartController), "RemoveArmor");
            MethodInfo removeArmorPrefix = AccessTools.Method(typeof(PlayerHealthOverrideHooks), "RemoveArmorPrefix");
            MethodInfo removeArmorPostfix = AccessTools.Method(typeof(PlayerHealthOverrideHooks), "RemoveArmorPostfix");

            if (gunChangedMethod == null || gunChangedPrefix == null || gunChangedPostfix == null ||
                setHealthMaximumMethod == null || setHealthMaximumPrefix == null ||
                addHeartMethod == null || addHeartPrefix == null || addHeartPostfix == null ||
                removeArmorMethod == null || removeArmorPrefix == null || removeArmorPostfix == null)
            {
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Init("Player health override hook skipped: " + hookLabel + ". Target or patch method was not found."));
                }

                return;
            }

            try
            {
                harmony.Patch(
                    gunChangedMethod,
                    prefix: new HarmonyMethod(gunChangedPrefix),
                    postfix: new HarmonyMethod(gunChangedPostfix));
                harmony.Patch(
                    setHealthMaximumMethod,
                    prefix: new HarmonyMethod(setHealthMaximumPrefix));
                harmony.Patch(
                    addHeartMethod,
                    prefix: new HarmonyMethod(addHeartPrefix),
                    postfix: new HarmonyMethod(addHeartPostfix));
                harmony.Patch(
                    removeArmorMethod,
                    prefix: new HarmonyMethod(removeArmorPrefix),
                    postfix: new HarmonyMethod(removeArmorPostfix));
                if (logger != null)
                {
                    logger.LogInfo(RandomLoadoutLog.Init("Player health override hook ready: " + hookLabel));
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Init("Player health override hook failed: " + hookLabel + ". " + ex.GetType().Name + ": " + ex.Message));
                }
            }
        }

        private static void OnGunChangedPrefix(PlayerController __instance)
        {
            if (__instance != null && s_service != null)
            {
                // ETG recalculates stats inside OnGunChanged. That recalculation can call
                // HealthHaver.SetHealthMaximum with the character's base value, briefly changing
                // an intentionally extended 6-heart player back to 3 hearts before the normal
                // GunChanged event reaches our service. Mark the scope early so the SetHealthMaximum
                // prefix can preserve the extended value before HealthHaver raises OnHealthChanged.
                GunChangePlayers.Add(__instance.GetInstanceID());
            }
        }

        private static void OnGunChangedPostfix(PlayerController __instance)
        {
            if (__instance != null)
            {
                GunChangePlayers.Remove(__instance.GetInstanceID());
            }
        }

        private static void SetHealthMaximumPrefix(
            HealthHaver __instance,
            ref float targetValue,
            ref float? amountOfHealthToGain)
        {
            if (__instance == null || s_service == null)
            {
                return;
            }

            PlayerController player = FindTrackedPlayer(__instance);
            if (player == null || !GunChangePlayers.Contains(player.GetInstanceID()))
            {
                return;
            }

            s_service.TryPreserveMaxHealthDuringGunChange(__instance, ref targetValue, ref amountOfHealthToGain);
        }

        private static void AddHeartPrefix(GameUIHeartController __instance)
        {
            // GameUIHeartController.AddHeart clears and recreates every Armor sprite so the new
            // heart can be positioned. Its internal RemoveArmor path also spawns the Armor-loss
            // animation, making Armor appear to flash whenever max health increases. Suppress only
            // that animation during this UI reflow; real gameplay Armor loss remains unchanged.
            s_rebuildingArmorForHeart = __instance != null &&
                __instance.extantArmors != null &&
                __instance.extantArmors.Count > 0;
        }

        private static void AddHeartPostfix()
        {
            s_rebuildingArmorForHeart = false;
        }

        private static void RemoveArmorPrefix(GameUIHeartController __instance, ref dfSprite __state)
        {
            __state = null;
            if (!s_rebuildingArmorForHeart || __instance == null)
            {
                return;
            }

            __state = __instance.damagedArmorAnimationPrefab;
            __instance.damagedArmorAnimationPrefab = null;
        }

        private static void RemoveArmorPostfix(GameUIHeartController __instance, ref dfSprite __state)
        {
            if (__instance != null && __state != null)
            {
                __instance.damagedArmorAnimationPrefab = __state;
            }
        }

        private static PlayerController FindTrackedPlayer(HealthHaver healthHaver)
        {
            PlayerController primaryPlayer = GameManager.Instance != null ? GameManager.Instance.PrimaryPlayer : null;
            if (primaryPlayer != null && ReferenceEquals(primaryPlayer.healthHaver, healthHaver))
            {
                return primaryPlayer;
            }

            PlayerController secondaryPlayer = GameManager.Instance != null ? GameManager.Instance.SecondaryPlayer : null;
            if (secondaryPlayer != null && ReferenceEquals(secondaryPlayer.healthHaver, healthHaver))
            {
                return secondaryPlayer;
            }

            return null;
        }
    }
}
