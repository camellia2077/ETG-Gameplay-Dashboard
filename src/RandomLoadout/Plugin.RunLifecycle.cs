// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using RandomLoadout.Core;
using UnityEngine;
using System;

namespace RandomLoadout
{
    public sealed partial class Plugin
    {
        private const int DeferredTeleportRequiredReadyFrames = 90;

        private void Update()
        {
            TryExportPickupCatalogOnce();

            PlayerController player = null;
            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager != null)
            {
                player = gameManager.PrimaryPlayer;
            }

            if (_commandController != null)
            {
                _commandController.Update();
                CommandPanelCursorRenderHooks.UpdateCursorOverride(_commandController.IsVisibleForDiagnostics);
            }

            if (_nearbyPickupTipService != null)
            {
                long startedAtTimestamp = BeginPerformanceSample();
                _nearbyPickupTipService.Update(player, IsPickupInfoOverlayEnabled());
                LogPerformanceStep("NearbyPickupTipService.Update", startedAtTimestamp);
            }

            if (_rapidFireToggleService != null)
            {
                long startedAtTimestamp = BeginPerformanceSample();
                _rapidFireToggleService.Update(player);
                LogPerformanceStep("RapidFireToggleService.Update", startedAtTimestamp);
            }

            if (_autoReloadToggleService != null &&
                !(_ammoModeToggleService != null && _ammoModeToggleService.Mode == AmmoMode.NoConsume))
            {
                // No-consume mode intentionally preserves the current clip state, including "last bullet"
                // behaviors that some guns use for special effects. Infinite-reserve mode still reloads.
                long startedAtTimestamp = BeginPerformanceSample();
                _autoReloadToggleService.Update(player);
                LogPerformanceStep("AutoReloadToggleService.Update", startedAtTimestamp);
            }

            if (_blankNoConsumeToggleService != null)
            {
                long startedAtTimestamp = BeginPerformanceSample();
                _blankNoConsumeToggleService.Update(player);
                LogPerformanceStep("BlankNoConsumeToggleService.Update", startedAtTimestamp);
            }

            if (_armorNoConsumeToggleService != null)
            {
                long startedAtTimestamp = BeginPerformanceSample();
                _armorNoConsumeToggleService.Update(player);
                LogPerformanceStep("ArmorNoConsumeToggleService.Update", startedAtTimestamp);
            }

            if (_keyNoConsumeToggleService != null)
            {
                long startedAtTimestamp = BeginPerformanceSample();
                _keyNoConsumeToggleService.Update(player);
                LogPerformanceStep("KeyNoConsumeToggleService.Update", startedAtTimestamp);
            }

            if (_currencyNoConsumeToggleService != null)
            {
                long startedAtTimestamp = BeginPerformanceSample();
                _currencyNoConsumeToggleService.Update(player);
                LogPerformanceStep("CurrencyNoConsumeToggleService.Update", startedAtTimestamp);
            }

            if (_invincibilityToggleService != null)
            {
                long startedAtTimestamp = BeginPerformanceSample();
                _invincibilityToggleService.Update(player);
                LogPerformanceStep("InvincibilityToggleService.Update", startedAtTimestamp);
            }

            if (_ammoModeToggleService != null)
            {
                long startedAtTimestamp = BeginPerformanceSample();
                _ammoModeToggleService.Update(player);
                LogPerformanceStep("AmmoModeToggleService.Update", startedAtTimestamp);
            }

            if (_playerHealthOverrideService != null)
            {
                long startedAtTimestamp = BeginPerformanceSample();
                _playerHealthOverrideService.Update(player);
                LogPerformanceStep("PlayerHealthOverrideService.Update", startedAtTimestamp);
            }

            if (_playerActiveItemCapacityOverrideService != null)
            {
                long startedAtTimestamp = BeginPerformanceSample();
                _playerActiveItemCapacityOverrideService.Update(player);
                LogPerformanceStep("PlayerActiveItemCapacityOverrideService.Update", startedAtTimestamp);
            }

