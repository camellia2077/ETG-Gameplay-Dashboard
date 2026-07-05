using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private static readonly Color PickupInfoConfigQualityColor = new Color(0.99f, 0.86f, 0.38f, 1f);
        private static readonly Color PickupInfoConfigTypeColor = new Color(0.67f, 0.84f, 0.98f, 1f);
        private static readonly Color PickupInfoConfigEffectsColor = new Color(0.62f, 0.92f, 0.65f, 1f);
        private static readonly Color PickupInfoConfigSynergiesColor = new Color(0.52f, 0.92f, 0.88f, 1f);
        private static readonly Color PickupInfoConfigSummaryColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        private static readonly Color PickupInfoConfigNotesColor = new Color(0.79f, 0.85f, 0.95f, 1f);
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
                GetLocalizedFallback("gui.pickup_info_config.title", "Pickup Info Config", "物品图鉴设置"),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 36f),
                GetLocalizedFallback("gui.pickup_info_config.subtitle", "Choose which sections appear in the nearby pickup info overlay.", "选择靠近掉落物时显示哪些物品图鉴分段。"),
                _wrappedHintStyle);

            float left = panelRect.x + 14f;
            float rowWidth = panelRect.width - 28f;
            float rowTop = panelRect.y + 92f;
            DrawPickupInfoConfigActionRow(
                new Rect(left, rowTop, rowWidth, PickupInfoConfigRowHeight),
                "pickup_info_config.quality",
                GetLocalizedFallback("gui.pickup_info_config.option.quality", "Quality", "品质"),
                PickupInfoConfigQualityColor,
                _showPickupInfoQuality,
                delegate { TogglePickupInfoQuality(); });
            DrawPickupInfoConfigActionRow(
                new Rect(left, rowTop + (PickupInfoConfigRowHeight + PickupInfoConfigRowGap) * 1f, rowWidth, PickupInfoConfigRowHeight),
                "pickup_info_config.type",
                GetLocalizedFallback("gui.pickup_info_config.option.type", "Type", "类型"),
                PickupInfoConfigTypeColor,
                _showPickupInfoType,
                delegate { TogglePickupInfoType(); });
            DrawPickupInfoConfigActionRow(
                new Rect(left, rowTop + (PickupInfoConfigRowHeight + PickupInfoConfigRowGap) * 2f, rowWidth, PickupInfoConfigRowHeight),
                "pickup_info_config.effects",
                GetLocalizedFallback("gui.pickup_info_config.option.effects", "Effects", "效果"),
                PickupInfoConfigEffectsColor,
                _showPickupInfoEffects,
                delegate { TogglePickupInfoEffects(); });
            DrawPickupInfoConfigActionRow(
                new Rect(left, rowTop + (PickupInfoConfigRowHeight + PickupInfoConfigRowGap) * 3f, rowWidth, PickupInfoConfigRowHeight),
                "pickup_info_config.synergies",
                GetLocalizedFallback("gui.pickup_info_config.option.synergies", "Synergies", "协同"),
                PickupInfoConfigSynergiesColor,
                _showPickupInfoSynergies,
                delegate { TogglePickupInfoSynergies(); });
            DrawPickupInfoConfigActionRow(
                new Rect(left, rowTop + (PickupInfoConfigRowHeight + PickupInfoConfigRowGap) * 4f, rowWidth, PickupInfoConfigRowHeight),
                "pickup_info_config.summary",
                GetLocalizedFallback("gui.pickup_info_config.option.summary", "Summary", "摘要"),
                PickupInfoConfigSummaryColor,
                _showPickupInfoSummary,
                delegate { TogglePickupInfoSummary(); });
            DrawPickupInfoConfigActionRow(
                new Rect(left, rowTop + (PickupInfoConfigRowHeight + PickupInfoConfigRowGap) * 5f, rowWidth, PickupInfoConfigRowHeight),
                "pickup_info_config.notes",
                GetLocalizedFallback("gui.pickup_info_config.option.notes", "Notes", "备注"),
                PickupInfoConfigNotesColor,
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

            GUI.Label(new Rect(rowRect.x, rowRect.y + 4f, labelWidth, rowRect.height - 8f), label, labelStyle);

            GUIStyle buttonStyle = isEnabled ? _enabledButtonStyle : _buttonStyle;
            Rect buttonRect = new Rect(rowRect.xMax - buttonWidth, rowRect.y, buttonWidth, rowRect.height);
            if (IsControllerFocusActive("pickup_info_config", controlId))
            {
                GUI.Box(
                    new Rect(buttonRect.x - 2f, buttonRect.y - 2f, buttonRect.width + 4f, buttonRect.height + 4f),
                    GUIContent.none,
                    _enabledButtonStyle);
            }

            if (GUI.Button(
                buttonRect,
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
