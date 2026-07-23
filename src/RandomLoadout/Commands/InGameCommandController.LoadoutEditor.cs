// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void OpenLoadoutEditorPage(ManualLogSource logger)
        {
            _currentPage = PanelPage.LoadoutEditor;
            _focusInputField = false;
            _focusPickupSearchField = false;
            _loadoutEditorMode = LoadoutEditorMode.PresetList;
            _loadoutEditorFocusedControlId = "loadout.preset_list.reload";
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetDisplayName();

            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command("Loadout editor opened."));
            }
        }

        private void DrawLoadoutEditorPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            if (_loadoutEditorMode == LoadoutEditorMode.RandomPoolDetail)
            {
                DrawLoadoutRandomPoolDetailPage(panelRect, logger);
                return;
            }

            if (_loadoutEditorMode == LoadoutEditorMode.PresetPickupsDetail)
            {
                DrawLoadoutPresetPickupsDetailPage(panelRect, logger);
                return;
            }

            if (_loadoutEditorMode == LoadoutEditorMode.PresetDetail)
            {
                DrawLoadoutPresetDetailPage(panelRect, player, logger);
                return;
            }

            DrawLoadoutPresetListPage(panelRect, player, logger);
        }

        private void DrawLoadoutPresetListPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            const float reloadConfigButtonWidth = 128f;
            Rect reloadButtonRect = new Rect(backButtonRect.x - ButtonGap - reloadConfigButtonWidth, backButtonRect.y, reloadConfigButtonWidth, 30f);
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), GetControllerButtonStyle("loadout.back", _buttonStyle)))
            {
                _currentPage = PanelPage.Command;
                _focusInputField = true;
                return;
            }

            if (GUI.Button(reloadButtonRect, GuiText.Get("gui.loadout_editor.button.reload"), GetControllerButtonStyle("loadout.preset_list.reload", _buttonStyle)))
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
            if (GUI.Button(newPresetButtonRect, GuiText.Get("gui.loadout_editor.button.new_preset"), GetControllerButtonStyle("loadout.preset_list.new", _buttonStyle)))
            {
                ExecuteLoadoutEditorCreatePreset(logger);
            }

            if (GUI.Button(duplicatePresetButtonRect, GuiText.Get("gui.loadout_editor.button.duplicate_preset"), GetControllerButtonStyle("loadout.preset_list.duplicate", _buttonStyle)))
            {
                ExecuteLoadoutEditorDuplicatePreset(logger);
            }

            if (GUI.Button(deletePresetButtonRect, GuiText.Get("gui.loadout_editor.button.delete_preset"), GetControllerButtonStyle("loadout.preset_list.delete", _buttonStyle)))
            {
                ExecuteLoadoutEditorDeletePreset(logger);
            }

            if (GUI.Button(fillCurrentPresetButtonRect, GuiText.Get("gui.loadout_editor.button.fill_current_preset"), GetControllerButtonStyle("loadout.preset_list.fill", _buttonStyle)))
            {
                ExecuteLoadoutEditorFillCurrentPreset(player, logger);
            }

            if (GUI.Button(
                randomPresetButtonRect,
                GetLoadoutPresetRandomButtonLabel(),
                GetControllerButtonStyle("loadout.preset_list.random", IsLoadoutPresetRandomEnabled() ? _enabledButtonStyle : _disabledToggleButtonStyle)))
            {
                ExecuteToggleLoadoutPresetRandom(logger);
            }

            Rect iconToggleRowRect = new Rect(panelRect.x + 14f, panelRect.y + 102f, panelRect.width - 28f, 28f);
            const float iconToggleButtonWidth = 180f;
            bool areIconsEnabled = IsStartItemsPresetIconsEnabled();
            if (GUI.Button(
                new Rect(iconToggleRowRect.xMax - iconToggleButtonWidth, iconToggleRowRect.y, iconToggleButtonWidth, iconToggleRowRect.height),
                GetStartItemsPresetIconsButtonLabel(areIconsEnabled),
                GetControllerButtonStyle("loadout.preset_list.icons", areIconsEnabled ? _enabledButtonStyle : _disabledToggleButtonStyle)))
            {
                ExecuteToggleStartItemsPresetIcons(logger);
            }

            const float renameButtonWidth = 92f;
            Rect renameLabelRect = new Rect(panelRect.x + 14f, panelRect.y + 136f, 92f, 28f);
            Rect renameButtonRect = new Rect(panelRect.x + panelRect.width - renameButtonWidth - 14f, renameLabelRect.y, renameButtonWidth, 28f);
            Rect renameFieldRect = new Rect(renameLabelRect.xMax + ButtonGap, renameLabelRect.y, renameButtonRect.x - renameLabelRect.xMax - (ButtonGap * 2f), 28f);
            GUI.Label(renameLabelRect, GuiText.Get("gui.loadout_editor.rename_label"), _hintStyle);
            _loadoutPresetRenameText = GUI.TextField(renameFieldRect, _loadoutPresetRenameText, 64, _textFieldStyle);
            if (GUI.Button(renameButtonRect, GuiText.Get("gui.loadout_editor.button.rename_preset"), GetControllerButtonStyle("loadout.preset_list.rename", _buttonStyle)))
            {
                ExecuteLoadoutEditorRenamePreset(logger);
            }

            DrawLoadoutPresetRows(new Rect(panelRect.x + 14f, panelRect.y + 176f, panelRect.width - 28f, panelRect.height - 190f), logger);
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
                logger.LogInfo(RandomLoadoutLog.Command("Start Items preset icons " + (isEnabled ? "enabled" : "disabled") + "."));
            }
        }

        private void DrawLoadoutPresetDetailPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
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
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), GetControllerButtonStyle("loadout.back", _buttonStyle)))
            {
                _loadoutEditorMode = LoadoutEditorMode.PresetList;
                _loadoutEditorFocusedControlId = "loadout.preset_list.reload";
                RefreshLoadoutPresetEntries();
                return;
            }

            if (GUI.Button(addItemButtonRect, GuiText.Get("gui.loadout_editor.button.add_item"), GetControllerButtonStyle("loadout.preset_detail.add_item", _buttonStyle)))
            {
                OpenPickupAddToStartItemsPage(logger);
                return;
            }

            if (GUI.Button(addRandomPoolButtonRect, GuiText.Get("gui.loadout_editor.button.add_random_pool"), GetControllerButtonStyle("loadout.preset_detail.add_random_pool", _buttonStyle)))
            {
                ExecuteLoadoutEditorCreateRandomPool(logger);
            }

            if (GUI.Button(addPresetPickupsButtonRect, GuiText.Get("gui.loadout_editor.button.pickups"), GetControllerButtonStyle("loadout.preset_detail.pickups", _buttonStyle)))
            {
                OpenLoadoutPresetPickupsDetail();
            }

            if (GUI.Button(fillCurrentPresetButtonRect, GuiText.Get("gui.loadout_editor.button.fill_current_preset"), GetControllerButtonStyle("loadout.preset_detail.fill", _buttonStyle)))
            {
                ExecuteLoadoutEditorFillCurrentPreset(player, logger);
            }

            if (GUI.Button(reloadButtonRect, GuiText.Get("gui.loadout_editor.button.reload"), GetControllerButtonStyle("loadout.preset_detail.reload", _buttonStyle)))
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
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            const float addItemButtonWidth = 112f;
            Rect addItemButtonRect = new Rect(backButtonRect.x - ButtonGap - addItemButtonWidth, backButtonRect.y, addItemButtonWidth, 30f);
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), GetControllerButtonStyle("loadout.back", _buttonStyle)))
            {
                _loadoutEditorMode = LoadoutEditorMode.PresetDetail;
                _loadoutEditorFocusedControlId = "loadout.preset_detail.add_item";
                RefreshLoadoutEditorEntries();
                return;
            }

            if (GUI.Button(addItemButtonRect, GuiText.Get("gui.loadout_editor.button.add_item"), GetControllerButtonStyle("loadout.random_pool.add_item", _buttonStyle)))
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
            if (GUI.Button(renameButtonRect, GuiText.Get("gui.loadout_editor.button.rename_random_pool"), GetControllerButtonStyle("loadout.random_pool.rename", _buttonStyle)))
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
            if (GUI.Button(selectButtonRect, GuiText.Get("gui.loadout_editor.button.select_preset"), GetControllerButtonStyle(GetLoadoutPresetSelectControlId(entry), selectButtonStyle)) &&
                !IsLoadoutPresetRandomEnabled())
            {
                if (entry != null)
                {
                    ExecuteLoadoutEditorSelectPreset(entry.Id, logger);
                }
            }

            if (GUI.Button(openButtonRect, GuiText.Get("gui.loadout_editor.button.open_preset"), GetControllerButtonStyle(GetLoadoutPresetOpenControlId(entry), _buttonStyle)))
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
                if (TryGetPickupIcon(pickupIds[index], out iconData))
                {
                    GUI.DrawTextureWithTexCoords(iconRect, iconData.Texture, iconData.TextureCoords, true);
                }
                else
                {
                    GUI.Box(iconRect, "?", _pickupIconFallbackStyle);
                }
            }
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
                GUI.Button(
                    toggleButtonRect,
                    entry.IsEnabled ? GuiText.Get("gui.settings.button.enable") : GuiText.Get("gui.settings.button.disable"),
                    GetControllerButtonStyle(GetLoadoutRuleToggleControlId(entry), entry.IsEnabled ? _enabledButtonStyle : _buttonStyle)))
            {
                ExecuteLoadoutEditorToggleRule(entry.Index, logger);
            }

            if (entry != null &&
                entry.IsRandomPool &&
                GUI.Button(editButtonRect, GuiText.Get("gui.loadout_editor.button.edit"), GetControllerButtonStyle(GetLoadoutRuleEditControlId(entry), _buttonStyle)))
            {
                OpenLoadoutRandomPoolDetail(entry.Index);
            }

            if (entry != null &&
                !entry.IsRandomPool &&
                (entry.IsPresetPickupCollection || !string.IsNullOrEmpty(entry.PickupType)) &&
                GUI.Button(editButtonRect, GuiText.Get("gui.loadout_editor.button.edit"), GetControllerButtonStyle(GetLoadoutRuleEditControlId(entry), _buttonStyle)))
            {
                OpenLoadoutPresetPickupsDetail();
            }

            if (GUI.Button(removeButtonRect, GuiText.Get("gui.loadout_editor.button.remove"), GetControllerButtonStyle(GetLoadoutRuleRemoveControlId(entry), _buttonStyle)))
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

            if (GUI.Button(removeButtonRect, GuiText.Get("gui.loadout_editor.button.remove"), GetControllerButtonStyle(GetLoadoutRandomPoolRemoveControlId(entry), _buttonStyle)))
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
            _cachedLoadoutRuleEntries = _loadoutRuleEditorService != null
                ? _loadoutRuleEditorService.GetEntries()
                : EmptyLoadoutRuleEditorEntries;
        }

        private void RefreshLoadoutPresetEntries()
        {
            _cachedLoadoutPresetEntries = _loadoutRuleEditorService != null
                ? _loadoutRuleEditorService.GetPresetEntries()
                : EmptyLoadoutPresetEditorEntries;
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
                logger.LogInfo(RandomLoadoutLog.Command("Start Items random preset selection " + (isEnabled ? "enabled" : "disabled") + "."));
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
            _cachedLoadoutRandomPoolEntries = _loadoutRuleEditorService != null && _loadoutRandomPoolRuleIndex >= 0
                ? _loadoutRuleEditorService.GetRandomPoolEntries(_loadoutRandomPoolRuleIndex)
                : EmptyLoadoutRandomPoolEditorEntries;
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

            if (!entry.IsActive)
            {
                GrantCommandExecutionResult result = _loadoutRuleEditorService.SelectPreset(entry.Id);
                ShowStatus(result.Message, !result.Succeeded);
                LogLoadoutEditorResult(result, logger);
                if (!result.Succeeded)
                {
                    RefreshLoadoutPresetEntries();
                    return;
                }
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
            return _loadoutRuleEditorService != null
                ? _loadoutRuleEditorService.GetActivePresetDisplayName()
                : StartItemsPresetNames.GetDisplayName(StartItemsPresetNames.DefaultPresetId, string.Empty, StartItemsPresetNames.DefaultPresetDisplayNameKey);
        }

        private string GetLoadoutEditorActiveRandomPoolDisplayName()
        {
            return _loadoutRuleEditorService != null
                ? _loadoutRuleEditorService.GetRandomPoolDisplayName(_loadoutRandomPoolRuleIndex)
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
                logger.LogInfo(RandomLoadoutLog.Command(result.LogMessage));
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(result.LogMessage));
            }
        }

        private ControllerFocusEntry[] GetLoadoutEditorFocusEntries()
        {
            if (_loadoutEditorMode == LoadoutEditorMode.PresetDetail)
            {
                int dynamicCount = 0;
                for (int index = 0; index < _cachedLoadoutRuleEntries.Length; index++)
                {
                    dynamicCount++;
                    if (_cachedLoadoutRuleEntries[index] != null && !_cachedLoadoutRuleEntries[index].IsPresetPickupCollection)
                    {
                        dynamicCount++;
                    }
                    if (DoesLoadoutRuleEntryHaveEditAction(_cachedLoadoutRuleEntries[index]))
                    {
                        dynamicCount++;
                    }
                }

                ControllerFocusEntry[] entries = new ControllerFocusEntry[6 + dynamicCount];
                entries[0] = new ControllerFocusEntry("loadout.back", 0, 1);
                entries[1] = new ControllerFocusEntry("loadout.preset_detail.reload", 0, 0);
                entries[2] = new ControllerFocusEntry("loadout.preset_detail.add_item", 1, 0);
                entries[3] = new ControllerFocusEntry("loadout.preset_detail.add_random_pool", 1, 1);
                entries[4] = new ControllerFocusEntry("loadout.preset_detail.pickups", 1, 2);
                entries[5] = new ControllerFocusEntry("loadout.preset_detail.fill", 1, 3);
                int writeIndex = 6;
                for (int index = 0; index < _cachedLoadoutRuleEntries.Length; index++)
                {
                    LoadoutRuleEditorEntry entry = _cachedLoadoutRuleEntries[index];
                    int row = 2 + index;
                    if (entry != null && !entry.IsPresetPickupCollection)
                    {
                        entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutRuleToggleControlId(entry), row, 0);
                    }

                    if (DoesLoadoutRuleEntryHaveEditAction(entry))
                    {
                        int editColumn = entry != null && !entry.IsPresetPickupCollection ? 1 : 0;
                        entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutRuleEditControlId(entry), row, editColumn);
                    }

                    int removeColumn = entry != null && !entry.IsPresetPickupCollection ? 2 : 1;
                    entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutRuleRemoveControlId(entry), row, removeColumn);
                }

                return entries;
            }

            if (_loadoutEditorMode == LoadoutEditorMode.RandomPoolDetail)
            {
                ControllerFocusEntry[] entries = new ControllerFocusEntry[3 + _cachedLoadoutRandomPoolEntries.Length];
                entries[0] = new ControllerFocusEntry("loadout.random_pool.add_item", 0, 0);
                entries[1] = new ControllerFocusEntry("loadout.back", 0, 1);
                entries[2] = new ControllerFocusEntry("loadout.random_pool.rename", 1, 0);
                for (int index = 0; index < _cachedLoadoutRandomPoolEntries.Length; index++)
                {
                    entries[index + 3] = new ControllerFocusEntry(GetLoadoutRandomPoolRemoveControlId(_cachedLoadoutRandomPoolEntries[index]), 2 + index, 0);
                }

                return entries;
            }

            if (_loadoutEditorMode == LoadoutEditorMode.PresetPickupsDetail)
            {
                int dynamicCount = 0;
                for (int index = 0; index < _cachedLoadoutPickupEntries.Length; index++)
                {
                    dynamicCount += 4;
                    if (_cachedLoadoutPickupEntries[index] != null && _cachedLoadoutPickupEntries[index].Index == _loadoutPickupCountEditIndex)
                    {
                        dynamicCount++;
                    }
                }

                ControllerFocusEntry[] entries = new ControllerFocusEntry[7 + dynamicCount];
                entries[0] = new ControllerFocusEntry("loadout.back", 0, 0);
                entries[1] = new ControllerFocusEntry("loadout.pickups.add_max_health", 1, 0);
                entries[2] = new ControllerFocusEntry("loadout.pickups.add_armor", 2, 0);
                entries[3] = new ControllerFocusEntry("loadout.pickups.add_key", 3, 0);
                entries[4] = new ControllerFocusEntry("loadout.pickups.add_rat_key", 4, 0);
                entries[5] = new ControllerFocusEntry("loadout.pickups.add_blank", 5, 0);
                entries[6] = new ControllerFocusEntry("loadout.pickups.add_casings", 6, 0);
                int writeIndex = 7;
                for (int index = 0; index < _cachedLoadoutPickupEntries.Length; index++)
                {
                    LoadoutRuleEditorEntry entry = _cachedLoadoutPickupEntries[index];
                    int row = 7 + index;
                    entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutPickupMinusControlId(entry), row, 0);
                    entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutPickupCountControlId(entry), row, 1);
                    if (entry != null && entry.Index == _loadoutPickupCountEditIndex)
                    {
                        entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutPickupConfirmControlId(entry), row, 2);
                        entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutPickupPlusControlId(entry), row, 3);
                        entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutPickupRemoveControlId(entry), row, 4);
                    }
                    else
                    {
                        entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutPickupPlusControlId(entry), row, 2);
                        entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutPickupRemoveControlId(entry), row, 3);
                    }
                }

                return entries;
            }

            int presetCount = _cachedLoadoutPresetEntries != null ? _cachedLoadoutPresetEntries.Length : 0;
            bool manualPresetSelectionEnabled = !IsLoadoutPresetRandomEnabled();
            ControllerFocusEntry[] presetListEntries = new ControllerFocusEntry[9 + (presetCount * (manualPresetSelectionEnabled ? 2 : 1))];
            presetListEntries[0] = new ControllerFocusEntry("loadout.back", 0, 1);
            presetListEntries[1] = new ControllerFocusEntry("loadout.preset_list.reload", 0, 0);
            presetListEntries[2] = new ControllerFocusEntry("loadout.preset_list.new", 1, 0);
            presetListEntries[3] = new ControllerFocusEntry("loadout.preset_list.duplicate", 1, 1);
            presetListEntries[4] = new ControllerFocusEntry("loadout.preset_list.delete", 1, 2);
            presetListEntries[5] = new ControllerFocusEntry("loadout.preset_list.fill", 1, 3);
            presetListEntries[6] = new ControllerFocusEntry("loadout.preset_list.random", 1, 4);
            presetListEntries[7] = new ControllerFocusEntry("loadout.preset_list.rename", 2, 0);
            presetListEntries[8] = new ControllerFocusEntry("loadout.preset_list.icons", 2, 1);
            for (int index = 0; index < presetCount; index++)
            {
                int baseIndex = 9 + (index * (manualPresetSelectionEnabled ? 2 : 1));
                int presetColumn = index % LoadoutPresetColumnCount;
                int presetRow = 3 + (index / LoadoutPresetColumnCount);
                int focusColumn = presetColumn * 2;
                if (manualPresetSelectionEnabled)
                {
                    presetListEntries[baseIndex++] = new ControllerFocusEntry(GetLoadoutPresetSelectControlId(_cachedLoadoutPresetEntries[index]), presetRow, focusColumn);
                }

                presetListEntries[baseIndex] = new ControllerFocusEntry(GetLoadoutPresetOpenControlId(_cachedLoadoutPresetEntries[index]), presetRow, focusColumn + 1);
            }

            return presetListEntries;
        }

        private bool TryGetLoadoutEntryIcon(LoadoutRuleEditorEntry entry, out PickupIconData iconData)
        {
            iconData = PickupIconData.Empty;
            if (entry == null)
            {
                return false;
            }

            if (entry.PickupId.HasValue && TryGetPickupIcon(entry.PickupId.Value, out iconData))
            {
                return true;
            }

            return TryGetStartItemPickupIcon(entry.PickupType, out iconData);
        }

        private bool TryGetStartItemPickupIcon(string pickupType, out PickupIconData iconData)
        {
            iconData = PickupIconData.Empty;
            string spriteName = GetStartItemPickupSpriteName(pickupType);
            if (string.IsNullOrEmpty(spriteName) ||
                !TryGetGameUiAtlasIcon(spriteName, out iconData))
            {
                return false;
            }

            return true;
        }

        private bool TryGetGameUiAtlasIcon(string spriteName, out PickupIconData iconData)
        {
            iconData = PickupIconData.Empty;
            dfAtlas atlas;
            if (string.IsNullOrEmpty(spriteName) || !TryGetGameUiAtlas(out atlas) || atlas == null)
            {
                return false;
            }

            dfAtlas.ItemInfo item = atlas[spriteName];
            Texture texture = atlas.Texture;
            if (item == null || texture == null)
            {
                return false;
            }

            // Use the atlas' own runtime region data so sprite-name changes do not get out of sync with hand-written UVs.
            Rect region = item.region;
            Rect textureCoords = Rect.MinMaxRect(region.xMin, region.yMin, region.xMax, region.yMax);
            iconData = new PickupIconData(texture, textureCoords);
            return true;
        }

        private bool TryGetGameUiAtlas(out dfAtlas atlas)
        {
            atlas = _gameUiAtlas;
            if (_hasResolvedGameUiAtlas)
            {
                return atlas != null;
            }

            _hasResolvedGameUiAtlas = true;
            UnityEngine.Object[] atlases = Resources.FindObjectsOfTypeAll(typeof(dfAtlas));
            if (atlases == null)
            {
                return false;
            }

            for (int index = 0; index < atlases.Length; index++)
            {
                dfAtlas candidate = atlases[index] as dfAtlas;
                if (candidate == null || candidate.Texture == null)
                {
                    continue;
                }

                if (string.Equals(candidate.Texture.name, "GameUIAtlas", System.StringComparison.Ordinal) ||
                    string.Equals(candidate.gameObject.name, "GameUIAtlas", System.StringComparison.Ordinal))
                {
                    _gameUiAtlas = candidate;
                    atlas = candidate;
                    return true;
                }
            }

            return false;
        }

        private static string GetStartItemPickupFallbackLabel(string pickupType)
        {
            switch (StartItemPickupCatalog.NormalizeType(pickupType))
            {
                case StartItemPickupCatalog.KeyType:
                    return "K";
                case StartItemPickupCatalog.RatKeyType:
                    return "R";
                case StartItemPickupCatalog.MaxHealthType:
                    return "H";
                case StartItemPickupCatalog.ArmorType:
                    return "A";
                case StartItemPickupCatalog.CasingsType:
                    return "C";
                case StartItemPickupCatalog.BlankType:
                    return "B";
                default:
                    return "?";
            }
        }

        private void ExecuteLoadoutEditorFocusedControl(PlayerController player, ManualLogSource logger)
        {
            switch (_loadoutEditorFocusedControlId)
            {
                case "loadout.back":
                    HandleLoadoutEditorBackNavigation();
                    return;
                case "loadout.preset_list.reload":
                case "loadout.preset_detail.reload":
                    ExecuteLoadoutEditorReload(logger);
                    return;
                case "loadout.preset_list.new":
                    ExecuteLoadoutEditorCreatePreset(logger);
                    return;
                case "loadout.preset_list.duplicate":
                    ExecuteLoadoutEditorDuplicatePreset(logger);
                    return;
                case "loadout.preset_list.delete":
                    ExecuteLoadoutEditorDeletePreset(logger);
                    return;
                case "loadout.preset_list.fill":
                case "loadout.preset_detail.fill":
                    ExecuteLoadoutEditorFillCurrentPreset(player, logger);
                    return;
                case "loadout.preset_list.random":
                    ExecuteToggleLoadoutPresetRandom(logger);
                    return;
                case "loadout.preset_list.rename":
                    ExecuteLoadoutEditorRenamePreset(logger);
                    return;
                case "loadout.preset_list.icons":
                    ExecuteToggleStartItemsPresetIcons(logger);
                    return;
                case "loadout.preset_detail.add_item":
                    OpenPickupAddToStartItemsPage(logger);
                    return;
                case "loadout.preset_detail.add_random_pool":
                    ExecuteLoadoutEditorCreateRandomPool(logger);
                    return;
                case "loadout.preset_detail.pickups":
                    OpenLoadoutPresetPickupsDetail();
                    return;
                case "loadout.random_pool.add_item":
                    OpenPickupAddToRandomPoolPage(logger);
                    return;
                case "loadout.random_pool.rename":
                    ExecuteLoadoutEditorRenameRandomPool(logger);
                    return;
                case "loadout.pickups.add_key":
                    ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.KeyType, logger);
                    return;
                case "loadout.pickups.add_rat_key":
                    ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.RatKeyType, logger);
                    return;
                case "loadout.pickups.add_max_health":
                    ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.MaxHealthType, logger);
                    return;
                case "loadout.pickups.add_armor":
                    ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.ArmorType, logger);
                    return;
                case "loadout.pickups.add_blank":
                    ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.BlankType, logger);
                    return;
                case "loadout.pickups.add_casings":
                    ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.CasingsType, logger);
                    return;
            }

            for (int index = 0; index < _cachedLoadoutRuleEntries.Length; index++)
            {
                LoadoutRuleEditorEntry entry = _cachedLoadoutRuleEntries[index];
                if (entry != null &&
                    !entry.IsPresetPickupCollection &&
                    string.Equals(_loadoutEditorFocusedControlId, GetLoadoutRuleToggleControlId(entry), System.StringComparison.Ordinal))
                {
                    ExecuteLoadoutEditorToggleRule(entry.Index, logger);
                    return;
                }

                if (DoesLoadoutRuleEntryHaveEditAction(entry) &&
                    string.Equals(_loadoutEditorFocusedControlId, GetLoadoutRuleEditControlId(entry), System.StringComparison.Ordinal))
                {
                    if (entry != null && entry.IsRandomPool)
                    {
                        OpenLoadoutRandomPoolDetail(entry.Index);
                    }
                    else
                    {
                        OpenLoadoutPresetPickupsDetail();
                    }

                    return;
                }

                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutRuleRemoveControlId(entry), System.StringComparison.Ordinal))
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
                        ExecuteLoadoutEditorRemove(entry != null ? entry.Index : -1, logger);
                    }

                    return;
                }
            }

            for (int index = 0; index < _cachedLoadoutRandomPoolEntries.Length; index++)
            {
                LoadoutRandomPoolEditorEntry entry = _cachedLoadoutRandomPoolEntries[index];
                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutRandomPoolRemoveControlId(entry), System.StringComparison.Ordinal))
                {
                    ExecuteLoadoutEditorRemoveFromRandomPool(entry != null ? entry.PoolIndex : -1, logger);
                    return;
                }
            }

            for (int index = 0; index < _cachedLoadoutPickupEntries.Length; index++)
            {
                LoadoutRuleEditorEntry entry = _cachedLoadoutPickupEntries[index];
                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutPickupMinusControlId(entry), System.StringComparison.Ordinal))
                {
                    ExecuteLoadoutEditorChangePresetPickupCount(entry != null ? entry.Index : -1, -1, logger);
                    return;
                }

                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutPickupCountControlId(entry), System.StringComparison.Ordinal))
                {
                    _loadoutPickupCountEditIndex = entry != null ? entry.Index : -1;
                    _loadoutPickupCountEditText = entry != null ? entry.Count.ToString() : "1";
                    _loadoutEditorFocusedControlId = GetLoadoutPickupConfirmControlId(entry);
                    return;
                }

                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutPickupConfirmControlId(entry), System.StringComparison.Ordinal))
                {
                    ExecuteLoadoutEditorSetPresetPickupCount(entry != null ? entry.Index : -1, _loadoutPickupCountEditText, logger);
                    return;
                }

                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutPickupPlusControlId(entry), System.StringComparison.Ordinal))
                {
                    ExecuteLoadoutEditorChangePresetPickupCount(entry != null ? entry.Index : -1, 1, logger);
                    return;
                }

                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutPickupRemoveControlId(entry), System.StringComparison.Ordinal))
                {
                    ExecuteLoadoutEditorRemovePresetPickup(entry != null ? entry.Index : -1, logger);
                    return;
                }
            }

            for (int index = 0; index < _cachedLoadoutPresetEntries.Length; index++)
            {
                LoadoutPresetEditorEntry entry = _cachedLoadoutPresetEntries[index];
                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutPresetSelectControlId(entry), System.StringComparison.Ordinal))
                {
                    ExecuteLoadoutEditorSelectPreset(entry.Id, logger);
                    return;
                }

                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutPresetOpenControlId(entry), System.StringComparison.Ordinal))
                {
                    OpenLoadoutPresetDetail(entry, logger);
                    return;
                }
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