            RecordPerformanceFrame();

            if (_sceneWatcher == null || !_sceneWatcher.IsPollDue(Time.unscaledTime))
            {
                return;
            }

            _sceneWatcher.MarkPolled(Time.unscaledTime);
            TryHandleCurrentScene("poll");
        }

        private void OnNewLevelFullyLoaded()
        {
            ScheduleGameWindowFocusRetryAfterSceneReady();

            if (_bossRushService != null)
            {
                _bossRushService.NotifyLevelLoaded();
            }

            TryHandleCurrentScene("event");
        }

        private void TryHandleCurrentScene(string source)
        {
            long startedAtTimestamp = BeginPerformanceSample();
            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager == null)
            {
                return;
            }

            string sceneName;
            if (!_sceneWatcher.TryGetCurrentSceneName(gameManager, out sceneName))
            {
                return;
            }

            PlayerController player = gameManager.PrimaryPlayer;
            int playerInstanceId = (object)player != null ? player.GetInstanceID() : 0;
            RunLifecycleObservation lifecycle = _runLifecycleTracker.Observe(sceneName, playerInstanceId);
            UpdateGameplayPerformanceWindow(lifecycle);
            if (lifecycle.SceneChanged)
            {
                MarkPerformanceEvent("Scene change via " + source + ": " + lifecycle.PreviousSceneName + " -> " + lifecycle.SceneName);
                Logger.LogInfo(RandomLoadoutLog.Run("Observed scene change via " + source + ": " + lifecycle.PreviousSceneName + " -> " + lifecycle.SceneName));
                if (string.Equals(lifecycle.SceneName, CharacterSelectSceneName, StringComparison.Ordinal))
                {
                    ScheduleGameWindowFocusRetryAfterSceneReady();
                }
            }

            if (_bossRushService != null)
            {
                _bossRushService.NotifySceneObserved(lifecycle.SceneName, lifecycle.SceneChanged);
            }

            if (lifecycle.ResetKind == RunLifecycleResetKind.PrimaryPlayerChanged)
            {
                if (_runState.HasGrantedThisRun || _runState.CurrentSeed != 0)
                {
                    Logger.LogInfo(RandomLoadoutLog.Run("Detected a new PrimaryPlayer instance in scene " + lifecycle.SceneName + ". Resetting run grant state."));
                }

                _runState.Reset();
            }

            if (lifecycle.ResetKind == RunLifecycleResetKind.EnteredCharacterSelectHub)
            {
                if (_runState.HasGrantedThisRun || _runState.CurrentSeed != 0)
                {
                    Logger.LogInfo(RandomLoadoutLog.Run("Entered character select hub. Resetting run grant state."));
                }

                _runState.Reset();
                return;
            }

            if (!lifecycle.IsGrantableDungeonScene)
            {
                return;
            }

            if (TryProcessPendingTeleport(lifecycle, gameManager, player))
            {
                LogPerformanceStep("TryHandleCurrentScene", startedAtTimestamp);
                return;
            }

            if (_bossRushService != null && _bossRushService.ShouldSuppressAutomaticLoadout)
            {
                if (lifecycle.SceneChanged)
                {
                    Logger.LogInfo(RandomLoadoutLog.Run("Boss Rush is active. Suppressing automatic random loadout for scene " + lifecycle.SceneName + "."));
                }

                return;
            }

            if (lifecycle.ShouldScheduleGrant)
            {
                _runState.ScheduleGrant(Time.unscaledTime, GrantDelaySeconds);
                if (lifecycle.PlayerChanged && !lifecycle.SceneChanged)
                {
                    Logger.LogInfo(RandomLoadoutLog.Run("PrimaryPlayer changed inside scene " + lifecycle.SceneName + ". Delaying loadout grant by " + GrantDelaySeconds + " seconds."));
                }
                else
                {
                    Logger.LogInfo(RandomLoadoutLog.Run("Scene " + lifecycle.SceneName + " entered. Delaying loadout grant by " + GrantDelaySeconds + " seconds."));
                }
            }

