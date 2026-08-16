// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace EtgGameplayDashboard
{
    internal static class BossIntroSkipHooks
    {
        private static readonly FieldInfo IsRunningField = AccessTools.Field(typeof(GenericIntroDoer), "m_isRunning");
        private static readonly FieldInfo SkipDelayField = AccessTools.Field(typeof(GenericIntroDoer), "m_singleFrameSkipDelay");
        private static readonly FieldInfo GatlingGullIsRunningField = AccessTools.Field(typeof(GatlingGullIntroDoer), "m_isRunning");
        private static readonly FieldInfo GatlingGullPhaseField = AccessTools.Field(typeof(GatlingGullIntroDoer), "m_currentPhase");
        private static ManualLogSource s_logger;
        private static System.Func<bool> s_verboseLoggingEnabledProvider;
        private static System.Action<bool> s_persistEnabledState;
        private static readonly System.Collections.Generic.HashSet<GenericIntroDoer> ObservedBossIntros =
            new System.Collections.Generic.HashSet<GenericIntroDoer>();
        private static readonly System.Collections.Generic.HashSet<GenericIntroDoer> PendingBossIntros =
            new System.Collections.Generic.HashSet<GenericIntroDoer>();
        private static readonly System.Collections.Generic.Dictionary<GatlingGullIntroDoer, int> GatlingGullIntroPhases =
            new System.Collections.Generic.Dictionary<GatlingGullIntroDoer, int>();

        public static bool IsEnabled { get; private set; }

        public static bool IsVerboseLoggingEnabled
        {
            get { return s_verboseLoggingEnabledProvider != null && s_verboseLoggingEnabledProvider(); }
        }

        public static bool Toggle()
        {
            IsEnabled = !IsEnabled;
            if (s_persistEnabledState != null)
            {
                s_persistEnabledState(IsEnabled);
            }

            return IsEnabled;
        }

        public static void Configure(bool initiallyEnabled, System.Action<bool> persistEnabledState)
        {
            IsEnabled = initiallyEnabled;
            s_persistEnabledState = persistEnabledState;
        }

        public static void Reset()
        {
            IsEnabled = false;
            s_verboseLoggingEnabledProvider = null;
            s_persistEnabledState = null;
            ObservedBossIntros.Clear();
            PendingBossIntros.Clear();
            GatlingGullIntroPhases.Clear();
        }

        public static void Install(Harmony harmony, ManualLogSource logger, System.Func<bool> verboseLoggingEnabledProvider)
        {
            s_logger = logger;
            s_verboseLoggingEnabledProvider = verboseLoggingEnabledProvider;
            if (!InstallGenericIntroHooks(harmony, logger))
            {
                return;
            }

            InstallGatlingGullDiagnosticsHooks(harmony, logger);
            InstallBossTriggerZoneHook(harmony, logger);
            if (logger != null)
            {
                logger.LogInfo(EtgGameplayDashboardLog.Init("Boss intro skip hook ready: GenericIntroDoer.InvariantUpdate postfix."));
                logger.LogInfo(EtgGameplayDashboardLog.Init("Boss intro diagnostics hook ready: GatlingGullIntroDoer.TriggerSequence and InvariantUpdate."));
            }
        }

        private static bool InstallGenericIntroHooks(Harmony harmony, ManualLogSource logger)
        {
            MethodInfo target = AccessTools.Method(typeof(GenericIntroDoer), "InvariantUpdate");
            MethodInfo patch = AccessTools.Method(typeof(BossIntroSkipHooks), "InvariantUpdatePostfix");
            MethodInfo enteredTarget = AccessTools.Method(typeof(GenericIntroDoer), "PlayerEntered", new[] { typeof(PlayerController) });
            MethodInfo enteredPatch = AccessTools.Method(typeof(BossIntroSkipHooks), "PlayerEnteredPostfix");
            MethodInfo triggerTarget = AccessTools.Method(typeof(GenericIntroDoer), "TriggerSequence", new[] { typeof(PlayerController) });
            MethodInfo triggerPatch = AccessTools.Method(typeof(BossIntroSkipHooks), "TriggerSequencePostfix");
            if (target == null || patch == null || enteredTarget == null || enteredPatch == null ||
                triggerTarget == null || triggerPatch == null || IsRunningField == null || SkipDelayField == null)
            {
                if (logger != null)
                {
                    logger.LogWarning(EtgGameplayDashboardLog.Init("Boss intro skip hook skipped because GenericIntroDoer members were unavailable."));
                }

                return false;
            }

            harmony.Patch(target, postfix: new HarmonyMethod(patch));
            harmony.Patch(enteredTarget, postfix: new HarmonyMethod(enteredPatch));
            harmony.Patch(triggerTarget, postfix: new HarmonyMethod(triggerPatch));
            return true;
        }

        private static void InstallGatlingGullDiagnosticsHooks(Harmony harmony, ManualLogSource logger)
        {
            MethodInfo target = AccessTools.Method(typeof(GatlingGullIntroDoer), "InvariantUpdate");
            MethodInfo patch = AccessTools.Method(typeof(BossIntroSkipHooks), "GatlingGullInvariantUpdatePostfix");
            MethodInfo triggerTarget = AccessTools.Method(typeof(GatlingGullIntroDoer), "TriggerSequence", new[] { typeof(PlayerController) });
            MethodInfo triggerPatch = AccessTools.Method(typeof(BossIntroSkipHooks), "GatlingGullTriggerSequencePostfix");
            if (target != null && patch != null && triggerTarget != null && triggerPatch != null &&
                GatlingGullIsRunningField != null && GatlingGullPhaseField != null)
            {
                harmony.Patch(target, postfix: new HarmonyMethod(patch));
                harmony.Patch(triggerTarget, postfix: new HarmonyMethod(triggerPatch));
            }
            else if (logger != null)
            {
                logger.LogWarning(EtgGameplayDashboardLog.Init("Boss intro diagnostics could not hook GatlingGullIntroDoer."));
            }
        }

        private static void InstallBossTriggerZoneHook(Harmony harmony, ManualLogSource logger)
        {
            MethodInfo target = AccessTools.Method(typeof(BossTriggerZone), "OnTriggerCollision");
            MethodInfo patch = AccessTools.Method(typeof(BossIntroSkipHooks), "BossTriggerZonePrefix");
            if (target != null && patch != null)
            {
                harmony.Patch(target, prefix: new HarmonyMethod(patch));
            }
            else if (logger != null)
            {
                logger.LogWarning(EtgGameplayDashboardLog.Init("Boss intro skip could not hook BossTriggerZone."));
            }
        }

        private static void InvariantUpdatePostfix(GenericIntroDoer __instance)
        {
            if (!IsEnabled || __instance == null || !IsBossIntroRunning(__instance) || !IsBoss(__instance))
            {
                return;
            }

            RequestNativeSkip(__instance);
        }

        private static void PlayerEnteredPostfix(GenericIntroDoer __instance)
        {
            PrepareAndScheduleSkip(__instance, "PlayerEntered");
        }

        private static void TriggerSequencePostfix(GenericIntroDoer __instance)
        {
            PrepareAndScheduleSkip(__instance, "TriggerSequence");
        }

        private static void GatlingGullTriggerSequencePostfix(GatlingGullIntroDoer __instance, PlayerController enterer)
        {
            if (!IsEnabled || __instance == null || !IsVerboseLoggingEnabled || s_logger == null)
            {
                return;
            }

            s_logger.LogInfo(
                EtgGameplayDashboardLog.Command(
                    "Boss intro diagnostic observed GatlingGullIntroDoer.TriggerSequence. " +
                    "Object=" + __instance.name +
                    ", Enterer=" + (enterer != null ? enterer.name : "<null>") +
                    ", Enabled=" + __instance.enabled + "."));
        }

        private static void GatlingGullInvariantUpdatePostfix(GatlingGullIntroDoer __instance)
        {
            if (!IsEnabled || __instance == null || !IsVerboseLoggingEnabled || s_logger == null)
            {
                return;
            }

            if (GatlingGullIsRunningField == null || GatlingGullPhaseField == null)
            {
                s_logger.LogWarning(EtgGameplayDashboardLog.Command("Boss intro diagnostic could not read GatlingGullIntroDoer state fields."));
                return;
            }

            object isRunningValue = GatlingGullIsRunningField.GetValue(__instance);
            bool isRunning = isRunningValue is bool && (bool)isRunningValue;
            int phase = (int)GatlingGullPhaseField.GetValue(__instance);
            int previousPhase;
            if (!GatlingGullIntroPhases.TryGetValue(__instance, out previousPhase) || previousPhase != phase)
            {
                GatlingGullIntroPhases[__instance] = phase;
                s_logger.LogInfo(
                    EtgGameplayDashboardLog.Command(
                        "Boss intro diagnostic GatlingGullIntroDoer state. " +
                        "Object=" + __instance.name +
                        ", IsRunning=" + isRunning +
                        ", Phase=" + phase +
                        ", Enabled=" + __instance.enabled +
                        ", GameIsBossIntro=" + (GameManager.HasInstance && GameManager.IsBossIntro) +
                        ", PreventPausing=" + (GameManager.HasInstance && GameManager.Instance.PreventPausing) + "."));
            }
        }

        private static void BossTriggerZonePrefix(BossTriggerZone __instance)
        {
            if (!IsEnabled || __instance == null)
            {
                return;
            }

            for (int index = 0; index < StaticReferenceManager.AllHealthHavers.Count; index++)
            {
                HealthHaver healthHaver = StaticReferenceManager.AllHealthHavers[index];
                if (healthHaver == null || !healthHaver.IsBoss)
                {
                    continue;
                }

                GenericIntroDoer introDoer = healthHaver.GetComponent<GenericIntroDoer>();
                AIActor actor = healthHaver.GetComponent<AIActor>();
                if (introDoer == null || actor == null || introDoer.triggerType != GenericIntroDoer.TriggerType.BossTriggerZone)
                {
                    continue;
                }

                if (__instance.ParentRoom != null && actor.ParentRoom != __instance.ParentRoom)
                {
                    continue;
                }

                PrepareAndScheduleSkip(introDoer, "BossTriggerZone");
                return;
            }

            if (IsVerboseLoggingEnabled && s_logger != null)
            {
                s_logger.LogWarning(EtgGameplayDashboardLog.Command("Boss intro skip entered a BossTriggerZone but found no matching GenericIntroDoer."));
            }
        }

        private static void PrepareAndScheduleSkip(GenericIntroDoer introDoer, string state)
        {
            if (!IsEnabled || !IsBoss(introDoer))
            {
                return;
            }

            EnsureIntroComponentsEnabled(introDoer);
            LogBossIntroStateOnce(introDoer, state);

            // GenericIntroDoer consumes this flag after it switches m_isRunning on.
            // Setting it at the trigger is therefore reliable even when the game's
            // setup coroutine reaches that state several frames later.
            RequestNativeSkip(introDoer);
            if (PendingBossIntros.Add(introDoer))
            {
                introDoer.StartCoroutine(WaitForBossIntroReady_CR(introDoer));
            }
        }

        private static IEnumerator WaitForBossIntroReady_CR(GenericIntroDoer introDoer)
        {
            const int maximumFrames = 120;
            for (int frame = 0; frame < maximumFrames; frame++)
            {
                if (!IsEnabled || introDoer == null)
                {
                    PendingBossIntros.Remove(introDoer);
                    yield break;
                }

                if (IsBossIntroRunning(introDoer))
                {
                    PendingBossIntros.Remove(introDoer);
                    yield break;
                }

                yield return null;
            }

            if (IsVerboseLoggingEnabled && s_logger != null)
            {
                s_logger.LogWarning(EtgGameplayDashboardLog.Command(BuildIntroTimeoutMessage(introDoer)));
            }

            PendingBossIntros.Remove(introDoer);
        }

        private static void EnsureIntroComponentsEnabled(GenericIntroDoer introDoer)
        {
            if (!introDoer.enabled)
            {
                introDoer.enabled = true;
            }

            AIActor actor = introDoer.GetComponent<AIActor>();
            if (actor != null && !actor.enabled)
            {
                actor.enabled = true;
            }
        }

        private static bool RequestNativeSkip(GenericIntroDoer introDoer)
        {
            object skipDelay = SkipDelayField.GetValue(introDoer);
            if (skipDelay == null || !skipDelay.Equals(Tribool.Unready))
            {
                return false;
            }

            SkipDelayField.SetValue(introDoer, Tribool.Ready);
            if (IsVerboseLoggingEnabled && s_logger != null)
            {
                AIActor actor = introDoer.GetComponent<AIActor>();
                s_logger.LogInfo(
                    EtgGameplayDashboardLog.Command(
                        "Boss intro skip requested through GenericIntroDoer. Enemy=" +
                        (actor != null ? actor.EnemyGuid : "<unknown>") + "."));
            }

            return true;
        }

        private static void LogBossIntroStateOnce(GenericIntroDoer introDoer, string state)
        {
            if (!IsVerboseLoggingEnabled || s_logger == null || introDoer == null || !ObservedBossIntros.Add(introDoer))
            {
                return;
            }

            AIActor actor = introDoer.GetComponent<AIActor>();
            s_logger.LogInfo(
                EtgGameplayDashboardLog.Command(
                    "Boss intro skip observed GenericIntroDoer. Enemy=" +
                    (actor != null ? actor.EnemyGuid : "<unknown>") +
                    ", State=" + state + "."));
        }

        private static string BuildIntroTimeoutMessage(GenericIntroDoer introDoer)
        {
            AIActor actor = introDoer != null ? introDoer.GetComponent<AIActor>() : null;
            bool preventPausing = GameManager.HasInstance && GameManager.Instance.PreventPausing;
            return "Boss intro skip did not enter the native running state. Enemy=" +
                (actor != null ? actor.EnemyGuid : "<unknown>") +
                ", GenericIntroEnabled=" + (introDoer != null && introDoer.enabled) +
                ", ActorEnabled=" + (actor != null && actor.enabled) +
                ", PreventPausing=" + preventPausing +
                ", IsBossIntro=" + GameManager.IsBossIntro + ".";
        }

        private static bool IsBossIntroRunning(GenericIntroDoer introDoer)
        {
            object isRunning = IsRunningField.GetValue(introDoer);
            return isRunning is bool && (bool)isRunning;
        }

        private static bool IsBoss(GenericIntroDoer introDoer)
        {
            AIActor actor = introDoer.GetComponent<AIActor>();
            return actor != null && actor.healthHaver != null && actor.healthHaver.IsBoss;
        }
    }
}
