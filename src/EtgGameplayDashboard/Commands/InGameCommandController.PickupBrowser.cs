// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Diagnostics;
using BepInEx.Logging;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private const string PickupSearchClearControlId = "pickups.search.clear";

        private bool DrawPickupControllerButton(Rect rect, string label, string controlId, GUIStyle style)
        {
            return GUI.Button(rect, label, GetControllerButtonStyle(controlId, style));
        }

        private void OpenPickupPage(ManualLogSource logger)
        {
            OpenPickupPage(PickupBrowserMode.Grant, logger);
        }

        private void OpenPickupAddToStartItemsPage(ManualLogSource logger)
        {
            OpenPickupPage(PickupBrowserMode.AddToStartItems, logger);
        }

        private void OpenPickupAddToRandomPoolPage(ManualLogSource logger)
        {
            OpenPickupPage(PickupBrowserMode.AddToRandomPool, logger);
        }

        private void OpenPickupPage(PickupBrowserMode mode, ManualLogSource logger)
        {
            long startedAtTimestamp = StartPickupBrowserPerformanceTimer();
            _currentPage = PanelPage.Pickups;
            _pickupBrowserMode = mode;
            _isPickupShortcutConfigurationMode = false;
            _focusInputField = false;
            _focusPickupSearchField = false;
            _pickupPageFocusedControlId = "pickups.back";
            RequestGuiFocusRelease();
            RefreshPickupBrowserData();

            LogPickupBrowserPerformance(
                "Pickup browser open completed. Mode=" + _pickupBrowserMode +
                ", CachedEntries=" + _cachedPickupEntries.Length +
                ", DurationMs=" + FormatPickupBrowserMilliseconds(startedAtTimestamp) + ".");

            if (logger != null)
            {
                logger.LogInfo(EtgGameplayDashboardLog.Command("Pickup browser opened. Mode=" + _pickupBrowserMode + "."));
            }
        }

        private void DrawPickupPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            long startedAtTimestamp = StartPickupBrowserPerformanceTimer();
            const float pickupSearchClearButtonWidth = 72f;

            Rect backButtonRect = GetSecondaryPageBackButtonRect(panelRect);
            Rect targetButtonRect = new Rect(backButtonRect.x - ButtonGap - ButtonWidth, panelRect.y + 12f, ButtonWidth, 30f);
            Rect shortcutConfigurationButtonRect = new Rect(
                targetButtonRect.x - ButtonGap - 146f,
                panelRect.y + 12f,
                146f,
                30f);
            GUIStyle targetButtonStyle = _characterSwitchTarget == CharacterSwitchTarget.SecondaryPlayer
                ? _enabledButtonStyle
                : _buttonStyle;
            if (DrawPickupControllerButton(targetButtonRect, GetCharacterSwitchTargetButtonLabel(), "pickups.target", targetButtonStyle))
            {
                ToggleCharacterSwitchTarget(logger);
            }

            if (_pickupBrowserMode == PickupBrowserMode.Grant &&
                !_isPickupShortcutConfigurationMode &&
                GUI.Button(
                    shortcutConfigurationButtonRect,
                    GetPickupShortcutConfigurationButtonLabel(),
                    _isPickupShortcutConfigurationMode ? _enabledButtonStyle : _buttonStyle))
            {
                TogglePickupShortcutConfigurationMode();
            }

            if (DrawPickupControllerButton(backButtonRect, GuiText.Get("gui.common.back"), "pickups.back", _buttonStyle))
            {
                ReturnFromPickupPage();
                return;
            }

            GUI.Label(
                    new Rect(
                        panelRect.x + 14f,
                        panelRect.y + 12f,
                    (_pickupBrowserMode == PickupBrowserMode.Grant && !_isPickupShortcutConfigurationMode
                        ? shortcutConfigurationButtonRect.x
                        : targetButtonRect.x) - panelRect.x - 24f,
                    24f),
                GetPickupBrowserTitle(),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.pickups.hint.search"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 58f, panelRect.width - 28f, 20f),
                GetPickupBrowserActionHint() + " " + GetPickupBrowserTargetHint(),
                _hintStyle);

            GUI.SetNextControlName(PickupSearchControlName);
            Rect searchRect = new Rect(panelRect.x + 14f, panelRect.y + 86f, panelRect.width - 28f - pickupSearchClearButtonWidth - 8f, 32f);
            Rect clearSearchButtonRect = new Rect(searchRect.xMax + 8f, searchRect.y, pickupSearchClearButtonWidth, searchRect.height);
            if (IsControllerFocusActive("pickups", "pickups.search"))
            {
                GUI.Box(
                    new Rect(searchRect.x - 2f, searchRect.y - 2f, searchRect.width + 4f, searchRect.height + 4f),
                    GUIContent.none,
                    _enabledButtonStyle);
            }

            _pickupSearchText = GUI.TextField(searchRect, _pickupSearchText, 128, _textFieldStyle);
            if (IsControllerFocusActive("pickups", PickupSearchClearControlId))
            {
                GUI.Box(
                    new Rect(clearSearchButtonRect.x - 2f, clearSearchButtonRect.y - 2f, clearSearchButtonRect.width + 4f, clearSearchButtonRect.height + 4f),
                    GUIContent.none,
                    _enabledButtonStyle);
            }

            if (GUI.Button(clearSearchButtonRect, GuiText.Get("gui.pickups.button.clear_search"), GetControllerButtonStyle(PickupSearchClearControlId, _buttonStyle)))
            {
                ClearPickupSearchText();
            }

            if (_focusPickupSearchField)
            {
                GUI.FocusControl(PickupSearchControlName);
                _focusPickupSearchField = false;
            }

            float filtersTop = searchRect.yMax + 10f;
            DrawPickupFilterButtons(panelRect.x + 14f, filtersTop);

            float listTop = filtersTop + GetPickupFilterAreaHeight();
            Rect listRect = new Rect(panelRect.x + 14f, listTop, panelRect.width - 28f, panelRect.height - (listTop - panelRect.y) - 14f);
            PlayerController grantPlayer = _pickupBrowserMode == PickupBrowserMode.Grant
                ? GetSelectedCommandTargetPlayer()
                : player;
            DrawPickupResults(listRect, grantPlayer, logger);
            LogSlowPickupBrowserDraw(startedAtTimestamp);
        }

        private void DrawPickupFilterButtons(float left, float top)
        {
            float currentLeft = left;
            currentLeft = DrawPickupFilterButton(new Rect(currentLeft, top, PickupFilterButtonWidth, 28f), PickupBrowserFilter.All, GuiText.Get("gui.pickups.filter.all"));
            currentLeft = DrawPickupFilterButton(new Rect(currentLeft, top, PickupFilterButtonWidth, 28f), PickupBrowserFilter.Gun, GuiText.Get("gui.pickups.filter.gun"));
            currentLeft = DrawPickupFilterButton(new Rect(currentLeft, top, PickupFilterButtonWidth, 28f), PickupBrowserFilter.Passive, GuiText.Get("gui.pickups.filter.passive"));
            DrawPickupFilterButton(new Rect(currentLeft, top, PickupFilterButtonWidth, 28f), PickupBrowserFilter.Active, GuiText.Get("gui.pickups.filter.active"));

            float qualityTop = top + 34f;
            currentLeft = left;
            currentLeft = DrawPickupQualityFilterButton(new Rect(currentLeft, qualityTop, PickupFilterSmallButtonWidth, 28f), PickupQualityFilter.S, "S");
            currentLeft = DrawPickupQualityFilterButton(new Rect(currentLeft, qualityTop, PickupFilterSmallButtonWidth, 28f), PickupQualityFilter.A, "A");
            currentLeft = DrawPickupQualityFilterButton(new Rect(currentLeft, qualityTop, PickupFilterSmallButtonWidth, 28f), PickupQualityFilter.B, "B");
            currentLeft = DrawPickupQualityFilterButton(new Rect(currentLeft, qualityTop, PickupFilterSmallButtonWidth, 28f), PickupQualityFilter.C, "C");
            currentLeft = DrawPickupQualityFilterButton(new Rect(currentLeft, qualityTop, PickupFilterSmallButtonWidth, 28f), PickupQualityFilter.D, "D");
            currentLeft = DrawPickupQualityFilterButton(new Rect(currentLeft, qualityTop, PickupFilterButtonWidth, 28f), PickupQualityFilter.All, GuiText.Get("gui.pickups.filter.quality_all"));
            currentLeft = DrawPickupQualityFilterButton(new Rect(currentLeft, qualityTop, PickupFilterButtonWidth, 28f), PickupQualityFilter.Special, GuiText.Get("gui.pickups.filter.quality_special"));
            DrawPickupQualityFilterButton(new Rect(currentLeft, qualityTop, PickupFilterButtonWidth, 28f), PickupQualityFilter.Excluded, GuiText.Get("gui.pickups.filter.quality_excluded"));

            if (_pickupBrowserFilter == PickupBrowserFilter.Gun)
            {
                DrawPickupGunClassFilterButtons(left, top + 68f);
            }
            else if (_pickupBrowserFilter == PickupBrowserFilter.Passive)
            {
                DrawPickupPassiveSubcategoryFilterButtons(left, top + 68f);
            }
            else if (_pickupBrowserFilter == PickupBrowserFilter.Active)
            {
                DrawPickupActiveCooldownFilterButtons(left, top + 68f);
            }
        }

        private float DrawPickupFilterButton(Rect rect, PickupBrowserFilter filter, string label)
        {
            GUIStyle style =
                _pickupBrowserFilter == filter || IsPickupFocusOnCategoryFilter(filter)
                    ? _pickupFilterActiveButtonStyle
                    : _pickupFilterButtonStyle;
            if (GUI.Button(rect, label, style))
            {
                ApplyPickupBrowserFilter(filter);
            }

            return rect.xMax + ButtonGap;
        }

        private float DrawPickupQualityFilterButton(Rect rect, PickupQualityFilter filter, string label)
        {
            GUIStyle style =
                _pickupQualityFilter == filter || IsPickupFocusOnQualityFilter(filter)
                    ? _pickupFilterActiveButtonStyle
                    : _pickupFilterButtonStyle;
            if (GUI.Button(rect, label, style))
            {
                ApplyPickupQualityFilter(filter);
            }

            return rect.xMax + ButtonGap;
        }

        private void DrawPickupGunClassFilterButtons(float left, float top)
        {
            float currentLeft = left;
            currentLeft = DrawPickupGunClassFilterButton(new Rect(currentLeft, top, PickupFilterButtonWidth, 28f), PickupGunClassFilter.All, GuiText.Get("gui.pickups.filter.gunclass_all"));
            currentLeft = DrawPickupGunClassFilterButton(new Rect(currentLeft, top, PickupFilterGunClassButtonWidth, 28f), PickupGunClassFilter.Pistol, GuiText.Get("gui.pickups.filter.gunclass_pistol"));
            currentLeft = DrawPickupGunClassFilterButton(new Rect(currentLeft, top, PickupFilterGunClassButtonWidth, 28f), PickupGunClassFilter.FullAuto, GuiText.Get("gui.pickups.filter.gunclass_fullauto"));
            currentLeft = DrawPickupGunClassFilterButton(new Rect(currentLeft, top, PickupFilterGunClassButtonWidth, 28f), PickupGunClassFilter.Shotgun, GuiText.Get("gui.pickups.filter.gunclass_shotgun"));
            currentLeft = DrawPickupGunClassFilterButton(new Rect(currentLeft, top, PickupFilterGunClassButtonWidth, 28f), PickupGunClassFilter.Rifle, GuiText.Get("gui.pickups.filter.gunclass_rifle"));
            currentLeft = DrawPickupGunClassFilterButton(new Rect(currentLeft, top, PickupFilterGunClassButtonWidth, 28f), PickupGunClassFilter.Beam, GuiText.Get("gui.pickups.filter.gunclass_beam"));
            DrawPickupGunClassFilterButton(new Rect(currentLeft, top, PickupFilterGunClassButtonWidth, 28f), PickupGunClassFilter.Charge, GuiText.Get("gui.pickups.filter.gunclass_charge"));

            currentLeft = left;
            float secondRowTop = top + 34f;
            currentLeft = DrawPickupGunClassFilterButton(new Rect(currentLeft, secondRowTop, PickupFilterGunClassButtonWidth, 28f), PickupGunClassFilter.Explosive, GuiText.Get("gui.pickups.filter.gunclass_explosive"));
            currentLeft = DrawPickupGunClassFilterButton(new Rect(currentLeft, secondRowTop, PickupFilterGunClassButtonWidth, 28f), PickupGunClassFilter.Elemental, GuiText.Get("gui.pickups.filter.gunclass_elemental"));
            DrawPickupGunClassFilterButton(new Rect(currentLeft, secondRowTop, PickupFilterGunClassButtonWidth, 28f), PickupGunClassFilter.Special, GuiText.Get("gui.pickups.filter.gunclass_special"));
        }

        private float DrawPickupGunClassFilterButton(Rect rect, PickupGunClassFilter filter, string label)
        {
            GUIStyle style =
                _pickupGunClassFilter == filter || IsPickupFocusOnGunClassFilter(filter)
                    ? _pickupFilterActiveButtonStyle
                    : _pickupFilterButtonStyle;
            if (GUI.Button(rect, label, style))
            {
                ApplyPickupGunClassFilter(filter);
            }

            return rect.xMax + ButtonGap;
        }

        private void DrawPickupPassiveSubcategoryFilterButtons(float left, float top)
        {
            float currentLeft = left;
            currentLeft = DrawPickupPassiveSubcategoryFilterButton(new Rect(currentLeft, top, PickupFilterButtonWidth, 28f), PickupPassiveSubcategoryFilter.All, GuiText.Get("gui.pickups.filter.passive_all"));
            DrawPickupPassiveSubcategoryFilterButton(new Rect(currentLeft, top, PickupFilterGunClassButtonWidth, 28f), PickupPassiveSubcategoryFilter.Bullet, GuiText.Get("gui.pickups.filter.passive_bullet"));
        }

        private float DrawPickupPassiveSubcategoryFilterButton(Rect rect, PickupPassiveSubcategoryFilter filter, string label)
        {
            GUIStyle style =
                _pickupPassiveSubcategoryFilter == filter || IsPickupFocusOnPassiveFilter(filter)
                    ? _pickupFilterActiveButtonStyle
                    : _pickupFilterButtonStyle;
            if (GUI.Button(rect, label, style))
            {
                ApplyPickupPassiveSubcategoryFilter(filter);
            }

            return rect.xMax + ButtonGap;
        }

        private void DrawPickupActiveCooldownFilterButtons(float left, float top)
        {
            float currentLeft = left;
            currentLeft = DrawPickupActiveCooldownFilterButton(new Rect(currentLeft, top, PickupFilterButtonWidth, 28f), PickupActiveCooldownFilter.All, GuiText.Get("gui.pickups.filter.activecooldown_all"));
            currentLeft = DrawPickupActiveCooldownFilterButton(new Rect(currentLeft, top, PickupFilterGunClassButtonWidth, 28f), PickupActiveCooldownFilter.Uses, GuiText.Get("gui.pickups.filter.activecooldown_uses"));
            currentLeft = DrawPickupActiveCooldownFilterButton(new Rect(currentLeft, top, PickupFilterGunClassButtonWidth, 28f), PickupActiveCooldownFilter.Damage, GuiText.Get("gui.pickups.filter.activecooldown_damage"));
            currentLeft = DrawPickupActiveCooldownFilterButton(new Rect(currentLeft, top, PickupFilterGunClassButtonWidth, 28f), PickupActiveCooldownFilter.Time, GuiText.Get("gui.pickups.filter.activecooldown_time"));
            DrawPickupActiveCooldownFilterButton(new Rect(currentLeft, top, PickupFilterGunClassButtonWidth, 28f), PickupActiveCooldownFilter.Room, GuiText.Get("gui.pickups.filter.activecooldown_room"));
        }

        private float DrawPickupActiveCooldownFilterButton(Rect rect, PickupActiveCooldownFilter filter, string label)
        {
            GUIStyle style =
                _pickupActiveCooldownFilter == filter || IsPickupFocusOnActiveCooldownFilter(filter)
                    ? _pickupFilterActiveButtonStyle
                    : _pickupFilterButtonStyle;
            if (GUI.Button(rect, label, style))
            {
                ApplyPickupActiveCooldownFilter(filter);
            }

            return rect.xMax + ButtonGap;
        }

        private float GetPickupFilterAreaHeight()
        {
            if (_pickupBrowserFilter == PickupBrowserFilter.Gun)
            {
                return 140f;
            }

            return _pickupBrowserFilter == PickupBrowserFilter.Passive || _pickupBrowserFilter == PickupBrowserFilter.Active ? 106f : 72f;
        }

        private void DrawPickupResults(Rect listRect, PlayerController player, ManualLogSource logger)
        {
            PickupBrowserEntry[] matches = GetFilteredPickupEntries();
            if (matches.Length == 0)
            {
                GUI.Box(listRect, GUIContent.none, _pickupRowStyle);
                GUI.Label(
                    new Rect(listRect.x + 12f, listRect.y + 12f, listRect.width - 24f, listRect.height - 24f),
                    GuiText.Get("gui.pickups.empty"),
                    _wrappedHintStyle);
                return;
            }

            float contentHeight = (matches.Length * PickupBrowserRowHeight) + (Mathf.Max(0, matches.Length - 1) * PickupBrowserRowGap) + 4f;
            Rect viewRect = new Rect(0f, 0f, listRect.width - SharedScrollViewStyles.ViewportScrollbarReserveWidth, contentHeight);
            EnsurePickupBrowserFocusedResultVisible(matches, listRect.height);
            _pickupScrollPosition = BeginCommandScrollView(listRect, _pickupScrollPosition, viewRect);
            float rowStride = PickupBrowserRowHeight + PickupBrowserRowGap;
            int firstVisibleIndex = Mathf.Max(0, Mathf.FloorToInt(_pickupScrollPosition.y / rowStride) - 1);
            int lastVisibleIndex = Mathf.Min(
                matches.Length - 1,
                Mathf.CeilToInt((_pickupScrollPosition.y + listRect.height) / rowStride) + 1);
            for (int i = firstVisibleIndex; i <= lastVisibleIndex; i++)
            {
                float rowTop = 2f + (i * rowStride);
                DrawPickupRow(new Rect(0f, rowTop, viewRect.width, PickupBrowserRowHeight - 4f), matches[i], player, logger);
            }

            GUI.EndScrollView();
        }

        private void DrawPickupRow(Rect rowRect, PickupBrowserEntry entry, PlayerController player, ManualLogSource logger)
        {
            LogPickupNameDiagnostic(entry);
            GUI.Box(rowRect, GUIContent.none, _pickupBrowserRowStyle);

            const float addButtonWidth = 64f;
            bool isAddMode = _pickupBrowserMode == PickupBrowserMode.AddToStartItems || _pickupBrowserMode == PickupBrowserMode.AddToRandomPool;
            float actionButtonsWidth = isAddMode
                ? addButtonWidth
                : _isPickupShortcutConfigurationMode
                    ? PickupShortcutButtonWidth + PickupShortcutClearButtonWidth + ButtonGap
                    : PickupGrantButtonWidth;
            Rect rowButtonRect = new Rect(rowRect.x, rowRect.y, rowRect.width - actionButtonsWidth - ButtonGap, rowRect.height);
            if (!_isPickupShortcutConfigurationMode && GUI.Button(rowButtonRect, GUIContent.none, _pickupRowButtonStyle))
            {
                if (_pickupBrowserMode == PickupBrowserMode.AddToStartItems)
                {
                    ExecuteLoadoutEditorAdd(entry.CatalogEntry, logger);
                }
                else if (_pickupBrowserMode == PickupBrowserMode.AddToRandomPool)
                {
                    ExecuteLoadoutEditorAddToRandomPool(entry.CatalogEntry, logger);
                }
                else
                {
                    ExecutePickupBrowserGrant(entry, player, logger);
                }
            }

            Rect iconRect = new Rect(
                rowRect.x + 8f,
                rowRect.y + ((rowRect.height - PickupBrowserIconHeight) * 0.5f),
                PickupBrowserIconWidth,
                PickupBrowserIconHeight);
            DrawPickupIcon(iconRect, entry);

            float textLeft = iconRect.xMax + 8f;
            float textWidth = rowRect.width - actionButtonsWidth - 32f - PickupBrowserIconWidth - 24f;
            GUI.Label(
                new Rect(textLeft, rowRect.y + 5f, textWidth, 20f),
                entry.DisplayName,
                _pickupPrimaryTextStyle);
            GUI.Label(
                new Rect(textLeft, rowRect.y + 24f, textWidth, 18f),
                entry.MetadataLine,
                _pickupSecondaryTextStyle);

            if (isAddMode)
            {
                Rect addButtonRect = new Rect(rowRect.x + rowRect.width - addButtonWidth - 8f, rowRect.y + 8f, addButtonWidth, rowRect.height - 16f);
                if (DrawPickupControllerButton(addButtonRect, GuiText.Get("gui.pickups.button.add_loadout"), GetPickupRowActionControlId(entry), _buttonStyle))
                {
                    if (_pickupBrowserMode == PickupBrowserMode.AddToRandomPool)
                    {
                        ExecuteLoadoutEditorAddToRandomPool(entry.CatalogEntry, logger);
                    }
                    else
                    {
                        ExecuteLoadoutEditorAdd(entry.CatalogEntry, logger);
                    }
                }

                return;
            }

            if (!_isPickupShortcutConfigurationMode)
            {
                Rect grantButtonRect = new Rect(rowRect.x + rowRect.width - PickupGrantButtonWidth - 8f, rowRect.y + 8f, PickupGrantButtonWidth, rowRect.height - 16f);
                if (DrawPickupControllerButton(grantButtonRect, GuiText.Get("gui.command.button.grant"), GetPickupRowActionControlId(entry), _pickupGrantButtonStyle))
                {
                    ExecutePickupBrowserGrant(entry, player, logger);
                }
            }

            Rect clearShortcutButtonRect = new Rect(
                rowRect.x + rowRect.width - PickupShortcutClearButtonWidth - 8f,
                rowRect.y + 8f,
                PickupShortcutClearButtonWidth,
                rowRect.height - 16f);
            if (_isPickupShortcutConfigurationMode && GUI.Button(clearShortcutButtonRect, GuiText.Get("gui.pickups.button.shortcut_clear"), _buttonStyle))
            {
                ClearPickupShortcut(entry);
            }

            Rect shortcutButtonRect = new Rect(
                clearShortcutButtonRect.x - ButtonGap - PickupShortcutButtonWidth,
                rowRect.y + 8f,
                PickupShortcutButtonWidth,
                rowRect.height - 16f);
            GUIStyle shortcutButtonStyle = string.Equals(_pickupShortcutCaptureTargetId, entry.CatalogEntry.PickupId.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                ? _enabledButtonStyle
                : _buttonStyle;
            if (_isPickupShortcutConfigurationMode && GUI.Button(shortcutButtonRect, GetPickupShortcutButtonLabel(entry), shortcutButtonStyle))
            {
                BeginPickupShortcutCapture(entry);
            }
        }

        private string GetPickupBrowserTitle()
        {
            if (_pickupBrowserMode == PickupBrowserMode.AddToStartItems)
            {
                return GuiText.Get("gui.pickups.title.add_start_items");
            }

            return _pickupBrowserMode == PickupBrowserMode.AddToRandomPool
                ? GuiText.Get("gui.pickups.title.add_random_pool")
                : GuiText.Get("gui.pickups.title");
        }

        private string GetPickupBrowserActionHint()
        {
            if (_isPickupShortcutConfigurationMode)
            {
                return GetLocalizedFallback(
                    "gui.pickups.hint.configure_shortcuts",
                    "Click a pickup key button, then press a keyboard key. Reserved control-panel keys are unavailable.",
                    "点击物品的快捷键按钮，再按下要绑定的键盘按键。控制面板占用的按键不可用。");
            }

            if (_pickupBrowserMode == PickupBrowserMode.AddToStartItems)
            {
                return GuiText.Get("gui.pickups.hint.add_start_items");
            }

            return _pickupBrowserMode == PickupBrowserMode.AddToRandomPool
                ? GuiText.Get("gui.pickups.hint.add_random_pool")
                : GuiText.Get("gui.pickups.hint.grant");
        }

        private void DrawPickupIcon(Rect iconRect, PickupBrowserEntry entry)
        {
            PickupIconData iconData;
            if (TryGetPickupIcon(entry.CatalogEntry.PickupId, out iconData))
            {
                Rect drawRect = GetAspectFitIconRect(iconRect, iconData);
                GUI.DrawTextureWithTexCoords(drawRect, iconData.Texture, iconData.TextureCoords, true);
                return;
            }

            GUI.Box(iconRect, entry.IconFallbackLabel, _pickupIconFallbackStyle);
        }

        private static Rect GetAspectFitIconRect(Rect slotRect, PickupIconData iconData)
        {
            if (iconData.Texture == null || iconData.TextureCoords.width <= 0f || iconData.TextureCoords.height <= 0f)
            {
                return slotRect;
            }

            float textureWidth = iconData.Texture.width * iconData.TextureCoords.width;
            float textureHeight = iconData.Texture.height * iconData.TextureCoords.height;
            if (textureWidth <= 0f || textureHeight <= 0f)
            {
                return slotRect;
            }

            float sourceAspect = textureWidth / textureHeight;
            float slotAspect = slotRect.width / slotRect.height;
            if (sourceAspect > slotAspect)
            {
                float fittedHeight = slotRect.width / sourceAspect;
                return new Rect(
                    slotRect.x,
                    slotRect.y + ((slotRect.height - fittedHeight) * 0.5f),
                    slotRect.width,
                    fittedHeight);
            }

            float fittedWidth = slotRect.height * sourceAspect;
            return new Rect(
                slotRect.x + ((slotRect.width - fittedWidth) * 0.5f),
                slotRect.y,
                fittedWidth,
                slotRect.height);
        }

        private void ExecutePickupBrowserGrant(PickupBrowserEntry entry, PlayerController player, ManualLogSource logger)
        {
            if (_pickupBrowserMode == PickupBrowserMode.Grant)
            {
                player = GetSelectedCommandTargetPlayer();
            }

            GrantCommandExecutionResult executionResult = ExecutePickupBrowserGrantForSelectedTarget(entry, player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);
            _inputText = entry.CommandText;

            if (executionResult.Succeeded)
            {
                if (logger != null)
                {
                    logger.LogInfo(EtgGameplayDashboardLog.Command(executionResult.LogMessage));
                }

                _focusPickupSearchField = true;
            }
            else if (logger != null)
            {
                logger.LogWarning(EtgGameplayDashboardLog.Command(executionResult.LogMessage));
            }
        }

        private void RefreshPickupBrowserData()
        {
            if (_cachedPickupEntries.Length > 0 || _pickupBrowserQueryService == null)
            {
                return;
            }

            long startedAtTimestamp = StartPickupBrowserPerformanceTimer();
            _pickupNameDiagnosticsLogged.Clear();
            _cachedPickupEntries = _pickupBrowserQueryService.BuildEntries();
            _filteredPickupEntriesCache = null;
            LogPickupBrowserPerformance(
                "Pickup browser data refreshed. CachedEntries=" + _cachedPickupEntries.Length +
                ", TotalMs=" + FormatPickupBrowserMilliseconds(startedAtTimestamp) + ".");
            LogPickupBrowserPerformance(
                "Pickup browser language context. CurrentLanguage=" + GuiText.CurrentLanguageCode
                + ", GameLanguage=" + GuiText.GameLanguageCode
                + ", EntryCount=" + _cachedPickupEntries.Length + ".");
        }

        private void LogPickupNameDiagnostic(PickupBrowserEntry entry)
        {
            if (!IsPickupBrowserPerformanceLoggingEnabled() || _performanceLogger == null || entry == null || entry.CatalogEntry == null)
            {
                return;
            }

            int pickupId = entry.CatalogEntry.PickupId;
            if (!_pickupNameDiagnosticsLogged.Add(pickupId))
            {
                return;
            }

            EtgPickupCatalogEntry catalogEntry = entry.CatalogEntry;
            _performanceLogger.LogInfo(
                EtgGameplayDashboardLog.Performance(
                    "PickupBrowserName: PickupId=" + pickupId
                    + ", CurrentLanguage=" + GuiText.CurrentLanguageCode
                    + ", GameLanguage=" + GuiText.GameLanguageCode
                    + ", DisplayName=" + (catalogEntry.DisplayName ?? string.Empty)
                    + ", EnglishDisplayName=" + (catalogEntry.EnglishDisplayName ?? string.Empty)
                    + ", GameDisplayName=" + (catalogEntry.GameDisplayName ?? string.Empty)
                    + ", InternalName=" + (catalogEntry.InternalName ?? string.Empty)
                    + ", ResolvedEntryDisplayName=" + (entry.DisplayName ?? string.Empty) + "."));
        }

        private long StartPickupBrowserPerformanceTimer()
        {
            return IsPickupBrowserPerformanceLoggingEnabled() ? Stopwatch.GetTimestamp() : 0L;
        }

        private void LogSlowPickupBrowserDraw(long startedAtTimestamp)
        {
            if (startedAtTimestamp == 0L)
            {
                return;
            }

            double durationMs = FormatPickupBrowserMilliseconds(startedAtTimestamp);
            if (durationMs >= 20d)
            {
                LogPickupBrowserPerformance("Slow pickup browser draw. DurationMs=" + durationMs + ".");
            }
        }

        private void LogPickupBrowserPerformance(string message)
        {
            if (!IsPickupBrowserPerformanceLoggingEnabled() || _performanceLogger == null)
            {
                return;
            }

            _performanceLogger.LogInfo(EtgGameplayDashboardLog.Performance("PickupBrowser: " + message));
        }

        private bool IsPickupBrowserPerformanceLoggingEnabled()
        {
            return _performanceVerboseLoggingEnabledProvider != null &&
                _performanceVerboseLoggingEnabledProvider();
        }

        private static double FormatPickupBrowserMilliseconds(long startedAtTimestamp)
        {
            if (startedAtTimestamp == 0L)
            {
                return 0d;
            }

            return Math.Round((Stopwatch.GetTimestamp() - startedAtTimestamp) * 1000d / Stopwatch.Frequency, 3);
        }

        private string GetPickupBrowserTargetHint()
        {
            if (_characterSwitchTarget == CharacterSwitchTarget.BothPlayers)
            {
                return GuiText.Get("gui.pickups.hint.both_players");
            }

            return GuiText.Get("gui.pickups.hint.target", GetCharacterSwitchTargetDisplayLabel());
        }

        private GrantCommandExecutionResult ExecutePickupBrowserGrantForSelectedTarget(
            PickupBrowserEntry entry,
            PlayerController fallbackPlayer)
        {
            if (_pickupBrowserMode != PickupBrowserMode.Grant)
            {
                return _commandService.ExecuteCatalogEntry(fallbackPlayer, entry.CatalogEntry);
            }

            if (_characterSwitchTarget != CharacterSwitchTarget.BothPlayers)
            {
                return _commandService.ExecuteCatalogEntry(GetSelectedCommandTargetPlayer(), entry.CatalogEntry);
            }

            GameManager gameManager = GameManager.Instance;
            PlayerController primaryPlayer = (object)gameManager != null ? gameManager.PrimaryPlayer : null;
            PlayerController secondaryPlayer = (object)gameManager != null ? gameManager.SecondaryPlayer : null;
            if ((object)primaryPlayer == null || (object)secondaryPlayer == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.pickups.both_players_required");
            }

            GrantCommandExecutionResult primaryResult = _commandService.ExecuteCatalogEntry(primaryPlayer, entry.CatalogEntry);
            if (!primaryResult.Succeeded)
            {
                return primaryResult;
            }

            GrantCommandExecutionResult secondaryResult = _commandService.ExecuteCatalogEntry(secondaryPlayer, entry.CatalogEntry);
            if (!secondaryResult.Succeeded)
            {
                return secondaryResult;
            }

            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.grant.success.both_players", entry.CatalogEntry.DisplayName),
                GuiText.GetEnglish("result.grant.success.both_players", entry.CatalogEntry.EnglishDisplayName),
                "result.grant.success.both_players");
        }

        private void ResetPickupBrowserState()
        {
            CancelPickupShortcutCapture();
            _isPickupShortcutConfigurationMode = false;
            _cachedPickupEntries = EmptyPickupBrowserEntries;
            _pickupSearchText = string.Empty;
            _pickupBrowserMode = PickupBrowserMode.Grant;
            _pickupBrowserFilter = PickupBrowserFilter.All;
            _pickupQualityFilter = PickupQualityFilter.All;
            _pickupGunClassFilter = PickupGunClassFilter.All;
            _pickupPassiveSubcategoryFilter = PickupPassiveSubcategoryFilter.All;
            _pickupActiveCooldownFilter = PickupActiveCooldownFilter.All;
            _pickupScrollPosition = Vector2.zero;
            _focusPickupSearchField = false;
            _pickupPageFocusedControlId = "pickups.back";
            _pickupIconCache.Clear();
        }

        private void ClearPickupSearchText()
        {
            _pickupSearchText = string.Empty;
            _focusPickupSearchField = true;
            _pickupPageFocusedControlId = "pickups.search";
            _pickupScrollPosition = Vector2.zero;
        }

        private void ReturnFromPickupPage()
        {
            if (_pickupBrowserMode == PickupBrowserMode.AddToStartItems)
            {
                _currentPage = PanelPage.LoadoutEditor;
                _loadoutEditorMode = LoadoutEditorMode.PresetDetail;
                _loadoutEditorFocusedControlId = "loadout.preset_detail.add_item";
                RefreshLoadoutEditorEntries();
            }
            else if (_pickupBrowserMode == PickupBrowserMode.AddToRandomPool)
            {
                _currentPage = PanelPage.LoadoutEditor;
                _loadoutEditorMode = LoadoutEditorMode.RandomPoolDetail;
                _loadoutEditorFocusedControlId = "loadout.random_pool.add_item";
                RefreshLoadoutEditorEntries();
                RefreshLoadoutRandomPoolEntries();
            }
            else
            {
                // The Grant Items page is a child page of the command panel. Returning from
                // it must keep the panel visible and route back to the control panel root.
                _isVisible = true;
                _currentPage = PanelPage.Command;
                _focusInputField = true;
            }

            _focusPickupSearchField = false;
            RequestGuiFocusRelease();
        }



        private PickupBrowserEntry[] GetFilteredPickupEntries()
        {
            if (_filteredPickupEntriesCache != null &&
                string.Equals(_filteredPickupEntriesCacheSearch, _pickupSearchText, StringComparison.Ordinal) &&
                _filteredPickupEntriesCacheFilter == _pickupBrowserFilter &&
                _filteredPickupEntriesCacheQualityFilter == _pickupQualityFilter &&
                _filteredPickupEntriesCacheGunClassFilter == _pickupGunClassFilter &&
                _filteredPickupEntriesCachePassiveFilter == _pickupPassiveSubcategoryFilter &&
                _filteredPickupEntriesCacheActiveCooldownFilter == _pickupActiveCooldownFilter)
            {
                return _filteredPickupEntriesCache;
            }

            _filteredPickupEntriesCache = PickupBrowserQueryService.Filter(
                _cachedPickupEntries,
                _pickupSearchText,
                _pickupBrowserFilter,
                _pickupQualityFilter,
                _pickupGunClassFilter,
                _pickupPassiveSubcategoryFilter,
                _pickupActiveCooldownFilter);
            _filteredPickupEntriesCacheSearch = _pickupSearchText;
            _filteredPickupEntriesCacheFilter = _pickupBrowserFilter;
            _filteredPickupEntriesCacheQualityFilter = _pickupQualityFilter;
            _filteredPickupEntriesCacheGunClassFilter = _pickupGunClassFilter;
            _filteredPickupEntriesCachePassiveFilter = _pickupPassiveSubcategoryFilter;
            _filteredPickupEntriesCacheActiveCooldownFilter = _pickupActiveCooldownFilter;
            return _filteredPickupEntriesCache;
        }

    }
}
