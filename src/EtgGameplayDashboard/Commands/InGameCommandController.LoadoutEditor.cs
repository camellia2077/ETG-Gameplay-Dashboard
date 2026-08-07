// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private bool DrawLoadoutButton(Rect rect, string label, string controlId, GUIStyle style)
        {
            return GUI.Button(rect, label, GetControllerButtonStyle(controlId, style));
        }

        private void OpenLoadoutEditorPage(ManualLogSource logger)
        {
            BeginLoadoutPagePerformanceTrace("PresetList");
            _currentPage = PanelPage.LoadoutEditor;
            _focusInputField = false;
            _focusPickupSearchField = false;
            _loadoutEditorMode = LoadoutEditorMode.PresetList;
            _loadoutEditorFocusedControlId = "loadout.preset_list.reload";
            long stageStartedAtTimestamp = BeginLoadoutPagePerformanceStage();
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetDisplayName();
            QueueLoadoutPresetPreviewIconWarmup();
            LogLoadoutPagePerformanceStage("Open.RefreshState", stageStartedAtTimestamp);

            if (logger != null)
            {
                logger.LogInfo(EtgGameplayDashboardLog.Command("Loadout editor opened."));
            }
        }

        private void DrawLoadoutEditorPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            LogLoadoutPagePerformanceStage("Draw.begin", 0L);
            if (_loadoutEditorMode == LoadoutEditorMode.RandomPoolDetail)
            {
                DrawLoadoutRandomPoolDetailPage(panelRect, logger);
                CompleteLoadoutPagePerformanceTrace("Draw.RandomPoolDetail.complete");
                return;
            }

            if (_loadoutEditorMode == LoadoutEditorMode.PresetPickupsDetail)
            {
                DrawLoadoutPresetPickupsDetailPage(panelRect, logger);
                CompleteLoadoutPagePerformanceTrace("Draw.PresetPickupsDetail.complete");
                return;
            }

            if (_loadoutEditorMode == LoadoutEditorMode.PresetDetail)
            {
                DrawLoadoutPresetDetailPage(panelRect, player, logger);
                CompleteLoadoutPagePerformanceTrace("Draw.PresetDetail.complete");
                return;
            }

            DrawLoadoutPresetListPage(panelRect, player, logger);
            CompleteLoadoutPagePerformanceTrace("Draw.PresetList.complete");
        }

        private void DrawLoadoutPresetListPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            Rect backButtonRect = GetSecondaryPageBackButtonRect(panelRect);
            const float reloadConfigButtonWidth = 128f;
            Rect reloadButtonRect = new Rect(backButtonRect.x - ButtonGap - reloadConfigButtonWidth, backButtonRect.y, reloadConfigButtonWidth, 30f);
            long stageStartedAtTimestamp = BeginLoadoutPagePerformanceStage();
            if (DrawLoadoutButton(backButtonRect, GuiText.Get("gui.common.back"), "loadout.back", _buttonStyle))
            {
                _currentPage = PanelPage.Command;
                _focusInputField = true;
                return;
            }

            if (DrawLoadoutButton(reloadButtonRect, GuiText.Get("gui.loadout_editor.button.reload"), "loadout.preset_list.reload", _buttonStyle))
            {
                ExecuteLoadoutEditorReload(logger);
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, reloadButtonRect.x - panelRect.x - 28f, 24f),
                GuiText.Get("gui.loadout_editor.title"),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.loadout_editor.preset_list_hint"),
                _hintStyle);

            const float presetActionButtonWidth = 92f;
            Rect newPresetButtonRect = new Rect(panelRect.x + 14f, panelRect.y + 66f, presetActionButtonWidth, 28f);
            Rect duplicatePresetButtonRect = new Rect(newPresetButtonRect.xMax + ButtonGap, newPresetButtonRect.y, presetActionButtonWidth, 28f);
            Rect deletePresetButtonRect = new Rect(duplicatePresetButtonRect.xMax + ButtonGap, newPresetButtonRect.y, presetActionButtonWidth, 28f);
            const float fillCurrentPresetButtonWidth = 124f;
            Rect fillCurrentPresetButtonRect = new Rect(deletePresetButtonRect.xMax + ButtonGap, newPresetButtonRect.y, fillCurrentPresetButtonWidth, 28f);
            const float randomPresetButtonWidth = 124f;
            Rect randomPresetButtonRect = new Rect(fillCurrentPresetButtonRect.xMax + ButtonGap, newPresetButtonRect.y, randomPresetButtonWidth, 28f);
            if (DrawLoadoutButton(newPresetButtonRect, GuiText.Get("gui.loadout_editor.button.new_preset"), "loadout.preset_list.new", _buttonStyle))
            {
                ExecuteLoadoutEditorCreatePreset(logger);
            }

            if (DrawLoadoutButton(duplicatePresetButtonRect, GuiText.Get("gui.loadout_editor.button.duplicate_preset"), "loadout.preset_list.duplicate", _buttonStyle))
            {
                ExecuteLoadoutEditorDuplicatePreset(logger);
            }

            if (DrawLoadoutButton(deletePresetButtonRect, GuiText.Get("gui.loadout_editor.button.delete_preset"), "loadout.preset_list.delete", _buttonStyle))
            {
                ExecuteLoadoutEditorDeletePreset(logger);
            }

            if (DrawLoadoutButton(fillCurrentPresetButtonRect, GuiText.Get("gui.loadout_editor.button.fill_current_preset"), "loadout.preset_list.fill", _buttonStyle))
            {
                ExecuteLoadoutEditorFillCurrentPreset(player, logger);
            }

            if (DrawLoadoutButton(
                randomPresetButtonRect,
                GetLoadoutPresetRandomButtonLabel(),
                "loadout.preset_list.random",
                IsLoadoutPresetRandomEnabled() ? _enabledButtonStyle : _disabledToggleButtonStyle))
            {
                ExecuteToggleLoadoutPresetRandom(logger);
            }

            Rect iconToggleRowRect = new Rect(panelRect.x + 14f, panelRect.y + 102f, panelRect.width - 28f, 28f);
            const float iconToggleButtonWidth = 180f;
            bool areIconsEnabled = IsStartItemsPresetIconsEnabled();
            if (DrawLoadoutButton(
                new Rect(iconToggleRowRect.xMax - iconToggleButtonWidth, iconToggleRowRect.y, iconToggleButtonWidth, iconToggleRowRect.height),
                GetStartItemsPresetIconsButtonLabel(areIconsEnabled),
                "loadout.preset_list.icons",
                areIconsEnabled ? _enabledButtonStyle : _disabledToggleButtonStyle))
            {
                ExecuteToggleStartItemsPresetIcons(logger);
            }

            const float renameButtonWidth = 92f;
            Rect renameLabelRect = new Rect(panelRect.x + 14f, panelRect.y + 136f, 92f, 28f);
            Rect renameButtonRect = new Rect(panelRect.x + panelRect.width - renameButtonWidth - 14f, renameLabelRect.y, renameButtonWidth, 28f);
            Rect renameFieldRect = new Rect(renameLabelRect.xMax + ButtonGap, renameLabelRect.y, renameButtonRect.x - renameLabelRect.xMax - (ButtonGap * 2f), 28f);
            GUI.Label(renameLabelRect, GuiText.Get("gui.loadout_editor.rename_label"), _hintStyle);
            _loadoutPresetRenameText = GUI.TextField(renameFieldRect, _loadoutPresetRenameText, 64, _textFieldStyle);
            if (DrawLoadoutButton(renameButtonRect, GuiText.Get("gui.loadout_editor.button.rename_preset"), "loadout.preset_list.rename", _buttonStyle))
            {
                ExecuteLoadoutEditorRenamePreset(logger);
            }
            LogLoadoutPagePerformanceStage("PresetList.Controls", stageStartedAtTimestamp);

            stageStartedAtTimestamp = BeginLoadoutPagePerformanceStage();
            DrawLoadoutPresetRows(new Rect(panelRect.x + 14f, panelRect.y + 176f, panelRect.width - 28f, panelRect.height - 190f), logger);
            LogLoadoutPagePerformanceStage("PresetList.Rows", stageStartedAtTimestamp);
        }

        private bool IsStartItemsPresetIconsEnabled()
        {
            return _startItemsPresetIconsEnabledProvider != null && _startItemsPresetIconsEnabledProvider();
        }

        private string GetStartItemsPresetIconsButtonLabel(bool isEnabled)
        {
            return isEnabled
                ? GuiText.Get("gui.loadout_editor.button.item_icons_on")
                : GuiText.Get("gui.loadout_editor.button.item_icons_off");
        }

        private void ExecuteToggleStartItemsPresetIcons(ManualLogSource logger)
        {
            if (_startItemsPresetIconsEnabledSetter == null)
            {
                return;
            }

            bool isEnabled = !IsStartItemsPresetIconsEnabled();
            _startItemsPresetIconsEnabledSetter(isEnabled);
            if (logger != null)
            {
                logger.LogInfo(EtgGameplayDashboardLog.Command("Start Items preset icons " + (isEnabled ? "enabled" : "disabled") + "."));
            }
        }

        private void DrawLoadoutPresetDetailPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            Rect backButtonRect = GetSecondaryPageBackButtonRect(panelRect);
            const float reloadConfigButtonWidth = 128f;
            Rect reloadButtonRect = new Rect(backButtonRect.x - ButtonGap - reloadConfigButtonWidth, backButtonRect.y, reloadConfigButtonWidth, 30f);
            const float addItemButtonWidth = 112f;
            const float addRandomPoolButtonWidth = 112f;
            const float addPresetPickupsButtonWidth = 112f;
            const float fillCurrentPresetButtonWidth = 124f;
            Rect addItemButtonRect = new Rect(panelRect.x + 14f, panelRect.y + 84f, addItemButtonWidth, 28f);
            Rect addRandomPoolButtonRect = new Rect(addItemButtonRect.xMax + ButtonGap, addItemButtonRect.y, addRandomPoolButtonWidth, 28f);
            Rect addPresetPickupsButtonRect = new Rect(addRandomPoolButtonRect.xMax + ButtonGap, addItemButtonRect.y, addPresetPickupsButtonWidth, 28f);
            Rect fillCurrentPresetButtonRect = new Rect(addPresetPickupsButtonRect.xMax + ButtonGap, addItemButtonRect.y, fillCurrentPresetButtonWidth, 28f);
            if (DrawLoadoutButton(backButtonRect, GuiText.Get("gui.common.back"), "loadout.back", _buttonStyle))
            {
                _loadoutEditorMode = LoadoutEditorMode.PresetList;
                _loadoutEditorFocusedControlId = "loadout.preset_list.reload";
                RefreshLoadoutPresetEntries();
                return;
            }

            if (DrawLoadoutButton(addItemButtonRect, GuiText.Get("gui.loadout_editor.button.add_item"), "loadout.preset_detail.add_item", _buttonStyle))
            {
                OpenPickupAddToStartItemsPage(logger);
                return;
            }

            if (DrawLoadoutButton(addRandomPoolButtonRect, GuiText.Get("gui.loadout_editor.button.add_random_pool"), "loadout.preset_detail.add_random_pool", _buttonStyle))
            {
                ExecuteLoadoutEditorCreateRandomPool(logger);
            }

            if (DrawLoadoutButton(addPresetPickupsButtonRect, GuiText.Get("gui.loadout_editor.button.pickups"), "loadout.preset_detail.pickups", _buttonStyle))
            {
                OpenLoadoutPresetPickupsDetail();
            }

            if (DrawLoadoutButton(fillCurrentPresetButtonRect, GuiText.Get("gui.loadout_editor.button.fill_current_preset"), "loadout.preset_detail.fill", _buttonStyle))
            {
                ExecuteLoadoutEditorFillCurrentPreset(player, logger);
            }

            if (DrawLoadoutButton(reloadButtonRect, GuiText.Get("gui.loadout_editor.button.reload"), "loadout.preset_detail.reload", _buttonStyle))
            {
                ExecuteLoadoutEditorReload(logger);
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, reloadButtonRect.x - panelRect.x - 28f, 24f),
                GuiText.Get("gui.loadout_editor.detail_title"),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.loadout_editor.hint"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 60f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.loadout_editor.preset", GetLoadoutEditorActivePresetDisplayName()),
                _hintStyle);

            DrawLoadoutEditorRows(new Rect(panelRect.x + 14f, panelRect.y + 122f, panelRect.width - 28f, panelRect.height - 136f), logger);
        }

        private void DrawLoadoutRandomPoolDetailPage(Rect panelRect, ManualLogSource logger)
        {
            Rect backButtonRect = GetSecondaryPageBackButtonRect(panelRect);
            const float addItemButtonWidth = 112f;
            Rect addItemButtonRect = new Rect(backButtonRect.x - ButtonGap - addItemButtonWidth, backButtonRect.y, addItemButtonWidth, 30f);
            if (DrawLoadoutButton(backButtonRect, GuiText.Get("gui.common.back"), "loadout.back", _buttonStyle))
            {
                _loadoutEditorMode = LoadoutEditorMode.PresetDetail;
                _loadoutEditorFocusedControlId = "loadout.preset_detail.add_item";
                RefreshLoadoutEditorEntries();
                return;
            }

            if (DrawLoadoutButton(addItemButtonRect, GuiText.Get("gui.loadout_editor.button.add_item"), "loadout.random_pool.add_item", _buttonStyle))
            {
                OpenPickupAddToRandomPoolPage(logger);
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, addItemButtonRect.x - panelRect.x - 28f, 24f),
                GetLoadoutEditorActiveRandomPoolDisplayName(),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.loadout_editor.random_pool_hint"),
                _hintStyle);
            const float renameButtonWidth = 92f;
            Rect renameLabelRect = new Rect(panelRect.x + 14f, panelRect.y + 60f, 92f, 28f);
            Rect renameButtonRect = new Rect(panelRect.x + panelRect.width - renameButtonWidth - 14f, renameLabelRect.y, renameButtonWidth, 28f);
            Rect renameFieldRect = new Rect(renameLabelRect.xMax + ButtonGap, renameLabelRect.y, renameButtonRect.x - renameLabelRect.xMax - (ButtonGap * 2f), 28f);
            GUI.Label(renameLabelRect, GuiText.Get("gui.loadout_editor.rename_label"), _hintStyle);
            _loadoutRandomPoolRenameText = GUI.TextField(renameFieldRect, _loadoutRandomPoolRenameText, 64, _textFieldStyle);
            if (DrawLoadoutButton(renameButtonRect, GuiText.Get("gui.loadout_editor.button.rename_random_pool"), "loadout.random_pool.rename", _buttonStyle))
            {
                ExecuteLoadoutEditorRenameRandomPool(logger);
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 94f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.loadout_editor.random_pool_summary", _cachedLoadoutRandomPoolEntries.Length),
                _hintStyle);

            DrawLoadoutRandomPoolRows(new Rect(panelRect.x + 14f, panelRect.y + 126f, panelRect.width - 28f, panelRect.height - 140f), logger);
        }

        private void DrawLoadoutPresetRows(Rect listRect, ManualLogSource logger)
        {
            if (_cachedLoadoutPresetEntries.Length == 0)
            {
                GUI.Box(listRect, GUIContent.none, _loadoutEditorRowStyle);
                GUI.Label(
                    new Rect(listRect.x + 12f, listRect.y + 12f, listRect.width - 24f, listRect.height - 24f),
                    GuiText.Get("gui.loadout_editor.empty_presets"),
                    _wrappedHintStyle);
                return;
            }

            long stageStartedAtTimestamp = BeginLoadoutPagePerformanceStage();
            float cardWidth = (listRect.width - SharedScrollViewStyles.ViewportScrollbarReserveWidth - ButtonGap) / LoadoutPresetColumnCount;
            int presetRowCount = (_cachedLoadoutPresetEntries.Length + LoadoutPresetColumnCount - 1) / LoadoutPresetColumnCount;
            float contentHeight = 4f;
            for (int rowIndex = 0; rowIndex < presetRowCount; rowIndex++)
            {
                int leftIndex = rowIndex * LoadoutPresetColumnCount;
                int rightIndex = leftIndex + 1;
                float rowHeight = GetLoadoutPresetRowHeight(_cachedLoadoutPresetEntries[leftIndex]);
                if (rightIndex < _cachedLoadoutPresetEntries.Length)
                {
                    rowHeight = Mathf.Max(rowHeight, GetLoadoutPresetRowHeight(_cachedLoadoutPresetEntries[rightIndex]));
                }

                contentHeight += rowHeight;
            }
            LogLoadoutPagePerformanceStage("PresetRows.Layout", stageStartedAtTimestamp);

            stageStartedAtTimestamp = BeginLoadoutPagePerformanceStage();
            Rect viewRect = new Rect(0f, 0f, listRect.width - SharedScrollViewStyles.ViewportScrollbarReserveWidth, contentHeight);
            _loadoutPresetScrollPosition = BeginCommandScrollView(listRect, _loadoutPresetScrollPosition, viewRect);
            float visibleTop = _loadoutPresetScrollPosition.y;
            float visibleBottom = visibleTop + listRect.height;
            float rowTop = 2f;
            for (int rowIndex = 0; rowIndex < presetRowCount; rowIndex++)
            {
                int leftIndex = rowIndex * LoadoutPresetColumnCount;
                int rightIndex = leftIndex + 1;
                float rowHeight = GetLoadoutPresetRowHeight(_cachedLoadoutPresetEntries[leftIndex]);
                if (rightIndex < _cachedLoadoutPresetEntries.Length)
                {
                    rowHeight = Mathf.Max(rowHeight, GetLoadoutPresetRowHeight(_cachedLoadoutPresetEntries[rightIndex]));
                }

                if (rowTop + rowHeight >= visibleTop - rowHeight && rowTop <= visibleBottom + rowHeight)
                {
                    DrawLoadoutPresetRow(new Rect(0f, rowTop, cardWidth, rowHeight - 4f), _cachedLoadoutPresetEntries[leftIndex], logger);
                    if (rightIndex < _cachedLoadoutPresetEntries.Length)
                    {
                        DrawLoadoutPresetRow(
                            new Rect(cardWidth + ButtonGap, rowTop, cardWidth, rowHeight - 4f),
                            _cachedLoadoutPresetEntries[rightIndex],
                            logger);
                    }
                }

                rowTop += rowHeight;
            }
            LogLoadoutPagePerformanceStage("PresetRows.VisibleDraw", stageStartedAtTimestamp);

            GUI.EndScrollView();
        }

        private void DrawLoadoutPresetRow(Rect rowRect, LoadoutPresetEditorEntry entry, ManualLogSource logger)
        {
            bool isActive = entry != null && entry.IsActive;
            GUIStyle rowStyle = isActive ? _activePresetRowStyle : _pickupRowStyle;
            GUI.Box(rowRect, GUIContent.none, rowStyle);

            const float selectWidth = 82f;
            const float openWidth = 82f;
            const float presetActionHeight = 32f;
            const float cardContentPadding = 10f;
            Rect openButtonRect = new Rect(rowRect.x + rowRect.width - openWidth - cardContentPadding, rowRect.y + cardContentPadding, openWidth, presetActionHeight);
            Rect selectButtonRect = new Rect(openButtonRect.x - ButtonGap - selectWidth, openButtonRect.y, selectWidth, openButtonRect.height);
            Rect rowButtonRect = new Rect(rowRect.x, rowRect.y, selectButtonRect.x - rowRect.x - ButtonGap, rowRect.height);
            if (GUI.Button(rowButtonRect, GUIContent.none, _pickupRowButtonStyle))
            {
                OpenLoadoutPresetDetail(entry, logger);
            }

            string primaryText = entry != null ? entry.DisplayName : string.Empty;
            string secondaryText = entry != null
                ? GuiText.Get("gui.loadout_editor.preset_summary", entry.RuleCount, entry.SpecificCount, entry.RandomCount, entry.PickupCount)
                : string.Empty;
            GUIStyle secondaryTextStyle = entry != null && entry.IsActive
                ? _pickupSecondaryActiveTextStyle
                : _pickupSecondaryTextStyle;
            float primaryTextLeft = rowRect.x + cardContentPadding;
            float primaryTextWidth = rowButtonRect.width - (cardContentPadding * 2f);
            if (isActive)
            {
                const float activeIndicatorWidth = 20f;
                GUI.Label(new Rect(primaryTextLeft, rowRect.y + 7f, activeIndicatorWidth, 20f), "✓", _activePresetAccentTextStyle);
                primaryTextLeft += activeIndicatorWidth;
                primaryTextWidth -= activeIndicatorWidth;
                GUI.Label(new Rect(primaryTextLeft, rowRect.y + 7f, primaryTextWidth, 20f), primaryText, _pickupPrimaryTextStyle);
                float nameWidth = _pickupPrimaryTextStyle.CalcSize(new GUIContent(primaryText)).x;
                GUI.Label(
                    new Rect(primaryTextLeft + nameWidth + 4f, rowRect.y + 7f, primaryTextWidth - nameWidth - 4f, 20f),
                    GuiText.Get("gui.loadout_editor.preset_active_suffix"),
                    _activePresetAccentTextStyle);
            }
            else
            {
                GUI.Label(new Rect(primaryTextLeft, rowRect.y + 7f, primaryTextWidth, 20f), primaryText, _pickupPrimaryTextStyle);
            }
            GUI.Label(new Rect(rowRect.x + cardContentPadding, rowRect.y + 26f, rowButtonRect.width - (cardContentPadding * 2f), 18f), secondaryText, secondaryTextStyle);
            if (IsStartItemsPresetIconsEnabled())
            {
                DrawLoadoutPresetPreviewRows(new Rect(rowRect.x + cardContentPadding, rowRect.y + 47f, rowRect.width - (cardContentPadding * 2f), rowRect.height - 50f), entry);
            }

            GUIStyle selectButtonStyle = IsLoadoutPresetRandomEnabled()
                ? _pickupFilterDisabledButtonStyle
                : isActive ? _enabledButtonStyle : _buttonStyle;
            if (DrawLoadoutButton(selectButtonRect, GuiText.Get("gui.loadout_editor.button.select_preset"), GetLoadoutPresetSelectControlId(entry), selectButtonStyle) &&
                !IsLoadoutPresetRandomEnabled())
            {
                if (entry != null)
                {
                    ExecuteLoadoutEditorSelectPreset(entry.Id, logger);
                }
            }

            if (DrawLoadoutButton(openButtonRect, GuiText.Get("gui.loadout_editor.button.open_preset"), GetLoadoutPresetOpenControlId(entry), _buttonStyle))
            {
                OpenLoadoutPresetDetail(entry, logger);
            }
        }

        private float GetLoadoutPresetRowHeight(LoadoutPresetEditorEntry entry)
        {
            if (!IsStartItemsPresetIconsEnabled())
            {
                return LoadoutPresetRowHeight;
            }

            int previewRowCount = entry != null && entry.PreviewRows != null ? entry.PreviewRows.Length : 0;
            return LoadoutPresetRowHeight + (previewRowCount * LoadoutPresetPreviewRowHeight) + (previewRowCount > 0 ? 4f : 0f);
        }

        private void DrawLoadoutPresetPreviewRows(Rect previewRect, LoadoutPresetEditorEntry entry)
        {
            if (entry == null || entry.PreviewRows == null)
            {
                return;
            }

            for (int rowIndex = 0; rowIndex < entry.PreviewRows.Length; rowIndex++)
            {
                LoadoutPresetPreviewRow previewRow = entry.PreviewRows[rowIndex];
                if (previewRow == null)
                {
                    continue;
                }

                Rect rowRect = new Rect(previewRect.x, previewRect.y + (rowIndex * LoadoutPresetPreviewRowHeight), previewRect.width, LoadoutPresetPreviewRowHeight);
                const float labelWidth = 92f;
                GUI.Label(new Rect(rowRect.x, rowRect.y + 2f, labelWidth, 20f), GuiText.Get(previewRow.LabelKey), _pickupSecondaryTextStyle);
                DrawLoadoutPresetPreviewIcons(
                    new Rect(rowRect.x + labelWidth + 6f, rowRect.y + 1f, rowRect.width - labelWidth - 6f, 22f),
                    previewRow.PickupIds);
            }
        }

        private void DrawLoadoutPresetPreviewIcons(Rect iconsRect, int[] pickupIds)
        {
            if (pickupIds == null)
            {
                return;
            }

            const float iconSize = 22f;
            const float iconGap = 3f;
            for (int index = 0; index < pickupIds.Length; index++)
            {
                float x = iconsRect.x + (index * (iconSize + iconGap));
                if (x + iconSize > iconsRect.xMax)
                {
                    break;
                }

                Rect iconRect = new Rect(x, iconsRect.y, iconSize, iconSize);
                PickupIconData iconData;
                if (TryGetLoadoutPreviewIcon(pickupIds[index], out iconData))
                {
                    GUI.DrawTextureWithTexCoords(iconRect, iconData.Texture, iconData.TextureCoords, true);
                }
                else
                {
                    GUI.Box(iconRect, "?", _pickupIconFallbackStyle);
                }
            }
        }

        private void QueueLoadoutPresetPreviewIconWarmup()
        {
            _loadoutPreviewIconWarmupQueue.Clear();
            _loadoutPreviewIconWarmupQueuedIds.Clear();
            if (!IsStartItemsPresetIconsEnabled() || _cachedLoadoutPresetEntries == null)
            {
                return;
            }

            for (int entryIndex = 0; entryIndex < _cachedLoadoutPresetEntries.Length; entryIndex++)
            {
                LoadoutPresetEditorEntry entry = _cachedLoadoutPresetEntries[entryIndex];
                if (entry == null || entry.PreviewRows == null)
                {
                    continue;
                }

                for (int rowIndex = 0; rowIndex < entry.PreviewRows.Length; rowIndex++)
                {
                    LoadoutPresetPreviewRow previewRow = entry.PreviewRows[rowIndex];
                    if (previewRow == null || previewRow.PickupIds == null)
                    {
                        continue;
                    }

                    for (int pickupIndex = 0; pickupIndex < previewRow.PickupIds.Length; pickupIndex++)
                    {
                        int pickupId = previewRow.PickupIds[pickupIndex];
                        if (!_pickupIconCache.ContainsKey(pickupId) && _loadoutPreviewIconWarmupQueuedIds.Add(pickupId))
                        {
                            _loadoutPreviewIconWarmupQueue.Enqueue(pickupId);
                        }
                    }
                }
            }
        }

        private bool TryGetLoadoutPreviewIcon(int pickupId, out PickupIconData iconData)
        {
            if (_pickupIconCache.TryGetValue(pickupId, out iconData))
            {
                return iconData.Texture != null;
            }

            if (_loadoutPreviewIconWarmupQueuedIds.Add(pickupId))
            {
                _loadoutPreviewIconWarmupQueue.Enqueue(pickupId);
            }

            iconData = PickupIconData.Empty;
            return false;
        }

        private void ProcessLoadoutPreviewIconWarmup()
        {
            if (_currentPage != PanelPage.LoadoutEditor ||
                !IsStartItemsPresetIconsEnabled() ||
                _loadoutPreviewIconWarmupQueue.Count == 0)
            {
                return;
            }

            int pickupId = _loadoutPreviewIconWarmupQueue.Dequeue();
            _loadoutPreviewIconWarmupQueuedIds.Remove(pickupId);
            PickupIconData iconData;
            TryGetPickupIcon(pickupId, out iconData);
        }

        private void DrawLoadoutEditorRows(Rect listRect, ManualLogSource logger)
        {
            if (_cachedLoadoutRuleEntries.Length == 0)
            {
                GUI.Box(listRect, GUIContent.none, _loadoutEditorRowStyle);
                GUI.Label(
                    new Rect(listRect.x + 12f, listRect.y + 12f, listRect.width - 24f, listRect.height - 24f),
                    GuiText.Get("gui.loadout_editor.empty"),
                    _wrappedHintStyle);
                return;
            }

            Rect viewRect = new Rect(0f, 0f, listRect.width - SharedScrollViewStyles.ViewportScrollbarReserveWidth, (_cachedLoadoutRuleEntries.Length * LoadoutRuleRowHeight) + 4f);
            _loadoutEditorScrollPosition = BeginCommandScrollView(listRect, _loadoutEditorScrollPosition, viewRect);
            float rowStride = LoadoutRuleRowHeight;
            int firstVisibleIndex = Mathf.Max(0, Mathf.FloorToInt(_loadoutEditorScrollPosition.y / rowStride) - 1);
            int lastVisibleIndex = Mathf.Min(
                _cachedLoadoutRuleEntries.Length - 1,
                Mathf.CeilToInt((_loadoutEditorScrollPosition.y + listRect.height) / rowStride) + 1);
            for (int i = firstVisibleIndex; i <= lastVisibleIndex; i++)
            {
                DrawLoadoutEditorRow(new Rect(0f, 2f + (i * LoadoutRuleRowHeight), viewRect.width, LoadoutRuleRowHeight - 4f), _cachedLoadoutRuleEntries[i], logger);
            }

            GUI.EndScrollView();
        }

        private void DrawLoadoutEditorRow(Rect rowRect, LoadoutRuleEditorEntry entry, ManualLogSource logger)
        {
            GUI.Box(rowRect, GUIContent.none, _loadoutEditorRowStyle);

            float removeWidth = 82f;
            Rect removeButtonRect = new Rect(rowRect.x + rowRect.width - removeWidth - 8f, rowRect.y + 8f, removeWidth, rowRect.height - 16f);
            const float editWidth = 82f;
            Rect editButtonRect = new Rect(removeButtonRect.x - ButtonGap - editWidth, removeButtonRect.y, editWidth, removeButtonRect.height);
            const float toggleWidth = 82f;
            bool hasToggleButton = entry != null && !entry.IsPresetPickupCollection && entry.Index >= 0;
            Rect toggleButtonRect = new Rect(editButtonRect.x - ButtonGap - toggleWidth, removeButtonRect.y, toggleWidth, removeButtonRect.height);
            Rect iconRect = new Rect(rowRect.x + 8f, rowRect.y + ((rowRect.height - PickupIconSize) * 0.5f), PickupIconSize, PickupIconSize);
            DrawLoadoutEditorIcon(iconRect, entry);

            float textLeft = iconRect.xMax + 8f;
            bool hasEditButton = entry != null && (entry.IsRandomPool || entry.IsPresetPickupCollection || !string.IsNullOrEmpty(entry.PickupType));
            float actionWidth = removeWidth + (hasEditButton ? editWidth + ButtonGap : 0f) + (hasToggleButton ? toggleWidth + ButtonGap : 0f);
            float textWidth = rowRect.width - actionWidth - PickupIconSize - 44f;
            GUI.Label(new Rect(textLeft, rowRect.y + 8f, textWidth, 22f), entry.PrimaryText, _pickupPrimaryTextStyle);
            GUI.Label(new Rect(textLeft, rowRect.y + 32f, textWidth, 20f), entry.SecondaryText, _pickupSecondaryTextStyle);

            if (hasToggleButton &&
                DrawLoadoutButton(
                    toggleButtonRect,
                    entry.IsEnabled ? GuiText.Get("gui.settings.button.enable") : GuiText.Get("gui.settings.button.disable"),
                    GetLoadoutRuleToggleControlId(entry),
                    entry.IsEnabled ? _enabledButtonStyle : _buttonStyle))
            {
                ExecuteLoadoutEditorToggleRule(entry.Index, logger);
            }

            if (entry != null &&
                entry.IsRandomPool &&
                DrawLoadoutButton(editButtonRect, GuiText.Get("gui.loadout_editor.button.edit"), GetLoadoutRuleEditControlId(entry), _buttonStyle))
            {
                OpenLoadoutRandomPoolDetail(entry.Index);
            }

            if (entry != null &&
                !entry.IsRandomPool &&
                (entry.IsPresetPickupCollection || !string.IsNullOrEmpty(entry.PickupType)) &&
                DrawLoadoutButton(editButtonRect, GuiText.Get("gui.loadout_editor.button.edit"), GetLoadoutRuleEditControlId(entry), _buttonStyle))
            {
                OpenLoadoutPresetPickupsDetail();
            }

            if (DrawLoadoutButton(removeButtonRect, GuiText.Get("gui.loadout_editor.button.remove"), GetLoadoutRuleRemoveControlId(entry), _buttonStyle))
            {
                if (entry != null && entry.IsPresetPickupCollection)
                {
                    ExecuteLoadoutEditorClearPresetPickups(logger);
                }
                else if (entry != null && !string.IsNullOrEmpty(entry.PickupType))
                {
                    ExecuteLoadoutEditorRemovePresetPickup(entry.Index, logger);
                }
                else
                {
                    ExecuteLoadoutEditorRemove(entry.Index, logger);
                }
            }
        }

        private void DrawLoadoutRandomPoolRows(Rect listRect, ManualLogSource logger)
        {
            if (_cachedLoadoutRandomPoolEntries.Length == 0)
            {
                GUI.Box(listRect, GUIContent.none, _pickupRowStyle);
                GUI.Label(
                    new Rect(listRect.x + 12f, listRect.y + 12f, listRect.width - 24f, listRect.height - 24f),
                    GuiText.Get("gui.loadout_editor.random_pool_empty"),
                    _wrappedHintStyle);
                return;
            }

            Rect viewRect = new Rect(0f, 0f, listRect.width - SharedScrollViewStyles.ViewportScrollbarReserveWidth, (_cachedLoadoutRandomPoolEntries.Length * PickupRowHeight) + 4f);
            _loadoutEditorScrollPosition = BeginCommandScrollView(listRect, _loadoutEditorScrollPosition, viewRect);
            float rowStride = PickupRowHeight;
            int firstVisibleIndex = Mathf.Max(0, Mathf.FloorToInt(_loadoutEditorScrollPosition.y / rowStride) - 1);
            int lastVisibleIndex = Mathf.Min(
                _cachedLoadoutRandomPoolEntries.Length - 1,
                Mathf.CeilToInt((_loadoutEditorScrollPosition.y + listRect.height) / rowStride) + 1);
            for (int i = firstVisibleIndex; i <= lastVisibleIndex; i++)
            {
                DrawLoadoutRandomPoolRow(new Rect(0f, 2f + (i * PickupRowHeight), viewRect.width, PickupRowHeight - 4f), _cachedLoadoutRandomPoolEntries[i], logger);
            }

            GUI.EndScrollView();
        }

        private void DrawLoadoutRandomPoolRow(Rect rowRect, LoadoutRandomPoolEditorEntry entry, ManualLogSource logger)
        {
            GUI.Box(rowRect, GUIContent.none, _pickupRowStyle);

            const float removeWidth = 82f;
            Rect removeButtonRect = new Rect(rowRect.x + rowRect.width - removeWidth - 8f, rowRect.y + 8f, removeWidth, rowRect.height - 16f);
            Rect iconRect = new Rect(rowRect.x + 8f, rowRect.y + ((rowRect.height - PickupIconSize) * 0.5f), PickupIconSize, PickupIconSize);
            PickupIconData iconData;
            if (entry != null && TryGetPickupIcon(entry.PickupId, out iconData))
            {
                GUI.DrawTextureWithTexCoords(iconRect, iconData.Texture, iconData.TextureCoords, true);
            }
            else
            {
                GUI.Box(iconRect, "?", _pickupIconFallbackStyle);
            }

            float textLeft = iconRect.xMax + 8f;
            float textWidth = rowRect.width - removeWidth - PickupIconSize - 44f;
            GUI.Label(new Rect(textLeft, rowRect.y + 5f, textWidth, 20f), entry != null ? entry.PrimaryText : string.Empty, _pickupPrimaryTextStyle);
            GUI.Label(new Rect(textLeft, rowRect.y + 24f, textWidth, 18f), entry != null ? entry.SecondaryText : string.Empty, _pickupSecondaryTextStyle);

            if (DrawLoadoutButton(removeButtonRect, GuiText.Get("gui.loadout_editor.button.remove"), GetLoadoutRandomPoolRemoveControlId(entry), _buttonStyle))
            {
                ExecuteLoadoutEditorRemoveFromRandomPool(entry != null ? entry.PoolIndex : -1, logger);
            }
        }

        private void DrawLoadoutEditorIcon(Rect iconRect, LoadoutRuleEditorEntry entry)
        {
            PickupIconData iconData;
            if (TryGetLoadoutEntryIcon(entry, out iconData))
            {
                GUI.DrawTextureWithTexCoords(iconRect, iconData.Texture, iconData.TextureCoords, true);
                return;
            }

            GUI.Box(iconRect, GetStartItemPickupFallbackLabel(entry != null ? entry.PickupType : string.Empty), _pickupIconFallbackStyle);
        }

        private void RefreshLoadoutEditorEntries()
        {
            _loadoutEditorDataCoordinator.RefreshAll(_loadoutRandomPoolRuleIndex);
            _cachedLoadoutRuleEntries = _loadoutEditorDataCoordinator.RuleEntries;
            _cachedLoadoutPresetEntries = _loadoutEditorDataCoordinator.PresetEntries;
            _cachedLoadoutRandomPoolEntries = _loadoutEditorDataCoordinator.RandomPoolEntries;
            _cachedLoadoutPickupEntries = _loadoutEditorDataCoordinator.PickupEntries;
        }

        private void RefreshLoadoutPresetEntries()
        {
            _loadoutEditorDataCoordinator.RefreshAll(_loadoutRandomPoolRuleIndex);
            _cachedLoadoutPresetEntries = _loadoutEditorDataCoordinator.PresetEntries;
            _cachedLoadoutRuleEntries = _loadoutEditorDataCoordinator.RuleEntries;
            _cachedLoadoutRandomPoolEntries = _loadoutEditorDataCoordinator.RandomPoolEntries;
            _cachedLoadoutPickupEntries = _loadoutEditorDataCoordinator.PickupEntries;
        }

        private bool IsLoadoutPresetRandomEnabled()
        {
            return _loadoutPresetRandomService != null && _loadoutPresetRandomService.IsEnabled;
        }

        private void ExecuteToggleLoadoutPresetRandom(ManualLogSource logger)
        {
            if (_loadoutPresetRandomService == null)
            {
                return;
            }

            bool isEnabled = _loadoutPresetRandomService.Toggle(logger);
            _loadoutEditorFocusedControlId = "loadout.preset_list.random";
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetDisplayName();
            if (logger != null)
            {
                logger.LogInfo(EtgGameplayDashboardLog.Command("Start Items random preset selection " + (isEnabled ? "enabled" : "disabled") + "."));
            }
        }

        private string GetLoadoutPresetRandomButtonLabel()
        {
            return IsLoadoutPresetRandomEnabled()
                ? GuiText.Get("gui.loadout_editor.button.random_on")
                : GuiText.Get("gui.loadout_editor.button.random_off");
        }

        private void RefreshLoadoutRandomPoolEntries()
        {
            _loadoutEditorDataCoordinator.RefreshAll(_loadoutRandomPoolRuleIndex);
            _cachedLoadoutRandomPoolEntries = _loadoutEditorDataCoordinator.RandomPoolEntries;
            _cachedLoadoutRuleEntries = _loadoutEditorDataCoordinator.RuleEntries;
            _cachedLoadoutPresetEntries = _loadoutEditorDataCoordinator.PresetEntries;
            _cachedLoadoutPickupEntries = _loadoutEditorDataCoordinator.PickupEntries;
        }

        private void ExecuteLoadoutEditorReload(ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.Reload();
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetDisplayName();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorSelectNextPreset(ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.SelectNextPreset();
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetDisplayName();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorSelectPreset(string presetId, ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.SelectPreset(presetId);
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetDisplayName();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void OpenLoadoutPresetDetail(LoadoutPresetEditorEntry entry, ManualLogSource logger)
        {
            if (entry == null || _loadoutRuleEditorService == null)
            {
                return;
            }

            BeginLoadoutPagePerformanceTrace("PresetDetail");
            if (!_loadoutRuleEditorService.OpenPreset(entry.Id))
            {
                RefreshLoadoutPresetEntries();
                return;
            }

            _loadoutEditorMode = LoadoutEditorMode.PresetDetail;
            _loadoutEditorFocusedControlId = "loadout.preset_detail.add_item";
            _loadoutEditorScrollPosition = Vector2.zero;
            _loadoutRandomPoolRuleIndex = -1;
            _loadoutPickupCountEditIndex = -1;
            _loadoutPickupCountEditText = string.Empty;
            _cachedLoadoutRandomPoolEntries = EmptyLoadoutRandomPoolEditorEntries;
            _cachedLoadoutPickupEntries = EmptyLoadoutPickupEditorEntries;
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutPickupEntries();
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetDisplayName();
        }

        private void OpenLoadoutRandomPoolDetail(int ruleIndex)
        {
            BeginLoadoutPagePerformanceTrace("RandomPoolDetail");
            _loadoutEditorMode = LoadoutEditorMode.RandomPoolDetail;
            _loadoutEditorFocusedControlId = "loadout.random_pool.add_item";
            _loadoutRandomPoolRuleIndex = ruleIndex;
            _loadoutEditorScrollPosition = Vector2.zero;
            RefreshLoadoutRandomPoolEntries();
            _loadoutRandomPoolRenameText = GetLoadoutEditorActiveRandomPoolDisplayName();
        }

        private void ExecuteLoadoutEditorCreatePreset(ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.CreatePreset();
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetDisplayName();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorDuplicatePreset(ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.DuplicateActivePreset();
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetDisplayName();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorDeletePreset(ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.DeleteActivePreset();
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetDisplayName();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorRenamePreset(ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.RenameActivePreset(_loadoutPresetRenameText);
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetDisplayName();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorFillCurrentPreset(PlayerController player, ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.FillActivePresetFromCurrentItems(player);
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorRemove(int index, ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.RemoveAt(index);
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorToggleRule(int index, ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.ToggleRuleEnabled(index);
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorCreateRandomPool(ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.AddRandomPoolRule();
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            if (result.Succeeded)
            {
                _loadoutRandomPoolRuleIndex = _cachedLoadoutRuleEntries.Length - 1;
                _loadoutEditorMode = LoadoutEditorMode.RandomPoolDetail;
                _loadoutEditorScrollPosition = Vector2.zero;
                RefreshLoadoutRandomPoolEntries();
                _loadoutRandomPoolRenameText = GetLoadoutEditorActiveRandomPoolDisplayName();
            }
            RefreshLoadoutPickupEntries();

            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorAdd(EtgPickupCatalogEntry entry, ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.AddSpecific(entry);
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorAddToRandomPool(EtgPickupCatalogEntry entry, ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.AddToRandomPool(_loadoutRandomPoolRuleIndex, entry);
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorRemoveFromRandomPool(int poolIndex, ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.RemoveFromRandomPool(_loadoutRandomPoolRuleIndex, poolIndex);
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorRenameRandomPool(ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.RenameRandomPool(_loadoutRandomPoolRuleIndex, _loadoutRandomPoolRenameText);
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            _loadoutRandomPoolRenameText = GetLoadoutEditorActiveRandomPoolDisplayName();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private string GetLoadoutEditorActivePresetDisplayName()
        {
            return _loadoutEditorDataCoordinator != null && !string.IsNullOrEmpty(_loadoutEditorDataCoordinator.GetActivePresetDisplayName())
                ? _loadoutEditorDataCoordinator.GetActivePresetDisplayName()
                : StartItemsPresetNames.GetDisplayName(StartItemsPresetNames.DefaultPresetId, string.Empty, StartItemsPresetNames.DefaultPresetDisplayNameKey);
        }

        private string GetLoadoutEditorActiveRandomPoolDisplayName()
        {
            return _loadoutEditorDataCoordinator != null && !string.IsNullOrEmpty(_loadoutEditorDataCoordinator.GetRandomPoolDisplayName(_loadoutRandomPoolRuleIndex))
                ? _loadoutEditorDataCoordinator.GetRandomPoolDisplayName(_loadoutRandomPoolRuleIndex)
                : GuiText.Get("gui.loadout_editor.rule.random_pool_title");
        }

        private static void LogLoadoutEditorResult(GrantCommandExecutionResult result, ManualLogSource logger)
        {
            if (result == null || logger == null)
            {
                return;
            }

            if (result.Succeeded)
            {
                logger.LogInfo(EtgGameplayDashboardLog.Command(result.LogMessage));
            }
            else
            {
                logger.LogWarning(EtgGameplayDashboardLog.Command(result.LogMessage));
            }
        }


        private void HandleLoadoutEditorBackNavigation()
        {
            switch (_loadoutEditorMode)
            {
                case LoadoutEditorMode.RandomPoolDetail:
                    _loadoutEditorMode = LoadoutEditorMode.PresetDetail;
                    _loadoutEditorFocusedControlId = "loadout.preset_detail.add_item";
                    RefreshLoadoutEditorEntries();
                    return;
                case LoadoutEditorMode.PresetPickupsDetail:
                    _loadoutEditorMode = LoadoutEditorMode.PresetDetail;
                    _loadoutEditorFocusedControlId = "loadout.preset_detail.pickups";
                    ResetLoadoutPresetPickupCountEdit();
                    RefreshLoadoutEditorEntries();
                    return;
                case LoadoutEditorMode.PresetDetail:
                    _loadoutEditorMode = LoadoutEditorMode.PresetList;
                    _loadoutEditorFocusedControlId = "loadout.preset_list.reload";
                    RefreshLoadoutPresetEntries();
                    return;
                case LoadoutEditorMode.PresetList:
                default:
                    _currentPage = PanelPage.Command;
                    _focusInputField = true;
                    return;
            }
        }

        private static string GetLoadoutPresetSelectControlId(LoadoutPresetEditorEntry entry)
        {
            string entryId = entry != null ? entry.Id : string.Empty;
            return "loadout.preset.select." + entryId;
        }

        private static string GetLoadoutPresetOpenControlId(LoadoutPresetEditorEntry entry)
        {
            string entryId = entry != null ? entry.Id : string.Empty;
            return "loadout.preset.open." + entryId;
        }

        private static bool DoesLoadoutRuleEntryHaveEditAction(LoadoutRuleEditorEntry entry)
        {
            return entry != null && (entry.IsRandomPool || entry.IsPresetPickupCollection || !string.IsNullOrEmpty(entry.PickupType));
        }

        private static string GetLoadoutRuleEditControlId(LoadoutRuleEditorEntry entry)
        {
            return "loadout.rule.edit." + GetLoadoutRuleEntryKey(entry);
        }

        private static string GetLoadoutRuleToggleControlId(LoadoutRuleEditorEntry entry)
        {
            return "loadout.rule.toggle." + GetLoadoutRuleEntryKey(entry);
        }

        private static string GetLoadoutRuleRemoveControlId(LoadoutRuleEditorEntry entry)
        {
            return "loadout.rule.remove." + GetLoadoutRuleEntryKey(entry);
        }

        private static string GetLoadoutRandomPoolRemoveControlId(LoadoutRandomPoolEditorEntry entry)
        {
            int poolIndex = entry != null ? entry.PoolIndex : -1;
            return "loadout.random_pool.remove." + poolIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string GetLoadoutPickupMinusControlId(LoadoutRuleEditorEntry entry)
        {
            return "loadout.pickup.minus." + GetLoadoutRuleEntryKey(entry);
        }

        private static string GetLoadoutPickupCountControlId(LoadoutRuleEditorEntry entry)
        {
            return "loadout.pickup.count." + GetLoadoutRuleEntryKey(entry);
        }

        private static string GetLoadoutPickupConfirmControlId(LoadoutRuleEditorEntry entry)
        {
            return "loadout.pickup.confirm." + GetLoadoutRuleEntryKey(entry);
        }

        private static string GetLoadoutPickupPlusControlId(LoadoutRuleEditorEntry entry)
        {
            return "loadout.pickup.plus." + GetLoadoutRuleEntryKey(entry);
        }

        private static string GetLoadoutPickupRemoveControlId(LoadoutRuleEditorEntry entry)
        {
            return "loadout.pickup.remove." + GetLoadoutRuleEntryKey(entry);
        }

        private static string GetLoadoutRuleEntryKey(LoadoutRuleEditorEntry entry)
        {
            int entryIndex = entry != null ? entry.Index : -1;
            return entryIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