            if (_runState.HasGrantedThisRun)
            {
                return;
            }

            if (!_enableRandomLoadoutConfig.Value)
            {
                if (lifecycle.SceneChanged)
                {
                    Logger.LogInfo(RandomLoadoutLog.Run("Automatic random loadout is disabled by config."));
                }

                return;
            }

            if ((object)player == null)
            {
                if (lifecycle.SceneChanged)
                {
                    Logger.LogInfo(RandomLoadoutLog.Run("Scene " + lifecycle.SceneName + " is active, but PrimaryPlayer is not ready yet."));
                }

                return;
            }

            if (!_runState.IsGrantReady(Time.unscaledTime))
            {
                return;
            }

            GrantConfiguredLoadout(player, lifecycle.SceneName);
            LogPerformanceStep("TryHandleCurrentScene", startedAtTimestamp);
        }

        private bool BeginDeferredTeleportFromFoyer(EtgFloorDefinition floorDefinition, string labelKey, string commandText)
        {
            long startedAtTimestamp = BeginPerformanceSample();
            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager == null || floorDefinition == null)
            {
                return false;
            }

            EtgFloorDefinition keepFloor;
            if (!EtgFloorSceneResolver.TryGetFloor("keep", out keepFloor) || keepFloor == null || string.IsNullOrEmpty(keepFloor.LoadSceneName))
            {
                Logger.LogWarning(RandomLoadoutLog.Command("Deferred teleport could not start because the Keep floor definition is unavailable."));
                return false;
            }

            _pendingTeleportFloor = floorDefinition;
            _pendingTeleportLabelKey = labelKey ?? string.Empty;
            _pendingTeleportCommandText = commandText ?? string.Empty;
            _pendingTeleportReadySceneName = string.Empty;
            _pendingTeleportReadyFrames = 0;
            MarkPerformanceEvent("Deferred teleport staged for " + floorDefinition.CommandToken);

            try
            {
                if (gameManager.IsFoyer && (object)Foyer.Instance != null)
                {
                    Foyer.Instance.OnDepartedFoyer();
                }

                LogFloorTeleportInfo(
                    "Deferred teleport staged from foyer. TargetToken=" +
                    floorDefinition.CommandToken +
                    ", TargetLoadScene=" +
                    floorDefinition.LoadSceneName +
                    ", BootstrapLoadScene=" +
                    keepFloor.LoadSceneName +
                    ".");
                gameManager.LoadCustomLevel(keepFloor.LoadSceneName);
                LogPerformanceOperation(
                    "BeginDeferredTeleportFromFoyer",
                    startedAtTimestamp,
                    "TargetToken=" + floorDefinition.CommandToken + ", BootstrapLoadScene=" + keepFloor.LoadSceneName);
                return true;
            }
            catch (Exception exception)
            {
                Logger.LogWarning(
                    RandomLoadoutLog.Command(
                        "Deferred teleport bootstrap failed. TargetToken=" +
                        floorDefinition.CommandToken +
                        ", BootstrapLoadScene=" +
                        keepFloor.LoadSceneName +
                        ", Exception=" +
                        exception +
                        "."));
                ClearPendingTeleport();
                return false;
            }
        }

        private bool TryProcessPendingTeleport(RunLifecycleObservation lifecycle, GameManager gameManager, PlayerController player)
        {
            long startedAtTimestamp = BeginPerformanceSample();
            if (_pendingTeleportFloor == null)
            {
                return false;
            }

            if (gameManager.IsFoyer)
            {
                return false;
            }

            if (string.IsNullOrEmpty(_pendingTeleportReadySceneName))
            {
                if (!lifecycle.SceneChanged)
                {
                    return true;
                }

                _pendingTeleportReadySceneName = lifecycle.SceneName;
                _pendingTeleportReadyFrames = 0;
                MarkPerformanceEvent("Deferred teleport armed in " + lifecycle.SceneName);
                LogFloorTeleportRunInfo(
                    "Deferred teleport armed after entering bootstrap floor. Scene=" +
                    lifecycle.SceneName +
                    ", TargetToken=" +
                    _pendingTeleportFloor.CommandToken +
                    ", TargetLoadScene=" +
                    _pendingTeleportFloor.LoadSceneName +
                    ".");
                return true;
            }

            if (!string.Equals(_pendingTeleportReadySceneName, lifecycle.SceneName, StringComparison.Ordinal))
            {
                LogFloorTeleportRunInfo(
                    "Deferred teleport re-armed on new scene. PreviousScene=" +
                    _pendingTeleportReadySceneName +
                    ", Scene=" +
                    lifecycle.SceneName +
                    ", TargetToken=" +
                    _pendingTeleportFloor.CommandToken +
                    ".");
                _pendingTeleportReadySceneName = lifecycle.SceneName;
                MarkPerformanceEvent("Deferred teleport re-armed in " + lifecycle.SceneName);
                return true;
            }

            string readinessSummary;
            if (!IsDeferredTeleportReady(gameManager, player, out readinessSummary))
            {
                if (_pendingTeleportReadyFrames != 0)
                {
                    LogFloorTeleportRunInfo(
                        "Deferred teleport readiness reset. Scene=" +
                        lifecycle.SceneName +
                        ", ReadyFrames=" +
                        _pendingTeleportReadyFrames +
                        "/" +
                        DeferredTeleportRequiredReadyFrames +
                        ", Reason=" +
                        readinessSummary +
                        ".");
                }

                _pendingTeleportReadyFrames = 0;
                LogPerformanceStep("TryProcessPendingTeleport", startedAtTimestamp);
                return true;
            }

            _pendingTeleportReadyFrames++;
            if (_pendingTeleportReadyFrames == 1 ||
                _pendingTeleportReadyFrames % 30 == 0 ||
                _pendingTeleportReadyFrames == DeferredTeleportRequiredReadyFrames)
            {
                MarkPerformanceEvent("Deferred teleport ready " + _pendingTeleportReadyFrames + "/" + DeferredTeleportRequiredReadyFrames);
                LogFloorTeleportRunInfo(
                    "Deferred teleport ready check " +
                    _pendingTeleportReadyFrames +
                    "/" +
                    DeferredTeleportRequiredReadyFrames +
                    ". Scene=" +
                    lifecycle.SceneName +
                    ", " +
                    readinessSummary +
                    ".");
            }

            if (_pendingTeleportReadyFrames < DeferredTeleportRequiredReadyFrames)
            {
                LogPerformanceStep("TryProcessPendingTeleport", startedAtTimestamp);
                return true;
            }

            EtgFloorDefinition pendingFloor = _pendingTeleportFloor;
            string pendingLabelKey = _pendingTeleportLabelKey;
            string pendingCommandText = _pendingTeleportCommandText;
            ClearPendingTeleport();

            try
            {
                MarkPerformanceEvent("Deferred teleport executing " + pendingFloor.CommandToken);
                LogFloorTeleportRunInfo(
                    "Executing deferred teleport. Scene=" +
                    lifecycle.SceneName +
                    ", TargetToken=" +
                    pendingFloor.CommandToken +
                    ", TargetLoadScene=" +
                    pendingFloor.LoadSceneName +
                    ", Command=" +
                    pendingCommandText +
                    ", LabelKey=" +
                    pendingLabelKey +
                    ".");
                gameManager.LoadCustomLevel(pendingFloor.LoadSceneName);
                LogPerformanceOperation(
                    "ExecuteDeferredTeleport",
                    startedAtTimestamp,
                    "Scene=" + lifecycle.SceneName + ", TargetToken=" + pendingFloor.CommandToken);
            }
            catch (Exception exception)
            {
                Logger.LogWarning(
                    RandomLoadoutLog.Run(
                        "Deferred teleport execution failed. Scene=" +
                        lifecycle.SceneName +
                        ", TargetToken=" +
                        pendingFloor.CommandToken +
                        ", TargetLoadScene=" +
                        pendingFloor.LoadSceneName +
                        ", Exception=" +
                        exception +
                        "."));
            }

            return true;
        }

        private void ClearPendingTeleport()
        {
            _pendingTeleportFloor = null;
            _pendingTeleportLabelKey = string.Empty;
            _pendingTeleportCommandText = string.Empty;
            _pendingTeleportReadySceneName = string.Empty;
            _pendingTeleportReadyFrames = 0;
        }

        private void LogFloorTeleportInfo(string message)
        {
            if (!IsFloorTeleportVerboseLoggingEnabled())
            {
                return;
            }

            Logger.LogInfo(RandomLoadoutLog.Command(message));
        }

        private void LogFloorTeleportRunInfo(string message)
        {
            if (!IsFloorTeleportVerboseLoggingEnabled())
            {
                return;
            }

            Logger.LogInfo(RandomLoadoutLog.Run(message));
        }

        private static bool IsDeferredTeleportReady(GameManager gameManager, PlayerController player, out string readinessSummary)
        {
            if ((object)gameManager == null)
            {
                readinessSummary = "GameManager is unavailable";
                return false;
            }

            if ((object)player == null)
            {
                readinessSummary = "PrimaryPlayer is unavailable";
                return false;
            }

            if ((object)gameManager.Dungeon == null || gameManager.Dungeon.data == null)
            {
                readinessSummary = "Dungeon data is unavailable";
                return false;
            }

            if ((object)player.healthHaver == null)
            {
                readinessSummary = "Player HealthHaver is unavailable";
                return false;
            }

            if ((object)player.CurrentRoom == null || player.CurrentRoom.area == null)
            {
                readinessSummary = "Player current room is not ready";
                return false;
            }

            if ((object)player.CurrentGun == null)
            {
                readinessSummary = "Player current gun is unavailable";
                return false;
            }

            if (GameManager.IsBossIntro)
            {
                readinessSummary = "Boss intro is active";
                return false;
            }

            readinessSummary =
                "CurrentRoom=" +
                GetDeferredTeleportRoomLabel(player) +
                ", CurrentGun=" +
                GetDeferredTeleportGunLabel(player) +
                ", InputOverridden=" +
                player.IsInputOverridden +
                ", CurrentInputState=" +
                player.CurrentInputState;
            return true;
        }

        private static string GetDeferredTeleportRoomLabel(PlayerController player)
        {
            if ((object)player == null || (object)player.CurrentRoom == null || player.CurrentRoom.area == null)
            {
                return "<none>";
            }

            return player.CurrentRoom.GetRoomName() + "@" + player.CurrentRoom.area.basePosition.x + "," + player.CurrentRoom.area.basePosition.y;
        }

        private static string GetDeferredTeleportGunLabel(PlayerController player)
        {
            if ((object)player == null || (object)player.CurrentGun == null)
            {
                return "<none>";
            }

            Gun currentGun = player.CurrentGun;
            return currentGun.PickupObjectId + ":" + currentGun.name;
        }

        private void GrantConfiguredLoadout(PlayerController player, string sceneName)
        {
            long operationStartedAt = BeginPerformanceSample();
            MarkPerformanceEvent("Automatic loadout grant in " + sceneName);
            if (_loadoutPresetRandomService != null)
            {
                if (_loadoutPresetRandomService.IsEnabled)
                {
                    _loadoutPresetRandomService.SelectNextPresetForGrant(Logger);
                }

                Logger.LogInfo(RandomLoadoutLog.Grant(_loadoutPresetRandomService.GetDiagnostics()));
            }
            EnsureResolvedLoadoutConfig();
            RefreshActivePresetPickupsForGrant();

            int seed = _seedProvider.CreateSeed();
            string activePresetName = GetActiveStartItemsPreset();
            RandomPoolSelectionState[] randomPoolStates = LoadRandomPoolSelectionStates(activePresetName);
            System.Collections.Generic.HashSet<int> ownedPickupIds = _ownedPickupReader.CollectOwnedPickupIds(player);
            LoadoutSelectionResult selectionResult = _selectionService.SelectLoadout(
                new LoadoutSelectionRequest(seed, _resolvedLoadoutConfig, ownedPickupIds, randomPoolStates));
            SaveRandomPoolSelectionStates(activePresetName, selectionResult.RandomPoolStates);

            _runState.MarkGranted(selectionResult.Seed);
            int configuredRuleCount = _resolvedLoadoutConfig != null && _resolvedLoadoutConfig.Rules != null
                ? _resolvedLoadoutConfig.Rules.Length
                : 0;
            Logger.LogInfo(
                RandomLoadoutLog.Grant(
                    "Granting configured loadout. Scene=" +
                    sceneName +
                    ", ActivePresetName=" +
                    activePresetName +
                    ", Seed=" +
                    selectionResult.Seed +
                    ", ConfigRuleCount=" +
                    configuredRuleCount +
                    ", OwnedPickupCount=" +
                    ownedPickupIds.Count +
                    ", SelectedPickupCount=" +
                    selectionResult.Selections.Length +
                    "."));

            LogSelectionWarnings(selectionResult.Warnings);

            for (int i = 0; i < selectionResult.Selections.Length; i++)
            {
                SelectedPickup selection = selectionResult.Selections[i];
                EtgGrantOutcome outcome;
                long grantStartedAt = BeginPerformanceSample();
                try
                {
                    outcome = _pickupGranter.Grant(player, selection);
                }
                catch (Exception exception)
                {
                    Logger.LogWarning(
                        RandomLoadoutLog.Grant(
                            "Unhandled exception while granting pickup ID " +
                            selection.PickupId +
                            ": " +
                            exception.GetType().Name +
                            ": " +
                            exception.Message));
                    continue;
                }

                LogPerformanceStep("EtgPickupGranter.Grant", grantStartedAt);

                if (outcome.Succeeded)
                {
                    Logger.LogInfo(
                        RandomLoadoutLog.Grant(
                            "Granted " + outcome.Category + ": " + outcome.PickupLabel + " (ID " + outcome.PickupId + "). " +
                            "[Path=" + outcome.GrantPath + "; Detail=" + outcome.GrantDetail + "]"));
                    continue;
                }

                Logger.LogWarning(
                    RandomLoadoutLog.Grant(
                        "Failed to grant " + outcome.Category + " pickup ID " + outcome.PickupId + " (" + outcome.PickupLabel + "): " +
                        outcome.FailureReason + " [Path=" + outcome.GrantPath + "; Detail=" + outcome.GrantDetail + "]"));
            }

            if (selectionResult.Selections.Length == 0)
            {
                Logger.LogWarning(RandomLoadoutLog.Grant("No pickups were selected for this run."));
            }

            GrantConfiguredPresetPickups(player);
            if (_loadoutPresetRandomService != null && _loadoutPresetRandomService.IsEnabled)
            {
                // Random mode displays the preset that will be used next, not the
                // preset that just finished granting. Advance only after all current
                // pickups are done, and keep this draw pending for the next run so
                // the UI and the next actual grant remain in sync.
                _loadoutPresetRandomService.SelectNextPreset(Logger);
                Logger.LogInfo(RandomLoadoutLog.Grant(_loadoutPresetRandomService.GetDiagnostics()));
            }
            LogPerformanceOperation(
                "GrantConfiguredLoadout",
                operationStartedAt,
                "Scene=" + sceneName + ", SelectedPickupCount=" + selectionResult.Selections.Length + ", PresetPickupCount=" + (_activePresetPickups != null ? _activePresetPickups.Length : 0));
        }

        private void RefreshActivePresetPickupsForGrant()
        {
            if (_ruleFileProvider == null)
            {
                return;
            }

            try
            {
                _ruleFileProvider.ActivePresetName = GetActiveStartItemsPreset();
                LoadoutRuleFileModel model = _ruleFileProvider.LoadEditableModel();
                _activePresetPickups = _ruleFileProvider.GetActivePresetPickups(model, null);
            }
            catch (Exception exception)
            {
                Logger.LogWarning(
                    RandomLoadoutLog.Grant(
                        "Failed to refresh preset pickups before grant. " +
                        exception.GetType().Name +
                        ": " +
                        exception.Message));
            }
        }

        private void GrantConfiguredPresetPickups(PlayerController player)
        {
            if (_playerDebugCommandService == null || _activePresetPickups == null || _activePresetPickups.Length == 0)
            {
                return;
            }

            for (int i = 0; i < _activePresetPickups.Length; i++)
            {
                string pickupType = StartItemPickupCatalog.NormalizeType(_activePresetPickups[i] != null ? _activePresetPickups[i].Type : string.Empty);
                if (string.IsNullOrEmpty(pickupType))
                {
                    continue;
                }

                int grantCount = StartItemPickupCatalog.NormalizeCount(_activePresetPickups[i] != null ? _activePresetPickups[i].Count : 1);
                bool succeeded = true;
                string failureDetail = string.Empty;
                for (int grantIndex = 0; grantIndex < grantCount; grantIndex++)
                {
                    GrantCommandExecutionResult result = _playerDebugCommandService.GrantStartItemPickup(player, pickupType);
                    if (!result.Succeeded)
                    {
                        succeeded = false;
                        failureDetail = result.LogMessage;
                        break;
                    }
                }

                if (succeeded)
                {
                    Logger.LogInfo(
                        RandomLoadoutLog.Grant(
                            "Granted preset pickup: " +
                            StartItemPickupCatalog.GetEnglishDisplayName(pickupType) +
                            " [Type=" +
                            pickupType +
                            "; Count=" +
                            grantCount +
                            "]."));
                }
                else
                {
                    Logger.LogWarning(
                        RandomLoadoutLog.Grant(
                            "Failed to grant preset pickup: " +
                            StartItemPickupCatalog.GetEnglishDisplayName(pickupType) +
                            " [Type=" +
                            pickupType +
                            "; Count=" +
                            grantCount +
                            "; Detail=" +
                            failureDetail +
                            "]."));
                }
            }
        }

        private RandomPoolSelectionState[] LoadRandomPoolSelectionStates(string presetName)
        {
            if (_randomPoolSelectionStateProvider == null)
            {
                return new RandomPoolSelectionState[0];
            }

            try
            {
                return _randomPoolSelectionStateProvider.Load(presetName);
            }
            catch (Exception exception)
            {
                Logger.LogWarning(
                    RandomLoadoutLog.Grant(
                        "Failed to load random-pool selection state. A new shuffle order will be created. " +
                        exception.GetType().Name +
                        ": " +
                        exception.Message));
                return new RandomPoolSelectionState[0];
            }
        }

        private void SaveRandomPoolSelectionStates(string presetName, RandomPoolSelectionState[] states)
        {
            if (_randomPoolSelectionStateProvider == null)
            {
                return;
            }

            try
            {
                _randomPoolSelectionStateProvider.Save(presetName, states);
            }
            catch (Exception exception)
            {
                Logger.LogWarning(
                    RandomLoadoutLog.Grant(
                        "Failed to save random-pool selection state. The next run may reuse an older shuffle order. " +
                        exception.GetType().Name +
                        ": " +
                        exception.Message));
            }
        }
    }
}
