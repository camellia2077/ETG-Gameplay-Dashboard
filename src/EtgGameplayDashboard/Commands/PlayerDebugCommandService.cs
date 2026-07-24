// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed class PlayerDebugCommandService
    {
        private const float HalfHeartAmount = 0.5f;
        private const float SingleHeartAmount = 1f;
        private const float SingleArmorAmount = 1f;
        private const int SingleKeyAmount = 1;
        private const int SingleBlankAmount = 1;
        private const int FullHeartPickupId = 85;
        private const int ArmorPickupId = 120;
        private const int KeyPickupId = 67;
        private const int BlankPickupId = 224;
        private const int RatKeyPickupId = 727;
        private const float CurrencySpawnDistance = 3f;
        // Run currency (casings): drops during dungeon runs and is consumed in-run.
        private const int CurrencyBundleAmount = 100;
        private const int LargeCurrencyBundleAmount = 100;
        // Breach meta currency (hegemony credits): used in the character-select hub economy.
        // META_CURRENCY is applied as a direct 1:1 stat increment in ETG runtime.
        private const float MetaCurrencyBundleAmount = 50f;
        private readonly PlayerHealthOverrideService _playerHealthOverrideService;

        public PlayerDebugCommandService(PlayerHealthOverrideService playerHealthOverrideService)
        {
            _playerHealthOverrideService = playerHealthOverrideService;
        }

        public GrantCommandExecutionResult FullHeal(PlayerController player)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            HealthHaver healthHaver = player.healthHaver;
            if ((object)healthHaver == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.health_not_ready");
            }

            healthHaver.FullHeal();
            return GrantCommandExecutionResult.Localized(true, "result.debug.full_heal.success");
        }

        public GrantCommandExecutionResult HealHalfHeart(PlayerController player)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            HealthHaver healthHaver = player.healthHaver;
            if ((object)healthHaver == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.health_not_ready");
            }

            float maxHealth = healthHaver.GetMaxHealth();
            if (maxHealth <= 0f)
            {
                return GrantCommandExecutionResult.Localized(false, "result.debug.health_type_invalid");
            }

            float currentHealth = healthHaver.GetCurrentHealth();
            float missingHealth = maxHealth - currentHealth;
            if (missingHealth <= 0f)
            {
                return GrantCommandExecutionResult.Localized(false, "result.debug.health_already_full");
            }

            float healAmount = missingHealth < HalfHeartAmount ? missingHealth : HalfHeartAmount;
            healthHaver.ApplyHealing(healAmount);
            return GrantCommandExecutionResult.Localized(true, "result.debug.heal_half.success", healAmount);
        }

        public GrantCommandExecutionResult AddArmor(PlayerController player)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            HealthHaver healthHaver = player.healthHaver;
            if ((object)healthHaver == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.health_not_ready");
            }

            float nextArmor = healthHaver.Armor + SingleArmorAmount;
            healthHaver.Armor = nextArmor;
            return GrantCommandExecutionResult.Localized(true, "result.debug.add_armor.success");
        }

        public GrantCommandExecutionResult AddMaxHealth(PlayerController player)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            HealthHaver healthHaver = player.healthHaver;
            if ((object)healthHaver == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.health_not_ready");
            }

            float maxHealth = healthHaver.GetMaxHealth();
            if (maxHealth <= 0f)
            {
                return GrantCommandExecutionResult.Localized(false, "result.debug.health_type_invalid");
            }

            float nextMaxHealth = maxHealth + SingleHeartAmount;
            // SetHealthMaximum can apply the gained heart and raise OnHealthChanged in one event.
            // Do not follow it with ApplyHealing: that would raise the same event twice, and the
            // HUD would replay both the heart-gained animation and its Armor UI refresh.
            // Keeping this as one event also preserves the normal single-heart visual feedback.
            healthHaver.SetHealthMaximum(nextMaxHealth, SingleHeartAmount, false);
            if (_playerHealthOverrideService != null)
            {
                _playerHealthOverrideService.TrackOverride(player, healthHaver);
            }
            return GrantCommandExecutionResult.Localized(true, "result.debug.add_max_health.success");
        }

        public GrantCommandExecutionResult ClearCurse(PlayerController player)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            PlayerStats stats = player.stats;
            if ((object)stats == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.stats_not_ready");
            }

            float totalCurse = stats.GetStatValue(PlayerStats.StatType.Curse);
            if (totalCurse <= 0f)
            {
                return GrantCommandExecutionResult.Localized(false, "result.debug.no_curse");
            }

            float currentBaseCurse = stats.GetBaseStatValue(PlayerStats.StatType.Curse);
            stats.SetBaseStatValue(PlayerStats.StatType.Curse, currentBaseCurse - totalCurse, player);
            stats.RecalculateStats(player, true, false);

            float remainingCurse = stats.GetStatValue(PlayerStats.StatType.Curse);
            if (remainingCurse > 0f)
            {
                return GrantCommandExecutionResult.Localized(true, "result.debug.clear_curse.partial", remainingCurse);
            }

            return GrantCommandExecutionResult.Localized(true, "result.debug.clear_curse.success");
        }

        public GrantCommandExecutionResult AddBlank(PlayerController player)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            player.Blanks = player.Blanks + SingleBlankAmount;
            return GrantCommandExecutionResult.Localized(true, "result.debug.add_blank.success");
        }

        public GrantCommandExecutionResult SpawnBlankNearPlayer(PlayerController player)
        {
            return SpawnPickupNearPlayer(
                player,
                BlankPickupId,
                "result.debug.spawn_blank.unavailable",
                "result.debug.spawn_blank.failed",
                "result.debug.spawn_blank.success");
        }

        public GrantCommandExecutionResult SpawnFullHeartNearPlayer(PlayerController player)
        {
            return SpawnPickupNearPlayer(
                player,
                FullHeartPickupId,
                "result.debug.spawn_full_heart.unavailable",
                "result.debug.spawn_full_heart.failed",
                "result.debug.spawn_full_heart.success");
        }

        public GrantCommandExecutionResult SpawnArmorNearPlayer(PlayerController player)
        {
            return SpawnPickupNearPlayer(
                player,
                ArmorPickupId,
                "result.debug.spawn_armor.unavailable",
                "result.debug.spawn_armor.failed",
                "result.debug.spawn_armor.success");
        }

        public GrantCommandExecutionResult SpawnKeyNearPlayer(PlayerController player)
        {
            return SpawnPickupNearPlayer(
                player,
                KeyPickupId,
                "result.debug.spawn_key.unavailable",
                "result.debug.spawn_key.failed",
                "result.debug.spawn_key.success");
        }

        public GrantCommandExecutionResult SpawnRatKeyNearPlayer(PlayerController player)
        {
            return SpawnPickupNearPlayer(
                player,
                RatKeyPickupId,
                "result.debug.spawn_rat_key.unavailable",
                "result.debug.spawn_rat_key.failed",
                "result.debug.spawn_rat_key.success");
        }

        public GrantCommandExecutionResult SpawnCurrencyNearPlayer(PlayerController player)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            try
            {
                LootEngine.SpawnCurrency(
                    player.CenterPosition + (Vector2.right * CurrencySpawnDistance),
                    CurrencyBundleAmount,
                    false,
                    Vector2.right,
                    0f,
                    1f);
            }
            catch (System.Exception)
            {
                return GrantCommandExecutionResult.Localized(false, "result.debug.spawn_currency.failed");
            }

            return GrantCommandExecutionResult.Localized(true, "result.debug.spawn_currency.success");
        }

        private static GrantCommandExecutionResult SpawnPickupNearPlayer(
            PlayerController player,
            int pickupId,
            string unavailableResultKey,
            string failedResultKey,
            string successResultKey)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            PickupObject pickup = PickupObjectDatabase.GetById(pickupId);
            if ((object)pickup == null || (object)pickup.gameObject == null)
            {
                return GrantCommandExecutionResult.Localized(false, unavailableResultKey);
            }

            try
            {
                DebrisObject spawnedPickup = LootEngine.SpawnItem(
                    pickup.gameObject,
                    player.CenterPosition + Vector2.right,
                    Vector2.right,
                    1f,
                    false,
                    true,
                    false);
                if ((object)spawnedPickup == null)
                {
                    return GrantCommandExecutionResult.Localized(false, failedResultKey);
                }
            }
            catch (System.Exception)
            {
                return GrantCommandExecutionResult.Localized(false, failedResultKey);
            }

            return GrantCommandExecutionResult.Localized(true, successResultKey);
        }

        public GrantCommandExecutionResult RefillCurrentGunAmmo(PlayerController player)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            Gun currentGun = player.CurrentGun;
            if ((object)currentGun == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.debug.no_equipped_gun");
            }

            if (currentGun.InfiniteAmmo)
            {
                return GrantCommandExecutionResult.Localized(false, "result.debug.gun_infinite_ammo");
            }

            int targetAmmo = currentGun.AdjustedMaxAmmo;
            if (targetAmmo <= 0)
            {
                return GrantCommandExecutionResult.Localized(false, "result.debug.gun_refill_unsupported");
            }

            bool changedAmmo = currentGun.CurrentAmmo < targetAmmo;
            bool changedClip = currentGun.ClipShotsRemaining < currentGun.ClipCapacity;
            if (!changedAmmo && !changedClip)
            {
                return GrantCommandExecutionResult.Localized(false, "result.debug.gun_already_full");
            }

            currentGun.CurrentAmmo = targetAmmo;
            if (currentGun.ClipCapacity > 0)
            {
                currentGun.ClipShotsRemaining = currentGun.ClipCapacity;
                currentGun.ForceImmediateReload(false);
            }

            string gunLabel = !string.IsNullOrEmpty(currentGun.DisplayName) ? currentGun.DisplayName : currentGun.name;
            return GrantCommandExecutionResult.Localized(true, "result.debug.refill_ammo.success", gunLabel);
        }

        public GrantCommandExecutionResult AddKey(PlayerController player)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            PlayerConsumables consumables = player.carriedConsumables;
            if ((object)consumables == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.consumables_not_ready");
            }

            consumables.KeyBullets = consumables.KeyBullets + SingleKeyAmount;
            return GrantCommandExecutionResult.Localized(true, "result.debug.add_key.success");
        }

        public GrantCommandExecutionResult AddRatKey(PlayerController player)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            PlayerConsumables consumables = player.carriedConsumables;
            if ((object)consumables == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.consumables_not_ready");
            }

            consumables.ResourcefulRatKeys = consumables.ResourcefulRatKeys + SingleKeyAmount;
            return GrantCommandExecutionResult.Localized(true, "result.debug.add_rat_key.success");
        }

        public GrantCommandExecutionResult AddCurrency(PlayerController player)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            PlayerConsumables consumables = player.carriedConsumables;
            if ((object)consumables == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.consumables_not_ready");
            }

            consumables.Currency = consumables.Currency + CurrencyBundleAmount;
            return GrantCommandExecutionResult.Localized(true, "result.debug.add_currency.success");
        }

        public GrantCommandExecutionResult AddLargeCurrency(PlayerController player)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            PlayerConsumables consumables = player.carriedConsumables;
            if ((object)consumables == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.consumables_not_ready");
            }

            consumables.Currency = consumables.Currency + LargeCurrencyBundleAmount;
            return GrantCommandExecutionResult.Localized(true, "result.debug.add_currency.success");
        }

        public GrantCommandExecutionResult ClearCurrency(PlayerController player)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            PlayerConsumables consumables = player.carriedConsumables;
            if ((object)consumables == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.consumables_not_ready");
            }

            int clearedAmount = consumables.Currency;
            consumables.Currency = 0;
            return GrantCommandExecutionResult.Localized(true, "result.debug.clear_currency.success", clearedAmount);
        }

        public GrantCommandExecutionResult AddMetaCurrency(PlayerController player)
        {
            GameStatsManager stats = GameStatsManager.Instance;
            if ((object)stats == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.game_stats_not_ready");
            }

            // META_CURRENCY is the Breach (character-select hub) currency, not in-run casings.
            // Runtime behavior is 1:1 with RegisterStatChange for this tracked stat.
            stats.RegisterStatChange(TrackedStats.META_CURRENCY, MetaCurrencyBundleAmount);
            GameStatsManager.Save();
            return GrantCommandExecutionResult.Localized(true, "result.debug.add_meta_currency.success");
        }

        public GrantCommandExecutionResult ClearMetaCurrency(PlayerController player)
        {
            GameStatsManager stats = GameStatsManager.Instance;
            if ((object)stats == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.game_stats_not_ready");
            }

            int clearedAmount = (int)stats.GetPlayerStatValue(TrackedStats.META_CURRENCY);
            // This is the same vanilla global-clear path used before setting the remaining
            // Hegemony balance after a Breach shop purchase.
            stats.ClearStatValueGlobal(TrackedStats.META_CURRENCY);
            GameStatsManager.Save();
            return GrantCommandExecutionResult.Localized(true, "result.debug.clear_meta_currency.success", clearedAmount);
        }

        public GrantCommandExecutionResult GrantStartItemPickup(PlayerController player, string pickupType)
        {
            switch (StartItemPickupCatalog.NormalizeType(pickupType))
            {
                case StartItemPickupCatalog.KeyType:
                    return AddKey(player);
                case StartItemPickupCatalog.RatKeyType:
                    return AddRatKey(player);
                case StartItemPickupCatalog.MaxHealthType:
                    return AddMaxHealth(player);
                case StartItemPickupCatalog.ArmorType:
                    return AddArmor(player);
                case StartItemPickupCatalog.CasingsType:
                    return AddCurrency(player);
                case StartItemPickupCatalog.BlankType:
                    return AddBlank(player);
                default:
                    return GrantCommandExecutionResult.Localized(false, "result.start_items.pickups.invalid");
            }
        }
    }
}
