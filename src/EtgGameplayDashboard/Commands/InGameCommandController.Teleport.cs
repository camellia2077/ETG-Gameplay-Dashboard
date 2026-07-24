// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using BepInEx.Logging;
using UnityEngine;
using Dungeonator;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private void DrawTeleportPanelIfEnabled(Rect mainPanelRect, ManualLogSource logger)
        {
            if (!_showTeleportPanel)
            {
                return;
            }

            float panelTop = Mathf.Max(8f, Mathf.Min(mainPanelRect.y, GetScaledScreenHeight() - TeleportPanelHeight - 8f));
            Rect panelRect = new Rect(
                mainPanelRect.x - TeleportPanelWidth - ButtonGap,
                panelTop,
                TeleportPanelWidth,
                TeleportPanelHeight);
            GUI.Box(ExpandPanelBorderRect(panelRect), GUIContent.none, _panelStyle);

            Rect closeButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            if (GUI.Button(closeButtonRect, GuiText.Get("gui.common.back"), _buttonStyle))
            {
                CloseTeleportPanel();
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, panelRect.width - ButtonWidth - 32f, 24f),
                GuiText.Get("gui.teleport.title"),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 42f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.teleport.hint"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 60f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.teleport.controller_hint"),
                _hintStyle);

            float rowY = panelRect.y + 90f;
            for (int i = 0; i < TeleportOptions.Length; i++)
            {
                DrawTeleportOptionButton(panelRect, ref rowY, TeleportOptions[i], i, logger);
            }
        }

        private void DrawTeleportOptionButton(Rect panelRect, ref float rowY, TeleportOption option, int optionIndex, ManualLogSource logger)
        {
            Rect buttonRect = new Rect(panelRect.x + 14f, rowY, panelRect.width - 28f, 28f);
            GUIStyle buttonStyle = optionIndex == _teleportSelectedIndex
                ? _commandContentFocusButtonStyle
                : _commandContentButtonStyle;
            if (GUI.Button(buttonRect, GuiText.Get(option.LabelKey), buttonStyle))
            {
                _teleportSelectedIndex = optionIndex;
                ExecuteTeleport(option, logger);
            }

            rowY += 32f;
        }

        private void ExecuteTeleport(TeleportOption option, ManualLogSource logger)
        {
            LogTeleportInfo(
                logger,
                "Teleport requested. Token=" +
                (option != null ? option.CommandToken : "<null>") +
                ", Command=" +
                (option != null ? option.CommandText : "<null>") +
                ", Runtime=" +
                DescribeTeleportRuntimeState(GameManager.Instance) +
                ".");
            GrantCommandExecutionResult executionResult = TryTeleport(option, logger);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                _inputText = option.CommandText;
                CloseTeleportPanel();
                _focusInputField = true;
            }

            if (logger == null)
            {
                return;
            }

            if (executionResult.Succeeded)
            {
                logger.LogInfo(EtgGameplayDashboardLog.Command(executionResult.LogMessage));
            }
            else
            {
                logger.LogWarning(EtgGameplayDashboardLog.Command(executionResult.LogMessage));
            }
        }

        private GrantCommandExecutionResult TryTeleport(TeleportOption option, ManualLogSource logger)
        {
            if (option == null || string.IsNullOrEmpty(option.CommandToken))
            {
                LogTeleportWarning(logger, "Teleport unavailable before resolve. Reason=MissingOption, UnityScene=" + GetLoadedUnitySceneName() + ".");
                return GrantCommandExecutionResult.Localized(false, "result.teleport.unavailable");
            }

            if (_bossRushService != null && _bossRushService.IsActive)
            {
                LogTeleportInfo(logger, "Teleport blocked because Boss Rush is active. Token=" + option.CommandToken + ", Command=" + option.CommandText + ".");
                return GrantCommandExecutionResult.Localized(false, "result.teleport.boss_rush_active");
            }

            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager == null)
            {
                LogTeleportWarning(
                    logger,
                    "Teleport unavailable before resolve. Reason=MissingGameManager, Token=" +
                    option.CommandToken +
                    ", Command=" +
                    option.CommandText +
                    ", UnityScene=" +
                    GetLoadedUnitySceneName() +
                    ".");
                return GrantCommandExecutionResult.Localized(false, "result.teleport.unavailable");
            }

            try
            {
                EtgFloorDefinition floorDefinition = ResolveTeleportFloorDefinition(gameManager, option, logger);
                if (floorDefinition == null)
                {
                    return GrantCommandExecutionResult.Localized(false, "result.teleport.unavailable");
                }

                if (gameManager.IsFoyer && !floorDefinition.CanLoadFromFoyer)
                {
                    bool deferredTeleportStarted =
                        _deferredTeleportRequestHandler != null &&
                        _deferredTeleportRequestHandler(floorDefinition, option.LabelKey, option.CommandText);
                    if (!deferredTeleportStarted)
                    {
                        LogTeleportWarning(
                            logger,
                            "Teleport blocked in foyer. Token=" +
                            option.CommandToken +
                            ", LoadScene=" +
                            floorDefinition.LoadSceneName +
                            ", NormalizedLoadScene=" +
                            floorDefinition.NormalizedSceneName +
                            ", Reason=FloorRequiresActiveRun, Runtime=" +
                            DescribeTeleportRuntimeState(gameManager) +
                            ".");
                        return GrantCommandExecutionResult.Localized(false, "result.teleport.unavailable");
                    }

                    LogTeleportInfo(
                        logger,
                        "Teleport deferred through first run floor. Token=" +
                        option.CommandToken +
                        ", LoadScene=" +
                        floorDefinition.LoadSceneName +
                        ", NormalizedLoadScene=" +
                        floorDefinition.NormalizedSceneName +
                        ", Runtime=" +
                        DescribeTeleportRuntimeState(gameManager) +
                        ".");
                    return new GrantCommandExecutionResult(
                        true,
                        GuiText.Get("result.teleport.success", GuiText.Get(option.LabelKey)),
                        GuiText.GetEnglish("result.teleport.success", GuiText.GetEnglish(option.LabelKey)) + " DeferredViaFirstRunFloor.");
                }

                string loadSceneName = floorDefinition.LoadSceneName;

                if (gameManager.IsFoyer && (object)Foyer.Instance != null)
                {
                    LogTeleportInfo(logger, "Departing foyer before teleport. Token=" + option.CommandToken + ", LoadScene=" + loadSceneName + ".");
                    Foyer.Instance.OnDepartedFoyer();
                }
                else if (gameManager.IsFoyer)
                {
                    LogTeleportWarning(logger, "Teleport is running from foyer, but Foyer.Instance is not available. Token=" + option.CommandToken + ", LoadScene=" + loadSceneName + ".");
                }

                LogTeleportInfo(
                    logger,
                    "Loading teleport destination. Token=" +
                    option.CommandToken +
                    ", LoadScene=" +
                    loadSceneName +
                    ", Command=" +
                    option.CommandText +
                    ", RuntimeBeforeLoad=" +
                    DescribeTeleportRuntimeState(gameManager) +
                    ".");
                gameManager.LoadCustomLevel(loadSceneName);
                LogTeleportInfo(
                    logger,
                    "LoadCustomLevel issued. Token=" +
                    option.CommandToken +
                    ", LoadScene=" +
                    loadSceneName +
                    ", UnitySceneAfterCall=" +
                    GetLoadedUnitySceneName() +
                    ", RuntimeAfterLoadCall=" +
                    DescribeTeleportRuntimeState(gameManager) +
                    ".");
                return new GrantCommandExecutionResult(
                    true,
                    GuiText.Get("result.teleport.success", GuiText.Get(option.LabelKey)),
                    GuiText.GetEnglish("result.teleport.success", GuiText.GetEnglish(option.LabelKey)) + " Command=" + option.CommandText + ".");
            }
            catch (Exception exception)
            {
                LogTeleportWarning(
                    logger,
                    "Teleport failed with exception. Token=" +
                    option.CommandToken +
                    ", Command=" +
                    option.CommandText +
                    ", Exception=" +
                    exception +
                    ".");
                return new GrantCommandExecutionResult(
                    false,
                    GuiText.Get("result.teleport.failure", GuiText.Get(option.LabelKey)),
                    GuiText.GetEnglish("result.teleport.failure", GuiText.GetEnglish(option.LabelKey)) + " " + exception.GetType().Name + ".");
            }
        }

        private EtgFloorDefinition ResolveTeleportFloorDefinition(GameManager gameManager, TeleportOption option, ManualLogSource logger)
        {
            string levelToken = option.CommandToken.Trim();
            EtgFloorDefinition floorDefinition;
            bool floorFound = EtgFloorSceneResolver.TryGetFloor(levelToken, out floorDefinition);
            string loadSceneName = floorFound ? floorDefinition.LoadSceneName : string.Empty;
            string normalizedLoadSceneName = floorFound ? floorDefinition.NormalizedSceneName : string.Empty;
            string canLoadFromFoyer = floorFound ? floorDefinition.CanLoadFromFoyer.ToString() : "<unknown>";

            string resolveMessage =
                "Teleport resolve. Token=" +
                levelToken +
                ", Command=" +
                option.CommandText +
                ", LabelKey=" +
                option.LabelKey +
                ", FloorDefinition=" +
                (floorFound ? "found" : "null") +
                ", LoadScene=" +
                (string.IsNullOrEmpty(loadSceneName) ? "<empty>" : loadSceneName) +
                ", NormalizedLoadScene=" +
                (string.IsNullOrEmpty(normalizedLoadSceneName) ? "<empty>" : normalizedLoadSceneName) +
                ", CanLoadFromFoyer=" +
                canLoadFromFoyer +
                ", GameManagerIsFoyer=" +
                gameManager.IsFoyer +
                ", FoyerInstance=" +
                ((object)Foyer.Instance != null ? "present" : "null") +
                ", UnityScene=" +
                GetLoadedUnitySceneName() +
                ", Runtime=" +
                DescribeTeleportRuntimeState(gameManager) +
                ".";

            if (string.IsNullOrEmpty(loadSceneName))
            {
                LogTeleportWarning(
                    logger,
                    resolveMessage +
                    " Result=Unavailable, KnownFloors=[" +
                    EtgFloorSceneResolver.DescribeKnownFloors() +
                    "].");
                return null;
            }

            LogTeleportInfo(logger, resolveMessage + " Result=Resolved.");
            return floorDefinition;
        }

        private static string GetLoadedUnitySceneName()
        {
#pragma warning disable 618
            string sceneName = Application.loadedLevelName;
#pragma warning restore 618
            return string.IsNullOrEmpty(sceneName) ? "<empty>" : sceneName;
        }

        private static string DescribeTeleportRuntimeState(GameManager gameManager)
        {
            if ((object)gameManager == null)
            {
                return "GameManager=null";
            }

            PlayerController player = gameManager.PrimaryPlayer;
            Dungeon dungeon = gameManager.Dungeon;
            RoomHandler room = (object)player != null ? player.CurrentRoom : null;
            string roomCategory = "<none>";
            if ((object)room != null && room.area != null)
            {
                roomCategory = room.area.PrototypeRoomCategory.ToString();
            }

            return
                "GameManager=ready" +
                ", IsFoyer=" + gameManager.IsFoyer +
                ", PrimaryPlayer=" + ((object)player != null ? "present" : "null") +
                ", PlayerHealthHaver=" + (((object)player != null && (object)player.healthHaver != null) ? "present" : "null") +
                ", PlayerRoom=" + ((object)room != null ? GetRoomDebugLabel(room) : "<none>") +
                ", PlayerRoomCategory=" + roomCategory +
                ", Dungeon=" + ((object)dungeon != null ? "present" : "null") +
                ", DungeonData=" + (((object)dungeon != null && dungeon.data != null) ? "present" : "null") +
                ", FoyerInstance=" + ((object)Foyer.Instance != null ? "present" : "null");
        }

        private static string GetRoomDebugLabel(RoomHandler room)
        {
            if ((object)room == null)
            {
                return "<null>";
            }

            IntVector2 basePosition = room.area != null ? room.area.basePosition : IntVector2.Zero;
            return basePosition.x + "," + basePosition.y;
        }

        private void LogTeleportInfo(ManualLogSource logger, string message)
        {
            if (!ShouldLogFloorTeleportVerbose())
            {
                return;
            }

            if (logger != null)
            {
                logger.LogInfo(EtgGameplayDashboardLog.Command(message));
            }
        }

        private static void LogTeleportWarning(ManualLogSource logger, string message)
        {
            if (logger != null)
            {
                logger.LogWarning(EtgGameplayDashboardLog.Command(message));
            }
        }

        private void ToggleTeleportPanel()
        {
            if (_showTeleportPanel)
            {
                LogGamepadShortcutState("Closing teleport panel.");
                CloseTeleportPanel();
                return;
            }

            OpenTeleportPanel();
        }

        private void OpenTeleportPanel()
        {
            _showTeleportPanel = true;
            _teleportSelectedIndex = 0;
            RequestGuiFocusRelease();
            LogGamepadShortcutState("Opened teleport panel. SelectedIndex=0.");
        }

        private void CloseTeleportPanel()
        {
            if (_showTeleportPanel)
            {
                LogGamepadShortcutState("Teleport panel closed. SelectedIndex=" + _teleportSelectedIndex + ".");
            }

            _showTeleportPanel = false;
            _teleportSelectedIndex = 0;
            ResetControllerNavigationAxes();
            RequestGuiFocusRelease();
        }
    }
}
