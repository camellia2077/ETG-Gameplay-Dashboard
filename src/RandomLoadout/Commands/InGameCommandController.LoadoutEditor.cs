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
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetName();

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
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), _buttonStyle))
            {
                _currentPage = PanelPage.Command;
                _focusInputField = true;
                return;
            }

            if (GUI.Button(reloadButtonRect, GuiText.Get("gui.loadout_editor.button.reload"), _buttonStyle))
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
            if (GUI.Button(newPresetButtonRect, GuiText.Get("gui.loadout_editor.button.new_preset"), _buttonStyle))
            {
                ExecuteLoadoutEditorCreatePreset(logger);
            }

            if (GUI.Button(duplicatePresetButtonRect, GuiText.Get("gui.loadout_editor.button.duplicate_preset"), _buttonStyle))
            {
                ExecuteLoadoutEditorDuplicatePreset(logger);
            }

            if (GUI.Button(deletePresetButtonRect, GuiText.Get("gui.loadout_editor.button.delete_preset"), _buttonStyle))
            {
                ExecuteLoadoutEditorDeletePreset(logger);
            }

            if (GUI.Button(fillCurrentPresetButtonRect, GuiText.Get("gui.loadout_editor.button.fill_current_preset"), _buttonStyle))
            {
                ExecuteLoadoutEditorFillCurrentPreset(player, logger);
            }

            const float renameButtonWidth = 92f;
            Rect renameLabelRect = new Rect(panelRect.x + 14f, panelRect.y + 102f, 92f, 28f);
            Rect renameButtonRect = new Rect(panelRect.x + panelRect.width - renameButtonWidth - 14f, renameLabelRect.y, renameButtonWidth, 28f);
            Rect renameFieldRect = new Rect(renameLabelRect.xMax + ButtonGap, renameLabelRect.y, renameButtonRect.x - renameLabelRect.xMax - (ButtonGap * 2f), 28f);
            GUI.Label(renameLabelRect, GuiText.Get("gui.loadout_editor.rename_label"), _hintStyle);
            _loadoutPresetRenameText = GUI.TextField(renameFieldRect, _loadoutPresetRenameText, 64, _textFieldStyle);
            if (GUI.Button(renameButtonRect, GuiText.Get("gui.loadout_editor.button.rename_preset"), _buttonStyle))
            {
                ExecuteLoadoutEditorRenamePreset(logger);
            }

            DrawLoadoutPresetRows(new Rect(panelRect.x + 14f, panelRect.y + 142f, panelRect.width - 28f, panelRect.height - 156f), logger);
        }

        private void DrawLoadoutPresetDetailPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            const float reloadConfigButtonWidth = 128f;
            Rect reloadButtonRect = new Rect(backButtonRect.x - ButtonGap - reloadConfigButtonWidth, backButtonRect.y, reloadConfigButtonWidth, 30f);
            const float addItemButtonWidth = 112f;
            const float addRandomPoolButtonWidth = 112f;
            const float fillCurrentPresetButtonWidth = 124f;
            Rect addItemButtonRect = new Rect(panelRect.x + 14f, panelRect.y + 84f, addItemButtonWidth, 28f);
            Rect addRandomPoolButtonRect = new Rect(addItemButtonRect.xMax + ButtonGap, addItemButtonRect.y, addRandomPoolButtonWidth, 28f);
            Rect fillCurrentPresetButtonRect = new Rect(addRandomPoolButtonRect.xMax + ButtonGap, addItemButtonRect.y, fillCurrentPresetButtonWidth, 28f);
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), _buttonStyle))
            {
                _loadoutEditorMode = LoadoutEditorMode.PresetList;
                RefreshLoadoutPresetEntries();
                return;
            }

            if (GUI.Button(addItemButtonRect, GuiText.Get("gui.loadout_editor.button.add_item"), _buttonStyle))
            {
                OpenPickupAddToStartItemsPage(logger);
                return;
            }

            if (GUI.Button(addRandomPoolButtonRect, GuiText.Get("gui.loadout_editor.button.add_random_pool"), _buttonStyle))
            {
                ExecuteLoadoutEditorCreateRandomPool(logger);
            }

            if (GUI.Button(fillCurrentPresetButtonRect, GuiText.Get("gui.loadout_editor.button.fill_current_preset"), _buttonStyle))
            {
                ExecuteLoadoutEditorFillCurrentPreset(player, logger);
            }

            if (GUI.Button(reloadButtonRect, GuiText.Get("gui.loadout_editor.button.reload"), _buttonStyle))
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
                GuiText.Get("gui.loadout_editor.preset", GetLoadoutEditorActivePresetName()),
                _hintStyle);

            DrawLoadoutEditorRows(new Rect(panelRect.x + 14f, panelRect.y + 122f, panelRect.width - 28f, panelRect.height - 136f), logger);
        }

        private void DrawLoadoutRandomPoolDetailPage(Rect panelRect, ManualLogSource logger)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            const float addItemButtonWidth = 112f;
            Rect addItemButtonRect = new Rect(backButtonRect.x - ButtonGap - addItemButtonWidth, backButtonRect.y, addItemButtonWidth, 30f);
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), _buttonStyle))
            {
                _loadoutEditorMode = LoadoutEditorMode.PresetDetail;
                RefreshLoadoutEditorEntries();
                return;
            }

            if (GUI.Button(addItemButtonRect, GuiText.Get("gui.loadout_editor.button.add_item"), _buttonStyle))
            {
                OpenPickupAddToRandomPoolPage(logger);
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, addItemButtonRect.x - panelRect.x - 28f, 24f),
                GuiText.Get("gui.loadout_editor.random_pool_title"),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.loadout_editor.random_pool_hint"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 60f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.loadout_editor.random_pool_summary", _cachedLoadoutRandomPoolEntries.Length),
                _hintStyle);

            DrawLoadoutRandomPoolRows(new Rect(panelRect.x + 14f, panelRect.y + 92f, panelRect.width - 28f, panelRect.height - 106f), logger);
        }

        private void DrawLoadoutPresetRows(Rect listRect, ManualLogSource logger)
        {
            if (_cachedLoadoutPresetEntries.Length == 0)
            {
                GUI.Box(listRect, GUIContent.none, _pickupRowStyle);
                GUI.Label(
                    new Rect(listRect.x + 12f, listRect.y + 12f, listRect.width - 24f, listRect.height - 24f),
                    GuiText.Get("gui.loadout_editor.empty_presets"),
                    _wrappedHintStyle);
                return;
            }

            Rect viewRect = new Rect(0f, 0f, listRect.width - 18f, (_cachedLoadoutPresetEntries.Length * PickupRowHeight) + 4f);
            _loadoutPresetScrollPosition = GUI.BeginScrollView(listRect, _loadoutPresetScrollPosition, viewRect);
            for (int i = 0; i < _cachedLoadoutPresetEntries.Length; i++)
            {
                DrawLoadoutPresetRow(new Rect(0f, 2f + (i * PickupRowHeight), viewRect.width, PickupRowHeight - 4f), _cachedLoadoutPresetEntries[i], logger);
            }

            GUI.EndScrollView();
        }

        private void DrawLoadoutPresetRow(Rect rowRect, LoadoutPresetEditorEntry entry, ManualLogSource logger)
        {
            GUIStyle rowStyle = entry != null && entry.IsActive ? _pickupFilterActiveButtonStyle : _pickupRowStyle;
            GUI.Box(rowRect, GUIContent.none, rowStyle);

            const float selectWidth = 82f;
            const float openWidth = 82f;
            Rect openButtonRect = new Rect(rowRect.x + rowRect.width - openWidth - 8f, rowRect.y + 8f, openWidth, rowRect.height - 16f);
            Rect selectButtonRect = new Rect(openButtonRect.x - ButtonGap - selectWidth, openButtonRect.y, selectWidth, openButtonRect.height);
            Rect rowButtonRect = new Rect(rowRect.x, rowRect.y, selectButtonRect.x - rowRect.x - ButtonGap, rowRect.height);
            if (GUI.Button(rowButtonRect, GUIContent.none, _pickupRowButtonStyle))
            {
                OpenLoadoutPresetDetail(entry, logger);
            }

            string primaryText = entry != null && entry.IsActive
                ? GuiText.Get("gui.loadout_editor.preset_active_name", entry.Name)
                : (entry != null ? entry.Name : string.Empty);
            string secondaryText = entry != null
                ? GuiText.Get("gui.loadout_editor.preset_summary", entry.RuleCount, entry.SpecificCount, entry.RandomCount)
                : string.Empty;
            GUI.Label(new Rect(rowRect.x + 10f, rowRect.y + 5f, rowButtonRect.width - 20f, 20f), primaryText, _pickupPrimaryTextStyle);
            GUI.Label(new Rect(rowRect.x + 10f, rowRect.y + 24f, rowButtonRect.width - 20f, 18f), secondaryText, _pickupSecondaryTextStyle);

            if (GUI.Button(selectButtonRect, GuiText.Get("gui.loadout_editor.button.select_preset"), _buttonStyle))
            {
                if (entry != null)
                {
                    ExecuteLoadoutEditorSelectPreset(entry.Name, logger);
                }
            }

            if (GUI.Button(openButtonRect, GuiText.Get("gui.loadout_editor.button.open_preset"), _buttonStyle))
            {
                OpenLoadoutPresetDetail(entry, logger);
            }
        }

        private void DrawLoadoutEditorRows(Rect listRect, ManualLogSource logger)
        {
            if (_cachedLoadoutRuleEntries.Length == 0)
            {
                GUI.Box(listRect, GUIContent.none, _pickupRowStyle);
                GUI.Label(
                    new Rect(listRect.x + 12f, listRect.y + 12f, listRect.width - 24f, listRect.height - 24f),
                    GuiText.Get("gui.loadout_editor.empty"),
                    _wrappedHintStyle);
                return;
            }

            Rect viewRect = new Rect(0f, 0f, listRect.width - 18f, (_cachedLoadoutRuleEntries.Length * LoadoutRuleRowHeight) + 4f);
            _loadoutEditorScrollPosition = GUI.BeginScrollView(listRect, _loadoutEditorScrollPosition, viewRect);
            for (int i = 0; i < _cachedLoadoutRuleEntries.Length; i++)
            {
                DrawLoadoutEditorRow(new Rect(0f, 2f + (i * LoadoutRuleRowHeight), viewRect.width, LoadoutRuleRowHeight - 4f), _cachedLoadoutRuleEntries[i], logger);
            }

            GUI.EndScrollView();
        }

        private void DrawLoadoutEditorRow(Rect rowRect, LoadoutRuleEditorEntry entry, ManualLogSource logger)
        {
            GUI.Box(rowRect, GUIContent.none, _pickupRowStyle);

            float removeWidth = 82f;
            Rect removeButtonRect = new Rect(rowRect.x + rowRect.width - removeWidth - 8f, rowRect.y + 8f, removeWidth, rowRect.height - 16f);
            const float editWidth = 82f;
            Rect editButtonRect = new Rect(removeButtonRect.x - ButtonGap - editWidth, removeButtonRect.y, editWidth, removeButtonRect.height);
            Rect iconRect = new Rect(rowRect.x + 8f, rowRect.y + ((rowRect.height - PickupIconSize) * 0.5f), PickupIconSize, PickupIconSize);
            DrawLoadoutEditorIcon(iconRect, entry);

            float textLeft = iconRect.xMax + 8f;
            float actionWidth = removeWidth + (entry != null && entry.IsRandomPool ? editWidth + ButtonGap : 0f);
            float textWidth = rowRect.width - actionWidth - PickupIconSize - 44f;
            GUI.Label(new Rect(textLeft, rowRect.y + 8f, textWidth, 22f), entry.PrimaryText, _pickupPrimaryTextStyle);
            GUI.Label(new Rect(textLeft, rowRect.y + 32f, textWidth, 20f), entry.SecondaryText, _pickupSecondaryTextStyle);

            if (entry != null && entry.IsRandomPool && GUI.Button(editButtonRect, GuiText.Get("gui.loadout_editor.button.edit"), _buttonStyle))
            {
                OpenLoadoutRandomPoolDetail(entry.Index);
            }

            if (GUI.Button(removeButtonRect, GuiText.Get("gui.loadout_editor.button.remove"), _buttonStyle))
            {
                ExecuteLoadoutEditorRemove(entry.Index, logger);
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

            Rect viewRect = new Rect(0f, 0f, listRect.width - 18f, (_cachedLoadoutRandomPoolEntries.Length * PickupRowHeight) + 4f);
            _loadoutEditorScrollPosition = GUI.BeginScrollView(listRect, _loadoutEditorScrollPosition, viewRect);
            for (int i = 0; i < _cachedLoadoutRandomPoolEntries.Length; i++)
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

            if (GUI.Button(removeButtonRect, GuiText.Get("gui.loadout_editor.button.remove"), _buttonStyle))
            {
                ExecuteLoadoutEditorRemoveFromRandomPool(entry != null ? entry.PoolIndex : -1, logger);
            }
        }

        private void DrawLoadoutEditorIcon(Rect iconRect, LoadoutRuleEditorEntry entry)
        {
            PickupIconData iconData;
            if (entry != null && entry.PickupId.HasValue && TryGetPickupIcon(entry.PickupId.Value, out iconData))
            {
                GUI.DrawTextureWithTexCoords(iconRect, iconData.Texture, iconData.TextureCoords, true);
                return;
            }

            GUI.Box(iconRect, "?", _pickupIconFallbackStyle);
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
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetName();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorSelectPreset(string presetName, ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.SelectPreset(presetName);
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetName();
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
                GrantCommandExecutionResult result = _loadoutRuleEditorService.SelectPreset(entry.Name);
                ShowStatus(result.Message, !result.Succeeded);
                LogLoadoutEditorResult(result, logger);
                if (!result.Succeeded)
                {
                    RefreshLoadoutPresetEntries();
                    return;
                }
            }

            _loadoutEditorMode = LoadoutEditorMode.PresetDetail;
            _loadoutPresetRenameText = entry.Name;
            _loadoutEditorScrollPosition = Vector2.zero;
            _loadoutRandomPoolRuleIndex = -1;
            _cachedLoadoutRandomPoolEntries = EmptyLoadoutRandomPoolEditorEntries;
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
        }

        private void OpenLoadoutRandomPoolDetail(int ruleIndex)
        {
            _loadoutEditorMode = LoadoutEditorMode.RandomPoolDetail;
            _loadoutRandomPoolRuleIndex = ruleIndex;
            _loadoutEditorScrollPosition = Vector2.zero;
            RefreshLoadoutRandomPoolEntries();
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
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetName();
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
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetName();
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
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetName();
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
            _loadoutPresetRenameText = GetLoadoutEditorActivePresetName();
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
            }

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
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private string GetLoadoutEditorActivePresetName()
        {
            return _loadoutRuleEditorService != null ? _loadoutRuleEditorService.GetActivePresetName() : "default";
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
    }
}
