// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private const float PickupInfoConfigRowHeight = 42f;
        private const float PickupInfoConfigRowGap = 8f;

        private void OpenPickupInfoConfigPage()
        {
            RefreshPickupInfoConfigState();
            _currentPage = PanelPage.PickupInfoConfig;
            _pickupInfoConfigFocusedControlId = "pickup_info_config.quality";
            _focusInputField = false;
            _focusPickupSearchField = false;
            RequestGuiFocusRelease();
        }

        private void DrawPickupInfoConfigPage(Rect panelRect)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            if (IsControllerFocusActive("pickup_info_config", "pickup_info_config.back"))
            {
                GUI.Box(
                    new Rect(backButtonRect.x - 2f, backButtonRect.y - 2f, backButtonRect.width + 4f, backButtonRect.height + 4f),
                    GUIContent.none,
                    _enabledButtonStyle);
            }

            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), _buttonStyle))
            {
                _currentPage = PanelPage.Command;
                RequestGuiFocusRelease();
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, backButtonRect.x - panelRect.x - 28f, 24f),
                GetLocalizedFallback("gui.pickup_info_config.title", "Items Info Config", "物品图鉴设置"),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 36f),
                GetLocalizedFallback("gui.pickup_info_config.subtitle", "Choose which sections appear in the nearby items info overlay.", "选择靠近掉落物时显示哪些物品图鉴分段。"),
                _wrappedHintStyle);

            float left = panelRect.x + 14f;
            float rowWidth = panelRect.width - 28f;
            float rowTop = panelRect.y + 92f;
            DrawPickupInfoConfigActionRow(
                new Rect(left, rowTop, rowWidth, PickupInfoConfigRowHeight),
                "pickup_info_config.quality",
                GetLocalizedFallback("gui.pickup_info_config.option.quality", "Quality", "品质"),
                DashboardTheme.PickupInfoQualityLabel,
                _showPickupInfoQuality,
                delegate { TogglePickupInfoQuality(); });
            DrawPickupInfoConfigActionRow(
                new Rect(left, rowTop + (PickupInfoConfigRowHeight + PickupInfoConfigRowGap) * 1f, rowWidth, PickupInfoConfigRowHeight),
                "pickup_info_config.type",
                GetLocalizedFallback("gui.pickup_info_config.option.type", "Type", "类型"),
                DashboardTheme.PickupInfoTypeLabel,
                _showPickupInfoType,
                delegate { TogglePickupInfoType(); });
            DrawPickupInfoConfigActionRow(
                new Rect(left, rowTop + (PickupInfoConfigRowHeight + PickupInfoConfigRowGap) * 2f, rowWidth, PickupInfoConfigRowHeight),
                "pickup_info_config.effects",
                GetLocalizedFallback("gui.pickup_info_config.option.effects", "Effects", "效果"),
                DashboardTheme.PickupInfoEffectsLabel,
                _showPickupInfoEffects,
                delegate { TogglePickupInfoEffects(); });
            DrawPickupInfoConfigActionRow(
                new Rect(left, rowTop + (PickupInfoConfigRowHeight + PickupInfoConfigRowGap) * 3f, rowWidth, PickupInfoConfigRowHeight),
                "pickup_info_config.synergies",
                GetLocalizedFallback("gui.pickup_info_config.option.synergies", "Synergies", "协同"),
                DashboardTheme.PickupInfoSynergiesLabel,
                _showPickupInfoSynergies,
                delegate { TogglePickupInfoSynergies(); });
            DrawPickupInfoConfigActionRow(
                new Rect(left, rowTop + (PickupInfoConfigRowHeight + PickupInfoConfigRowGap) * 4f, rowWidth, PickupInfoConfigRowHeight),
                "pickup_info_config.summary",
                GetLocalizedFallback("gui.pickup_info_config.option.summary", "Summary", "摘要"),
                DashboardTheme.PickupInfoSummaryLabel,
                _showPickupInfoSummary,
                delegate { TogglePickupInfoSummary(); });
            DrawPickupInfoConfigActionRow(
                new Rect(left, rowTop + (PickupInfoConfigRowHeight + PickupInfoConfigRowGap) * 5f, rowWidth, PickupInfoConfigRowHeight),
                "pickup_info_config.notes",
                GetLocalizedFallback("gui.pickup_info_config.option.notes", "Notes", "备注"),
                DashboardTheme.PickupInfoNotesLabel,
                _showPickupInfoNotes,
                delegate { TogglePickupInfoNotes(); });
        }

        private void DrawPickupInfoConfigActionRow(Rect rowRect, string controlId, string label, Color labelColor, bool isEnabled, System.Action onClick)
        {
            const float labelWidth = 160f;
            const float buttonWidth = 132f;
            GUIStyle labelStyle = new GUIStyle(_hintStyle);
            labelStyle.normal.textColor = labelColor;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.fontSize = 20;

            Rect labelRect = new Rect(rowRect.x, rowRect.y + 4f, labelWidth, rowRect.height - 8f);
            if (DashboardTheme.UsePickupInfoLabelOutline)
            {
                GUIStyle labelOutlineStyle = new GUIStyle(labelStyle);
                labelOutlineStyle.normal.textColor = DashboardTheme.PickupInfoLabelOutline;
                for (int verticalOffset = -1; verticalOffset <= 1; verticalOffset++)
                {
                    for (int horizontalOffset = -1; horizontalOffset <= 1; horizontalOffset++)
                    {
                        if (horizontalOffset == 0 && verticalOffset == 0)
                        {
                            continue;
                        }

                        GUI.Label(
                            new Rect(
                                labelRect.x + horizontalOffset,
                                labelRect.y + verticalOffset,
                                labelRect.width,
                                labelRect.height),
                            label,
                            labelOutlineStyle);
                    }
                }
            }

            GUI.Label(labelRect, label, labelStyle);

            GUIStyle buttonStyle = isEnabled ? _enabledButtonStyle : _buttonStyle;
            Rect buttonRect = new Rect(rowRect.xMax - buttonWidth, rowRect.y, buttonWidth, rowRect.height);
            if (DrawControllerButton(
                buttonRect,
                controlId,
                GetOnOffStatusLabel(isEnabled),
                buttonStyle))
            {
                if (onClick != null)
                {
                    onClick();
                }
            }
        }

        private void ExecutePickupInfoConfigPageFocusedControl()
        {
            switch (_pickupInfoConfigFocusedControlId)
            {
                case "pickup_info_config.back":
                    _currentPage = PanelPage.Command;
                    return;
                case "pickup_info_config.quality":
                    TogglePickupInfoQuality();
                    return;
                case "pickup_info_config.type":
                    TogglePickupInfoType();
                    return;
                case "pickup_info_config.effects":
                    TogglePickupInfoEffects();
                    return;
                case "pickup_info_config.synergies":
                    TogglePickupInfoSynergies();
                    return;
                case "pickup_info_config.summary":
                    TogglePickupInfoSummary();
                    return;
                case "pickup_info_config.notes":
                    TogglePickupInfoNotes();
                    return;
                default:
                    return;
            }
        }

        private void RefreshPickupInfoConfigState()
        {
            _showPickupInfoQuality = _pickupInfoQualityEnabledProvider == null || _pickupInfoQualityEnabledProvider();
            _showPickupInfoType = _pickupInfoTypeEnabledProvider == null || _pickupInfoTypeEnabledProvider();
            _showPickupInfoEffects = _pickupInfoEffectsEnabledProvider == null || _pickupInfoEffectsEnabledProvider();
            _showPickupInfoSynergies = _pickupInfoSynergiesEnabledProvider == null || _pickupInfoSynergiesEnabledProvider();
            _showPickupInfoSummary = _pickupInfoSummaryEnabledProvider == null || _pickupInfoSummaryEnabledProvider();
            _showPickupInfoNotes = _pickupInfoNotesEnabledProvider == null || _pickupInfoNotesEnabledProvider();
        }

        private void TogglePickupInfoQuality()
        {
            SetPickupInfoQualityShown(!_showPickupInfoQuality);
            ShowPickupInfoSectionChangedStatus(GetLocalizedFallback("gui.pickup_info_config.option.quality", "Quality", "品质"), _showPickupInfoQuality);
        }

        private void TogglePickupInfoType()
        {
            SetPickupInfoTypeShown(!_showPickupInfoType);
            ShowPickupInfoSectionChangedStatus(GetLocalizedFallback("gui.pickup_info_config.option.type", "Type", "类型"), _showPickupInfoType);
        }

        private void TogglePickupInfoEffects()
        {
            SetPickupInfoEffectsShown(!_showPickupInfoEffects);
            ShowPickupInfoSectionChangedStatus(GetLocalizedFallback("gui.pickup_info_config.option.effects", "Effects", "效果"), _showPickupInfoEffects);
        }

        private void TogglePickupInfoSummary()
        {
            SetPickupInfoSummaryShown(!_showPickupInfoSummary);
            ShowPickupInfoSectionChangedStatus(GetLocalizedFallback("gui.pickup_info_config.option.summary", "Summary", "摘要"), _showPickupInfoSummary);
        }

        private void TogglePickupInfoSynergies()
        {
            SetPickupInfoSynergiesShown(!_showPickupInfoSynergies);
            ShowPickupInfoSectionChangedStatus(GetLocalizedFallback("gui.pickup_info_config.option.synergies", "Synergies", "协同"), _showPickupInfoSynergies);
        }

        private void TogglePickupInfoNotes()
        {
            SetPickupInfoNotesShown(!_showPickupInfoNotes);
            ShowPickupInfoSectionChangedStatus(GetLocalizedFallback("gui.pickup_info_config.option.notes", "Notes", "备注"), _showPickupInfoNotes);
        }

        private void SetPickupInfoQualityShown(bool isEnabled)
        {
            _showPickupInfoQuality = isEnabled;
            if (_pickupInfoQualityEnabledSetter != null)
            {
                _pickupInfoQualityEnabledSetter(isEnabled);
            }
        }

        private void SetPickupInfoTypeShown(bool isEnabled)
        {
            _showPickupInfoType = isEnabled;
            if (_pickupInfoTypeEnabledSetter != null)
            {
                _pickupInfoTypeEnabledSetter(isEnabled);
            }
        }

        private void SetPickupInfoEffectsShown(bool isEnabled)
        {
            _showPickupInfoEffects = isEnabled;
            if (_pickupInfoEffectsEnabledSetter != null)
            {
                _pickupInfoEffectsEnabledSetter(isEnabled);
            }
        }

        private void SetPickupInfoSummaryShown(bool isEnabled)
        {
            _showPickupInfoSummary = isEnabled;
            if (_pickupInfoSummaryEnabledSetter != null)
            {
                _pickupInfoSummaryEnabledSetter(isEnabled);
            }
        }

        private void SetPickupInfoSynergiesShown(bool isEnabled)
        {
            _showPickupInfoSynergies = isEnabled;
            if (_pickupInfoSynergiesEnabledSetter != null)
            {
                _pickupInfoSynergiesEnabledSetter(isEnabled);
            }
        }

        private void SetPickupInfoNotesShown(bool isEnabled)
        {
            _showPickupInfoNotes = isEnabled;
            if (_pickupInfoNotesEnabledSetter != null)
            {
                _pickupInfoNotesEnabledSetter(isEnabled);
            }
        }

        private void ShowPickupInfoSectionChangedStatus(string sectionLabel, bool isEnabled)
        {
            ShowStatus(
                GetLocalizedFormattedFallback(
                    string.Empty,
                    "Pickup Info section {0} set to {1}.",
                    "物品图鉴分段“{0}”已切换为{1}。",
                    sectionLabel,
                    GetOnOffStatusLabel(isEnabled)),
                false);
        }
    }
}
