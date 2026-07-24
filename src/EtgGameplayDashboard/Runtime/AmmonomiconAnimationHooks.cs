// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal static class AmmonomiconAnimationHooks
    {
        private static readonly MethodInfo SetFrameMethod = typeof(AmmonomiconController).GetMethod(
            "SetFrame",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo AmmonomiconInstanceField = typeof(AmmonomiconController).GetField(
            "m_AmmonomiconInstance",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo IsPageTransitioningField = typeof(AmmonomiconController).GetField(
            "m_isPageTransitioning",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo HandleQueuedUnlocksMethod = typeof(AmmonomiconController).GetMethod(
            "HandleQueuedUnlocks",
            BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Install(Harmony harmony, ManualLogSource logger)
        {
            if (harmony == null)
            {
                return;
            }

            const string hookLabel = "AmmonomiconController.HandleOpenAmmonomicon -> HandleOpenAmmonomiconPrefix";
            MethodInfo targetMethod = AccessTools.Method(
                typeof(AmmonomiconController),
                "HandleOpenAmmonomicon",
                new[] { typeof(bool), typeof(bool), typeof(EncounterTrackable) });
            MethodInfo patchMethod = AccessTools.Method(typeof(AmmonomiconAnimationHooks), "HandleOpenAmmonomiconPrefix");

            if (targetMethod == null || patchMethod == null)
            {
                if (logger != null)
                {
                    logger.LogWarning(EtgGameplayDashboardLog.Init("Ammonomicon animation hook skipped: " + hookLabel + ". Target or patch method was not found."));
                }

                return;
            }

            try
            {
                harmony.Patch(targetMethod, prefix: new HarmonyMethod(patchMethod));
                if (logger != null)
                {
                    logger.LogInfo(EtgGameplayDashboardLog.Init("Ammonomicon animation hook ready: " + hookLabel));
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.LogWarning(EtgGameplayDashboardLog.Init("Ammonomicon animation hook failed: " + hookLabel + ". " + ex.GetType().Name + ": " + ex.Message));
                }
            }
        }

        private static bool HandleOpenAmmonomiconPrefix(
            AmmonomiconController __instance,
            bool isDeath,
            EncounterTrackable targetTrackable,
            ref IEnumerator __result)
        {
            if (!AmmonomiconFastOpenToggleService.IsFastOpenEnabled || isDeath)
            {
                return true;
            }

            if (!CanSkipOpenAnimation(__instance))
            {
                return true;
            }

            __result = HandleOpenAmmonomiconWithoutAnimation(__instance, targetTrackable);
            return false;
        }

        private static bool CanSkipOpenAnimation(AmmonomiconController controller)
        {
            return (object)controller != null &&
                   controller.OpenAnimationFrames != null &&
                   controller.OpenAnimationFrames.Count > 0 &&
                   SetFrameMethod != null &&
                   AmmonomiconInstanceField != null &&
                   IsPageTransitioningField != null &&
                   HandleQueuedUnlocksMethod != null;
        }

        private static IEnumerator HandleOpenAmmonomiconWithoutAnimation(
            AmmonomiconController controller,
            EncounterTrackable targetTrackable)
        {
            List<AmmonomiconFrameDefinition> frames = controller.OpenAnimationFrames;
            SetFrameMethod.Invoke(controller, new object[] { frames[frames.Count - 1] });

            AmmonomiconInstanceManager ammonomiconInstance = AmmonomiconInstanceField.GetValue(controller) as AmmonomiconInstanceManager;
            if (ammonomiconInstance != null)
            {
                ammonomiconInstance.Open();
            }

            if (targetTrackable != null && controller.CurrentLeftPageRenderer != null)
            {
                AmmonomiconPokedexEntry pokedexEntry = controller.CurrentLeftPageRenderer.GetPokedexEntry(targetTrackable);
                if (pokedexEntry != null)
                {
                    Debug.Log("GET INFO SUCCESS");
                    pokedexEntry.ForceFocus();
                }
            }

            IsPageTransitioningField.SetValue(controller, false);
            HandleQueuedUnlocksMethod.Invoke(controller, new object[0]);
            yield break;
        }
    }
}
