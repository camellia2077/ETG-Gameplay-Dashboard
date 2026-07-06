// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Globalization;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private const string PlayerStatsCharacterSelectSceneName = "foyer";
        private const string PlayerStatsLoadingSceneName = "LoadingDungeon";

        private void DrawPlayerStatsPanelIfEnabled(PlayerController player)
        {
            if (!ShouldDrawPlayerStatsPanel(player))
            {
                return;
            }

            Rect panelRect = GetPlayerStatsPanelRect();
            GUI.Box(panelRect, GUIContent.none, _playerStatsPanelStyle);

            if ((object)player == null)
            {
                GUI.Label(
                    new Rect(panelRect.x + 12f, panelRect.y + 12f, panelRect.width - 24f, 22f),
                    GuiText.Get("result.common.player_not_ready"),
                    _hintStyle);
                return;
            }

            float rowY = panelRect.y + 12f;

            string healthText = "--";
            string armorText = "--";
            HealthHaver healthHaver = player.healthHaver;
            if ((object)healthHaver != null)
            {
                healthText = FormatNumber(healthHaver.GetCurrentHealth()) + "/" + FormatNumber(healthHaver.GetMaxHealth());
                armorText = FormatNumber(healthHaver.Armor);
            }

            DrawPlayerStatsRow(panelRect, ref rowY, "gui.command.stats.hp", healthText);
            DrawPlayerStatsRow(panelRect, ref rowY, "gui.command.stats.armor", armorText);
            DrawPlayerStatsRow(panelRect, ref rowY, "gui.command.stats.blanks", player.Blanks.ToString(CultureInfo.InvariantCulture));

            PlayerStats stats = player.stats;
            if ((object)stats == null)
            {
                GUI.Label(
                    new Rect(panelRect.x + 12f, rowY, panelRect.width - 24f, 22f),
                    GuiText.Get("result.common.stats_not_ready"),
                    _hintStyle);
                return;
            }

            DrawPlayerStatsRow(panelRect, ref rowY, "gui.command.stats.damage", FormatStat(stats, PlayerStats.StatType.Damage));
            DrawPlayerStatsRow(panelRect, ref rowY, "gui.command.stats.rate_of_fire", FormatStat(stats, PlayerStats.StatType.RateOfFire));
            DrawPlayerStatsRow(panelRect, ref rowY, "gui.command.stats.reload", FormatStat(stats, PlayerStats.StatType.ReloadSpeed));
            DrawPlayerStatsRow(panelRect, ref rowY, "gui.command.stats.move_speed", FormatStat(stats, PlayerStats.StatType.MovementSpeed));
            DrawPlayerStatsRow(panelRect, ref rowY, "gui.command.stats.shot_speed", FormatStat(stats, PlayerStats.StatType.ProjectileSpeed));
            DrawPlayerStatsRow(panelRect, ref rowY, "gui.command.stats.luck", FormatStat(stats, PlayerStats.StatType.Coolness));
            DrawPlayerStatsRow(panelRect, ref rowY, "gui.command.stats.curse", FormatStat(stats, PlayerStats.StatType.Curse));
            DrawCurrentGunStatsRows(panelRect, ref rowY, player);
        }

        private bool ShouldDrawPlayerStatsPanel(PlayerController player)
        {
            if (!_showPlayerStatsPanel || (object)player == null)
            {
                return false;
            }

            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager == null || gameManager.IsFoyer)
            {
                return false;
            }

            return !IsPlayerStatsBlockedScene(GetCurrentSceneName(gameManager));
        }

        private static bool IsPlayerStatsBlockedScene(string sceneName)
        {
            return string.Equals(sceneName, PlayerStatsCharacterSelectSceneName, StringComparison.Ordinal) ||
                   string.Equals(sceneName, PlayerStatsLoadingSceneName, StringComparison.Ordinal);
        }

        private static string GetCurrentSceneName(GameManager gameManager)
        {
            if ((object)gameManager != null)
            {
                try
                {
                    GameLevelDefinition levelDefinition = gameManager.GetLastLoadedLevelDefinition();
                    if (levelDefinition != null && !string.IsNullOrEmpty(levelDefinition.dungeonSceneName))
                    {
                        return SceneNameNormalizer.Normalize(levelDefinition.dungeonSceneName);
                    }
                }
                catch (NullReferenceException)
                {
                    // ETG can briefly expose a GameManager before its level definition is stable.
                }
            }

#pragma warning disable 618
            string sceneName = Application.loadedLevelName ?? string.Empty;
#pragma warning restore 618
            return SceneNameNormalizer.Normalize(sceneName);
        }

        private void DrawPlayerStatsRow(Rect panelRect, ref float rowY, string localizationKey, string value)
        {
            Rect rowRect = new Rect(panelRect.x + 8f, rowY - 1f, panelRect.width - 16f, PlayerStatsRowHeight + 2f);
            GUI.Box(rowRect, GUIContent.none, _playerStatsRowStyle);
            GUI.Label(
                new Rect(panelRect.x + 12f, rowY, panelRect.width - 24f, PlayerStatsRowHeight),
                GuiText.Get(localizationKey, value),
                _playerStatsTextStyle);
            rowY += PlayerStatsRowHeight + PlayerStatsRowGap;
        }

        private void DrawCurrentGunStatsRows(Rect panelRect, ref float rowY, PlayerController player)
        {
            Gun currentGun = (object)player != null ? player.CurrentGun : null;
            if ((object)currentGun == null)
            {
                DrawPlayerStatsRow(panelRect, ref rowY, "gui.command.stats.gun_shot", "--");
                DrawPlayerStatsRow(panelRect, ref rowY, "gui.command.stats.gun_reload", "--");
                return;
            }

            DrawPlayerStatsRow(panelRect, ref rowY, "gui.command.stats.gun_shot", FormatNumber(GetEstimatedSingleShotDamage(currentGun, player)));
            DrawPlayerStatsRow(panelRect, ref rowY, "gui.command.stats.gun_reload", FormatNumber(currentGun.AdjustedReloadTime));
        }

        private Rect GetPlayerStatsPanelRect()
        {
            float x = 12f;
            float scaledScreenHeight = GetScaledScreenHeight();
            float y = Mathf.Clamp((scaledScreenHeight - PlayerStatsPanelHeight) * 0.5f, 12f, Mathf.Max(12f, scaledScreenHeight - PlayerStatsPanelHeight - 12f));
            return new Rect(x, y, PlayerStatsPanelWidth, PlayerStatsPanelHeight);
        }

        private static float GetEstimatedSingleShotDamage(Gun gun, PlayerController player)
        {
            if ((object)gun == null)
            {
                return 0f;
            }

            float damage = 0f;
            AddVolleyDamage(ref damage, gun.Volley);
            if (damage <= 0f)
            {
                AddModuleDamage(ref damage, gun.DefaultModule);
            }

            PlayerStats stats = (object)player != null ? player.stats : null;
            if ((object)stats != null)
            {
                damage *= stats.GetStatValue(PlayerStats.StatType.Damage);
            }

            if (damage <= 0f && (object)gun.projectile != null && gun.projectile.baseData != null)
            {
                damage = gun.projectile.baseData.damage;
            }

            return damage;
        }

        private static void AddVolleyDamage(ref float damage, ProjectileVolleyData volley)
        {
            if ((object)volley == null || volley.projectiles == null)
            {
                return;
            }

            for (int index = 0; index < volley.projectiles.Count; index++)
            {
                AddModuleDamage(ref damage, volley.projectiles[index]);
            }
        }

        private static void AddModuleDamage(ref float damage, ProjectileModule module)
        {
            if ((object)module == null || module.projectiles == null || module.projectiles.Count == 0)
            {
                return;
            }

            Projectile projectile = module.projectiles[0];
            if ((object)projectile != null && projectile.baseData != null)
            {
                damage += projectile.baseData.damage;
            }
        }

        private static string FormatStat(PlayerStats stats, PlayerStats.StatType statType)
        {
            return FormatNumber(stats.GetStatValue(statType));
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

    }
}
