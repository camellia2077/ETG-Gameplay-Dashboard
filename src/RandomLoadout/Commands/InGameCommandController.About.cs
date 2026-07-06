// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void OpenAboutPage()
        {
            _currentPage = PanelPage.About;
            _focusInputField = false;
            _focusPickupSearchField = false;
        }

        private void DrawAboutPage(Rect panelRect)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), _buttonStyle))
            {
                _currentPage = PanelPage.Command;
                _focusInputField = true;
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, backButtonRect.x - panelRect.x - 28f, 24f),
                GuiText.Get("gui.about.title"),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.about.subtitle"),
                _hintStyle);

            float left = panelRect.x + 14f;
            float top = panelRect.y + 74f;
            float width = panelRect.width - 28f;
            DrawAboutLine(left, top, width, GuiText.Get("gui.about.project_repo"));
            DrawAboutLine(left, top + 24f, width, "https://github.com/camellia2077/ETG-Gameplay-Dashboard");
            DrawAboutLine(left, top + 56f, width, GuiText.Get("gui.about.release_downloads"));
            DrawAboutLine(left, top + 80f, width, "https://github.com/camellia2077/ETG-Gameplay-Dashboard/releases");

            GUI.Label(
                new Rect(left, top + 116f, width, 20f),
                GetLocalizedFallback(string.Empty, "Project statement", "项目声明"),
                _pickupPrimaryTextStyle);
            GUI.Label(
                new Rect(left, top + 140f, width, 42f),
                GetProjectDisclaimerText(),
                _wrappedHintStyle);

            GUI.Label(
                new Rect(left, top + 188f, width, 20f),
                GuiText.Get("gui.about.dependencies_title"),
                _pickupPrimaryTextStyle);
            GUI.Label(
                new Rect(left, top + 212f, width, 58f),
                GuiText.Get("gui.about.dependencies"),
                _wrappedHintStyle);

            GUI.Label(
                new Rect(left, top + 276f, width, 20f),
                GuiText.Get("gui.about.references_title"),
                _pickupPrimaryTextStyle);
            GUI.Label(
                new Rect(left, top + 300f, width, 42f),
                GuiText.Get("gui.about.references"),
                _wrappedHintStyle);
        }

        private void DrawAboutLine(float left, float top, float width, string text)
        {
            GUI.Label(new Rect(left, top, width, 20f), text, _hintStyle);
        }
    }
}
