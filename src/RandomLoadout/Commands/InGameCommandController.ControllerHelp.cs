// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void OpenControllerHelpPage()
        {
            _currentPage = PanelPage.ControllerHelp;
            _focusInputField = false;
            _focusPickupSearchField = false;
            RequestGuiFocusRelease();
        }

        private void OpenKeyboardHelpPage()
        {
            _currentPage = PanelPage.KeyboardHelp;
            _focusInputField = false;
            _focusPickupSearchField = false;
            RequestGuiFocusRelease();
        }

        private void DrawControllerHelpPage(Rect panelRect)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), _buttonStyle))
            {
                OpenSettingsPage();
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, backButtonRect.x - panelRect.x - 28f, 24f),
                GuiText.Get("gui.controller_help.title"),
                _controllerHelpTitleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.controller_help.subtitle"),
                _controllerHelpTextStyle);

            float left = panelRect.x + 14f;
            float top = panelRect.y + 78f;
            float width = panelRect.width - 28f;
            DrawControllerHelpLine(left, top, width, GuiText.Get("gui.controller_help.open"));
            DrawControllerHelpLine(left, top + 24f, width, GuiText.Get("gui.controller_help.confirm"));
            DrawControllerHelpLine(left, top + 48f, width, GuiText.Get("gui.controller_help.back"));
            DrawControllerHelpLine(left, top + 72f, width, GuiText.Get("gui.controller_help.move"));
            DrawControllerHelpLine(left, top + 96f, width, GuiText.Get("gui.controller_help.category"));
        }

        private void DrawControllerHelpLine(float left, float top, float width, string text)
        {
            GUI.Label(new Rect(left, top, width, 20f), text, _controllerHelpTextStyle);
        }

        private void DrawKeyboardHelpPage(Rect panelRect)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), _buttonStyle))
            {
                OpenSettingsPage();
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, backButtonRect.x - panelRect.x - 28f, 24f),
                GuiText.Get("gui.keyboard_help.title"),
                _controllerHelpTitleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.keyboard_help.subtitle"),
                _controllerHelpTextStyle);

            float left = panelRect.x + 14f;
            float top = panelRect.y + 78f;
            float width = panelRect.width - 28f;
            DrawControllerHelpLine(left, top, width, GuiText.Get("gui.keyboard_help.open"));
            DrawControllerHelpLine(left, top + 24f, width, GuiText.Get("gui.keyboard_help.move"));
            DrawControllerHelpLine(left, top + 48f, width, GuiText.Get("gui.keyboard_help.confirm"));
            DrawControllerHelpLine(left, top + 72f, width, GuiText.Get("gui.keyboard_help.back"));
            DrawControllerHelpLine(left, top + 96f, width, GuiText.Get("gui.keyboard_help.command_submit"));
            DrawControllerHelpLine(left, top + 120f, width, GuiText.Get("gui.keyboard_help.mouse"));
        }
    }
}
