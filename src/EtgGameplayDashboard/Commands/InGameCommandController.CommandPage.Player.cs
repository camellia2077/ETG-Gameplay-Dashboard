// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using BepInEx.Logging;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private static readonly ControllerFocusEntry[] PlayerSectionCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.player.section.character", 2, 0),
            new ControllerFocusEntry("cmd.player.section.combat", 2, 1),
            new ControllerFocusEntry("cmd.player.target", 2, 2),
        };

        private static readonly ControllerFocusEntry[] PlayerCharacterSectionCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.player.character.pickups", 3, 0),
            new ControllerFocusEntry("cmd.player.character.stats", 3, 1),
            new ControllerFocusEntry("cmd.player.character.projectiles", 3, 2),
        };

        private static readonly ControllerFocusEntry[] PlayerPickupCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.player.target", 4, 0),
            new ControllerFocusEntry("cmd.player.heal_half", 5, 0),
            new ControllerFocusEntry("cmd.player.full_heal", 5, 1),
            new ControllerFocusEntry("cmd.player.add_max_health", 5, 2),
            new ControllerFocusEntry("cmd.player.spawn_full_heart", 5, 3),
            new ControllerFocusEntry("cmd.player.add_armor", 6, 0),
            new ControllerFocusEntry("cmd.player.armor_no_consume", 6, 1),
            new ControllerFocusEntry("cmd.player.spawn_armor", 6, 2),
            new ControllerFocusEntry("cmd.player.add_blank", 7, 0),
            new ControllerFocusEntry("cmd.player.blank_no_consume", 7, 1),
            new ControllerFocusEntry("cmd.player.spawn_blank", 7, 2),
            new ControllerFocusEntry("cmd.player.add_key", 8, 0),
            new ControllerFocusEntry("cmd.player.key_no_consume", 8, 1),
            new ControllerFocusEntry("cmd.player.spawn_key", 8, 2),
            new ControllerFocusEntry("cmd.player.add_rat_key", 9, 0),
            new ControllerFocusEntry("cmd.player.spawn_rat_key", 9, 1),
            new ControllerFocusEntry("cmd.player.add_currency_large", 10, 0),
            new ControllerFocusEntry("cmd.player.currency_no_consume", 10, 1),
            new ControllerFocusEntry("cmd.player.clear_currency", 10, 2),
            new ControllerFocusEntry("cmd.player.spawn_currency", 10, 3),
        };

        private void DrawPlayerTargetButton(Rect contentRect, float buttonWidth, float controlHeight, ManualLogSource logger)
        {
            Rect targetRect = new Rect(contentRect.xMax - buttonWidth, contentRect.y, buttonWidth, controlHeight);
            GUIStyle targetStyle = _characterSwitchTarget == CharacterSwitchTarget.SecondaryPlayer
                ? _enabledButtonStyle
                : _buttonStyle;
            if (DrawControllerButton(targetRect, "cmd.player.target", GetCharacterSwitchTargetButtonLabel(), targetStyle))
            {
                ToggleCharacterSwitchTarget(logger);
            }
        }

        private static readonly ControllerFocusEntry[] PlayerCharacterStatsCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.player.damage_apply", 3, 0),
            new ControllerFocusEntry("cmd.player.damage_clear", 3, 1),
            new ControllerFocusEntry("cmd.player.movement_apply", 4, 0),
            new ControllerFocusEntry("cmd.player.movement_clear", 4, 1),
            new ControllerFocusEntry("cmd.player.coolness_apply", 5, 0),
            new ControllerFocusEntry("cmd.player.coolness_clear", 5, 1),
            new ControllerFocusEntry("cmd.player.magnificence_apply", 6, 0),
            new ControllerFocusEntry("cmd.player.magnificence_clear", 6, 1),
            new ControllerFocusEntry("cmd.player.curse_apply", 7, 0),
            new ControllerFocusEntry("cmd.player.curse_clear", 7, 1),
        };

        private static readonly ControllerFocusEntry[] PlayerCharacterProjectileCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.player.bullet_size_apply", 3, 0),
            new ControllerFocusEntry("cmd.player.bullet_size_clear", 3, 1),
            new ControllerFocusEntry("cmd.player.bullet_speed_apply", 4, 0),
            new ControllerFocusEntry("cmd.player.bullet_speed_clear", 4, 1),
            new ControllerFocusEntry("cmd.player.reload_speed_apply", 5, 0),
            new ControllerFocusEntry("cmd.player.reload_speed_clear", 5, 1),
            new ControllerFocusEntry("cmd.player.accuracy_apply", 6, 0),
            new ControllerFocusEntry("cmd.player.accuracy_clear", 6, 1),
        };

        private static readonly ControllerFocusEntry[] PlayerCombatCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.combat.rapid", 3, 0),
            new ControllerFocusEntry("cmd.combat.auto_reload", 3, 1),
            new ControllerFocusEntry("cmd.combat.ammo_mode", 4, 0),
            new ControllerFocusEntry("cmd.combat.invincible", 4, 1),
            new ControllerFocusEntry("cmd.combat.ammonomicon", 5, 0),
            new ControllerFocusEntry("cmd.combat.enemy_health_bars", 5, 1),
            new ControllerFocusEntry("cmd.combat.controller_aim_lock", 7, 0),
            new ControllerFocusEntry(KeyboardAimAssistUiDefinition.ModeControlId, 8, 0),
            new ControllerFocusEntry(KeyboardAimAssistUiDefinition.MultiplierControlId, 8, 1),
            new ControllerFocusEntry("cmd.combat.boss_intro", 6, 0),
            new ControllerFocusEntry("cmd.combat.full_ammo", 6, 1),
            new ControllerFocusEntry("cmd.combat.skip_charge", 8, 1),
            new ControllerFocusEntry("cmd.combat.active_item_no_cooldown", 7, 1),
        };

        private static readonly ControllerFocusEntry[] PlayerCombatStandardCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.combat.rapid", 3, 0),
            new ControllerFocusEntry("cmd.combat.auto_reload", 3, 1),
            new ControllerFocusEntry("cmd.combat.ammo_mode", 4, 0),
            new ControllerFocusEntry("cmd.combat.invincible", 4, 1),
            new ControllerFocusEntry("cmd.combat.ammonomicon", 5, 0),
            new ControllerFocusEntry("cmd.combat.enemy_health_bars", 5, 1),
            new ControllerFocusEntry("cmd.combat.controller_aim_lock", 7, 0),
            new ControllerFocusEntry(KeyboardAimAssistUiDefinition.ModeControlId, 8, 0),
            new ControllerFocusEntry(KeyboardAimAssistUiDefinition.MultiplierControlId, 8, 1),
            new ControllerFocusEntry("cmd.combat.full_ammo", 6, 1),
            new ControllerFocusEntry("cmd.combat.skip_charge", 8, 1),
            new ControllerFocusEntry("cmd.combat.active_item_no_cooldown", 7, 1),
        };

        private void DrawPlayerContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            const float sectionButtonWidth = 92f;
            const float sectionButtonHeight = 28f;
            Rect characterSectionRect = new Rect(contentRect.x, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect combatSectionRect = new Rect(characterSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            long stageStartedAtTimestamp = BeginCommandPanelPerformanceStage();
            DrawPlayerSectionButton(characterSectionRect, "cmd.player.section.character", PlayerMenuSection.Character, GetLocalizedFallback("gui.command.player.section.character", "Character", "角色"));
            DrawPlayerSectionButton(combatSectionRect, "cmd.player.section.combat", PlayerMenuSection.Combat, GetLocalizedFallback("gui.command.player.section.combat", "Combat", "战斗"));
            DrawPlayerTargetButton(contentRect, buttonWidth, controlHeight, logger);
            LogCommandPanelPerformanceStage("CommandPage.Player.TargetAndSections", stageStartedAtTimestamp);

            Rect subsectionContentRect = new Rect(contentRect.x, contentRect.y + sectionButtonHeight + 8f, contentRect.width, contentRect.height - sectionButtonHeight - 8f);
            if (_playerMenuSection == PlayerMenuSection.Combat)
            {
                stageStartedAtTimestamp = BeginCommandPanelPerformanceStage();
                DrawPlayerCombatContent(subsectionContentRect, buttonWidth, controlHeight, player, logger);
                LogCommandPanelPerformanceStage("CommandPage.Player.CombatContent", stageStartedAtTimestamp);
                return;
            }

            Rect pickupsSectionRect = new Rect(contentRect.x, subsectionContentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect statsSectionRect = new Rect(pickupsSectionRect.xMax + 2f, subsectionContentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect projectilesSectionRect = new Rect(statsSectionRect.xMax + 2f, subsectionContentRect.y, sectionButtonWidth, sectionButtonHeight);
            stageStartedAtTimestamp = BeginCommandPanelPerformanceStage();
            DrawCharacterSectionButton(pickupsSectionRect, "cmd.player.character.pickups", CharacterMenuSection.Pickups, GetLocalizedFallback("gui.command.player.section.pickups", "Pickups", "拾取物"));
            DrawCharacterSectionButton(statsSectionRect, "cmd.player.character.stats", CharacterMenuSection.Stats, GetLocalizedFallback("gui.command.player.section.stats", "Stats", "属性"));
            DrawCharacterSectionButton(projectilesSectionRect, "cmd.player.character.projectiles", CharacterMenuSection.Projectiles, GetLocalizedFallback("gui.command.player.section.projectiles", "Projectiles", "子弹"));
            LogCommandPanelPerformanceStage("CommandPage.Player.CharacterSubsections", stageStartedAtTimestamp);

            Rect sectionContentRect = new Rect(contentRect.x, subsectionContentRect.y + sectionButtonHeight + 8f, contentRect.width, subsectionContentRect.height - sectionButtonHeight - 8f);
            if (_characterMenuSection == CharacterMenuSection.Stats)
            {
                stageStartedAtTimestamp = BeginCommandPanelPerformanceStage();
                DrawPlayerCharacterStatsContent(sectionContentRect, buttonWidth, controlHeight, player, logger);
                LogCommandPanelPerformanceStage("CommandPage.Player.CharacterStatsContent", stageStartedAtTimestamp);
                return;
            }

            if (_characterMenuSection == CharacterMenuSection.Projectiles)
            {
                stageStartedAtTimestamp = BeginCommandPanelPerformanceStage();
                DrawPlayerProjectileContent(sectionContentRect, buttonWidth, controlHeight, player, logger);
                LogCommandPanelPerformanceStage("CommandPage.Player.CharacterProjectileContent", stageStartedAtTimestamp);
                return;
            }

            const float actionRowHeight = 38f;
            stageStartedAtTimestamp = BeginCommandPanelPerformanceStage();
            PickupActionRowDefinition[] pickupRows = BuildPlayerPickupRows(player, logger);
            LogCommandPanelPerformanceStage("CommandPage.Player.PickupRows.Build", stageStartedAtTimestamp);
            float pickupRowsTop = sectionContentRect.y + controlHeight + ButtonGap;
            Rect shortcutConfigurationButtonRect = new Rect(
                sectionContentRect.xMax - buttonWidth,
                sectionContentRect.y,
                buttonWidth,
                controlHeight);
            if (_isPickupShortcutConfigurationMode && DrawControllerButton(
                shortcutConfigurationButtonRect,
                "cmd.player.pickups.shortcuts.back",
                GetPickupShortcutExitConfigurationButtonLabel(),
                _buttonStyle))
            {
                CancelPickupShortcutCapture();
                _isPickupShortcutConfigurationMode = false;
            }
            else if (!_isPickupShortcutConfigurationMode && DrawControllerButton(
                shortcutConfigurationButtonRect,
                "cmd.player.pickups.shortcuts",
                GetPickupShortcutConfigurationButtonLabel(),
                _buttonStyle))
            {
                TogglePickupShortcutConfigurationMode();
            }
            stageStartedAtTimestamp = BeginCommandPanelPerformanceStage();
            DrawPickupActionRows(sectionContentRect, pickupRowsTop, actionRowHeight, ButtonGap, pickupRows);
            LogCommandPanelPerformanceStage("CommandPage.Player.PickupRows.Draw", stageStartedAtTimestamp);
        }

        private void DrawPlayerCharacterStatsContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            PlayerController targetPlayer = GetSelectedCommandTargetPlayer() ?? player;
            EnsurePlayerStatsEditText(targetPlayer);
            if (DrawPlayerIntegerStatEditor(
                contentRect,
                buttonWidth,
                controlHeight,
                0f,
                "gui.player.stats.damage_multiplier",
                ref _damageEditText,
                "cmd.player.damage_apply",
                CommandInfoPage.DamageMultiplier,
                delegate { ExecuteApplyDamageMultiplier(targetPlayer, logger); }))
            {
                return;
            }

            if (DrawPlayerIntegerStatEditor(
                contentRect,
                buttonWidth,
                controlHeight,
                controlHeight + ButtonGap,
                "gui.player.stats.move_multiplier",
                ref _movementEditText,
                "cmd.player.movement_apply",
                CommandInfoPage.MovementMultiplier,
                delegate { ExecuteApplyMovementMultiplier(targetPlayer, logger); }))
            {
                return;
            }
            if (DrawPlayerIntegerStatEditor(
                contentRect,
                buttonWidth,
                controlHeight,
                (controlHeight + ButtonGap) * 2f,
                "gui.command.stats.coolness",
                ref _coolnessEditText,
                "cmd.player.coolness_apply",
                CommandInfoPage.Coolness,
                delegate { ExecuteApplyCoolnessValue(targetPlayer, logger); }))
            {
                return;
            }
            if (DrawPlayerIntegerStatEditor(
                contentRect,
                buttonWidth,
                controlHeight,
                (controlHeight + ButtonGap) * 3f,
                "gui.command.stats.magnificence",
                ref _magnificenceEditText,
                "cmd.player.magnificence_apply",
                CommandInfoPage.Magnificence,
                delegate { ExecuteApplyMagnificenceValue(targetPlayer, logger); }))
            {
                return;
            }
            if (DrawPlayerIntegerStatEditor(
                contentRect,
                buttonWidth,
                controlHeight,
                (controlHeight + ButtonGap) * 4f,
                "gui.command.stats.curse",
                ref _curseEditText,
                "cmd.player.curse_apply",
                CommandInfoPage.Curse,
                delegate { ExecuteApplyCurseValue(targetPlayer, logger); }))
            {
                return;
            }
        }

        private bool DrawPlayerIntegerStatEditor(
            Rect contentRect,
            float buttonWidth,
            float controlHeight,
            float rowOffset,
            string labelKey,
            ref string editText,
            string controlId,
            CommandInfoPage infoPage,
            Action applyAction)
        {
            const float labelWidth = 150f;
            const float infoButtonWidth = 32f;
            const float fieldWidth = 72f;
            Rect rowRect = new Rect(contentRect.x, contentRect.y + rowOffset, contentRect.width, controlHeight);
            GUI.Label(new Rect(rowRect.x, rowRect.y, labelWidth, controlHeight), GuiText.Get(labelKey, string.Empty), _buttonStyle);
            if (DrawInfoButton(
                new Rect(rowRect.x + labelWidth + ButtonGap, rowRect.y, infoButtonWidth, controlHeight),
                infoPage))
            {
                return true;
            }

            float fieldX = rowRect.x + labelWidth + infoButtonWidth + (ButtonGap * 2f);
            editText = GUI.TextField(new Rect(fieldX, rowRect.y, fieldWidth, controlHeight), editText, 6, _textFieldStyle);
            if (DrawControllerButton(
                new Rect(fieldX + fieldWidth + ButtonGap, rowRect.y, buttonWidth, controlHeight),
                controlId,
                GuiText.Get("gui.command.stats.apply"),
                _buttonStyle))
            {
                applyAction();
            }

            if (DrawControllerButton(
                new Rect(fieldX + fieldWidth + ButtonGap + buttonWidth + ButtonGap, rowRect.y, 62f, controlHeight),
                controlId.Replace("_apply", "_clear"),
                GuiText.Get("gui.command.stats.clear"),
                _buttonStyle))
            {
                editText = controlId == "cmd.player.damage_apply" || controlId == "cmd.player.movement_apply" || controlId == "cmd.player.bullet_size_apply" || controlId == "cmd.player.bullet_speed_apply" ? "1" : "0";
                applyAction();
            }

            return false;
        }

        private void EnsurePlayerStatsEditText(PlayerController player)
        {
            if ((object)player == (object)_playerStatsEditPlayer)
            {
                return;
            }

            _playerStatsEditPlayer = player;
            PlayerStats stats = (object)player != null ? player.stats : null;
            _coolnessEditText = stats != null ? Mathf.RoundToInt(stats.GetStatValue(PlayerStats.StatType.Coolness)).ToString(System.Globalization.CultureInfo.InvariantCulture) : "0";
            _curseEditText = stats != null ? Mathf.RoundToInt(stats.GetStatValue(PlayerStats.StatType.Curse)).ToString(System.Globalization.CultureInfo.InvariantCulture) : "0";
            _magnificenceEditText = stats != null ? Mathf.RoundToInt(stats.Magnificence).ToString(System.Globalization.CultureInfo.InvariantCulture) : "0";
            _damageEditText = _playerRuntimeStatOverrideService != null ? _playerRuntimeStatOverrideService.GetDamageMultiplier(player).ToString(System.Globalization.CultureInfo.InvariantCulture) : "1";
            _movementEditText = _playerRuntimeStatOverrideService != null ? _playerRuntimeStatOverrideService.GetMovementMultiplier(player).ToString(System.Globalization.CultureInfo.InvariantCulture) : "1";
            _bulletSizeEditText = _projectileModifierService != null ? _projectileModifierService.GetBulletSizeMultiplier(player).ToString(System.Globalization.CultureInfo.InvariantCulture) : "1";
            _bulletSpeedEditText = _projectileModifierService != null ? _projectileModifierService.GetBulletSpeedMultiplier(player).ToString(System.Globalization.CultureInfo.InvariantCulture) : "1";
            _reloadSpeedEditText = _projectileModifierService != null ? _projectileModifierService.GetReloadSpeedMultiplier(player).ToString("0.0###", System.Globalization.CultureInfo.InvariantCulture) : "1.0";
            _accuracyEditText = stats != null ? Mathf.RoundToInt(stats.GetStatValue(PlayerStats.StatType.Accuracy)).ToString(System.Globalization.CultureInfo.InvariantCulture) : "0";
        }

        private void DrawPlayerProjectileContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            PlayerController targetPlayer = GetSelectedCommandTargetPlayer() ?? player;
            EnsurePlayerStatsEditText(targetPlayer);
            if (DrawPlayerIntegerStatEditor(contentRect, buttonWidth, controlHeight, 0f, "gui.player.projectiles.size", ref _bulletSizeEditText, "cmd.player.bullet_size_apply", CommandInfoPage.BulletSize, delegate { ExecuteApplyBulletSize(targetPlayer, logger); }))
            {
                return;
            }

            if (DrawPlayerIntegerStatEditor(contentRect, buttonWidth, controlHeight, controlHeight + ButtonGap, "gui.player.projectiles.speed", ref _bulletSpeedEditText, "cmd.player.bullet_speed_apply", CommandInfoPage.BulletSpeed, delegate { ExecuteApplyBulletSpeed(targetPlayer, logger); }))
            {
                return;
            }

            if (DrawPlayerFloatMultiplierEditor(contentRect, buttonWidth, controlHeight, (controlHeight + ButtonGap) * 2f, "gui.player.projectiles.reload_speed", ref _reloadSpeedEditText, "cmd.player.reload_speed_apply", CommandInfoPage.ReloadSpeed, delegate { ExecuteApplyReloadSpeed(targetPlayer, logger); }))
            {
                return;
            }

            DrawPlayerIntegerStatEditor(contentRect, buttonWidth, controlHeight, (controlHeight + ButtonGap) * 3f, "gui.player.projectiles.accuracy", ref _accuracyEditText, "cmd.player.accuracy_apply", CommandInfoPage.Accuracy, delegate { ExecuteApplyAccuracy(targetPlayer, logger); });
        }

        private bool DrawPlayerFloatMultiplierEditor(
            Rect contentRect,
            float buttonWidth,
            float controlHeight,
            float rowOffset,
            string labelKey,
            ref string editText,
            string controlId,
            CommandInfoPage infoPage,
            Action applyAction)
        {
            const float labelWidth = 150f;
            const float infoButtonWidth = 32f;
            const float fieldWidth = 72f;
            Rect rowRect = new Rect(contentRect.x, contentRect.y + rowOffset, contentRect.width, controlHeight);
            GUI.Label(new Rect(rowRect.x, rowRect.y, labelWidth, controlHeight), GuiText.Get(labelKey, string.Empty), _buttonStyle);
            if (DrawInfoButton(new Rect(rowRect.x + labelWidth + ButtonGap, rowRect.y, infoButtonWidth, controlHeight), infoPage))
            {
                return true;
            }

            float fieldX = rowRect.x + labelWidth + infoButtonWidth + (ButtonGap * 2f);
            editText = GUI.TextField(new Rect(fieldX, rowRect.y, fieldWidth, controlHeight), editText, 8, _textFieldStyle);
            if (DrawControllerButton(new Rect(fieldX + fieldWidth + ButtonGap, rowRect.y, buttonWidth, controlHeight), controlId, GuiText.Get("gui.command.stats.apply"), _buttonStyle))
            {
                applyAction();
            }

            if (DrawControllerButton(new Rect(fieldX + fieldWidth + ButtonGap + buttonWidth + ButtonGap, rowRect.y, 62f, controlHeight), controlId.Replace("_apply", "_clear"), GuiText.Get("gui.command.stats.clear"), _buttonStyle))
            {
                editText = "1.0";
                applyAction();
            }

            return false;
        }

        private void ExecuteApplyBulletSize(PlayerController player, ManualLogSource logger)
        {
            ApplyPlayerBulletMultiplierValue(player, logger, _bulletSizeEditText, true);
        }

        private void ExecuteApplyBulletSpeed(PlayerController player, ManualLogSource logger)
        {
            ApplyPlayerBulletMultiplierValue(player, logger, _bulletSpeedEditText, false);
        }

        private void ExecuteApplyReloadSpeed(PlayerController player, ManualLogSource logger)
        {
            if (_projectileModifierService == null || (object)player == null)
            {
                ShowStatus(GuiText.Get("result.common.player_not_ready"), true);
                return;
            }

            float value;
            if (!TryParsePlayerFloatRange(_reloadSpeedEditText, 0.25f, 4.0f, out value))
            {
                ShowStatus(GuiText.Get("result.player.projectiles.invalid_reload_speed"), true);
                return;
            }

            _projectileModifierService.SetReloadSpeedMultiplier(player, value);
            _reloadSpeedEditText = value.ToString("0.0###", System.Globalization.CultureInfo.InvariantCulture);
            ShowStatus(GuiText.Get("result.player.projectiles.reload_speed_set", _reloadSpeedEditText), false);
            LogPlayerStatValue(logger, "Reload speed multiplier x", _reloadSpeedEditText);
        }


        private void ExecuteApplyAccuracy(PlayerController player, ManualLogSource logger)
        {
            if (_projectileModifierService == null || (object)player == null)
            {
                ShowStatus(GuiText.Get("result.common.player_not_ready"), true);
                return;
            }

            int value;
            if (!TryParsePlayerStatInteger(_accuracyEditText, out value))
            {
                ShowStatus(GuiText.Get("result.player.stats.invalid_integer"), true);
                return;
            }

            _projectileModifierService.SetAccuracyValue(player, value);
            _accuracyEditText = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ShowStatus(GuiText.Get("result.player.projectiles.accuracy_set", value), false);
            LogPlayerStatValue(logger, "Accuracy ", value);
        }

        private void ApplyPlayerBulletMultiplierValue(PlayerController player, ManualLogSource logger, string text, bool size)
        {
            if (_projectileModifierService == null || (object)player == null)
            {
                ShowStatus(GuiText.Get("result.common.player_not_ready"), true);
                return;
            }

            int value;
            if (size ? !TryParsePlayerIntegerRange(text, 1, 30, out value) : !TryParsePlayerMultiplierInteger(text, out value))
            {
                ShowStatus(GuiText.Get(size ? "result.player.projectiles.invalid_size" : "result.player.stats.invalid_multiplier"), true);
                return;
            }

            if (size)
            {
                _projectileModifierService.SetBulletSizeMultiplier(player, value);
                _bulletSizeEditText = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                _projectileModifierService.SetBulletSpeedMultiplier(player, value);
                _bulletSpeedEditText = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            string resultKey = size ? "result.player.projectiles.size_set" : "result.player.projectiles.speed_set";
            ShowStatus(GuiText.Get(resultKey, value), false);
            LogPlayerStatValue(logger, size ? "Bullet size multiplier x" : "Bullet speed multiplier x", value);
        }

        private void DrawPlayerCombatContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            // The former top-level Combat page now lives under Player -> Combat.
            DrawCombatSettings(contentRect, controlHeight, player, logger);
        }

        private void ExecuteToggleInvincibilityForSelectedTargets(PlayerController fallbackPlayer, ManualLogSource logger)
        {
            ExecuteToggleInvincibility(GetSelectedCommandTargetPlayer() ?? fallbackPlayer, logger);
        }

        private PickupActionRowDefinition[] BuildPlayerPickupRows(PlayerController player, ManualLogSource logger)
        {
            if (_isPickupShortcutConfigurationMode)
            {
                return BuildPlayerPickupShortcutRows();
            }

            return new[]
            {
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteHealthPickup,
                    GetLocalizedFallback("gui.command.label.health", "Health", "血量"),
                    new[]
                    {
                        new PickupActionButtonDefinition("cmd.player.heal_half", GuiText.Get("gui.command.player.health.heal_half"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteHealHalfHeart(targetPlayer, logger); }); }, _buttonStyle),
                        new PickupActionButtonDefinition("cmd.player.full_heal", GuiText.Get("gui.command.player.health.full_heal"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteFullHeal(targetPlayer, logger); }); }, _buttonStyle),
                        new PickupActionButtonDefinition("cmd.player.add_max_health", GuiText.Get("gui.command.player.health.add_max"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddMaxHealth(targetPlayer, logger); }); }, _buttonStyle),
                        new PickupActionButtonDefinition("cmd.player.spawn_full_heart", GuiText.Get("gui.command.action.spawn"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteSpawnFullHeartNearPlayer(targetPlayer, logger); }); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteArmorPickup,
                    GetLocalizedFallback("gui.command.label.armor", "Armor", "护甲"),
                    new[]
                    {
                        new PickupActionButtonDefinition("cmd.player.add_armor", GuiText.Get("gui.command.player.armor.add_one"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddArmor(targetPlayer, logger); }); }, _buttonStyle),
                        // Armor no-consume is player-specific: when the target is P2, the
                        // enable operation must seed P2's baseline armor, not P1's.
                        new PickupActionButtonDefinition("cmd.player.armor_no_consume", GetNoConsumeActionLabel(_armorNoConsumeToggleService.IsEnabled), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteToggleArmorNoConsume(targetPlayer, logger); }); }, GetNoConsumeActionStyle(_armorNoConsumeToggleService.IsEnabled)),
                        new PickupActionButtonDefinition("cmd.player.spawn_armor", GuiText.Get("gui.command.action.spawn"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteSpawnArmorNearPlayer(targetPlayer, logger); }); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteBlankPickup,
                    GetLocalizedFallback("gui.command.label.blank", "Blank", "空响弹"),
                    new[]
                    {
                        new PickupActionButtonDefinition("cmd.player.add_blank", GuiText.Get("gui.command.player.blank.add_one"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddBlank(targetPlayer, logger); }); }, _buttonStyle),
                        new PickupActionButtonDefinition("cmd.player.blank_no_consume", GetNoConsumeActionLabel(_blankNoConsumeToggleService.IsEnabled), delegate { ExecuteToggleBlankNoConsume(player, logger); }, GetNoConsumeActionStyle(_blankNoConsumeToggleService.IsEnabled)),
                        new PickupActionButtonDefinition("cmd.player.spawn_blank", GuiText.Get("gui.command.player.blank.spawn"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteSpawnBlankNearPlayer(targetPlayer, logger); }); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteKeyPickup,
                    GetLocalizedFallback("gui.command.label.key", "Key", "钥匙"),
                    new[]
                    {
                        new PickupActionButtonDefinition("cmd.player.add_key", GetLocalizedFallback("gui.command.action.add_one", "+1", "+1"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddKey(targetPlayer, logger); }); }, _buttonStyle),
                        new PickupActionButtonDefinition("cmd.player.key_no_consume", GetNoConsumeActionLabel(_keyNoConsumeToggleService.IsEnabled), delegate { ExecuteToggleKeyNoConsume(player, logger); }, GetNoConsumeActionStyle(_keyNoConsumeToggleService.IsEnabled)),
                        new PickupActionButtonDefinition("cmd.player.spawn_key", GuiText.Get("gui.command.action.spawn"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteSpawnKeyNearPlayer(targetPlayer, logger); }); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteRatRewardKeyPickup,
                    GetLocalizedFallback("gui.command.label.rat_key", "Rat Key", "老鼠钥匙"),
                    new[]
                    {
                        new PickupActionButtonDefinition("cmd.player.add_rat_key", GetLocalizedFallback("gui.command.action.add_one", "+1", "+1"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddRatKey(targetPlayer, logger); }); }, _buttonStyle),
                        new PickupActionButtonDefinition("cmd.player.spawn_rat_key", GuiText.Get("gui.command.action.spawn"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteSpawnRatKeyNearPlayer(targetPlayer, logger); }); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteCasingsPickup,
                    GetLocalizedFallback("gui.command.label.casings", "Casings", "弹壳"),
                    new[]
                    {
                        new PickupActionButtonDefinition("cmd.player.add_currency_large", GetLocalizedFallback("gui.command.action.add_hundred", "+100", "+100"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddLargeCurrency(targetPlayer, logger); }); }, _buttonStyle),
                        new PickupActionButtonDefinition("cmd.player.currency_no_consume", GetNoConsumeActionLabel(_currencyNoConsumeToggleService.IsEnabled), delegate { ExecuteToggleCurrencyNoConsume(player, logger); }, GetNoConsumeActionStyle(_currencyNoConsumeToggleService.IsEnabled)),
                        new PickupActionButtonDefinition("cmd.player.clear_currency", GetLocalizedFallback("gui.command.currency.button.clear", "Clear", "清除"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteClearCurrency(targetPlayer, logger); }); }, _buttonStyle),
                        new PickupActionButtonDefinition("cmd.player.spawn_currency", GuiText.Get("gui.command.action.spawn"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteSpawnCurrencyNearPlayer(targetPlayer, logger); }); }, _buttonStyle),
                    }),
            };
        }

        private void DrawPlayerSectionButton(Rect rect, string controlId, PlayerMenuSection section, string label)
        {
            GUIStyle style = _playerMenuSection == section ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle;
            if (DrawControllerButton(rect, controlId, label, style))
            {
                CancelPickupShortcutCapture();
                _isPickupShortcutConfigurationMode = false;
                _playerMenuSection = section;
            }
        }

        private void ExecuteApplyDamageMultiplier(PlayerController player, ManualLogSource logger)
        {
            ApplyPlayerMultiplierValue(player, logger, _damageEditText, true);
        }

        private void ExecuteClearDamageMultiplier(PlayerController player, ManualLogSource logger)
        {
            _damageEditText = "1";
            ExecuteApplyDamageMultiplier(player, logger);
        }

        private void ExecuteApplyMovementMultiplier(PlayerController player, ManualLogSource logger)
        {
            ApplyPlayerMultiplierValue(player, logger, _movementEditText, false);
        }

        private void ExecuteClearMovementMultiplier(PlayerController player, ManualLogSource logger)
        {
            _movementEditText = "1";
            ExecuteApplyMovementMultiplier(player, logger);
        }

        private void ApplyPlayerMultiplierValue(PlayerController player, ManualLogSource logger, string text, bool damage)
        {
            if (_playerRuntimeStatOverrideService == null || (object)player == null)
            {
                ShowStatus(GuiText.Get("result.common.player_not_ready"), true);
                return;
            }

            int value;
            if (!TryParsePlayerMultiplierInteger(text, out value))
            {
                ShowStatus(GuiText.Get("result.player.stats.invalid_multiplier"), true);
                return;
            }

            if (damage)
            {
                _playerRuntimeStatOverrideService.SetDamageMultiplier(player, value);
                _damageEditText = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                _playerRuntimeStatOverrideService.SetMovementMultiplier(player, value);
                _movementEditText = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            string resultKey = damage ? "result.player.stats.damage_multiplier" : "result.player.stats.move_multiplier";
            ShowStatus(GuiText.Get(resultKey, value.ToString(System.Globalization.CultureInfo.InvariantCulture)), false);
            LogPlayerStatValue(logger, damage ? "Damage multiplier x" : "Movement multiplier x", value);
        }

        private static bool TryParsePlayerMultiplierInteger(string text, out int value)
        {
            return TryParsePlayerIntegerRange(text, 1, 999, out value);
        }

        private static bool TryParsePlayerIntegerRange(string text, int minimum, int maximum, out int value)
        {
            return int.TryParse(
                (text ?? string.Empty).Trim(),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out value) && value >= minimum && value <= maximum;
        }

        private static bool TryParsePlayerFloatRange(string text, float minimum, float maximum, out float value)
        {
            return float.TryParse(
                (text ?? string.Empty).Trim(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out value) && !float.IsNaN(value) && !float.IsInfinity(value) && value >= minimum && value <= maximum;
        }


        private void ExecuteApplyCoolnessValue(PlayerController player, ManualLogSource logger)
        {
            if (_playerRuntimeStatOverrideService == null || (object)player == null)
            {
                ShowStatus(GuiText.Get("result.common.player_not_ready"), true);
                return;
            }

            int value;
            if (!TryParsePlayerStatInteger(_coolnessEditText, out value))
            {
                ShowStatus(GuiText.Get("result.player.stats.invalid_integer"), true);
                return;
            }

            _playerRuntimeStatOverrideService.SetCoolnessValue(player, value);
            _coolnessEditText = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ShowStatus(GuiText.Get("result.player.stats.coolness_set", value), false);
            LogPlayerStatValue(logger, "Coolness", value);
        }

        private void ExecuteClearCoolnessValue(PlayerController player, ManualLogSource logger)
        {
            _coolnessEditText = "0";
            ExecuteApplyCoolnessValue(player, logger);
        }

        private void ExecuteApplyCurseValue(PlayerController player, ManualLogSource logger)
        {
            if (_playerRuntimeStatOverrideService == null || (object)player == null)
            {
                ShowStatus(GuiText.Get("result.common.player_not_ready"), true);
                return;
            }

            int value;
            if (!TryParsePlayerStatInteger(_curseEditText, out value))
            {
                ShowStatus(GuiText.Get("result.player.stats.invalid_integer"), true);
                return;
            }

            _playerRuntimeStatOverrideService.SetCurseValue(player, value);
            _curseEditText = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ShowStatus(GuiText.Get("result.player.stats.curse_set", value), false);
            LogPlayerStatValue(logger, "Curse", value);
        }

        private void ExecuteClearCurseValue(PlayerController player, ManualLogSource logger)
        {
            _curseEditText = "0";
            ExecuteApplyCurseValue(player, logger);
        }

        private void ExecuteApplyMagnificenceValue(PlayerController player, ManualLogSource logger)
        {
            if (_playerRuntimeStatOverrideService == null || (object)player == null || (object)player.stats == null)
            {
                ShowStatus(GuiText.Get("result.common.player_not_ready"), true);
                return;
            }

            int value;
            if (!TryParsePlayerStatInteger(_magnificenceEditText, out value))
            {
                ShowStatus(GuiText.Get("result.player.stats.invalid_integer"), true);
                return;
            }

            PlayerRuntimeStatOverrideService.SetMagnificenceValue(player, value);
            _magnificenceEditText = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ShowStatus(GuiText.Get("result.player.stats.magnificence_set", value), false);
            LogPlayerStatValue(logger, "Magnificence", value);
        }

        private void ExecuteClearMagnificenceValue(PlayerController player, ManualLogSource logger)
        {
            _magnificenceEditText = "0";
            ExecuteApplyMagnificenceValue(player, logger);
        }

        private static bool TryParsePlayerStatInteger(string text, out int value)
        {
            return int.TryParse(
                (text ?? string.Empty).Trim(),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out value) && value >= 0 && value <= 999;
        }

        private static void LogPlayerStatValue(ManualLogSource logger, string statName, int value)
        {
            if (logger != null)
            {
                logger.LogInfo(EtgGameplayDashboardLog.Command("Player " + statName + " set to " + value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "."));
            }
        }

        private static void LogPlayerStatValue(ManualLogSource logger, string statName, string value)
        {
            if (logger != null)
            {
                logger.LogInfo(EtgGameplayDashboardLog.Command("Player " + statName + " set to " + (value ?? string.Empty) + "."));
            }
        }

        private void DrawCharacterSectionButton(Rect rect, string controlId, CharacterMenuSection section, string label)
        {
            GUIStyle style = _characterMenuSection == section ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle;
            if (DrawControllerButton(rect, controlId, label, style))
            {
                CancelPickupShortcutCapture();
                _isPickupShortcutConfigurationMode = false;
                _characterMenuSection = section;
            }
        }

        private ControllerFocusEntry[] GetPlayerCommandPageFocusEntries()
        {
            if (_playerMenuSection == PlayerMenuSection.Combat)
            {
                ControllerFocusEntry[] combatEntries = IsExperimentalModeEnabled()
                    ? PlayerCombatCommandPageFocusEntries
                    : PlayerCombatStandardCommandPageFocusEntries;
                return BuildCommandPageFocusEntries(PlayerSectionCommandPageFocusEntries, combatEntries);
            }

            ControllerFocusEntry[] characterEntries = _characterMenuSection == CharacterMenuSection.Stats
                ? PlayerCharacterStatsCommandPageFocusEntries
                : _characterMenuSection == CharacterMenuSection.Projectiles
                    ? PlayerCharacterProjectileCommandPageFocusEntries
                    : PlayerPickupCommandPageFocusEntries;
            return BuildCommandPageFocusEntries(PlayerSectionCommandPageFocusEntries, PlayerCharacterSectionCommandPageFocusEntries, characterEntries);
        }

        private GUIStyle GetNoConsumeActionStyle(bool isEnabled)
        {
            return isEnabled ? _enabledButtonStyle : _buttonStyle;
        }

        private void ExecuteForSelectedPickupTargets(PlayerController fallbackPlayer, Action<PlayerController> action)
        {
            if (action == null)
            {
                return;
            }

            if (_characterSwitchTarget != CharacterSwitchTarget.BothPlayers)
            {
                PlayerController selectedPlayer = GetSelectedCommandTargetPlayer();
                action(_characterSwitchTarget == CharacterSwitchTarget.PrimaryPlayer && (object)selectedPlayer == null
                    ? fallbackPlayer
                    : selectedPlayer);
                return;
            }

            GameManager gameManager = GameManager.Instance;
            PlayerController primaryPlayer = (object)gameManager != null ? gameManager.PrimaryPlayer : null;
            PlayerController secondaryPlayer = (object)gameManager != null ? gameManager.SecondaryPlayer : null;
            if ((object)primaryPlayer != null)
            {
                action(primaryPlayer);
            }

            if ((object)secondaryPlayer != null && (object)secondaryPlayer != (object)primaryPlayer)
            {
                action(secondaryPlayer);
            }
        }

        private CommandPageActionBinding[] GetPlayerCommandPageActionBindings(PlayerController player)
        {
            return new[]
            {
                new CommandPageActionBinding("cmd.player.heal_half", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteHealHalfHeart(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.player.full_heal", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteFullHeal(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.player.add_max_health", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddMaxHealth(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.player.spawn_full_heart", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteSpawnFullHeartNearPlayer(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.player.add_armor", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddArmor(targetPlayer, null); }); }),
                // Armor no-consume needs the selected player's current armor so an empty
                // P2 receives its required one-point baseline when the feature is enabled.
                new CommandPageActionBinding("cmd.player.armor_no_consume", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteToggleArmorNoConsume(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.player.spawn_armor", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteSpawnArmorNearPlayer(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.player.add_blank", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddBlank(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.player.spawn_blank", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteSpawnBlankNearPlayer(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.player.add_key", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddKey(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.player.key_no_consume", delegate { ExecuteToggleKeyNoConsume(player, null); }),
                new CommandPageActionBinding("cmd.player.spawn_key", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteSpawnKeyNearPlayer(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.player.add_rat_key", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddRatKey(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.player.spawn_rat_key", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteSpawnRatKeyNearPlayer(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.player.add_currency_large", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddLargeCurrency(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.player.currency_no_consume", delegate { ExecuteToggleCurrencyNoConsume(player, null); }),
                new CommandPageActionBinding("cmd.player.clear_currency", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteClearCurrency(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.player.spawn_currency", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteSpawnCurrencyNearPlayer(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.player.blank_no_consume", delegate { ExecuteToggleBlankNoConsume(player, null); }),
                new CommandPageActionBinding("cmd.combat.rapid", delegate { ExecuteToggleRapidFire(player, null); }),
                new CommandPageActionBinding("cmd.combat.auto_reload", delegate { ExecuteToggleAutoReload(null); }),
                new CommandPageActionBinding("cmd.combat.ammo_mode", delegate { ExecuteCycleAmmoMode(null); }),
                new CommandPageActionBinding("cmd.combat.skip_charge", delegate { ExecuteToggleSkipCharge(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.combat.invincible", delegate { ExecuteToggleInvincibility(player, null); }),
                new CommandPageActionBinding("cmd.combat.ammonomicon", delegate { ExecuteToggleAmmonomiconFastOpen(null); }),
                new CommandPageActionBinding("cmd.combat.enemy_health_bars", delegate { ExecuteToggleEnemyHealthBars(player, null); }),
                new CommandPageActionBinding("cmd.combat.boss_intro", delegate { ExecuteToggleBossIntroSkip(null); }),
                new CommandPageActionBinding("cmd.combat.full_ammo", delegate { ExecuteRefillCurrentGunAmmo(player, null); }),
                new CommandPageActionBinding("cmd.player.damage_apply", delegate { ExecuteApplyDamageMultiplier(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.damage_clear", delegate { ExecuteClearDamageMultiplier(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.movement_apply", delegate { ExecuteApplyMovementMultiplier(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.movement_clear", delegate { ExecuteClearMovementMultiplier(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.coolness_apply", delegate { ExecuteApplyCoolnessValue(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.coolness_clear", delegate { ExecuteClearCoolnessValue(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.curse_apply", delegate { ExecuteApplyCurseValue(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.curse_clear", delegate { ExecuteClearCurseValue(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.magnificence_apply", delegate { ExecuteApplyMagnificenceValue(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.magnificence_clear", delegate { ExecuteClearMagnificenceValue(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.bullet_size_apply", delegate { ExecuteApplyBulletSize(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.bullet_size_clear", delegate { _bulletSizeEditText = "1"; ExecuteApplyBulletSize(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.bullet_speed_apply", delegate { ExecuteApplyBulletSpeed(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.bullet_speed_clear", delegate { _bulletSpeedEditText = "1"; ExecuteApplyBulletSpeed(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.reload_speed_apply", delegate { ExecuteApplyReloadSpeed(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.reload_speed_clear", delegate { _reloadSpeedEditText = "1.0"; ExecuteApplyReloadSpeed(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.accuracy_apply", delegate { ExecuteApplyAccuracy(GetSelectedCommandTargetPlayer() ?? player, null); }),
                new CommandPageActionBinding("cmd.player.accuracy_clear", delegate { _accuracyEditText = "0"; ExecuteApplyAccuracy(GetSelectedCommandTargetPlayer() ?? player, null); }),
            };
        }
    }
}
