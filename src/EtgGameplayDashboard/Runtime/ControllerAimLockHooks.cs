// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal static class ControllerAimLockHooks
    {
        private static ControllerAimLockService s_service;
        private static Func<bool> s_isVerboseLoggingEnabled;
        private static ManualLogSource s_logger;
        private static int s_lastLoggedFrame = -1;
        private static int s_lastCameraAimLoggedFrame = -1;

        public static void Configure(ControllerAimLockService service, Func<bool> isVerboseLoggingEnabled, ManualLogSource logger)
        {
            s_service = service;
            s_isVerboseLoggingEnabled = isVerboseLoggingEnabled;
            s_logger = logger;
            s_lastLoggedFrame = -1;
            s_lastCameraAimLoggedFrame = -1;
        }

        public static void ClearConfiguration()
        {
            if (s_service != null)
            {
                s_service.Reset();
            }

            s_service = null;
            s_isVerboseLoggingEnabled = null;
            s_logger = null;
            s_lastLoggedFrame = -1;
            s_lastCameraAimLoggedFrame = -1;
        }

        public static void Install(Harmony harmony, ManualLogSource logger)
        {
            if (harmony == null)
            {
                return;
            }

            MethodInfo target = AccessTools.Method(typeof(PlayerController), "DetermineAimPointInWorld");
            MethodInfo postfix = AccessTools.Method(typeof(ControllerAimLockHooks), "DetermineAimPointInWorldPostfix");
            MethodInfo cameraOffsetTarget = AccessTools.Method(
                typeof(CameraController),
                "GetCoreOffset",
                new[] { typeof(Vector2), typeof(bool), typeof(bool) });
            MethodInfo cameraOffsetPrefix = AccessTools.Method(typeof(ControllerAimLockHooks), "GetCoreOffsetPrefix");
            MethodInfo cameraOffsetPostfix = AccessTools.Method(typeof(ControllerAimLockHooks), "GetCoreOffsetPostfix");
            if (target == null || postfix == null ||
                cameraOffsetTarget == null || cameraOffsetPrefix == null || cameraOffsetPostfix == null)
            {
                if (logger != null)
                {
                    logger.LogWarning(EtgGameplayDashboardLog.Init("Controller aim lock hook skipped: PlayerController.DetermineAimPointInWorld was not found."));
                }

                return;
            }

            try
            {
                harmony.Patch(target, postfix: new HarmonyMethod(postfix));
                harmony.Patch(
                    cameraOffsetTarget,
                    prefix: new HarmonyMethod(cameraOffsetPrefix),
                    postfix: new HarmonyMethod(cameraOffsetPostfix));
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.LogWarning(EtgGameplayDashboardLog.Init("Controller aim lock hook failed: " + ex.GetType().Name + ": " + ex.Message));
                }
            }
        }

        private static void GetCoreOffsetPrefix(CameraController __instance, ref bool __state)
        {
            // ETG computes the camera aim offset directly from ActiveActions.Aim.Vector in
            // CameraController.GetCoreOffset(). It does not use PlayerController's final aim
            // point, so locking DetermineAimPointInWorld alone cannot freeze the screen view.
            // Temporarily setting PreventAimLook leaves the aim vector and gun rotation intact.
            __state = false;
            bool lockActive = s_service != null && s_service.IsControllerCameraAimLockActive();
            BraveInput input = BraveInput.GetInstanceForPlayer(0);
            bool preventAimLookBefore = __instance != null && __instance.PreventAimLook;
            if (s_service == null || __instance == null || !lockActive)
            {
                LogCameraAimSample(__instance, input, preventAimLookBefore, preventAimLookBefore, lockActive);
                return;
            }

            if (!__instance.PreventAimLook)
            {
                __instance.PreventAimLook = true;
                __state = true;
            }

            LogCameraAimSample(__instance, input, preventAimLookBefore, __instance.PreventAimLook, lockActive);
        }

        private static void GetCoreOffsetPostfix(CameraController __instance, bool __state)
        {
            if (__state && __instance != null)
            {
                __instance.PreventAimLook = false;
            }
        }

        private static void DetermineAimPointInWorldPostfix(PlayerController __instance, ref Vector3 __result)
        {
            LogAimSample(__instance, __result);
        }

        private static void LogAimSample(PlayerController player, Vector3 rawAimPoint)
        {
            if (player == null || s_logger == null ||
                s_isVerboseLoggingEnabled == null || !s_isVerboseLoggingEnabled() ||
                Time.frameCount == s_lastLoggedFrame || Time.frameCount % 10 != 0)
            {
                return;
            }

            s_lastLoggedFrame = Time.frameCount;
            BraveInput input = BraveInput.GetInstanceForPlayer(player.PlayerIDX);
            bool isKeyboardAndMouse = input != null && input.IsKeyboardAndMouse();
            Vector2 center = player.CenterPosition;
            Vector2 rawOffset = new Vector2(rawAimPoint.x - center.x, rawAimPoint.y - center.y);
            Vector2 aimVector = Vector2.zero;
            if (input != null && input.ActiveActions != null && input.ActiveActions.Aim != null)
            {
                aimVector = input.ActiveActions.Aim.Vector;
            }

            s_logger.LogInfo(EtgGameplayDashboardLog.Aim(
                "Sample Frame=" + Time.frameCount +
                ", Player=" + player.PlayerIDX +
                ", DeviceMode=" + (isKeyboardAndMouse ? "KeyboardMouse" : "ControllerOrOther") +
                ", Center=" + FormatVector(center) +
                ", RawAimPoint=" + FormatVector(rawAimPoint) +
                ", RawAimOffset=" + FormatVector(rawOffset) +
                ", RawAimDistance=" + rawOffset.magnitude.ToString("F3") +
                ", AimVector=" + FormatVector(aimVector) +
                ", UnadjustedAimPoint=" + FormatVector(player.unadjustedAimPoint) +
                ", Locked=" + (s_service != null && s_service.IsEnabled(player)) +
                ", AimPointOverrideApplied=False" +
                "."));
        }

        private static void LogCameraAimSample(
            CameraController camera,
            BraveInput input,
            bool preventAimLookBefore,
            bool preventAimLookAfter,
            bool lockActive)
        {
            if (s_logger == null ||
                s_isVerboseLoggingEnabled == null || !s_isVerboseLoggingEnabled() ||
                Time.frameCount == s_lastCameraAimLoggedFrame || Time.frameCount % 10 != 0)
            {
                return;
            }

            s_lastCameraAimLoggedFrame = Time.frameCount;
            Vector2 aimVector = Vector2.zero;
            if (input != null && input.ActiveActions != null && input.ActiveActions.Aim != null)
            {
                aimVector = input.ActiveActions.Aim.Vector;
            }

            s_logger.LogInfo(EtgGameplayDashboardLog.CameraAim(
                "Sample Frame=" + Time.frameCount +
                ", LockActive=" + lockActive +
                ", AimVector=" + FormatVector(aimVector) +
                ", PreventAimLookBefore=" + preventAimLookBefore +
                ", PreventAimLookAfter=" + preventAimLookAfter +
                ", Camera=" + (camera != null ? camera.GetInstanceID().ToString() : "<null>") +
                "."));
        }

        private static bool IsControllerAimLockActive()
        {
            return s_service != null && s_service.IsControllerCameraAimLockActive();
        }

        private static string FormatVector(Vector2 value)
        {
            return "(" + value.x.ToString("F3") + "," + value.y.ToString("F3") + ")";
        }

        private static string FormatVector(Vector3 value)
        {
            return "(" + value.x.ToString("F3") + "," + value.y.ToString("F3") + "," + value.z.ToString("F3") + ")";
        }
    }
}
