// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
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
            new ControllerFocusEntry("cmd.player.clear_curse", 3, 0),
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
            new ControllerFocusEntry("cmd.player.damage_multiplier", 10, 0),
            new ControllerFocusEntry("cmd.player.move_multiplier", 10, 1),
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
            new ControllerFocusEntry("cmd.player.damage_multiplier", 10, 0),
            new ControllerFocusEntry("cmd.player.move_multiplier", 10, 1),
        };

        private void DrawPlayerContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            const float sectionButtonWidth = 92f;
            const float sectionButtonHeight = 28f;
            Rect characterSectionRect = new Rect(contentRect.x, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect combatSectionRect = new Rect(characterSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            DrawPlayerSectionButton(characterSectionRect, "cmd.player.section.character", PlayerMenuSection.Character, GetLocalizedFallback("gui.command.player.section.character", "Character", "角色"));
            DrawPlayerSectionButton(combatSectionRect, "cmd.player.section.combat", PlayerMenuSection.Combat, GetLocalizedFallback("gui.command.player.section.combat", "Combat", "战斗"));
            DrawPlayerTargetButton(contentRect, buttonWidth, controlHeight, logger);

            Rect subsectionContentRect = new Rect(contentRect.x, contentRect.y + sectionButtonHeight + 8f, contentRect.width, contentRect.height - sectionButtonHeight - 8f);
            if (_playerMenuSection == PlayerMenuSection.Combat)
            {
                DrawPlayerCombatContent(subsectionContentRect, buttonWidth, controlHeight, player, logger);
                return;
            }

            Rect pickupsSectionRect = new Rect(contentRect.x, subsectionContentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect statsSectionRect = new Rect(pickupsSectionRect.xMax + 2f, subsectionContentRect.y, sectionButtonWidth, sectionButtonHeight);
            DrawCharacterSectionButton(pickupsSectionRect, "cmd.player.character.pickups", CharacterMenuSection.Pickups, GetLocalizedFallback("gui.command.player.section.pickups", "Pickups", "拾取物"));
            DrawCharacterSectionButton(statsSectionRect, "cmd.player.character.stats", CharacterMenuSection.Stats, GetLocalizedFallback("gui.command.player.section.stats", "Stats", "属性"));

            Rect sectionContentRect = new Rect(contentRect.x, subsectionContentRect.y + sectionButtonHeight + 8f, contentRect.width, subsectionContentRect.height - sectionButtonHeight - 8f);
            if (_characterMenuSection == CharacterMenuSection.Stats)
            {
                DrawPlayerCharacterStatsContent(sectionContentRect, buttonWidth, controlHeight, player, logger);
                return;
            }

            const float actionRowHeight = 38f;
            DrawPickupActionRows(sectionContentRect, sectionContentRect.y + controlHeight + ButtonGap, actionRowHeight, ButtonGap, BuildPlayerPickupRows(player, logger));
        }

        private void DrawPlayerCharacterStatsContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            if (DrawControllerButton(new Rect(contentRect.x, contentRect.y, buttonWidth, controlHeight), "cmd.player.clear_curse", GuiText.Get("gui.command.button.clear_curse"), _buttonStyle))
            {
                ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteClearCurse(targetPlayer, logger); });
            }

        }

        private void DrawPlayerCombatContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            // The former top-level Combat page now lives under Player -> Combat.
            DrawCombatSettings(contentRect, controlHeight, player, logger);

            float multiplierRowY = contentRect.y + (controlHeight + ButtonGap) * 7f;
            if (DrawControllerButton(
                new Rect(contentRect.x, multiplierRowY, buttonWidth, controlHeight),
                "cmd.player.damage_multiplier",
                GetLocalizedFallback("gui.player.stats.damage_multiplier", "Damage", "伤害") + " x" + GetDamageMultiplierLabel(),
                _buttonStyle))
            {
                ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteCycleDamageMultiplier(targetPlayer, logger); });
            }

            if (DrawControllerButton(
                new Rect(contentRect.x + buttonWidth + ButtonGap, multiplierRowY, buttonWidth, controlHeight),
                "cmd.player.move_multiplier",
                GetLocalizedFallback("gui.player.stats.move_multiplier", "Move Speed", "移速") + " x" + GetMovementMultiplierLabel(),
                _buttonStyle))
            {
                ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteCycleMovementMultiplier(targetPlayer, logger); });
            }
        }

        private string GetDamageMultiplierLabel()
        {
            return _playerStatMultiplierService != null ? _playerStatMultiplierService.DamageMultiplier.ToString("0.##") : "1";
        }

        private string GetMovementMultiplierLabel()
        {
            return _playerStatMultiplierService != null ? _playerStatMultiplierService.MovementMultiplier.ToString("0.##") : "1";
        }

        private void ExecuteCycleDamageMultiplier(PlayerController player, ManualLogSource logger)
        {
            if (_playerStatMultiplierService == null || (object)player == null)
            {
                ShowStatus(GuiText.Get("result.common.player_not_ready"), true);
                return;
            }

            float multiplier = _playerStatMultiplierService.CycleDamageMultiplier(player);
            ShowStatus(GuiText.Get("result.player.stats.damage_multiplier", multiplier.ToString("0.##")), false);
            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command("Player damage multiplier set to x" + multiplier.ToString("0.##") + "."));
            }
        }

        private void ExecuteCycleMovementMultiplier(PlayerController player, ManualLogSource logger)
        {
            if (_playerStatMultiplierService == null || (object)player == null)
            {
                ShowStatus(GuiText.Get("result.common.player_not_ready"), true);
                return;
            }

            float multiplier = _playerStatMultiplierService.CycleMovementMultiplier(player);
            ShowStatus(GuiText.Get("result.player.stats.move_multiplier", multiplier.ToString("0.##")), false);
            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command("Player movement multiplier set to x" + multiplier.ToString("0.##") + "."));
            }
        }

        private void ExecuteToggleInvincibilityForSelectedTargets(PlayerController fallbackPlayer, ManualLogSource logger)
        {
            ExecuteToggleInvincibility(GetSelectedCommandTargetPlayer() ?? fallbackPlayer, logger);
        }

        private PickupActionRowDefinition[] BuildPlayerPickupRows(PlayerController player, ManualLogSource logger)
        {
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
                _playerMenuSection = section;
            }
        }

        private void DrawCharacterSectionButton(Rect rect, string controlId, CharacterMenuSection section, string label)
        {
            GUIStyle style = _characterMenuSection == section ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle;
            if (DrawControllerButton(rect, controlId, label, style))
            {
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
                new CommandPageActionBinding("cmd.player.clear_curse", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteClearCurse(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.combat.rapid", delegate { ExecuteToggleRapidFire(player, null); }),
                new CommandPageActionBinding("cmd.combat.auto_reload", delegate { ExecuteToggleAutoReload(null); }),
                new CommandPageActionBinding("cmd.combat.ammo_mode", delegate { ExecuteCycleAmmoMode(null); }),
                new CommandPageActionBinding("cmd.combat.invincible", delegate { ExecuteToggleInvincibility(player, null); }),
                new CommandPageActionBinding("cmd.combat.ammonomicon", delegate { ExecuteToggleAmmonomiconFastOpen(null); }),
                new CommandPageActionBinding("cmd.combat.enemy_health_bars", delegate { ExecuteToggleEnemyHealthBars(player, null); }),
                new CommandPageActionBinding("cmd.combat.boss_intro", delegate { ExecuteToggleBossIntroSkip(null); }),
                new CommandPageActionBinding("cmd.combat.full_ammo", delegate { ExecuteRefillCurrentGunAmmo(player, null); }),
                new CommandPageActionBinding("cmd.player.damage_multiplier", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteCycleDamageMultiplier(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.player.move_multiplier", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteCycleMovementMultiplier(targetPlayer, null); }); }),
            };
        }
    }
}
