using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void OpenAdvancedToolsPage()
        {
            _currentPage = PanelPage.AdvancedTools;
            _focusInputField = false;
            _focusPickupSearchField = false;
            RequestGuiFocusRelease();
        }

        private void DrawAdvancedToolsPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            const float controlHeight = 34f;
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), _buttonStyle))
            {
                OpenSettingsPage();
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, backButtonRect.x - panelRect.x - 28f, 24f),
                GuiText.Get("gui.advanced_tools.title"),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.advanced_tools.subtitle"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 58f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.advanced_tools.hint.input"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 76f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.advanced_tools.hint.grant"),
                _hintStyle);

            GUI.SetNextControlName(InputControlName);
            float textFieldWidth = panelRect.width - 42f - ButtonWidth - 12f;
            Rect textFieldRect = new Rect(panelRect.x + 14f, panelRect.y + 108f, textFieldWidth, controlHeight);
            Rect grantButtonRect = new Rect(textFieldRect.xMax + 12f, textFieldRect.y, ButtonWidth, controlHeight);
            _inputText = GUI.TextField(textFieldRect, _inputText, 256, _textFieldStyle);

            if (_focusInputField)
            {
                GUI.FocusControl(InputControlName);
                _focusInputField = false;
            }

            if (GUI.Button(grantButtonRect, GuiText.Get("gui.command.button.grant"), _buttonStyle))
            {
                Submit(player, logger);
            }
        }

        private void HandleAdvancedToolsPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState("Controller back press is returning from advanced tools to settings.");
                OpenSettingsPage();
                return;
            }

            if (IsPanelConfirmPressed())
            {
                LogGamepadShortcutState("Controller confirm is submitting the advanced tools Grant action.");
                Submit(GetCurrentPlayer(), null);
                return;
            }

            ResetControllerNavigationAxes();
        }
    }
}
