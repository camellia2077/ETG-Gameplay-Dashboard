// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        public void EnsureGooptonActiveOnFoyerLoad()
        {
            if (_roomDebugCommandService != null)
            {
                _roomDebugCommandService.EnsureGooptonActiveOnFoyerLoad();
            }
        }

        private static readonly ControllerFocusEntry[] RoomSectionCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.room.section.chest", 2, 0),
            new ControllerFocusEntry("cmd.room.section.neutral", 2, 1),
            new ControllerFocusEntry("cmd.room.section.npc", 2, 2),
            new ControllerFocusEntry("cmd.room.section.enemies", 2, 3),
            new ControllerFocusEntry("cmd.room.section.rewind", 2, 4),
            new ControllerFocusEntry("cmd.room.section.boss", 2, 5),
        };

        private static readonly ControllerFocusEntry[] RoomChestCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.room.chest_tier.brown", 3, 0),
            new ControllerFocusEntry("cmd.room.chest_tier.blue", 3, 1),
            new ControllerFocusEntry("cmd.room.chest_tier.green", 3, 2),
            new ControllerFocusEntry("cmd.room.chest_tier.red", 3, 3),
            new ControllerFocusEntry("cmd.room.chest_tier.black", 4, 0),
            new ControllerFocusEntry("cmd.room.chest_tier.synergy", 4, 1),
            new ControllerFocusEntry("cmd.room.chest_tier.rainbow", 4, 2),
        };

        private static readonly ControllerFocusEntry[] RoomNeutralCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.room.spawn_gunber_muncher", 3, 0),
            new ControllerFocusEntry("cmd.room.spawn_evil_muncher", 3, 1),
        };

        private static readonly ControllerFocusEntry[] RoomNpcCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.room.unlock_cadence_ox", 3, 0),
            new ControllerFocusEntry("cmd.room.unlock_goopton", 3, 1),
            new ControllerFocusEntry("cmd.room.unlock_doug", 3, 2),
        };

        private static readonly ControllerFocusEntry[] RoomRewindCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.room.enemy_refresh_recording", 3, 0),
            new ControllerFocusEntry("cmd.room.enemy_refresh_method", 3, 1),
            new ControllerFocusEntry("cmd.room.enemy_refresh_method.info", 3, 2),
            new ControllerFocusEntry("cmd.room.rewind.shortcut", 4, 0),
            new ControllerFocusEntry("cmd.room.rewind.shortcut.clear", 4, 1),
            new ControllerFocusEntry("cmd.room.player_rewind", 5, 0),
            new ControllerFocusEntry("cmd.room.player_rewind.info", 5, 1),
            new ControllerFocusEntry("cmd.room.rewind_cleanup", 5, 2),
            new ControllerFocusEntry("cmd.room.rewind_cleanup.info", 5, 3),
            new ControllerFocusEntry("cmd.room.enemy_refresh_execute", 6, 0),
        };

        private static readonly ControllerFocusEntry[] RoomRewindDisabledCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.room.enemy_refresh_recording", 3, 0),
            new ControllerFocusEntry("cmd.room.enemy_refresh_method", 3, 1),
            new ControllerFocusEntry("cmd.room.enemy_refresh_method.info", 3, 2),
            new ControllerFocusEntry("cmd.room.player_rewind", 4, 0),
            new ControllerFocusEntry("cmd.room.player_rewind.info", 4, 1),
            new ControllerFocusEntry("cmd.room.rewind_cleanup", 4, 2),
            new ControllerFocusEntry("cmd.room.rewind_cleanup.info", 4, 3),
            new ControllerFocusEntry("cmd.room.enemy_refresh_execute", 5, 0),
        };

        private static readonly ControllerFocusEntry[] EmptyRoomBossCommandPageFocusEntries = new ControllerFocusEntry[0];

        private void DrawRoomContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            const float sectionButtonWidth = 92f;
            const float sectionButtonHeight = 28f;
            Rect chestSectionRect = new Rect(contentRect.x, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect neutralSectionRect = new Rect(chestSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect npcSectionRect = new Rect(neutralSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect enemiesSectionRect = new Rect(npcSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect rewindSectionRect = new Rect(enemiesSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect bossSectionRect = new Rect(rewindSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            DrawRoomSectionButton(chestSectionRect, "cmd.room.section.chest", RoomMenuSection.Chest, GetLocalizedFallback("gui.room.section.chest", "Chest", "宝箱"));
            DrawRoomSectionButton(neutralSectionRect, "cmd.room.section.neutral", RoomMenuSection.Neutral, GetLocalizedFallback("gui.room.section.neutral", "Neutral", "中立生物"));
            DrawRoomSectionButton(npcSectionRect, "cmd.room.section.npc", RoomMenuSection.Npc, GetLocalizedFallback("gui.room.section.npc", "NPC", "NPC"));
            DrawRoomSectionButton(enemiesSectionRect, "cmd.room.section.enemies", RoomMenuSection.Enemies, GetLocalizedFallback("gui.room.section.enemies", "Enemies", "怪物"));
            DrawRoomSectionButton(rewindSectionRect, "cmd.room.section.rewind", RoomMenuSection.Rewind, GetLocalizedFallback("gui.room.section.rewind", "Rewind", "回溯"));
            DrawRoomSectionButton(bossSectionRect, "cmd.room.section.boss", RoomMenuSection.Boss, GetLocalizedFallback("gui.room.section.boss", "Boss", "Boss"));

            Rect sectionContentRect = new Rect(contentRect.x, contentRect.y + sectionButtonHeight + 12f, contentRect.width, contentRect.height - sectionButtonHeight - 12f);
            switch (_roomMenuSection)
            {
                case RoomMenuSection.Neutral:
                    DrawRoomNeutralSection(sectionContentRect, buttonWidth, controlHeight, player, logger);
                    return;
                case RoomMenuSection.Npc:
                    DrawRoomNpcSection(sectionContentRect, buttonWidth, controlHeight, player, logger);
                    return;
                case RoomMenuSection.Enemies:
                    DrawRoomPlaceholderSection(
                        sectionContentRect,
                        GetLocalizedFallback("gui.room.section.enemies", "Enemies", "怪物"),
                        GetLocalizedFallback("gui.room.placeholder.enemies", "Enemy tools will go here next.", "后续会在这里加入怪物相关功能。"));
                    return;
                case RoomMenuSection.Rewind:
                    DrawRoomRewindSection(sectionContentRect, buttonWidth, controlHeight, player, logger);
                    return;
                case RoomMenuSection.Boss:
                    DrawRoomBossSection(sectionContentRect, buttonWidth, controlHeight, player, logger);
                    return;
                case RoomMenuSection.Chest:
                default:
                    DrawRoomChestSection(sectionContentRect, buttonWidth, controlHeight, player, logger);
                    return;
            }
        }

        private void DrawRoomRewindSection(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            float firstRowY = contentRect.y;
            float firstButtonY = firstRowY;
            float secondButtonY = firstButtonY + controlHeight + ButtonGap;
            float thirdButtonY = secondButtonY + controlHeight + ButtonGap;
            float fourthButtonY = thirdButtonY + controlHeight + ButtonGap;
            float rewindButtonWidth = buttonWidth * 2f + ButtonGap;
            const float infoButtonWidth = 32f;
            float settingButtonWidth = rewindButtonWidth - infoButtonWidth - ButtonGap;
            float secondColumnX = contentRect.x + rewindButtonWidth + ButtonGap;
            bool recordingEnabled = _roomDebugCommandService != null && _roomDebugCommandService.IsRoomEnemyRefreshRecordingEnabled;
            bool playerRewindEnabled = _roomDebugCommandService != null && _roomDebugCommandService.IsPlayerRewindEnabled;
            bool rewindCleanupEnabled = _roomDebugCommandService == null || _roomDebugCommandService.IsRoomRewindCleanupEnabled;

            if (DrawControllerButton(
                new Rect(contentRect.x, secondButtonY, rewindButtonWidth, controlHeight),
                "cmd.room.rewind.shortcut",
                GetLocalizedFormattedFallback(
                    "gui.room.rewind.shortcut.configure",
                    "Set Shortcut: {0}",
                    "设置快捷键：{0}",
                    GetRoomEnemyRewindShortcutButtonLabel()),
                recordingEnabled ? _buttonStyle : _pickupFilterDisabledButtonStyle) && recordingEnabled)
            {
                BeginRoomEnemyRewindShortcutCapture();
            }

            if (DrawControllerButton(
                new Rect(secondColumnX, secondButtonY, buttonWidth, controlHeight),
                "cmd.room.rewind.shortcut.clear",
                GetLocalizedFallback("gui.pickups.button.shortcut_clear", "Clear", "清除"),
                recordingEnabled ? _buttonStyle : _pickupFilterDisabledButtonStyle) && recordingEnabled)
            {
                ClearRoomEnemyRewindShortcut();
            }

            if (DrawControllerButton(
                new Rect(contentRect.x, firstButtonY, rewindButtonWidth, controlHeight),
                "cmd.room.enemy_refresh_recording",
                GetLocalizedFallback(
                    recordingEnabled ? "gui.room.rewind.toggle.on" : "gui.room.rewind.toggle.off",
                    recordingEnabled ? "Rewind: ON" : "Rewind: OFF",
                    recordingEnabled ? "回溯：开启" : "回溯：关闭"),
                // Keep the toggle fill identical in both states. The enabled
                // state uses the selected border, while the disabled state
                // falls back to the ordinary button's unselected border.
                recordingEnabled ? _cursorColorSelectedButtonStyle : _buttonStyle))
            {
                ExecuteToggleRoomEnemyRefreshRecording(logger);
            }

            if (DrawControllerButton(
                new Rect(secondColumnX, firstButtonY, settingButtonWidth, controlHeight),
                "cmd.room.enemy_refresh_method",
                GetRoomEnemyRefreshMethodLabel(),
                recordingEnabled ? _buttonStyle : _pickupFilterDisabledButtonStyle) && recordingEnabled)
            {
                ExecuteCycleRoomEnemyRefreshMethod(logger);
            }

            if (DrawInfoButton(
                new Rect(secondColumnX + settingButtonWidth + ButtonGap, firstButtonY, infoButtonWidth, controlHeight),
                CommandInfoPage.RefreshMethod))
            {
                return;
            }

            if (DrawControllerButton(
                new Rect(contentRect.x, thirdButtonY, settingButtonWidth, controlHeight),
                "cmd.room.player_rewind",
                GetLocalizedFallback(
                    playerRewindEnabled ? "gui.room.player_rewind.on" : "gui.room.player_rewind.off",
                    playerRewindEnabled ? "Player Rewind: ON" : "Player Rewind: OFF",
                    playerRewindEnabled ? "玩家回溯：开启" : "玩家回溯：关闭"),
                !recordingEnabled
                    ? _pickupFilterDisabledButtonStyle
                    : (playerRewindEnabled ? _cursorColorSelectedButtonStyle : _buttonStyle)) && recordingEnabled)
            {
                ExecuteTogglePlayerRewind(logger);
            }

            if (DrawInfoButton(
                new Rect(contentRect.x + settingButtonWidth + ButtonGap, thirdButtonY, infoButtonWidth, controlHeight),
                CommandInfoPage.PlayerRewind))
            {
                return;
            }

            if (DrawControllerButton(
                new Rect(secondColumnX, thirdButtonY, settingButtonWidth, controlHeight),
                "cmd.room.rewind_cleanup",
                GetLocalizedFallback(
                    rewindCleanupEnabled ? "gui.room.rewind_cleanup.on" : "gui.room.rewind_cleanup.off",
                    rewindCleanupEnabled ? "Room Residual Cleanup: ON" : "Room Residual Cleanup: OFF",
                    rewindCleanupEnabled ? "房间残留清理：开启" : "房间残留清理：关闭"),
                !recordingEnabled
                    ? _pickupFilterDisabledButtonStyle
                    : (rewindCleanupEnabled ? _cursorColorSelectedButtonStyle : _buttonStyle)) && recordingEnabled)
            {
                ExecuteToggleRoomRewindCleanup(logger);
            }

            if (DrawInfoButton(
                new Rect(secondColumnX + settingButtonWidth + ButtonGap, thirdButtonY, infoButtonWidth, controlHeight),
                CommandInfoPage.Cleanup))
            {
                return;
            }

            if (DrawControllerButton(
                new Rect(contentRect.x, fourthButtonY, rewindButtonWidth * 2f + ButtonGap, controlHeight),
                "cmd.room.enemy_refresh_execute",
                GetLocalizedFallback("gui.room.rewind.execute", "Spawn", "生成"),
                recordingEnabled ? _buttonStyle : _pickupFilterDisabledButtonStyle) && recordingEnabled)
            {
                ExecuteSelectedRoomEnemyRefresh(player, logger);
            }
        }

        private bool DrawInfoButton(Rect rect, CommandInfoPage page)
        {
            if (!GUI.Button(rect, "?", _infoButtonStyle))
            {
                return false;
            }

            Event currentEvent = Event.current;
            if (currentEvent != null)
            {
                currentEvent.Use();
            }

            GUIUtility.hotControl = 0;
            GUIUtility.keyboardControl = 0;
            OpenCommandInfoPage(page);
            return true;
        }

        private void OpenCommandInfoPage(CommandInfoPage page)
        {
            _commandInfoPage = page;
            _commandInfoPageFocusedControlId = "room_rewind_info.back";
            _currentPage = PanelPage.CommandInfo;
            _focusInputField = false;
            _focusPickupSearchField = false;
            RequestGuiFocusRelease();
        }

        private void CloseCommandInfoPage()
        {
            _currentPage = PanelPage.Command;
            switch (_commandInfoPage)
            {
                case CommandInfoPage.Coolness:
                    _commandMenuCategory = CommandMenuCategory.Player;
                    _playerMenuSection = PlayerMenuSection.Character;
                    _characterMenuSection = CharacterMenuSection.Stats;
                    _commandPageFocusedControlId = "cmd.player.coolness_apply";
                    break;
                case CommandInfoPage.Curse:
                    _commandMenuCategory = CommandMenuCategory.Player;
                    _playerMenuSection = PlayerMenuSection.Character;
                    _characterMenuSection = CharacterMenuSection.Stats;
                    _commandPageFocusedControlId = "cmd.player.curse_apply";
                    break;
                case CommandInfoPage.Magnificence:
                    _commandMenuCategory = CommandMenuCategory.Player;
                    _playerMenuSection = PlayerMenuSection.Character;
                    _characterMenuSection = CharacterMenuSection.Stats;
                    _commandPageFocusedControlId = "cmd.player.magnificence_apply";
                    break;
                case CommandInfoPage.DamageMultiplier:
                    _commandMenuCategory = CommandMenuCategory.Player;
                    _playerMenuSection = PlayerMenuSection.Character;
                    _characterMenuSection = CharacterMenuSection.Stats;
                    _commandPageFocusedControlId = "cmd.player.damage_apply";
                    break;
                case CommandInfoPage.MovementMultiplier:
                    _commandMenuCategory = CommandMenuCategory.Player;
                    _playerMenuSection = PlayerMenuSection.Character;
                    _characterMenuSection = CharacterMenuSection.Stats;
                    _commandPageFocusedControlId = "cmd.player.movement_apply";
                    break;
                case CommandInfoPage.BulletSize:
                    _commandMenuCategory = CommandMenuCategory.Player;
                    _playerMenuSection = PlayerMenuSection.Character;
                    _characterMenuSection = CharacterMenuSection.Projectiles;
                    _commandPageFocusedControlId = "cmd.player.bullet_size_apply";
                    break;
                case CommandInfoPage.BulletSpeed:
                    _commandMenuCategory = CommandMenuCategory.Player;
                    _playerMenuSection = PlayerMenuSection.Character;
                    _characterMenuSection = CharacterMenuSection.Projectiles;
                    _commandPageFocusedControlId = "cmd.player.bullet_speed_apply";
                    break;
                case CommandInfoPage.ReloadSpeed:
                    _commandMenuCategory = CommandMenuCategory.Player;
                    _playerMenuSection = PlayerMenuSection.Character;
                    _characterMenuSection = CharacterMenuSection.Projectiles;
                    _commandPageFocusedControlId = "cmd.player.reload_speed_apply";
                    break;
                case CommandInfoPage.Accuracy:
                    _commandMenuCategory = CommandMenuCategory.Player;
                    _playerMenuSection = PlayerMenuSection.Character;
                    _characterMenuSection = CharacterMenuSection.Projectiles;
                    _commandPageFocusedControlId = "cmd.player.accuracy_apply";
                    break;
                case CommandInfoPage.RefreshMethod:
                    _commandPageFocusedControlId = "cmd.room.enemy_refresh_method.info";
                    break;
                case CommandInfoPage.Cleanup:
                    _commandPageFocusedControlId = "cmd.room.rewind_cleanup.info";
                    break;
                case CommandInfoPage.PlayerRewind:
                default:
                    _commandPageFocusedControlId = "cmd.room.player_rewind.info";
                    break;
            }
            RequestGuiFocusRelease();
        }

        private void HandleCommandInfoPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed || IsPanelConfirmPressed())
            {
                CloseCommandInfoPage();
                return;
            }

            ResetControllerNavigationAxes();
        }

        private void DrawCommandInfoPage(Rect panelRect)
        {
            Rect backButtonRect = GetSecondaryPageBackButtonRect(panelRect);
            if (DrawSecondaryPageBackButton(panelRect, "room_rewind_info.back", CloseCommandInfoPage))
            {
                return;
            }

            bool isPlayerRewind = _commandInfoPage == CommandInfoPage.PlayerRewind;
            bool isCleanup = _commandInfoPage == CommandInfoPage.Cleanup;
            bool isCoolness = _commandInfoPage == CommandInfoPage.Coolness;
            bool isCurse = _commandInfoPage == CommandInfoPage.Curse;
            bool isMagnificence = _commandInfoPage == CommandInfoPage.Magnificence;
            bool isDamageMultiplier = _commandInfoPage == CommandInfoPage.DamageMultiplier;
            bool isMovementMultiplier = _commandInfoPage == CommandInfoPage.MovementMultiplier;
            bool isBulletSize = _commandInfoPage == CommandInfoPage.BulletSize;
            bool isBulletSpeed = _commandInfoPage == CommandInfoPage.BulletSpeed;
            bool isReloadSpeed = _commandInfoPage == CommandInfoPage.ReloadSpeed;
            bool isAccuracy = _commandInfoPage == CommandInfoPage.Accuracy;
            bool isRespawnMethod = _roomEnemyRefreshMethod == RoomEnemyRefreshMethod.RespawnEnemies;
            string titleKey;
            string titleEnglish;
            string titleChinese;
            string bodyKey;
            string bodyEnglish;
            string bodyChinese;
            GetPlayerStatInfoText(
                isCoolness,
                isCurse,
                isMagnificence,
                isDamageMultiplier,
                isMovementMultiplier,
                isBulletSize,
                isBulletSpeed,
                isReloadSpeed,
                isAccuracy,
                isPlayerRewind,
                isCleanup,
                isRespawnMethod,
                out titleKey,
                out titleEnglish,
                out titleChinese,
                out bodyKey,
                out bodyEnglish,
                out bodyChinese);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, backButtonRect.x - panelRect.x - 28f, 24f),
                GetLocalizedFallback(titleKey, titleEnglish, titleChinese),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 52f, panelRect.width - 28f, 250f),
                GetLocalizedFallback(bodyKey, bodyEnglish, bodyChinese),
                _wrappedHintStyle);
        }

        private static void GetPlayerStatInfoText(
            bool isCoolness,
            bool isCurse,
            bool isMagnificence,
            bool isDamageMultiplier,
            bool isMovementMultiplier,
            bool isBulletSize,
            bool isBulletSpeed,
            bool isReloadSpeed,
            bool isAccuracy,
            bool isPlayerRewind,
            bool isCleanup,
            bool isRespawnMethod,
            out string titleKey,
            out string titleEnglish,
            out string titleChinese,
            out string bodyKey,
            out string bodyEnglish,
            out string bodyChinese)
        {
            if (isCoolness)
            {
                titleKey = "gui.player.stats.info.coolness.title";
                titleEnglish = "Coolness";
                titleChinese = "酷气值（Coolness）";
                bodyKey = "gui.player.stats.info.coolness.body";
                bodyEnglish = "Coolness is a hidden luck stat. Each point reduces active-item cooldown by 5%, up to a 50% reduction, and increases the chance of receiving a reward after clearing a combat room.";
                bodyChinese = "酷气值（Coolness）是游戏中的隐藏属性。每点酷气值可使主动道具冷却时间减少 5%，最多减少 50%，并提高战斗房间清空后获得物品奖励的概率。";
                return;
            }

            if (isCurse)
            {
                titleKey = "gui.player.stats.info.curse.title";
                titleEnglish = "Curse";
                titleChinese = "诅咒（Curse）";
                bodyKey = "gui.player.stats.info.curse.body";
                bodyEnglish = "Curse is a run-wide risk stat. Higher Curse increases Jammed-enemy and special-threat chances, can make chests more dangerous, and reduces the chance of a reward after clearing a combat room. It does not reduce active-item cooldown.";
                bodyChinese = "Curse 是贯穿本局的风险属性。数值越高，出现 Jammed 敌人和特殊威胁的概率越高，宝箱也可能更危险，并会降低战斗房间清空后的奖励概率。Curse 不会降低主动道具冷却时间。";
                return;
            }

            if (isMagnificence)
            {
                titleKey = "gui.player.stats.info.magnificence.title";
                titleEnglish = "Magnificence";
                titleChinese = "华丽值（Magnificence）";
                bodyKey = "gui.player.stats.info.magnificence.body";
                bodyEnglish = "Magnificence is a hidden run stat that limits how many high-quality guns and items can be obtained. Higher values make future high-quality chest rewards less likely.";
                bodyChinese = "华丽值（Magnificence）是本局隐藏属性，用来限制高品质枪械和道具的获取数量。数值越高，之后从高品质宝箱中获得高品质奖励的概率越低。";
                return;
            }

            if (isDamageMultiplier)
            {
                titleKey = "gui.player.stats.info.damage.title";
                titleEnglish = "Damage multiplier";
                titleChinese = "伤害倍率";
                bodyKey = "gui.player.stats.info.damage.body";
                bodyEnglish = "This value multiplies the player's Damage stat. 1 is the normal value; higher integers increase damage.";
                bodyChinese = "这个数值会乘到玩家的伤害属性上。1 为正常值，更高的整数会提高伤害。";
                return;
            }

            if (isMovementMultiplier)
            {
                titleKey = "gui.player.stats.info.movement.title";
                titleEnglish = "Movement speed multiplier";
                titleChinese = "移速倍率";
                bodyKey = "gui.player.stats.info.movement.body";
                bodyEnglish = "This value multiplies the player's Movement Speed stat. 1 is the normal value; higher integers increase movement speed.";
                bodyChinese = "这个数值会乘到玩家的移速属性上。1 为正常值，更高的整数会提高移动速度。";
                return;
            }

            if (isBulletSize || isBulletSpeed || isReloadSpeed || isAccuracy)
            {
                titleKey = isBulletSize ? "gui.player.projectiles.info.size.title" : isBulletSpeed ? "gui.player.projectiles.info.speed.title" : isReloadSpeed ? "gui.player.projectiles.info.reload_speed.title" : "gui.player.projectiles.info.accuracy.title";
                titleEnglish = isBulletSize ? "Projectile size" : isBulletSpeed ? "Projectile speed" : isReloadSpeed ? "Reload speed" : "Spread";
                titleChinese = isBulletSize ? "子弹大小" : isBulletSpeed ? "子弹射速" : isReloadSpeed ? "换弹速度" : "扩散程度";
                bodyKey = isBulletSize ? "gui.player.projectiles.info.size.body" : isBulletSpeed ? "gui.player.projectiles.info.speed.body" : isReloadSpeed ? "gui.player.projectiles.info.reload_speed.body" : "gui.player.projectiles.info.accuracy.body";
                bodyEnglish = isBulletSize
                    ? "This integer multiplies the player's projectile scale. The accepted range is 1 to 30; 1 is the normal size and higher values make fired projectiles larger."
                    : isBulletSpeed
                        ? "This integer multiplies the player's projectile speed. The accepted range is 1 to 999; 1 is the normal speed and higher values make fired projectiles travel faster."
                        : isReloadSpeed
                            ? "This decimal multiplier controls reload speed. The accepted range is 0.25x to 4.0x; 1.0x is normal, higher values reload faster, and lower values reload slower."
                            : "This value controls projectile spread. The accepted range is 0 to 999; higher values increase spread and 0 means no spread.";
                bodyChinese = isBulletSize
                    ? "这个整数会乘到玩家的子弹大小上。允许范围是 1 到 30；1 为正常大小，更高的数值会让发射的子弹变大。"
                    : isBulletSpeed
                        ? "这个整数会乘到玩家的子弹速度上。允许范围是 1 到 999；1 为正常速度，更高的数值会让发射的子弹飞得更快。"
                        : isReloadSpeed
                            ? "这个小数倍率控制换弹速度。允许范围是 0.25x 到 4.0x；1.0x 为正常速度，数值越大换弹越快，数值越小换弹越慢。"
                            : "这个数值控制子弹扩散程度。允许范围是 0 到 999；数值越大，扩散越大；数值为 0 则不扩散。";
                return;
            }

            titleKey = isPlayerRewind
                ? "gui.room.rewind.info.player.title"
                : isCleanup
                    ? "gui.room.rewind.info.cleanup.title"
                    : (isRespawnMethod ? "gui.room.rewind.info.mode.respawn.title" : "gui.room.rewind.info.mode.rewind.title");
            titleEnglish = isPlayerRewind
                ? "Player Rewind"
                : isCleanup
                    ? "Room Residual Cleanup"
                    : (isRespawnMethod ? "Respawn Enemies" : "Rewind Room");
            titleChinese = isPlayerRewind
                ? "玩家回溯"
                : isCleanup
                    ? "房间残留清理"
                    : (isRespawnMethod ? "重新生成怪物" : "回溯房间");
            bodyKey = isPlayerRewind
                ? "gui.room.rewind.info.player.body"
                : isCleanup
                    ? "gui.room.rewind.info.cleanup.body"
                    : (isRespawnMethod ? "gui.room.rewind.info.mode.respawn.body" : "gui.room.rewind.info.mode.rewind.body");
            bodyEnglish = isPlayerRewind
                ? "When rewinding a room, restores the room-entry player snapshot: health, max health, armor, blanks, stats, guns and ammo, passive items, active items, selected slots, and active-item charge/cooldown state. The snapshot is captured only when Player Rewind is enabled before entering the room."
                : isCleanup
                    ? "Before rewinding a room, removes room-local projectiles, decals, drops, currency, corpses, death effects, and Boss reward pedestals. Player-, gun-, and pickup-owned effects, other rooms, and other floors are not affected."
                    : (isRespawnMethod
                        ? "Respawn Enemies uses the room template to generate enemies again. Enemy batches, variants, positions, and waves may differ from the original room."
                        : "Rewind Room restores the recorded enemy batches, variants, positions, and reinforcement waves from the current room. It is intended to reproduce the room's recorded state rather than generate a new one.");
            bodyChinese = isPlayerRewind
                ? "回溯房间时恢复进入房间时记录的玩家状态：生命值、最大生命值、护甲、空响弹、属性、枪械和弹药、被动道具、主动道具、选择槽位，以及主动道具充能/冷却状态。只有在进入房间前开启玩家回溯，才会记录该房间的状态。"
                : isCleanup
                    ? "回溯房间前清理房间内的投射物、地面痕迹、掉落物、货币、尸体、死亡特效和 Boss 奖励台。不会影响玩家、枪械、道具拥有的特效，也不会影响其他房间或楼层。"
                    : (isRespawnMethod
                        ? "重新生成怪物会读取房间模板再次生成敌人，怪物批次、变体、站位和波次可能与原房间不同。"
                        : "回溯房间会恢复当前房间记录的敌人批次、变体、站位和后续增援波次，用于复现房间已记录的状态，而不是重新生成一个新房间。");
        }

        private void DrawRoomChestSection(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            float secondColumnX = contentRect.x + buttonWidth + ButtonGap;
            float thirdColumnX = secondColumnX + buttonWidth + ButtonGap;
            float fourthColumnX = thirdColumnX + buttonWidth + ButtonGap;
            float firstRowY = contentRect.y;
            float secondRowY = firstRowY + controlHeight + ButtonGap;

            GUI.Label(
                new Rect(contentRect.x, firstRowY, contentRect.width, 20f),
                GetLocalizedFallback("gui.room.chest_tier.title", "Chest Tier", "宝箱等级"),
                _hintStyle);

            float optionsTop = firstRowY + 24f;
            float optionsSecondRowY = optionsTop + controlHeight + ButtonGap;
            DrawRoomChestTierButton(new Rect(contentRect.x, optionsTop, buttonWidth, controlHeight), "cmd.room.chest_tier.brown", RoomChestTier.Brown, player, logger);
            DrawRoomChestTierButton(new Rect(secondColumnX, optionsTop, buttonWidth, controlHeight), "cmd.room.chest_tier.blue", RoomChestTier.Blue, player, logger);
            DrawRoomChestTierButton(new Rect(thirdColumnX, optionsTop, buttonWidth, controlHeight), "cmd.room.chest_tier.green", RoomChestTier.Green, player, logger);
            DrawRoomChestTierButton(new Rect(fourthColumnX, optionsTop, buttonWidth, controlHeight), "cmd.room.chest_tier.red", RoomChestTier.Red, player, logger);

            DrawRoomChestTierButton(new Rect(contentRect.x, optionsSecondRowY, buttonWidth, controlHeight), "cmd.room.chest_tier.black", RoomChestTier.Black, player, logger);
            DrawRoomChestTierButton(new Rect(secondColumnX, optionsSecondRowY, buttonWidth, controlHeight), "cmd.room.chest_tier.synergy", RoomChestTier.Synergy, player, logger);
            DrawRoomChestTierButton(new Rect(thirdColumnX, optionsSecondRowY, buttonWidth, controlHeight), "cmd.room.chest_tier.rainbow", RoomChestTier.Rainbow, player, logger);
        }

        private void DrawRoomNeutralSection(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            float thirdColumnX = contentRect.x + (buttonWidth * 2f) + (ButtonGap * 2f);
            float firstRowY = contentRect.y;
            float secondRowY = firstRowY + 24f + controlHeight + ButtonGap;

            GUI.Label(
                new Rect(contentRect.x, firstRowY, contentRect.width, 20f),
                GetLocalizedFallback("gui.room.neutral.title", "Neutral NPCs", "中立生物"),
                _hintStyle);

            GUI.Label(
                new Rect(contentRect.x, firstRowY + 24f, contentRect.width, 36f),
                GetLocalizedFallback(
                    "gui.room.neutral.hint",
                    "Spawn utility NPC-style objects in the current room.",
                    "在当前房间生成偏中立、功能型的 NPC 对象。"),
                _wrappedHintStyle);

            float fourthRowY = secondRowY + 18f;
            string spawnGunberMuncherLabel = GetLocalizedFallback("gui.room.button.spawn_gunber_muncher", "Spawn Gunber Muncher", "生成吃枪怪");
            if (DrawControllerButton(new Rect(contentRect.x, fourthRowY, buttonWidth * 2f + ButtonGap, controlHeight), "cmd.room.spawn_gunber_muncher", spawnGunberMuncherLabel, _buttonStyle))
            {
                ExecuteSpawnGunberMuncher(player, logger);
            }

            string spawnEvilMuncherLabel = GetLocalizedFallback("gui.room.button.spawn_evil_muncher", "Spawn Evil Muncher", "生成邪恶吃枪怪");
            if (DrawControllerButton(new Rect(thirdColumnX, fourthRowY, buttonWidth * 2f, controlHeight), "cmd.room.spawn_evil_muncher", spawnEvilMuncherLabel, _buttonStyle))
            {
                ExecuteSpawnEvilMuncher(player, logger);
            }
        }

        private void DrawRoomNpcSection(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            float secondColumnX = contentRect.x + buttonWidth + ButtonGap;
            float thirdColumnX = secondColumnX + buttonWidth + ButtonGap;
            GUI.Label(
                new Rect(contentRect.x, contentRect.y, contentRect.width, 20f),
                GetLocalizedFallback("gui.room.npc.title", "NPC", "NPC"),
                _hintStyle);
            GUI.Label(
                new Rect(contentRect.x, contentRect.y + 24f, contentRect.width, 36f),
                GetLocalizedFallback(
                    "gui.room.npc.hint",
                    "Unlock NPCs in the Breach by setting their vanilla foyer flags.",
                    "通过设置游戏原版裂隙标记来解锁 NPC。"),
                _wrappedHintStyle);

            float buttonY = contentRect.y + 78f;
            if (DrawControllerButton(
                new Rect(contentRect.x, buttonY, buttonWidth, controlHeight),
                "cmd.room.unlock_cadence_ox",
                GetLocalizedFallback("gui.room.button.unlock_cadence_ox", "Cadence & Ox", "牛津与卡登"),
                _buttonStyle))
            {
                ExecuteUnlockCadenceOx(logger);
            }

            if (DrawControllerButton(
                new Rect(secondColumnX, buttonY, buttonWidth, controlHeight),
                "cmd.room.unlock_goopton",
                GetLocalizedFallback("gui.room.button.unlock_goopton", "Professor Goopton", "古普顿教授"),
                _buttonStyle))
            {
                ExecuteUnlockGoopton(logger);
            }

            if (DrawControllerButton(
                new Rect(thirdColumnX, buttonY, buttonWidth, controlHeight),
                "cmd.room.unlock_doug",
                GetLocalizedFallback("gui.room.button.unlock_doug", "Doug", "道格"),
                _buttonStyle))
            {
                ExecuteUnlockDoug(logger);
            }

        }

        private void DrawRoomBossSection(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            long drawStartedAtTimestamp = BeginBossSelectionPagePerformanceStage();
            int bossOptionCount = 0;
            try
            {
                long optionsStartedAtTimestamp = BeginBossSelectionPagePerformanceStage();
                List<RoomBossOption> bossOptions = GetBossSelectionBossOptions();
                bossOptionCount = bossOptions.Count;
                LogBossSelectionPagePerformanceStage("Options", optionsStartedAtTimestamp, bossOptionCount);
                string currentBossNames = _roomDebugCommandService != null
                    ? _roomDebugCommandService.GetSelectedBossName()
                    : "Random";
                string currentFloorBossName = _roomDebugCommandService != null
                    ? _roomDebugCommandService.GetCurrentFloorBossName()
                    : "None";
                GUI.Label(
                    new Rect(contentRect.x, contentRect.y, contentRect.width, 20f),
                    GetLocalizedFallback("gui.room.boss.current_floor_prefix", "Current floor Boss: ", "本层 Boss：") + currentFloorBossName,
                    _hintStyle);
                GUI.Label(
                    new Rect(contentRect.x, contentRect.y + 24f, contentRect.width, 20f),
                    GetLocalizedFallback("gui.room.boss.next_floor_prefix", "Next floor Boss: ", "下一层 Boss：") + currentBossNames,
                    _hintStyle);
                GUI.Label(
                    new Rect(contentRect.x, contentRect.y + 48f, contentRect.width, 20f),
                    GetLocalizedFallback(
                        "gui.room.boss.hint",
                        "Select a Boss before the next floor is generated; no selection uses the first Boss.",
                        "在下一层生成前选择 Boss；不选择时使用第一个 Boss。"),
                    _hintStyle);

                if (bossOptions.Count == 0)
                {
                    GUI.Label(
                        new Rect(contentRect.x, contentRect.y + 76f, contentRect.width, 20f),
                        GetLocalizedFallback("gui.room.boss.empty", "No Boss options are available for the next floor.", "下一层没有可选择的 Boss。"),
                        _hintStyle);
                    return;
                }

                const int bossOptionsPerRow = 4;
                float optionsTop = contentRect.y + 76f;
                for (int index = 0; index < bossOptions.Count; index++)
                {
                    int row = index / bossOptionsPerRow;
                    int column = index % bossOptionsPerRow;
                    Rect buttonRect = new Rect(
                        contentRect.x + (column * (buttonWidth + ButtonGap)),
                        optionsTop + (row * (controlHeight + ButtonGap)),
                        buttonWidth,
                        controlHeight);
                    RoomBossOption bossOption = bossOptions[index];
                    GUIStyle style = (object)player != null && string.Equals(bossOption.BossName, currentBossNames, System.StringComparison.Ordinal)
                        ? _enabledButtonStyle
                        : _buttonStyle;
                    if (DrawControllerButton(buttonRect, GetBossRoomControlId(index), GetBossOptionLabel(bossOption, index, bossOptions), style))
                    {
                        ExecuteSwitchBoss(player, bossOption, logger);
                    }
                }

                string selectedBossName = currentBossNames;
                List<RoomBossOption> roomOptions = !string.Equals(selectedBossName, "Random", System.StringComparison.Ordinal)
                    ? GetBossRoomOptions(selectedBossName)
                    : new List<RoomBossOption>();
                if (roomOptions.Count <= 1)
                {
                    return;
                }

                int bossRowCount = (bossOptions.Count + bossOptionsPerRow - 1) / bossOptionsPerRow;
                float roomTitleY = optionsTop + (bossRowCount * (controlHeight + ButtonGap)) + 4f;
                GUI.Label(
                    new Rect(contentRect.x, roomTitleY, contentRect.width, 20f),
                    GetLocalizedFallback("gui.room.boss.room_title", "Room layout", "房间布局"),
                    _hintStyle);
                float roomOptionsTop = roomTitleY + 24f;
                for (int index = 0; index < roomOptions.Count; index++)
                {
                    int row = index / bossOptionsPerRow;
                    int column = index % bossOptionsPerRow;
                    Rect buttonRect = new Rect(
                        contentRect.x + (column * (buttonWidth + ButtonGap)),
                        roomOptionsTop + (row * (controlHeight + ButtonGap)),
                        buttonWidth,
                        controlHeight);
                    RoomBossOption roomOption = roomOptions[index];
                    GUIStyle style = BossManager.PriorFloorSelectedBossRoom == roomOption.BossRoomPrototype
                        ? _enabledButtonStyle
                        : _buttonStyle;
                    string roomLabel = _roomDebugCommandService != null
                        ? _roomDebugCommandService.GetBossRoomDisplayName(roomOption)
                        : "Unknown Room";
                    if (DrawControllerButton(buttonRect, GetBossRoomVariantControlId(index), roomLabel, style))
                    {
                        ExecuteSwitchBoss(player, roomOption, logger);
                    }
                }
            }
            finally
            {
                CompleteBossSelectionPagePerformanceTrace("Draw.complete", bossOptionCount, drawStartedAtTimestamp);
            }
        }

        private void DrawRoomSectionButton(Rect rect, string controlId, RoomMenuSection section, string label)
        {
            DrawRoomSectionButton(rect, controlId, section, label, true);
        }

        private void DrawRoomSectionButton(Rect rect, string controlId, RoomMenuSection section, string label, bool isEnabled)
        {
            GUIStyle style = !isEnabled
                ? _pickupFilterDisabledButtonStyle
                : (_roomMenuSection == section ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle);
            if (DrawControllerButton(rect, controlId, label, style) && isEnabled)
            {
                SetRoomMenuSection(section);
            }
        }

        private void SetRoomMenuSection(RoomMenuSection section)
        {
            if (_roomMenuSection == section)
            {
                return;
            }

            if (_roomMenuSection == RoomMenuSection.Boss)
            {
                CancelBossSelectionPagePerformanceTrace();
            }

            _roomMenuSection = section;
            if (section == RoomMenuSection.Boss)
            {
                BeginBossSelectionPagePerformanceTrace();
            }
        }

        private void DrawRoomPlaceholderSection(Rect contentRect, string title, string hint)
        {
            GUI.Label(
                new Rect(contentRect.x, contentRect.y, contentRect.width, 20f),
                title,
                _titleStyle);
            GUI.Label(
                new Rect(contentRect.x, contentRect.y + 28f, contentRect.width, 20f),
                hint,
                _hintStyle);
        }

        private ControllerFocusEntry[] GetRoomCommandPageFocusEntries()
        {
            ControllerFocusEntry[] sectionFocusEntries = RoomSectionCommandPageFocusEntries;
            if (_roomMenuSection == RoomMenuSection.Neutral)
            {
                return BuildCommandPageFocusEntries(
                    sectionFocusEntries,
                    RoomNeutralCommandPageFocusEntries);
            }

            if (_roomMenuSection == RoomMenuSection.Npc)
            {
                return BuildCommandPageFocusEntries(
                    sectionFocusEntries,
                    RoomNpcCommandPageFocusEntries);
            }

            if (_roomMenuSection == RoomMenuSection.Rewind)
            {
                bool recordingEnabled = _roomDebugCommandService != null && _roomDebugCommandService.IsRoomEnemyRefreshRecordingEnabled;
                if (!recordingEnabled &&
                    (string.Equals(_commandPageFocusedControlId, "cmd.room.rewind.shortcut", System.StringComparison.Ordinal) ||
                     string.Equals(_commandPageFocusedControlId, "cmd.room.rewind.shortcut.clear", System.StringComparison.Ordinal)))
                {
                    _commandPageFocusedControlId = "cmd.room.enemy_refresh_recording";
                }

                return BuildCommandPageFocusEntries(
                    sectionFocusEntries,
                    recordingEnabled ? RoomRewindCommandPageFocusEntries : RoomRewindDisabledCommandPageFocusEntries);
            }

            if (_roomMenuSection == RoomMenuSection.Boss)
            {
                return BuildCommandPageFocusEntries(sectionFocusEntries, BuildRoomBossCommandPageFocusEntries());
            }

            return BuildCommandPageFocusEntries(sectionFocusEntries, RoomChestCommandPageFocusEntries);
        }

        private CommandPageActionBinding[] GetRoomCommandPageActionBindings(PlayerController player)
        {
            if (_roomMenuSection == RoomMenuSection.Boss)
            {
                List<CommandPageActionBinding> bossBindings = new List<CommandPageActionBinding>();
                List<RoomBossOption> bossOptions = GetBossSelectionBossOptions();
                for (int index = 0; index < bossOptions.Count; index++)
                {
                    RoomBossOption bossOption = bossOptions[index];
                    bossBindings.Add(new CommandPageActionBinding(
                        GetBossRoomControlId(index),
                        delegate { ExecuteSwitchBoss(player, bossOption, null); }));
                }

                string selectedBossName = _roomDebugCommandService != null
                    ? _roomDebugCommandService.GetSelectedBossName()
                    : "Random";
                List<RoomBossOption> roomOptions = !string.Equals(selectedBossName, "Random", System.StringComparison.Ordinal)
                    ? GetBossRoomOptions(selectedBossName)
                    : new List<RoomBossOption>();
                if (roomOptions.Count > 1)
                {
                    for (int index = 0; index < roomOptions.Count; index++)
                    {
                        RoomBossOption roomOption = roomOptions[index];
                        bossBindings.Add(new CommandPageActionBinding(
                            GetBossRoomVariantControlId(index),
                            delegate { ExecuteSwitchBoss(player, roomOption, null); }));
                    }
                }

                return bossBindings.ToArray();
            }

            return new[]
            {
                new CommandPageActionBinding("cmd.room.chest_tier.brown", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Brown); }),
                new CommandPageActionBinding("cmd.room.chest_tier.blue", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Blue); }),
                new CommandPageActionBinding("cmd.room.chest_tier.green", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Green); }),
                new CommandPageActionBinding("cmd.room.chest_tier.red", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Red); }),
                new CommandPageActionBinding("cmd.room.chest_tier.black", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Black); }),
                new CommandPageActionBinding("cmd.room.chest_tier.synergy", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Synergy); }),
                new CommandPageActionBinding("cmd.room.chest_tier.rainbow", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Rainbow); }),
                new CommandPageActionBinding("cmd.room.enemy_refresh_recording", delegate { ExecuteToggleRoomEnemyRefreshRecording(null); }),
                new CommandPageActionBinding("cmd.room.enemy_refresh_method", delegate { ExecuteCycleRoomEnemyRefreshMethod(null); }),
                new CommandPageActionBinding("cmd.room.enemy_refresh_method.info", delegate { OpenCommandInfoPage(CommandInfoPage.RefreshMethod); }),
                new CommandPageActionBinding("cmd.room.rewind.shortcut", delegate { BeginRoomEnemyRewindShortcutCapture(); }),
                new CommandPageActionBinding("cmd.room.rewind.shortcut.clear", delegate { ClearRoomEnemyRewindShortcut(); }),
                new CommandPageActionBinding("cmd.room.player_rewind.info", delegate { OpenCommandInfoPage(CommandInfoPage.PlayerRewind); }),
                new CommandPageActionBinding("cmd.room.rewind_cleanup", delegate { ExecuteToggleRoomRewindCleanup(null); }),
                new CommandPageActionBinding("cmd.room.rewind_cleanup.info", delegate { OpenCommandInfoPage(CommandInfoPage.Cleanup); }),
                new CommandPageActionBinding("cmd.room.player_rewind", delegate { ExecuteTogglePlayerRewind(null); }),
                new CommandPageActionBinding("cmd.room.enemy_refresh_execute", delegate { ExecuteSelectedRoomEnemyRefresh(player, null); }),
                new CommandPageActionBinding("cmd.room.spawn_gunber_muncher", delegate { ExecuteSpawnGunberMuncher(player, null); }),
                new CommandPageActionBinding("cmd.room.spawn_evil_muncher", delegate { ExecuteSpawnEvilMuncher(player, null); }),
                new CommandPageActionBinding("cmd.room.unlock_cadence_ox", delegate { ExecuteUnlockCadenceOx(null); }),
                new CommandPageActionBinding("cmd.room.unlock_goopton", delegate { ExecuteUnlockGoopton(null); }),
                new CommandPageActionBinding("cmd.room.unlock_doug", delegate { ExecuteUnlockDoug(null); }),
            };
        }


        private void DrawRoomChestTierButton(Rect rect, string controlId, RoomChestTier chestTier, PlayerController player, ManualLogSource logger)
        {
            GUIStyle style = _selectedRoomChestTier == chestTier ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle;
            if (DrawControllerButton(rect, controlId, GetRoomChestTierLabel(chestTier), style))
            {
                _selectedRoomChestTier = chestTier;
                ExecuteSpawnChest(player, logger, chestTier);
            }
        }

        private static string GetRoomChestTierLabel(RoomChestTier chestTier)
        {
            switch (chestTier)
            {
                case RoomChestTier.Brown:
                    return GetLocalizedFallback("label.room.chest_tier.brown", "Brown", "棕箱");
                case RoomChestTier.Blue:
                    return GetLocalizedFallback("label.room.chest_tier.blue", "Blue", "蓝箱");
                case RoomChestTier.Green:
                    return GetLocalizedFallback("label.room.chest_tier.green", "Green", "绿箱");
                case RoomChestTier.Red:
                    return GetLocalizedFallback("label.room.chest_tier.red", "Red", "红箱");
                case RoomChestTier.Black:
                    return GetLocalizedFallback("label.room.chest_tier.black", "Black", "黑箱");
                case RoomChestTier.Synergy:
                    return GetLocalizedFallback("label.room.chest_tier.synergy", "Synergy", "协同箱");
                case RoomChestTier.Rainbow:
                    return GetLocalizedFallback("label.room.chest_tier.rainbow", "Rainbow", "彩虹箱");
                default:
                    return GetLocalizedFallback("label.room.chest_tier.brown", "Brown", "棕箱");
            }
        }

        private void ExecuteSpawnChest(PlayerController player, ManualLogSource logger)
        {
            ExecuteSpawnChest(player, logger, _selectedRoomChestTier);
        }

        private void ExecuteSpawnChest(PlayerController player, ManualLogSource logger, RoomChestTier chestTier)
        {
            _selectedRoomChestTier = chestTier;
            ShowRoomActionResult(RoomDebugCommandService.SpawnChest(player, chestTier), logger);
        }

        private void ExecuteRefreshRoomEnemies(PlayerController player, ManualLogSource logger)
        {
            ShowRoomActionResult(_roomDebugCommandService.RefreshCurrentRoomEnemies(player, logger), logger);
        }

        private void ExecuteSelectedRoomEnemyRefresh(PlayerController player, ManualLogSource logger)
        {
            if (_bossRushService != null && _bossRushService.IsActive)
            {
                ShowStatus(
                    GetLocalizedFallback(
                        "result.room.rewind.boss_rush_disabled",
                        "Rewind is temporarily disabled during Boss Rush because it can cause a game UI bug. A fix is in progress.",
                        "Boss Rush 期间暂时禁用回溯，回溯可能导致游戏 UI 出现 Bug。修复正在进行中。"),
                    true);
                return;
            }

            if (_roomDebugCommandService == null || !_roomDebugCommandService.IsRoomEnemyRefreshRecordingEnabled)
            {
                ShowStatus(
                    GetLocalizedFallback(
                        "result.room.rewind.recording_required",
                        "Enable Rewind before spawning or rewinding the room.",
                        "请先开启回溯功能，再生成或回溯房间。"),
                    true);
                return;
            }

            if (_roomEnemyRefreshMethod == RoomEnemyRefreshMethod.RespawnEnemies)
            {
                ExecuteRefreshTemplateRoomEnemies(player, logger);
                return;
            }

            ExecuteRefreshRoomEnemies(player, logger);
        }

        private void ExecuteToggleRoomEnemyRefreshRecording(ManualLogSource logger)
        {
            GrantCommandExecutionResult result = _roomDebugCommandService != null
                ? _roomDebugCommandService.ToggleRoomEnemyRefreshRecording()
                : GrantCommandExecutionResult.Localized(false, "result.room.refresh_enemies.failed");
            ShowRoomActionResult(result, logger);
        }

        private void ExecuteTogglePlayerRewind(ManualLogSource logger)
        {
            if (_roomDebugCommandService == null || !_roomDebugCommandService.IsRoomEnemyRefreshRecordingEnabled)
            {
                ShowStatus(
                    GetLocalizedFallback(
                        "result.room.rewind.recording_required",
                        "Enable Rewind before changing player rewind.",
                        "请先开启回溯功能，再修改玩家回溯设置。"),
                    true);
                return;
            }

            GrantCommandExecutionResult result = _roomDebugCommandService != null
                ? _roomDebugCommandService.TogglePlayerRewind()
                : GrantCommandExecutionResult.Localized(false, "result.room.player_rewind.unavailable");
            ShowRoomActionResult(result, logger);
        }

        private void ExecuteToggleRoomRewindCleanup(ManualLogSource logger)
        {
            if (_roomDebugCommandService == null || !_roomDebugCommandService.IsRoomEnemyRefreshRecordingEnabled)
            {
                ShowStatus(
                    GetLocalizedFallback(
                        "result.room.rewind.recording_required",
                        "Enable Rewind before changing rewind cleanup.",
                        "请先开启回溯功能，再修改房间残留清理设置。"),
                    true);
                return;
            }

            GrantCommandExecutionResult result = _roomDebugCommandService != null
                ? _roomDebugCommandService.ToggleRoomRewindCleanup()
                : GrantCommandExecutionResult.Localized(false, "result.room.rewind_cleanup.unavailable");
            ShowRoomActionResult(result, logger);
        }

        private void ExecuteRefreshTemplateRoomEnemies(PlayerController player, ManualLogSource logger)
        {
            ShowRoomActionResult(_roomDebugCommandService.RefreshCurrentRoomEnemiesFromTemplate(player, logger), logger);
        }

        private void ExecuteCycleRoomEnemyRefreshMethod(ManualLogSource logger)
        {
            if (_roomDebugCommandService == null || !_roomDebugCommandService.IsRoomEnemyRefreshRecordingEnabled)
            {
                ShowStatus(
                    GetLocalizedFallback(
                        "result.room.rewind.recording_required",
                        "Enable Rewind before selecting the room refresh mode.",
                        "请先开启回溯功能，再选择房间刷新模式。"),
                    true);
                return;
            }

            if (_roomDebugCommandService != null)
            {
                _roomDebugCommandService.EnsureRoomEnemyRefreshRecordingEnabled();
            }

            _roomEnemyRefreshMethod = _roomEnemyRefreshMethod == RoomEnemyRefreshMethod.Rewind
                ? RoomEnemyRefreshMethod.RespawnEnemies
                : RoomEnemyRefreshMethod.Rewind;
            if (_roomEnemyRefreshMethodSetter != null)
            {
                _roomEnemyRefreshMethodSetter(_roomEnemyRefreshMethod == RoomEnemyRefreshMethod.Rewind ? "rewind" : "respawn");
            }
            ShowStatus(GuiText.Get("result.room.rewind.method_changed", GetRoomEnemyRefreshMethodName()), false);

            if (logger != null)
            {
                logger.LogInfo(EtgGameplayDashboardLog.Command("Room enemy refresh method changed to " + _roomEnemyRefreshMethod + "."));
            }
        }

        private string GetRoomEnemyRefreshMethodLabel()
        {
            return GuiText.Get("gui.room.rewind.method", GetRoomEnemyRefreshMethodName());
        }

        private string GetRoomEnemyRefreshMethodName()
        {
            return _roomEnemyRefreshMethod == RoomEnemyRefreshMethod.RespawnEnemies
                ? GetLocalizedFallback("gui.room.button.refresh_template_enemies", "Respawn Enemies", "重新生成怪物")
                : GetLocalizedFallback("gui.room.button.refresh_enemies", "Rewind Room", "回溯房间");
        }

        private void ExecuteSpawnGunberMuncher(PlayerController player, ManualLogSource logger)
        {
            ShowRoomActionResult(_roomDebugCommandService.SpawnGunberMuncher(player, logger), logger);
        }

        private void ExecuteSpawnEvilMuncher(PlayerController player, ManualLogSource logger)
        {
            ShowRoomActionResult(_roomDebugCommandService.SpawnEvilMuncher(player, logger), logger);
        }

        private void ExecuteUnlockCadenceOx(ManualLogSource logger)
        {
            ShowRoomActionResult(_roomDebugCommandService.UnlockCadenceOx(), logger);
        }

        private void ExecuteUnlockGoopton(ManualLogSource logger)
        {
            ShowRoomActionResult(_roomDebugCommandService.UnlockGoopton(), logger);
        }

        private void ExecuteUnlockDoug(ManualLogSource logger)
        {
            ShowRoomActionResult(_roomDebugCommandService.UnlockDoug(), logger);
        }

        private void ShowRoomActionResult(GrantCommandExecutionResult executionResult, ManualLogSource logger)
        {
            ShowStatus(executionResult.Message, GetRoomActionStatusSeverity(executionResult));

            if (logger == null)
            {
                return;
            }

            if (executionResult.Succeeded)
            {
                logger.LogInfo(EtgGameplayDashboardLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(EtgGameplayDashboardLog.Command(executionResult.LogMessage));
            }
        }

        private static StatusSeverity GetRoomActionStatusSeverity(GrantCommandExecutionResult executionResult)
        {
            if (executionResult != null && executionResult.Succeeded)
            {
                return StatusSeverity.Success;
            }

            string key = executionResult != null ? executionResult.LocalizationKey : string.Empty;
            if (key == "result.room.refresh_enemies.room_not_cleared" ||
                key == "result.room.rewind.boss_clear_pending" ||
                key == "result.room.rewind.boss_death_animation_pending" ||
                key == "result.room.refresh_enemies.no_snapshot" ||
                key == "result.room.rewind.recording_disabled" ||
                key == "result.room.rewind.no_enemies" ||
                key == "result.room.respawn_enemies.no_enemies" ||
                key == "result.room.enemy_refresh.corridor" ||
                key == "result.room.enemy_refresh.player_not_in_room")
            {
                return StatusSeverity.Warning;
            }

            return StatusSeverity.Failure;
        }
    }
}
