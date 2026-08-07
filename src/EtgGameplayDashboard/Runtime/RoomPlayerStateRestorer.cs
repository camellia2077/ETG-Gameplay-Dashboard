// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;

namespace EtgGameplayDashboard
{
    /// <summary>
    /// Captures and restores the player state associated with a room replay.
    /// </summary>
    internal sealed class RoomPlayerStateRestorer
    {
        private readonly Action<string> _log;
        private readonly Action<string> _logWarning;

        public RoomPlayerStateRestorer(Action<string> log, Action<string> logWarning)
        {
            _log = log;
            _logWarning = logWarning;
        }

        public static PlayerRoomSnapshot Capture(PlayerController player)
        {
            if ((object)player == null || (object)player.healthHaver == null || player.stats == null)
            {
                return null;
            }

            PlayerRoomSnapshot snapshot = new PlayerRoomSnapshot();
            snapshot.CurrentHealth = player.healthHaver.GetCurrentHealth();
            snapshot.MaximumHealth = player.healthHaver.GetMaxHealth();
            snapshot.Armor = player.healthHaver.Armor;
            snapshot.Blanks = player.Blanks;
            snapshot.BaseStats = CopyList(player.stats.BaseStatValues);
            snapshot.StatValues = CopyList(PrivateFieldAccessor.GetPrivateList<float>(player.stats, "StatValues"));
            snapshot.PreviouslyActiveSynergies = CopyList(player.stats.PreviouslyActiveSynergies);

            if (player.inventory != null && player.inventory.AllGuns != null)
            {
                snapshot.SelectedGunIndex = player.inventory.AllGuns.IndexOf(player.CurrentGun);
                for (int index = 0; index < player.inventory.AllGuns.Count; index++)
                {
                    Gun gun = player.inventory.AllGuns[index];
                    if ((object)gun != null)
                    {
                        snapshot.Guns.Add(new GunRoomState(
                            gun.PickupObjectId,
                            gun.ammo,
                            PrivateFieldAccessor.GetPrivateFloat(gun, "m_remainingActiveCooldownAmount")));
                    }
                }
            }

            if (player.passiveItems != null)
            {
                for (int index = 0; index < player.passiveItems.Count; index++)
                {
                    PassiveItem item = player.passiveItems[index];
                    if ((object)item != null)
                    {
                        snapshot.PassiveIds.Add(item.PickupObjectId);
                    }
                }
            }

            if (player.activeItems != null)
            {
                snapshot.SelectedActiveIndex = PrivateFieldAccessor.GetPrivateInt(player, "m_selectedItemIndex");
                for (int index = 0; index < player.activeItems.Count; index++)
                {
                    PlayerItem item = player.activeItems[index];
                    if ((object)item != null)
                    {
                        snapshot.ActiveItems.Add(new ActiveRoomState(
                            item.PickupObjectId,
                            item.CurrentRoomCooldown,
                            item.CurrentTimeCooldown,
                            item.CurrentDamageCooldown,
                            PrivateFieldAccessor.GetPrivateFloat(item, "m_activeElapsed"),
                            PrivateFieldAccessor.GetPrivateFloat(item, "m_activeDuration"),
                            PrivateFieldAccessor.GetPrivateBool(item, "m_isCurrentlyActive")));
                    }
                }
            }

            return snapshot;
        }

        public void Restore(PlayerController player, PlayerRoomSnapshot snapshot)
        {
            if ((object)player == null || snapshot == null)
            {
                LogWarning("Room player rewind skipped because the player or snapshot was unavailable. Player=" + ((object)player != null) + ".");
                return;
            }

            Log("Restoring room-entry player state. Before=" + DescribeLive(player) + ", Snapshot=" + Describe(snapshot) + ".");
            bool restoredInventoryInPlace = TryRestoreInventoryInPlace(player, snapshot);
            if (!restoredInventoryInPlace && player.inventory != null)
            {
                player.inventory.DestroyAllGuns();
            }
            if (!restoredInventoryInPlace)
            {
                player.RemoveAllPassiveItems();
                player.RemoveAllActiveItems();
            }

            if (player.stats != null)
            {
                player.stats.BaseStatValues = CopyList(snapshot.BaseStats);
                player.stats.PreviouslyActiveSynergies = CopyList(snapshot.PreviouslyActiveSynergies);
            }

            if (!restoredInventoryInPlace)
            {
                RestoreGuns(player, snapshot);
                RestorePassives(player, snapshot);
                RestoreActiveItems(player, snapshot);
            }

            if (player.stats != null)
            {
                player.stats.RecalculateStats(player, true);
                PrivateFieldAccessor.SetPrivateList(player.stats, "StatValues", CopyList(snapshot.StatValues));
            }

            player.Blanks = snapshot.Blanks;
            player.healthHaver.SetHealthMaximum(snapshot.MaximumHealth, null, false);
            player.healthHaver.ForceSetCurrentHealth(snapshot.CurrentHealth);
            player.healthHaver.Armor = snapshot.Armor;
            if (player.inventory != null && snapshot.SelectedGunIndex >= 0 && snapshot.SelectedGunIndex < player.inventory.AllGuns.Count)
            {
                player.ChangeToGunSlot(snapshot.SelectedGunIndex, true);
            }
            PrivateFieldAccessor.SetPrivateInt(player, "m_selectedItemIndex", snapshot.SelectedActiveIndex);
            Log("Restored room-entry player state. InPlaceInventory=" + restoredInventoryInPlace + ", After=" + DescribeLive(player) + ".");
        }

