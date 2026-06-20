using System;
using System.Globalization;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private const BindingFlags PlayerIdentityFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const string PlayerStatsCharacterSelectSceneName = "tt_foyer";
        private const string PlayerStatsLegacyCharacterSelectSceneName = "tt_breach";
        private const string PlayerStatsLoadingSceneName = "LoadingDungeon";

        private void DrawPlayerStatsPanelIfEnabled(PlayerController player, ManualLogSource logger)
        {
            PlayerStatsVisibilityState visibility = GetPlayerStatsVisibilityState(player);
            LogPlayerStatsVisibility(visibility, logger);
            if (!visibility.ShouldDraw)
            {
                return;
            }

            Rect panelRect = GetPlayerStatsPanelRect();
            GUI.Box(panelRect, GUIContent.none, _panelStyle);

            GUI.Label(
                new Rect(panelRect.x + 12f, panelRect.y + 10f, panelRect.width - 24f, 22f),
                GuiText.Get("gui.command.stats.title"),
                _titleStyle);

            if ((object)player == null)
            {
                GUI.Label(
                    new Rect(panelRect.x + 12f, panelRect.y + 42f, panelRect.width - 24f, 22f),
                    GuiText.Get("result.common.player_not_ready"),
                    _hintStyle);
                return;
            }

            float rowY = panelRect.y + 42f;
            DrawPlayerStatsRow(panelRect, ref rowY, "gui.command.stats.character", GetCharacterDisplayName(player));

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

        private PlayerStatsVisibilityState GetPlayerStatsVisibilityState(PlayerController player)
        {
            GameManager gameManager = GameManager.Instance;
            string sceneName = GetLoadedSceneName();
            bool hasPlayer = (object)player != null;
            bool hasGameManager = (object)gameManager != null;
            bool isFoyer = hasGameManager && gameManager.IsFoyer;
            string currentRoomName = hasPlayer && (object)player.CurrentRoom != null ? GetRoomDebugLabel(player.CurrentRoom) : "<none>";
            string playerName = hasPlayer ? (player.name ?? string.Empty).Replace("(Clone)", string.Empty).Trim() : "<null>";

            string reason = "Visible";
            if ((object)player == null)
            {
                reason = "NoPlayer";
            }
            else if ((object)gameManager == null)
            {
                reason = "NoGameManager";
            }
            else if (gameManager.IsFoyer)
            {
                reason = "GameManagerIsFoyer";
            }
            else if (IsPlayerStatsBlockedScene(sceneName))
            {
                reason = "BlockedScene";
            }

            bool shouldDraw = _showPlayerStatsPanel && string.Equals(reason, "Visible", StringComparison.Ordinal);
            if (!_showPlayerStatsPanel)
            {
                reason = "ToggleOff";
            }

            return new PlayerStatsVisibilityState(
                shouldDraw,
                reason,
                sceneName,
                hasGameManager,
                isFoyer,
                hasPlayer,
                playerName,
                currentRoomName);
        }

        private void LogPlayerStatsVisibility(PlayerStatsVisibilityState visibility, ManualLogSource logger)
        {
            if (logger == null || visibility == null)
            {
                return;
            }

            string summary =
                "Player stats visibility: Enabled=" +
                _showPlayerStatsPanel +
                ", ShouldDraw=" +
                visibility.ShouldDraw +
                ", Reason=" +
                visibility.Reason +
                ", Scene=" +
                visibility.SceneName +
                ", HasGameManager=" +
                visibility.HasGameManager +
                ", IsFoyer=" +
                visibility.IsFoyer +
                ", HasPlayer=" +
                visibility.HasPlayer +
                ", Player=" +
                visibility.PlayerName +
                ", CurrentRoom=" +
                visibility.CurrentRoomName +
                ".";

            if (string.Equals(summary, _lastPlayerStatsVisibilityLog, StringComparison.Ordinal) &&
                Time.unscaledTime < _nextPlayerStatsVisibilityLogAt)
            {
                return;
            }

            _lastPlayerStatsVisibilityLog = summary;
            _nextPlayerStatsVisibilityLogAt = Time.unscaledTime + 3f;
            logger.LogInfo(RandomLoadoutLog.Command(summary));
        }

        private static bool IsPlayerStatsBlockedScene(string sceneName)
        {
            return string.Equals(sceneName, PlayerStatsCharacterSelectSceneName, StringComparison.Ordinal) ||
                   string.Equals(sceneName, PlayerStatsLegacyCharacterSelectSceneName, StringComparison.Ordinal) ||
                   string.Equals(sceneName, PlayerStatsLoadingSceneName, StringComparison.Ordinal);
        }

        private static string GetLoadedSceneName()
        {
#pragma warning disable 618
            string sceneName = Application.loadedLevelName ?? string.Empty;
#pragma warning restore 618
            return sceneName;
        }

        private static string GetRoomDebugLabel(object room)
        {
            return room != null ? room.ToString() : "<none>";
        }

        private void DrawPlayerStatsRow(Rect panelRect, ref float rowY, string localizationKey, string value)
        {
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

        private static Rect GetPlayerStatsPanelRect()
        {
            float x = 12f;
            float y = Mathf.Clamp((Screen.height - PlayerStatsPanelHeight) * 0.5f, 12f, Mathf.Max(12f, Screen.height - PlayerStatsPanelHeight - 12f));
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

        private static string GetCharacterDisplayName(PlayerController player)
        {
            string label = NormalizeCharacterLabel(ReadCharacterIdentityToken(player));
            if (string.IsNullOrEmpty(label))
            {
                label = NormalizeCharacterLabel(player.name);
            }

            if (!string.IsNullOrEmpty(label))
            {
                return GuiText.GetCharacterLabel(label);
            }

            string reflectedName = ReadStringMember(player, "OverrideDisplayName");
            if (!string.IsNullOrEmpty(reflectedName))
            {
                return reflectedName;
            }

            return !string.IsNullOrEmpty(player.name) ? player.name.Replace("(Clone)", string.Empty).Trim() : string.Empty;
        }

        private static string ReadCharacterIdentityToken(PlayerController player)
        {
            return ReadStringMember(player, "characterIdentity");
        }

        private static string ReadStringMember(object target, string memberName)
        {
            if (target == null || string.IsNullOrEmpty(memberName))
            {
                return string.Empty;
            }

            try
            {
                Type targetType = target.GetType();
                PropertyInfo property = targetType.GetProperty(memberName, PlayerIdentityFlags);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    object value = property.GetValue(target, null);
                    return value != null ? value.ToString() : string.Empty;
                }

                FieldInfo field = targetType.GetField(memberName, PlayerIdentityFlags);
                if (field != null)
                {
                    object value = field.GetValue(target);
                    return value != null ? value.ToString() : string.Empty;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string NormalizeCharacterLabel(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue))
            {
                return string.Empty;
            }

            string value = rawValue.Replace("(Clone)", string.Empty).Trim();
            string lower = value.ToLowerInvariant();
            if (lower.IndexOf("marine") >= 0 || lower.IndexOf("soldier") >= 0)
            {
                return "Marine";
            }

            if (lower.IndexOf("hunter") >= 0 || lower.IndexOf("guide") >= 0)
            {
                return "Hunter";
            }

            if (lower.IndexOf("pilot") >= 0 || lower.IndexOf("rogue") >= 0)
            {
                return "Pilot";
            }

            if (lower.IndexOf("convict") >= 0 || lower.IndexOf("ninja") >= 0)
            {
                return "Convict";
            }

            if (lower.IndexOf("robot") >= 0)
            {
                return "Robot";
            }

            if (lower.IndexOf("bullet") >= 0)
            {
                return "Bullet";
            }

            if (lower.IndexOf("eevee") >= 0 || lower.IndexOf("paradox") >= 0)
            {
                return "Paradox";
            }

            if (lower.IndexOf("gunslinger") >= 0)
            {
                return "Gunslinger";
            }

            return string.Empty;
        }

        private sealed class PlayerStatsVisibilityState
        {
            public PlayerStatsVisibilityState(
                bool shouldDraw,
                string reason,
                string sceneName,
                bool hasGameManager,
                bool isFoyer,
                bool hasPlayer,
                string playerName,
                string currentRoomName)
            {
                ShouldDraw = shouldDraw;
                Reason = reason ?? string.Empty;
                SceneName = sceneName ?? string.Empty;
                HasGameManager = hasGameManager;
                IsFoyer = isFoyer;
                HasPlayer = hasPlayer;
                PlayerName = playerName ?? string.Empty;
                CurrentRoomName = currentRoomName ?? string.Empty;
            }

            public bool ShouldDraw { get; private set; }

            public string Reason { get; private set; }

            public string SceneName { get; private set; }

            public bool HasGameManager { get; private set; }

            public bool IsFoyer { get; private set; }

            public bool HasPlayer { get; private set; }

            public string PlayerName { get; private set; }

            public string CurrentRoomName { get; private set; }
        }
    }
}
