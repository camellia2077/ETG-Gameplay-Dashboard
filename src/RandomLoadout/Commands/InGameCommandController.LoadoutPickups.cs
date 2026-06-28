using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void DrawLoadoutPresetPickupsDetailPage(Rect panelRect, ManualLogSource logger)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            const float actionButtonWidth = 132f;
            const float actionButtonHeight = 28f;
            Rect addKeyButtonRect = new Rect(panelRect.x + 14f, panelRect.y + 84f, actionButtonWidth, actionButtonHeight);
            Rect addRatKeyButtonRect = new Rect(addKeyButtonRect.xMax + ButtonGap, addKeyButtonRect.y, actionButtonWidth, actionButtonHeight);
            Rect addMaxHealthButtonRect = new Rect(addRatKeyButtonRect.xMax + ButtonGap, addKeyButtonRect.y, actionButtonWidth, actionButtonHeight);
            Rect addArmorButtonRect = new Rect(panelRect.x + 14f, addKeyButtonRect.yMax + ButtonGap, actionButtonWidth, actionButtonHeight);
            Rect addBlankButtonRect = new Rect(addArmorButtonRect.xMax + ButtonGap, addArmorButtonRect.y, actionButtonWidth, actionButtonHeight);
            Rect addCasingsButtonRect = new Rect(addBlankButtonRect.xMax + ButtonGap, addArmorButtonRect.y, actionButtonWidth, actionButtonHeight);
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), GetControllerButtonStyle("loadout.back", _buttonStyle)))
            {
                _loadoutEditorMode = LoadoutEditorMode.PresetDetail;
                _loadoutEditorFocusedControlId = "loadout.preset_detail.pickups";
                ResetLoadoutPresetPickupCountEdit();
                RefreshLoadoutEditorEntries();
                return;
            }

            if (GUI.Button(addKeyButtonRect, GuiText.Get("gui.loadout_editor.button.add_pickup_key"), GetControllerButtonStyle("loadout.pickups.add_key", _buttonStyle)))
            {
                ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.KeyType, logger);
            }

            if (GUI.Button(addRatKeyButtonRect, GuiText.Get("gui.loadout_editor.button.add_pickup_rat_key"), GetControllerButtonStyle("loadout.pickups.add_rat_key", _buttonStyle)))
            {
                ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.RatKeyType, logger);
            }

            if (GUI.Button(addMaxHealthButtonRect, GuiText.Get("gui.loadout_editor.button.add_pickup_max_health"), GetControllerButtonStyle("loadout.pickups.add_max_health", _buttonStyle)))
            {
                ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.MaxHealthType, logger);
            }

            if (GUI.Button(addArmorButtonRect, GuiText.Get("gui.loadout_editor.button.add_pickup_armor"), GetControllerButtonStyle("loadout.pickups.add_armor", _buttonStyle)))
            {
                ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.ArmorType, logger);
            }

            if (GUI.Button(addBlankButtonRect, GuiText.Get("gui.loadout_editor.button.add_pickup_blank"), GetControllerButtonStyle("loadout.pickups.add_blank", _buttonStyle)))
            {
                ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.BlankType, logger);
            }

            if (GUI.Button(addCasingsButtonRect, GuiText.Get("gui.loadout_editor.button.add_pickup_casings"), GetControllerButtonStyle("loadout.pickups.add_casings", _buttonStyle)))
            {
                ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.CasingsType, logger);
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, backButtonRect.x - panelRect.x - 28f, 24f),
                GuiText.Get("gui.loadout_editor.pickups_title"),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.loadout_editor.pickups_hint"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 60f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.loadout_editor.pickups_summary", _cachedLoadoutPickupEntries.Length),
                _hintStyle);

            DrawLoadoutPresetPickupRows(new Rect(panelRect.x + 14f, panelRect.y + 156f, panelRect.width - 28f, panelRect.height - 170f), logger);
        }

        private void DrawLoadoutPresetPickupRows(Rect listRect, ManualLogSource logger)
        {
            if (_cachedLoadoutPickupEntries.Length == 0)
            {
                GUI.Box(listRect, GUIContent.none, _pickupRowStyle);
                GUI.Label(
                    new Rect(listRect.x + 12f, listRect.y + 12f, listRect.width - 24f, listRect.height - 24f),
                    GuiText.Get("gui.loadout_editor.pickups_empty"),
                    _wrappedHintStyle);
                return;
            }

            Rect viewRect = new Rect(0f, 0f, listRect.width - 18f, (_cachedLoadoutPickupEntries.Length * PickupRowHeight) + 4f);
            _loadoutEditorScrollPosition = BeginCommandScrollView(listRect, _loadoutEditorScrollPosition, viewRect);
            for (int i = 0; i < _cachedLoadoutPickupEntries.Length; i++)
            {
                DrawLoadoutPresetPickupRow(new Rect(0f, 2f + (i * PickupRowHeight), viewRect.width, PickupRowHeight - 4f), _cachedLoadoutPickupEntries[i], logger);
            }

            GUI.EndScrollView();
        }

        private void DrawLoadoutPresetPickupRow(Rect rowRect, LoadoutRuleEditorEntry entry, ManualLogSource logger)
        {
            GUI.Box(rowRect, GUIContent.none, _pickupRowStyle);

            const float removeWidth = 82f;
            const float countButtonWidth = 36f;
            const float countFieldWidth = 52f;
            const float countConfirmWidth = 36f;
            Rect removeButtonRect = new Rect(rowRect.x + rowRect.width - removeWidth - 8f, rowRect.y + 8f, removeWidth, rowRect.height - 16f);
            Rect plusButtonRect = new Rect(removeButtonRect.x - ButtonGap - countButtonWidth, removeButtonRect.y, countButtonWidth, removeButtonRect.height);
            Rect countConfirmRect = new Rect(plusButtonRect.x - ButtonGap - countConfirmWidth, removeButtonRect.y, countConfirmWidth, removeButtonRect.height);
            Rect countLabelRect = new Rect(countConfirmRect.x - ButtonGap - countFieldWidth, removeButtonRect.y, countFieldWidth, removeButtonRect.height);
            Rect minusButtonRect = new Rect(countLabelRect.x - ButtonGap - countButtonWidth, removeButtonRect.y, countButtonWidth, removeButtonRect.height);
            float textWidth = rowRect.width - removeWidth - countButtonWidth - countConfirmWidth - countFieldWidth - countButtonWidth - 52f;
            GUI.Label(new Rect(rowRect.x + 10f, rowRect.y + 5f, textWidth, 20f), entry != null ? entry.PrimaryText : string.Empty, _pickupPrimaryTextStyle);
            GUI.Label(new Rect(rowRect.x + 10f, rowRect.y + 24f, textWidth, 18f), entry != null ? entry.SecondaryText : string.Empty, _pickupSecondaryTextStyle);

            bool isEditingCount = entry != null && entry.Index == _loadoutPickupCountEditIndex;
            if (isEditingCount)
            {
                _loadoutPickupCountEditText = GUI.TextField(countLabelRect, _loadoutPickupCountEditText, 6, _textFieldStyle);
                if (GUI.Button(countConfirmRect, "OK", GetControllerButtonStyle(GetLoadoutPickupConfirmControlId(entry), _buttonStyle)))
                {
                    ExecuteLoadoutEditorSetPresetPickupCount(entry.Index, _loadoutPickupCountEditText, logger);
                }
            }
            else
            {
                if (GUI.Button(countLabelRect, entry != null ? entry.Count.ToString() : "1", GetControllerButtonStyle(GetLoadoutPickupCountControlId(entry), _buttonStyle)))
                {
                    _loadoutPickupCountEditIndex = entry != null ? entry.Index : -1;
                    _loadoutPickupCountEditText = entry != null ? entry.Count.ToString() : "1";
                }
            }

            if (GUI.Button(minusButtonRect, "-", GetControllerButtonStyle(GetLoadoutPickupMinusControlId(entry), _buttonStyle)))
            {
                ExecuteLoadoutEditorChangePresetPickupCount(entry != null ? entry.Index : -1, -1, logger);
            }

            if (GUI.Button(plusButtonRect, "+", GetControllerButtonStyle(GetLoadoutPickupPlusControlId(entry), _buttonStyle)))
            {
                ExecuteLoadoutEditorChangePresetPickupCount(entry != null ? entry.Index : -1, 1, logger);
            }

            if (GUI.Button(removeButtonRect, GuiText.Get("gui.loadout_editor.button.remove"), GetControllerButtonStyle(GetLoadoutPickupRemoveControlId(entry), _buttonStyle)))
            {
                ExecuteLoadoutEditorRemovePresetPickup(entry != null ? entry.Index : -1, logger);
            }
        }

        private void RefreshLoadoutPickupEntries()
        {
            _cachedLoadoutPickupEntries = _loadoutRuleEditorService != null
                ? _loadoutRuleEditorService.GetPresetPickupEntries()
                : EmptyLoadoutPickupEditorEntries;
        }

        private void OpenLoadoutPresetPickupsDetail()
        {
            _loadoutEditorMode = LoadoutEditorMode.PresetPickupsDetail;
            _loadoutEditorFocusedControlId = "loadout.pickups.add_key";
            _loadoutEditorScrollPosition = Vector2.zero;
            ResetLoadoutPresetPickupCountEdit();
            RefreshLoadoutPickupEntries();
        }

        private void ExecuteLoadoutEditorAddPresetPickup(string pickupType, ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.AddPresetPickup(pickupType);
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorRemovePresetPickup(int pickupIndex, ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.RemovePresetPickupAt(pickupIndex);
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            ResetLoadoutPresetPickupCountEdit();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorClearPresetPickups(ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.ClearPresetPickups();
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            ResetLoadoutPresetPickupCountEdit();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorChangePresetPickupCount(int pickupIndex, int delta, ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.ChangePresetPickupCount(pickupIndex, delta);
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            ResetLoadoutPresetPickupCountEdit();
            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ExecuteLoadoutEditorSetPresetPickupCount(int pickupIndex, string countText, ManualLogSource logger)
        {
            if (_loadoutRuleEditorService == null)
            {
                ShowStatus(GuiText.Get("result.loadout_editor.unavailable"), true);
                return;
            }

            int parsedCount;
            if (!int.TryParse((countText ?? string.Empty).Trim(), out parsedCount) || parsedCount <= 0)
            {
                ShowStatus(GuiText.Get("result.start_items.pickups.invalid_count"), true);
                return;
            }

            GrantCommandExecutionResult result = _loadoutRuleEditorService.SetPresetPickupCount(pickupIndex, parsedCount);
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();
            if (result.Succeeded)
            {
                ResetLoadoutPresetPickupCountEdit();
            }

            ShowStatus(result.Message, !result.Succeeded);
            LogLoadoutEditorResult(result, logger);
        }

        private void ResetLoadoutPresetPickupCountEdit()
        {
            _loadoutPickupCountEditIndex = -1;
            _loadoutPickupCountEditText = string.Empty;
        }
    }
}
