// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Reflection;
using Dungeonator;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class RoomDebugCommandService
    {
        private const string GooptonJailedPrefabAssetBundle = "shared_auto_002";
        private const string GooptonJailedPrefabAssetPath = "assets/data/prefabs/npcs/jailed/npc_jellyfish_jailed.prefab";
        private const string NpcCellUnlockedEvent = "npcCellUnlocked";
        private const string PlayMakerFsmTypeName = "PlayMakerFSM";

        // These are the game's own persistent foyer-visibility flags. Do not set
        // the "ever talked" flags here, so the vanilla first-encounter dialogue remains intact.
        public GrantCommandExecutionResult UnlockCadenceOx()
        {
            return SetFoyerNpcFlag(GungeonFlags.META_SHOP_ACTIVE_IN_FOYER, "result.room.unlock_cadence_ox.success");
        }

        public GrantCommandExecutionResult UnlockGoopton()
        {
            GameStatsManager stats = GameStatsManager.Instance;
            if ((object)stats == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.game_stats_not_ready");
            }

            GameObject jailedPrefab = ResolveGooptonJailedPrefab();
            if ((object)jailedPrefab == null)
            {
                _logger.LogWarning(
                    EtgGameplayDashboardLog.Command(
                        "Goopton unlock failed: jailed NPC prefab was not found. Bundle=" +
                        GooptonJailedPrefabAssetBundle + ", AssetPath=" + GooptonJailedPrefabAssetPath + "."));
                return GrantCommandExecutionResult.Localized(false, "result.room.npc_unlock.failed");
            }

            int eventTargetCount;
            string eventFailure;
            if (!TrySendGooptonCellUnlockedEvent(jailedPrefab, out eventTargetCount, out eventFailure))
            {
                _logger.LogWarning(
                    EtgGameplayDashboardLog.Command(
                        "Goopton unlock failed: could not dispatch vanilla jailed-NPC FSM event. " +
                        "Prefab=" + jailedPrefab.name + ", Targets=" + eventTargetCount +
                        ", Reason=" + eventFailure + "."));
                return GrantCommandExecutionResult.Localized(false, "result.room.npc_unlock.failed");
            }

            if (!stats.GetFlag(GungeonFlags.SHOP_GOOP_ACTIVE))
            {
                _logger.LogWarning(
                    EtgGameplayDashboardLog.Command(
                        "Goopton unlock failed: vanilla jailed-NPC FSM event did not set SHOP_GOOP_ACTIVE. " +
                        "Prefab=" + jailedPrefab.name + ", Targets=" + eventTargetCount + "."));
                return GrantCommandExecutionResult.Localized(false, "result.room.npc_unlock.failed");
            }

            GameStatsManager.Save();
            LogGooptonState("unlock button");
            return stats.GetFlag(GungeonFlags.SHOP_GOOP_ACTIVE)
                ? GrantCommandExecutionResult.Localized(true, "result.room.unlock_goopton.success")
                : GrantCommandExecutionResult.Localized(false, "result.room.npc_unlock.failed");
        }

        private static bool TrySendGooptonCellUnlockedEvent(GameObject jailedPrefab, out int eventTargetCount, out string failure)
        {
            eventTargetCount = 0;
            failure = string.Empty;
            GameObject runtimeInstance = null;
            try
            {
                // A bundle asset is not a running Unity object: its PlayMaker FSM has
                // not received Awake/Start, so sending the event to the asset silently
                // does nothing. Instantiate the vanilla prefab, initialize its FSMs,
                // and dispatch the same event used by InteractableLock.Interact.
                runtimeInstance = Object.Instantiate(jailedPrefab);
                if ((object)runtimeInstance == null)
                {
                    failure = "runtime prefab instantiation returned null";
                    return false;
                }

                Component[] components = runtimeInstance.GetComponentsInChildren<Component>(includeInactive: true);
                for (int index = 0; index < components.Length; index++)
                {
                    Component component = components[index];
                    if ((object)component == null || component.GetType().FullName != PlayMakerFsmTypeName)
                    {
                        continue;
                    }

                    System.Type componentType = component.GetType();
                    MethodInfo startMethod = componentType.GetMethod("Start", InstanceFlags);
                    if (startMethod != null)
                    {
                        startMethod.Invoke(component, null);
                    }

                    MethodInfo sendEventMethod = componentType.GetMethod(
                        "SendEvent",
                        InstanceFlags,
                        binder: null,
                        types: new[] { typeof(string) },
                        modifiers: null);
                    if (sendEventMethod == null)
                    {
                        continue;
                    }

                    sendEventMethod.Invoke(component, new object[] { NpcCellUnlockedEvent });
                    eventTargetCount++;
                }

                if (eventTargetCount == 0)
                {
                    failure = "NPC_Jellyfish_Jailed contains no PlayMakerFSM with SendEvent(string)";
                    return false;
                }

                return true;
            }
            catch (System.Exception exception)
            {
                failure = exception.GetBaseException().Message;
                return false;
            }
            finally
            {
                if ((object)runtimeInstance != null)
                {
                    Object.Destroy(runtimeInstance);
                }
            }
        }

        public void EnsureGooptonActiveOnFoyerLoad()
        {
            GameStatsManager stats = GameStatsManager.Instance;
            if ((object)stats == null || GameManager.Instance == null || !GameManager.Instance.IsFoyer)
            {
                return;
            }

            LogGooptonState("foyer load");
        }

        private static GameObject ResolveGooptonJailedPrefab()
        {
            AssetBundle assetBundle;
            try
            {
                assetBundle = ResourceManager.LoadAssetBundle(GooptonJailedPrefabAssetBundle);
            }
            catch (System.Exception)
            {
                return null;
            }

            if ((object)assetBundle == null)
            {
                return null;
            }

            try
            {
                return assetBundle.LoadAsset<GameObject>(GooptonJailedPrefabAssetPath);
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private static BaseShopController FindFoyerGooptonShop()
        {
            BaseShopController[] shops = Object.FindObjectsOfType<BaseShopController>();
            for (int index = 0; index < shops.Length; index++)
            {
                BaseShopController shop = shops[index];
                if ((object)shop != null &&
                    shop.baseShopType == BaseShopController.AdditionalShopType.FOYER_META &&
                    !shop.IsBeetleMerchant &&
                    shop.FlagToSetOnEncounter == GungeonFlags.SHOP_HAS_MET_GOOP)
                {
                    return shop;
                }
            }

            return null;
        }

        private void LogGooptonState(string source)
        {
            if (_logger == null)
            {
                return;
            }

            GameStatsManager stats = GameStatsManager.Instance;
            bool hasMet = (object)stats != null && stats.GetFlag(GungeonFlags.SHOP_HAS_MET_GOOP);
            bool active = (object)stats != null && stats.GetFlag(GungeonFlags.SHOP_GOOP_ACTIVE);
            BaseShopController[] shops = Object.FindObjectsOfType<BaseShopController>();
            int foyerGoopShopCount = 0;
            for (int index = 0; index < shops.Length; index++)
            {
                BaseShopController shop = shops[index];
                if ((object)shop != null &&
                    shop.baseShopType == BaseShopController.AdditionalShopType.FOYER_META &&
                    !shop.IsBeetleMerchant &&
                    shop.FlagToSetOnEncounter == GungeonFlags.SHOP_HAS_MET_GOOP)
                {
                    foyerGoopShopCount++;
                }
            }

            _logger.LogInfo(
                EtgGameplayDashboardLog.Command(
                    "Goopton diagnostic: source=" + source +
                    ", scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name +
                    ", isFoyer=" + (GameManager.Instance != null && GameManager.Instance.IsFoyer) +
                    ", SHOP_HAS_MET_GOOP=" + hasMet +
                    ", SHOP_GOOP_ACTIVE=" + active +
                    ", activeFoyerGoopShopControllers=" + foyerGoopShopCount + "."));
        }

        public GrantCommandExecutionResult UnlockDoug()
        {
            GameStatsManager stats = GameStatsManager.Instance;
            if ((object)stats == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.game_stats_not_ready");
            }

            // SHOP_HAS_MET_BEETLE is the persistent rescue/unlock flag. The active
            // flag only requests Doug for the current Breach load.
            stats.SetFlag(GungeonFlags.SHOP_HAS_MET_BEETLE, true);
            stats.SetFlag(GungeonFlags.SHOP_BEETLE_ACTIVE, true);
            GameStatsManager.Save();
            return stats.GetFlag(GungeonFlags.SHOP_HAS_MET_BEETLE) && stats.GetFlag(GungeonFlags.SHOP_BEETLE_ACTIVE)
                ? GrantCommandExecutionResult.Localized(true, "result.room.unlock_doug.success")
                : GrantCommandExecutionResult.Localized(false, "result.room.npc_unlock.failed");
        }

        private static GrantCommandExecutionResult SetFoyerNpcFlag(GungeonFlags flag, string successKey)
        {
            GameStatsManager stats = GameStatsManager.Instance;
            if ((object)stats == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.game_stats_not_ready");
            }

            stats.SetFlag(flag, true);
            GameStatsManager.Save();
            return stats.GetFlag(flag)
                ? GrantCommandExecutionResult.Localized(true, successKey)
                : GrantCommandExecutionResult.Localized(false, "result.room.npc_unlock.failed");
        }
    }
}
