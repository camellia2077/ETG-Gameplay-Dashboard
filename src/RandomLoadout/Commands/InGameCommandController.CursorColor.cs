// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void OpenCursorColorPage()
        {
            _currentPage = PanelPage.CursorColor;
            _cursorColorPageFocusedControlId = "cursor_color.back";
            _focusInputField = false;
            _focusPickupSearchField = false;
            RequestGuiFocusRelease();
        }

        private void DrawCursorColorPage(Rect panelRect)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), GetControllerButtonStyle("cursor_color.back", _buttonStyle)))
            {
                CloseCursorColorPage();
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, backButtonRect.x - panelRect.x - 28f, 24f),
                GuiText.Get("gui.combat.cursor_color.title"),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 36f),
                GuiText.Get("gui.combat.cursor_color.subtitle"),
                _hintStyle);

            CombatCursorColorOption[] options = CombatCursorColorCatalog.GetOptions();
            string selectedId = _combatCursorColorProvider != null
                ? _combatCursorColorProvider()
                : CombatCursorColorCatalog.DisabledId;
            float left = panelRect.x + 14f;
            Rect toggleRect = new Rect(panelRect.x + 14f, panelRect.y + 82f, panelRect.width - 28f, 34f);
            bool isEnabled = CombatCursorColorCatalog.IsEnabled(selectedId);
            GUIStyle toggleStyle = isEnabled ? _enabledButtonStyle : _buttonStyle;
            string toggleKey = isEnabled
                ? "gui.combat.cursor_color.toggle.disable"
                : "gui.combat.cursor_color.toggle.enable";
            if (GUI.Button(
                toggleRect,
                GuiText.Get(toggleKey),
                GetControllerButtonStyle("cursor_color.toggle", toggleStyle)))
            {
                ToggleCombatCursorColor(selectedId, options);
            }

            float top = panelRect.y + 128f;
            float columnWidth = (panelRect.width - 42f) * 0.5f;
            const float rowHeight = 54f;

            for (int index = 0; index < options.Length; index++)
            {
                CombatCursorColorOption option = options[index];
                int row = index / 2;
                int column = index % 2;
                float x = left + column * (columnWidth + ButtonGap);
                float y = top + row * rowHeight;
                Rect optionRect = new Rect(x, y, columnWidth, 44f);
                Rect swatchRect = new Rect(optionRect.x + 8f, optionRect.y + 8f, 28f, 28f);
                Rect buttonRect = new Rect(optionRect.x + 44f, optionRect.y, optionRect.width - 52f, optionRect.height);

                Color previousColor = GUI.color;
                GUI.color = option.Color;
                GUI.DrawTexture(swatchRect, Texture2D.whiteTexture);
                GUI.color = previousColor;

                bool isSelected = string.Equals(option.Id, selectedId, System.StringComparison.OrdinalIgnoreCase);
                GUIStyle style = isSelected ? _enabledButtonStyle : _buttonStyle;
                if (GUI.Button(
                    buttonRect,
                    GuiText.Get(option.DisplayNameKey) + "  " + option.Hex,
                    GetControllerButtonStyle("cursor_color." + option.Id, style)))
                {
                    SetCombatCursorColor(option.Id);
                }
            }
        }

        private void HandleCursorColorPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                CloseCursorColorPage();
                return;
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                _cursorColorPageFocusedControlId = MoveControllerFocus(
                    CursorColorPageFocusEntries,
                    _cursorColorPageFocusedControlId,
                    navigationDirection.Value);
            }

            if (IsPanelConfirmPressed())
            {
                ExecuteCursorColorPageFocusedControl();
            }
        }
    
        private void ExecuteCursorColorPageFocusedControl()
        {
            if (string.Equals(_cursorColorPageFocusedControlId, "cursor_color.back", System.StringComparison.Ordinal))
            {
                CloseCursorColorPage();
                return;
            }

            if (string.Equals(_cursorColorPageFocusedControlId, "cursor_color.toggle", System.StringComparison.Ordinal))
            {
                CombatCursorColorOption[] options = CombatCursorColorCatalog.GetOptions();
                string selectedId = _combatCursorColorProvider != null
                    ? _combatCursorColorProvider()
                    : CombatCursorColorCatalog.DisabledId;
                ToggleCombatCursorColor(selectedId, options);
                return;
            }

            const string prefix = "cursor_color.";
            if (!_cursorColorPageFocusedControlId.StartsWith(prefix, System.StringComparison.Ordinal))
            {
                return;
            }

            string colorId = _cursorColorPageFocusedControlId.Substring(prefix.Length);
            if (_combatCursorColorSetter != null)
            {
                _combatCursorColorSetter(colorId);
            }
        }

        private void CloseCursorColorPage()
        {
            _currentPage = PanelPage.Command;
            _commandMenuCategory = CommandMenuCategory.General;
            _commandPageFocusedControlId = "cmd.general.cursor_color";
            RequestGuiFocusRelease();
        }

        private void SetCombatCursorColor(string colorId)
        {
            if (_combatCursorColorSetter != null)
            {
                _combatCursorColorSetter(colorId);
            }
        }

        private void ToggleCombatCursorColor(string selectedId, CombatCursorColorOption[] options)
        {
            if (_combatCursorColorSetter == null)
            {
                return;
            }

            if (CombatCursorColorCatalog.IsEnabled(selectedId))
            {
                _combatCursorColorSetter(CombatCursorColorCatalog.DisabledId);
                return;
            }

            string enabledColorId = options != null && options.Length > 0
                ? options[0].Id
                : CombatCursorColorCatalog.DisabledId;
            _combatCursorColorSetter(enabledColorId);
        }
    }
}
