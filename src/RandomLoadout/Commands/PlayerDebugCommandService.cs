namespace RandomLoadout
{
    internal sealed class PlayerDebugCommandService
    {
        private const float HalfHeartAmount = 0.5f;
        private const float SingleHeartAmount = 1f;
        private const float SingleArmorAmount = 1f;
        private const int SingleKeyAmount = 1;
        private const int SingleBlankAmount = 1;
        // Run currency (casings): drops during dungeon runs and is consumed in-run.
        private const int CurrencyBundleAmount = 50;
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
            healthHaver.SetHealthMaximum(nextMaxHealth, null, false);
            healthHaver.ApplyHealing(SingleHeartAmount);
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

        public GrantCommandExecutionResult RefillBlanks(PlayerController player)
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

            int targetBlankCount = stats.NumBlanksPerFloor;
            if (targetBlankCount <= 0)
            {
                return GrantCommandExecutionResult.Localized(false, "result.debug.no_blank_allotment");
            }

            if (player.Blanks >= targetBlankCount)
            {
                return GrantCommandExecutionResult.Localized(false, "result.debug.blanks_already_full");
            }

            player.Blanks = targetBlankCount;
            return GrantCommandExecutionResult.Localized(true, "result.debug.refill_blanks.success", targetBlankCount);
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
