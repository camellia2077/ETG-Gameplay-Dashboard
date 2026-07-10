// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void OpenSettingsPage()
        {
            _currentPage = PanelPage.Settings;
            _settingsPageFocusedControlId = "settings.toggle_key";
            _focusInputField = false;
            _focusPickupSearchField = false;
        }

        private void DrawSettingsPage(Rect panelRect, ManualLogSource logger)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), GetControllerButtonStyle("settings.back", _buttonStyle)))
            {
                _currentPage = PanelPage.Command;
                _focusInputField = false;
                _focusPickupSearchField = false;
                RequestGuiFocusRelease();
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, backButtonRect.x - panelRect.x - 28f, 24f),
                GuiText.Get("gui.settings.title"),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.settings.subtitle"),
                _hintStyle);

            float left = panelRect.x + 14f;
            float rowWidth = panelRect.width - 28f;
            float rowTop = panelRect.y + 78f;
            DrawSettingsSectionLabel(left, rowTop, rowWidth, GuiText.Get("gui.settings.section.panel"));
            DrawSettingsActionRow(
                new Rect(left, rowTop + 28f, rowWidth, 34f),
                "settings.toggle_key",
                GuiText.Get("gui.settings.setting.toggle_key"),
                GetConfiguredToggleKeyName(),
                delegate { ExecuteCycleCommandPanelKey(logger); });
            DrawSettingsActionRow(
                new Rect(left, rowTop + 68f, rowWidth, 34f),
                "settings.controller_help",
                GuiText.Get("gui.settings.setting.controller_help"),
                GuiText.Get("gui.settings.value.controller_help"),
                GuiText.Get("gui.settings.button.view_details"),
                delegate { OpenControllerHelpPage(); });
            DrawSettingsActionRow(
                new Rect(left, rowTop + 108f, rowWidth, 34f),
                "settings.keyboard_help",
                GuiText.Get("gui.settings.setting.keyboard_help"),
                GuiText.Get("gui.settings.value.keyboard_help"),
                GuiText.Get("gui.settings.button.view_details"),
                delegate { OpenKeyboardHelpPage(); });
            DrawSettingsActionRow(
                new Rect(left, rowTop + 148f, rowWidth, 34f),
                "settings.advanced_tools",
                GuiText.Get("gui.settings.setting.advanced_tools"),
                GuiText.Get("gui.settings.value.advanced_tools"),
                GuiText.Get("gui.settings.button.view_details"),
                delegate { OpenAdvancedToolsPage(); });

            rowTop += 214f;
            DrawSettingsSectionLabel(left, rowTop, rowWidth, GuiText.Get("gui.settings.section.display"));
            DrawSettingsActionRow(
                new Rect(left, rowTop + 28f, rowWidth, 34f),
                "settings.ui_scale",
                GetLocalizedFallback("gui.settings.setting.ui_scale", "UI Size", "界面大小"),
                GetUiScalePresetDisplayName(GetConfiguredUiScalePreset()),
                delegate { ExecuteCycleUiScalePreset(logger); });
            DrawSettingsActionRow(
                new Rect(left, rowTop + 68f, rowWidth, 34f),
                "settings.language",
                GuiText.Get("gui.settings.setting.language"),
                GetLanguageDisplayName(GetConfiguredLanguage()),
                delegate { ExecuteToggleLanguage(logger); });
            rowTop += 134f;
            DrawSettingsSectionLabel(left, rowTop, rowWidth, GuiText.Get("gui.settings.section.experimental"));
            DrawSettingsActionRow(
                new Rect(left, rowTop + 28f, rowWidth, 34f),
                "settings.experimental_mode",
                GuiText.Get("gui.settings.setting.experimental_mode"),
                GetOnOffStatusLabel(IsExperimentalModeEnabled()),
                GetExperimentalModeButtonLabel(),
                delegate { ExecuteExperimentalModeToggle(logger); });
            GUI.Label(
                new Rect(left, rowTop + 74f, rowWidth, 20f),
                GuiText.Get("gui.settings.version", Plugin.VERSION),
                _settingsInfoTextStyle);
            GUI.Label(
                new Rect(left, rowTop + 98f, rowWidth, 20f),
                GuiText.Get("gui.settings.author"),
                _settingsInfoTextStyle);
            GUI.Label(
                new Rect(left, rowTop + 122f, rowWidth, 20f),
                GuiText.Get("gui.settings.author_github"),
                _settingsInfoTextStyle);
            GUI.Label(
                new Rect(left, rowTop + 146f, rowWidth, 20f),
                GuiText.Get("gui.settings.repo"),
                _settingsInfoTextStyle);
            GUI.Label(
                new Rect(left, rowTop + 170f, rowWidth, 20f),
                GuiText.Get("gui.settings.releases"),
                _settingsInfoTextStyle);
            GUI.Label(
                new Rect(left, rowTop + 194f, rowWidth, 72f),
                GetProjectDisclaimerText(),
                _settingsInfoTextStyle);
        }

        private static string GetProjectDisclaimerText()
        {
            return GetLocalizedFallback(
                string.Empty,
                "This project is open source, free, and ad-free. Please download from GitHub, read the update notes for each release, and avoid third-party repackaged builds that may add ads or tampering.",
                "本项目开源、免费、无广告。推荐从 GitHub 下载，并查看每次 Release 的更新说明，避免使用可能被加入广告或篡改内容的第三方打包版本。");
        }

        private void DrawSettingsSectionLabel(float left, float top, float width, string text)
        {
            GUI.Label(new Rect(left, top, width, 20f), text, _pickupPrimaryTextStyle);
        }

        private void DrawSettingsActionRow(Rect rowRect, string controlId, string label, string value, System.Action onClick)
        {
            DrawSettingsActionRow(rowRect, controlId, label, value, GuiText.Get("gui.settings.button.change"), onClick);
        }

        private void DrawSettingsActionRow(Rect rowRect, string controlId, string label, string value, string buttonLabel, System.Action onClick)
        {
            const float labelWidth = 160f;
            const float buttonWidth = 132f;
            GUI.Label(new Rect(rowRect.x, rowRect.y + 7f, labelWidth, 20f), label, _hintStyle);
            GUI.Label(
                new Rect(rowRect.x + labelWidth + ButtonGap, rowRect.y + 7f, rowRect.width - labelWidth - buttonWidth - (ButtonGap * 2f), 20f),
                value,
                _hintStyle);

            if (string.IsNullOrEmpty(buttonLabel) || onClick == null)
            {
                return;
            }

            GUIStyle buttonStyle = IsEnabledStatusLabel(value) ? _enabledButtonStyle : _buttonStyle;
            if (GUI.Button(new Rect(rowRect.xMax - buttonWidth, rowRect.y, buttonWidth, rowRect.height), buttonLabel, GetControllerButtonStyle(controlId, buttonStyle)))
            {
                onClick();
            }
        }

        private string GetExperimentalModeButtonLabel()
        {
            return IsExperimentalModeEnabled()
                ? GuiText.Get("gui.settings.button.disable")
                : GuiText.Get("gui.settings.button.enable");
        }

        private void ExecuteExperimentalModeToggle(ManualLogSource logger)
        {
            if (IsExperimentalModeEnabled())
            {
                SetExperimentalModeEnabled(false, logger);
                return;
            }

            _showExperimentalModeConfirmDialog = true;
            RequestGuiFocusRelease();
        }

        private void SetExperimentalModeEnabled(bool isEnabled, ManualLogSource logger)
        {
            if (_experimentalModeSetter == null)
            {
                return;
            }

            _experimentalModeSetter(isEnabled);
            _showExperimentalModeConfirmDialog = false;
            ShowStatus(GetExperimentalModeChangedMessage(isEnabled), false);
            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command(GetEnglishExperimentalModeChangedMessage(isEnabled)));
            }
        }

        private void DrawExperimentalModeConfirmDialog(Rect panelRect, ManualLogSource logger)
        {
            if (!_showExperimentalModeConfirmDialog)
            {
                return;
            }

            GUI.Box(panelRect, GUIContent.none, _modalOverlayStyle);

            const float dialogWidth = 420f;
            const float dialogHeight = 204f;
            Rect dialogRect = new Rect(
                panelRect.x + (panelRect.width - dialogWidth) * 0.5f,
                panelRect.y + 54f,
                dialogWidth,
                dialogHeight);
            GUI.Box(dialogRect, GUIContent.none, _modalPanelStyle);

            GUI.Label(
                new Rect(dialogRect.x + 14f, dialogRect.y + 14f, dialogRect.width - 28f, 24f),
                GuiText.Get("gui.settings.experimental.confirm.title"),
                _titleStyle);
            GUI.Label(
                new Rect(dialogRect.x + 14f, dialogRect.y + 48f, dialogRect.width - 28f, 72f),
                GuiText.Get("gui.settings.experimental.confirm.body"),
                _modalBodyStyle);

            Rect cancelButtonRect = new Rect(dialogRect.x + dialogRect.width - 212f, dialogRect.y + dialogRect.height - 48f, 96f, 30f);
            Rect confirmButtonRect = new Rect(dialogRect.x + dialogRect.width - 108f, dialogRect.y + dialogRect.height - 48f, 96f, 30f);
            if (GUI.Button(cancelButtonRect, GuiText.Get("gui.settings.experimental.confirm.cancel"), _buttonStyle))
            {
                _showExperimentalModeConfirmDialog = false;
                return;
            }

            if (GUI.Button(confirmButtonRect, GuiText.Get("gui.settings.experimental.confirm.confirm"), _enabledButtonStyle))
            {
                SetExperimentalModeEnabled(true, logger);
            }
        }

        private static bool IsEnabledStatusLabel(string value)
        {
            return string.Equals(value, GetLocalizedFallback("gui.command.status.on", "ON", "开"), System.StringComparison.OrdinalIgnoreCase);
        }

        private static string GetExperimentalModeChangedMessage(bool isEnabled)
        {
            string statusLabel = isEnabled
                ? GuiText.Get("gui.settings.experimental.status.enabled")
                : GuiText.Get("gui.settings.experimental.status.disabled");
            return GuiText.Get("result.experimental_mode.changed", statusLabel);
        }

        private static string GetEnglishExperimentalModeChangedMessage(bool isEnabled)
        {
            string statusLabel = isEnabled
                ? GuiText.GetEnglish("gui.settings.experimental.status.enabled")
                : GuiText.GetEnglish("gui.settings.experimental.status.disabled");
            return GuiText.GetEnglish("result.experimental_mode.changed", statusLabel);
        }

        private void ExecuteCycleCommandPanelKey(ManualLogSource logger)
        {
            if (_toggleKeySetter == null)
            {
                return;
            }

            string nextKeyName = GetNextCommandPanelKeyName(GetConfiguredToggleKeyName());
            _toggleKeySetter(nextKeyName);
            ShowStatus(GuiText.Get("result.command_panel_key.changed", nextKeyName), false);
            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command(GuiText.GetEnglish("result.command_panel_key.changed", nextKeyName)));
            }
        }

        private string GetConfiguredToggleKeyName()
        {
            return _toggleKeyNameProvider != null ? _toggleKeyNameProvider() : "F7";
        }

        private void ExecuteCycleUiScalePreset(ManualLogSource logger)
        {
            if (_uiScalePresetSetter == null)
            {
                return;
            }

            string nextPreset = GetNextUiScalePreset(GetConfiguredUiScalePreset());
            _uiScalePresetSetter(nextPreset);
            ShowStatus(GetUiScaleChangedMessage(nextPreset), false);
            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command(GetEnglishUiScaleChangedMessage(nextPreset)));
            }
        }

        private string GetConfiguredUiScalePreset()
        {
            return _uiScalePresetProvider != null ? UiScalePresetCatalog.Normalize(_uiScalePresetProvider()) : UiScalePresetCatalog.DefaultPreset;
        }

        private static string GetNextUiScalePreset(string currentPreset)
        {
            return UiScalePresetCatalog.GetNext(currentPreset);
        }

        private static string GetUiScalePresetDisplayName(string presetName)
        {
            return UiScalePresetCatalog.GetDisplayName(presetName);
        }

        private static string GetEnglishUiScalePresetDisplayName(string presetName)
        {
            return UiScalePresetCatalog.GetEnglishDisplayName(presetName);
        }

        private static string GetUiScaleChangedMessage(string presetName)
        {
            string displayName = GetUiScalePresetDisplayName(presetName);
            string template = GuiText.Get("result.ui_scale.changed", displayName);
            if (string.Equals(template, "result.ui_scale.changed", System.StringComparison.Ordinal))
            {
                return string.Equals(GuiText.CurrentLanguageCode, "zh-CN", System.StringComparison.OrdinalIgnoreCase)
                    ? "控制面板界面大小已切换为 " + displayName + "。"
                    : "Command panel UI size set to " + displayName + ".";
            }

            return template;
        }

        private static string GetEnglishUiScaleChangedMessage(string presetName)
        {
            string displayName = GetEnglishUiScalePresetDisplayName(presetName);
            string template = GuiText.GetEnglish("result.ui_scale.changed", displayName);
            if (string.Equals(template, "result.ui_scale.changed", System.StringComparison.Ordinal))
            {
                return "Command panel UI size set to " + displayName + ".";
            }

            return template;
        }

        private static string GetNextCommandPanelKeyName(string currentKeyName)
        {
            for (int i = 0; i < CommandPanelKeyOptions.Length; i++)
            {
                if (string.Equals(CommandPanelKeyOptions[i], currentKeyName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return CommandPanelKeyOptions[(i + 1) % CommandPanelKeyOptions.Length];
                }
            }

            return CommandPanelKeyOptions[0];
        }

        private ControllerFocusEntry[] GetSettingsPageFocusEntries()
        {
            return SettingsPageFocusEntries;
        }

        private void ExecuteSettingsPageFocusedControl()
        {
            switch (_settingsPageFocusedControlId)
            {
                case "settings.back":
                    _currentPage = PanelPage.Command;
                    return;
                case "settings.toggle_key":
                    ExecuteCycleCommandPanelKey(null);
                    return;
                case "settings.controller_help":
                    OpenControllerHelpPage();
                    return;
                case "settings.keyboard_help":
                    OpenKeyboardHelpPage();
                    return;
                case "settings.advanced_tools":
                    OpenAdvancedToolsPage();
                    return;
                case "settings.ui_scale":
                    ExecuteCycleUiScalePreset(null);
                    return;
                case "settings.language":
                    ExecuteToggleLanguage(null);
                    return;
                case "settings.experimental_mode":
                    ExecuteExperimentalModeToggle(null);
                    return;
                default:
                    return;
            }
        }
    }
}