        public static string Describe(PlayerRoomSnapshot snapshot)
        {
            return snapshot == null
                ? "PlayerSnapshot=<none>"
                : "Health=" + snapshot.CurrentHealth + "/" + snapshot.MaximumHealth +
                  ", Armor=" + snapshot.Armor + ", Blanks=" + snapshot.Blanks +
                  ", Guns=" + snapshot.Guns.Count + ", Passives=" + snapshot.PassiveIds.Count +
                  ", Actives=" + snapshot.ActiveItems.Count + ", SelectedGun=" + snapshot.SelectedGunIndex +
                  ", SelectedActive=" + snapshot.SelectedActiveIndex;
        }

        private void RestoreGuns(PlayerController player, PlayerRoomSnapshot snapshot)
        {
            for (int index = 0; index < snapshot.Guns.Count; index++)
            {
                GunRoomState savedGun = snapshot.Guns[index];
                Gun prefab = PickupObjectDatabase.GetById(savedGun.PickupId) as Gun;
                if ((object)prefab == null || player.inventory == null)
                {
                    LogWarning("Room player rewind could not restore gun. PickupId=" + savedGun.PickupId + ".");
                    continue;
                }

                Gun restoredGun = player.inventory.AddGunToInventory(prefab, index == snapshot.SelectedGunIndex);
                if ((object)restoredGun != null)
                {
                    restoredGun.ammo = savedGun.Ammo;
                    PrivateFieldAccessor.SetPrivateFloat(restoredGun, "m_remainingActiveCooldownAmount", savedGun.RemainingActiveCooldownAmount);
                }
            }
        }

        private void RestorePassives(PlayerController player, PlayerRoomSnapshot snapshot)
        {
            for (int index = 0; index < snapshot.PassiveIds.Count; index++)
            {
                PassiveItem prefab = PickupObjectDatabase.GetById(snapshot.PassiveIds[index]) as PassiveItem;
                if ((object)prefab == null)
                {
                    LogWarning("Room player rewind could not restore passive. PickupId=" + snapshot.PassiveIds[index] + ".");
                    continue;
                }

                player.AcquirePassiveItemPrefabDirectly(prefab);
            }
        }

        private void RestoreActiveItems(PlayerController player, PlayerRoomSnapshot snapshot)
        {
            for (int index = 0; index < snapshot.ActiveItems.Count; index++)
            {
                ActiveRoomState savedItem = snapshot.ActiveItems[index];
                PlayerItem prefab = PickupObjectDatabase.GetById(savedItem.PickupId) as PlayerItem;
                if ((object)prefab == null)
                {
                    LogWarning("Room player rewind could not restore active item. PickupId=" + savedItem.PickupId + ".");
                    continue;
                }

                EncounterTrackable.SuppressNextNotification = true;
                prefab.Pickup(player);
                if (player.activeItems != null && player.activeItems.Count > index)
                {
                    PlayerItem restoredItem = player.activeItems[index];
                    restoredItem.CurrentRoomCooldown = savedItem.RoomCooldown;
                    restoredItem.CurrentTimeCooldown = savedItem.TimeCooldown;
                    restoredItem.CurrentDamageCooldown = savedItem.DamageCooldown;
                    PrivateFieldAccessor.SetPrivateFloat(restoredItem, "m_activeElapsed", savedItem.ActiveElapsed);
                    PrivateFieldAccessor.SetPrivateFloat(restoredItem, "m_activeDuration", savedItem.ActiveDuration);
                    PrivateFieldAccessor.SetPrivateBool(restoredItem, "m_isCurrentlyActive", savedItem.IsCurrentlyActive);
                }
            }
        }

