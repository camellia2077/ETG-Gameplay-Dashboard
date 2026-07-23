// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RandomLoadout
{
    internal static class BossAudioDiagnosticsHooks
    {
        private static ManualLogSource _logger;

        public static void Install(Harmony harmony, ManualLogSource logger)
        {
            _logger = logger;
            InstallPostfix(harmony, typeof(AkSoundEngine), "PostEvent", new[] { typeof(string), typeof(GameObject) }, "PostEventStringPostfix", logger);
            InstallPrefix(harmony, typeof(TankTreaderIntroDoer), "StartIntro", new[] { typeof(List<tk2dSpriteAnimator>) }, "TankTreaderIntroStartPrefix", logger);
            InstallPostfix(harmony, typeof(TankTreaderIntroDoer), "OnCleanup", Type.EmptyTypes, "TankTreaderIntroCleanupPostfix", logger);
            InstallPostfix(harmony, typeof(TankTreaderDeathController), "Start", Type.EmptyTypes, "TankTreaderDeathStartPostfix", logger);
            InstallPrefix(harmony, typeof(TankTreaderDeathController), "OnBossDeath", new[] { typeof(Vector2) }, "TankTreaderDeathPrefix", logger);
            InstallPostfix(harmony, typeof(TankTreaderController), "OnDestroy", Type.EmptyTypes, "TankTreaderDestroyPostfix", logger);
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
            MethodInfo patch = AccessTools.Method(typeof(BossAudioDiagnosticsHooks), patchName);
            if (target == null || patch == null)
            {
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Init("Boss audio diagnostics hook skipped: " + type.Name + "." + targetName + "."));
                }

                return;
            }

            harmony.Patch(target, prefix ? new HarmonyMethod(patch) : null, prefix ? null : new HarmonyMethod(patch));
            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Init("Boss audio diagnostics hook ready: " + type.Name + "." + targetName + "."));
            }
        }

        private static void PostEventStringPostfix(string in_pszEventName, GameObject in_gameObjectID, uint __result)
        {
            if (!IsTankTreaderObject(in_gameObjectID))
            {
                return;
            }

            LogAudioEvent(
                "PostEvent",
                in_gameObjectID,
                "Event=" + (in_pszEventName ?? "<null>") +
                ", PlayingId=" + __result +
                ", PlayingIdIsZero=" + (__result == 0));
        }

        private static void TankTreaderIntroStartPrefix(TankTreaderIntroDoer __instance)
        {
            LogAudioEvent("TankTreaderIntro.StartIntro", __instance != null ? __instance.gameObject : null, "ExpectedEvent=Play_BOSS_tank_idle_01");
        }

        private static void TankTreaderIntroCleanupPostfix(TankTreaderIntroDoer __instance)
        {
            LogAudioEvent("TankTreaderIntro.OnCleanup", __instance != null ? __instance.gameObject : null, "ExpectedEvent=Stop_BOSS_tank_idle_01");
        }

        private static void TankTreaderDeathStartPostfix(TankTreaderDeathController __instance)
        {
            if (__instance == null || __instance.healthHaver == null)
            {
                return;
            }

            __instance.healthHaver.overrideDeathAudioEvent = "Play_BOSS_tank_death_01";
            LogAudioEvent(
                "TankTreaderDeath.Start",
                __instance.gameObject,
                "DeathAudioOverride=Play_BOSS_tank_death_01");
        }

        private static void TankTreaderDeathPrefix(TankTreaderDeathController __instance)
        {
            LogAudioEvent("TankTreaderDeath.OnBossDeath", __instance != null ? __instance.gameObject : null, "DeathAnimationStarted=True");
        }

        private static void TankTreaderDestroyPostfix(TankTreaderController __instance)
        {
            LogAudioEvent("TankTreaderController.OnDestroy", __instance != null ? __instance.gameObject : null, "ExpectedEvent=Stop_BOSS_tank_idle_01");
        }

        public static void StartReplayedTankTreaderIdle(AIActor boss)
        {
            if (boss == null || boss.GetComponentInChildren<TankTreaderController>(true) == null)
            {
                return;
            }

            AkSoundEngine.PostEvent("Play_BOSS_tank_idle_01", boss.gameObject);
            LogAudioEvent(
                "ReplayBossAudioRestore",
                boss.gameObject,
                "RestoredEvent=Play_BOSS_tank_idle_01");
        }

        private static bool IsTankTreaderObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            try
            {
                if (gameObject.GetComponentInParent<TankTreaderController>() != null ||
                    gameObject.GetComponentInParent<TankTreaderDeathController>() != null ||
                    gameObject.GetComponentInParent<TankTreaderIntroDoer>() != null)
                {
                    return true;
                }

                return gameObject.name != null && gameObject.name.IndexOf("TankTreader", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private static void LogAudioEvent(string phase, GameObject gameObject, string details)
        {
            if (_logger == null)
            {
                return;
            }

            _logger.LogInfo(RandomLoadoutLog.Command(
                "Boss audio diagnostic. Phase=" + phase +
                ", Object=" + DescribeObject(gameObject) +
                ", Frame=" + Time.frameCount +
                ", Scene=" + (SceneManager.GetActiveScene().name ?? "<unknown>") +
                ", " + details + "."));
        }

        private static string DescribeObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return "<null>";
            }

            return gameObject.name + "#" + gameObject.GetInstanceID() +
                ",ActiveSelf=" + gameObject.activeSelf +
                ",ActiveInHierarchy=" + gameObject.activeInHierarchy;
        }
    }
}