        private bool TryRestoreInventoryInPlace(PlayerController player, PlayerRoomSnapshot snapshot)
        {
            if (player.inventory == null || player.inventory.AllGuns == null ||
                player.passiveItems == null || player.activeItems == null ||
                player.inventory.AllGuns.Count != snapshot.Guns.Count ||
                player.passiveItems.Count != snapshot.PassiveIds.Count ||
                player.activeItems.Count != snapshot.ActiveItems.Count)
            {
                return false;
            }

            for (int index = 0; index < snapshot.Guns.Count; index++)
            {
                Gun currentGun = player.inventory.AllGuns[index];
                if ((object)currentGun == null || currentGun.PickupObjectId != snapshot.Guns[index].PickupId)
                {
                    return false;
                }
            }

            for (int index = 0; index < snapshot.PassiveIds.Count; index++)
            {
                PassiveItem currentItem = player.passiveItems[index];
                if ((object)currentItem == null || currentItem.PickupObjectId != snapshot.PassiveIds[index])
                {
                    return false;
                }
            }

            for (int index = 0; index < snapshot.ActiveItems.Count; index++)
            {
                PlayerItem currentItem = player.activeItems[index];
                if ((object)currentItem == null || currentItem.PickupObjectId != snapshot.ActiveItems[index].PickupId)
                {
                    return false;
                }
            }

            for (int index = 0; index < snapshot.Guns.Count; index++)
            {
                Gun currentGun = player.inventory.AllGuns[index];
                GunRoomState savedGun = snapshot.Guns[index];
                currentGun.ammo = savedGun.Ammo;
                PrivateFieldAccessor.SetPrivateFloat(currentGun, "m_remainingActiveCooldownAmount", savedGun.RemainingActiveCooldownAmount);
            }

            for (int index = 0; index < snapshot.ActiveItems.Count; index++)
            {
                PlayerItem currentItem = player.activeItems[index];
                ActiveRoomState savedItem = snapshot.ActiveItems[index];
                currentItem.CurrentRoomCooldown = savedItem.RoomCooldown;
                currentItem.CurrentTimeCooldown = savedItem.TimeCooldown;
                currentItem.CurrentDamageCooldown = savedItem.DamageCooldown;
                PrivateFieldAccessor.SetPrivateFloat(currentItem, "m_activeElapsed", savedItem.ActiveElapsed);
                PrivateFieldAccessor.SetPrivateFloat(currentItem, "m_activeDuration", savedItem.ActiveDuration);
                PrivateFieldAccessor.SetPrivateBool(currentItem, "m_isCurrentlyActive", savedItem.IsCurrentlyActive);
            }

            Log("Used in-place Boss-room inventory restore because the current inventory matches the snapshot.");
            return true;
        }

        private static string DescribeLive(PlayerController player)
        {
            return (object)player == null || (object)player.healthHaver == null
                ? "Player=<none>"
                : "Health=" + player.healthHaver.GetCurrentHealth() + "/" + player.healthHaver.GetMaxHealth() +
                  ", Armor=" + player.healthHaver.Armor + ", Blanks=" + player.Blanks +
                  ", Guns=" + (player.inventory != null && player.inventory.AllGuns != null ? player.inventory.AllGuns.Count : 0) +
                  ", Passives=" + (player.passiveItems != null ? player.passiveItems.Count : 0) +
                  ", Actives=" + (player.activeItems != null ? player.activeItems.Count : 0);
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log(message);
            }
        }

        private void LogWarning(string message)
        {
            if (_logWarning != null)
            {
                _logWarning(message);
            }
        }

        private static List<T> CopyList<T>(List<T> source)
        {
            return source != null ? new List<T>(source) : new List<T>();
        }

    }

    internal sealed class PlayerRoomSnapshot
    {
        public float CurrentHealth;
        public float MaximumHealth;
        public float Armor;
        public int Blanks;
        public int SelectedGunIndex = -1;
        public int SelectedActiveIndex = -1;
        public List<float> BaseStats = new List<float>();
        public List<float> StatValues = new List<float>();
        public List<int> PreviouslyActiveSynergies = new List<int>();
        public readonly List<GunRoomState> Guns = new List<GunRoomState>();
        public readonly List<int> PassiveIds = new List<int>();
        public readonly List<ActiveRoomState> ActiveItems = new List<ActiveRoomState>();
    }

    internal sealed class GunRoomState
    {
        public GunRoomState(int pickupId, int ammo, float remainingActiveCooldownAmount)
        {
            PickupId = pickupId;
            Ammo = ammo;
            RemainingActiveCooldownAmount = remainingActiveCooldownAmount;
        }

        public int PickupId;
        public int Ammo;
        public float RemainingActiveCooldownAmount;
    }

    internal sealed class ActiveRoomState
    {
        public ActiveRoomState(int pickupId, int roomCooldown, float timeCooldown, float damageCooldown, float activeElapsed, float activeDuration, bool isCurrentlyActive)
        {
            PickupId = pickupId;
            RoomCooldown = roomCooldown;
            TimeCooldown = timeCooldown;
            DamageCooldown = damageCooldown;
            ActiveElapsed = activeElapsed;
            ActiveDuration = activeDuration;
            IsCurrentlyActive = isCurrentlyActive;
        }

        public int PickupId;
        public int RoomCooldown;
        public float TimeCooldown;
        public float DamageCooldown;
        public float ActiveElapsed;
        public float ActiveDuration;
        public bool IsCurrentlyActive;
    }
}
