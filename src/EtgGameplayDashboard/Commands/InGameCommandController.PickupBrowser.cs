// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using BepInEx.Logging;
using EtgGameplayDashboard.Core;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private const string PickupSearchClearControlId = "pickups.search.clear";

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

            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            Rect targetButtonRect = new Rect(backButtonRect.x - ButtonGap - ButtonWidth, panelRect.y + 12f, ButtonWidth, 30f);
            GUIStyle targetButtonStyle = _characterSwitchTarget == CharacterSwitchTarget.SecondaryPlayer
                ? _enabledButtonStyle
                : _buttonStyle;
            if (GUI.Button(targetButtonRect, GetCharacterSwitchTargetButtonLabel(), GetControllerButtonStyle("pickups.target", targetButtonStyle)))
            {
                ToggleCharacterSwitchTarget(logger);
            }

            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), GetControllerButtonStyle("pickups.back", _buttonStyle)))
            {
                ClosePickupPage();
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, targetButtonRect.x - panelRect.x - 24f, 24f),
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
            float actionButtonsWidth = isAddMode ? addButtonWidth : PickupGrantButtonWidth;
            Rect rowButtonRect = new Rect(rowRect.x, rowRect.y, rowRect.width - actionButtonsWidth - ButtonGap, rowRect.height);
            if (GUI.Button(rowButtonRect, GUIContent.none, _pickupRowButtonStyle))
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
                if (GUI.Button(addButtonRect, GuiText.Get("gui.pickups.button.add_loadout"), GetControllerButtonStyle(GetPickupRowActionControlId(entry), _buttonStyle)))
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

            Rect grantButtonRect = new Rect(rowRect.x + rowRect.width - PickupGrantButtonWidth - 8f, rowRect.y + 8f, PickupGrantButtonWidth, rowRect.height - 16f);
            if (GUI.Button(grantButtonRect, GuiText.Get("gui.command.button.grant"), GetControllerButtonStyle(GetPickupRowActionControlId(entry), _pickupGrantButtonStyle)))
            {
                ExecutePickupBrowserGrant(entry, player, logger);
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
            if (_cachedPickupEntries.Length > 0 || _pickupCatalogProvider == null)
            {
                return;
            }

            long startedAtTimestamp = StartPickupBrowserPerformanceTimer();
            _pickupNameDiagnosticsLogged.Clear();
            EtgPickupCatalogEntry[] catalogEntries = _pickupCatalogProvider() ?? new EtgPickupCatalogEntry[0];
            double catalogDurationMs = FormatPickupBrowserMilliseconds(startedAtTimestamp);
            PickupAliasRegistry aliasRegistry = _aliasRegistryProvider != null ? _aliasRegistryProvider() : PickupAliasRegistry.Empty;
            long aliasStartedAtTimestamp = StartPickupBrowserPerformanceTimer();
            Dictionary<int, List<string>> aliasesByPickupId = BuildAliasesByPickupId(aliasRegistry);
            double aliasDurationMs = FormatPickupBrowserMilliseconds(aliasStartedAtTimestamp);
            List<PickupBrowserEntry> browserEntries = new List<PickupBrowserEntry>(catalogEntries.Length);
            for (int index = 0; index < catalogEntries.Length; index++)
            {
                EtgPickupCatalogEntry entry = catalogEntries[index];
                if (entry == null)
                {
                    continue;
                }

                List<string> aliases;
                aliasesByPickupId.TryGetValue(entry.PickupId, out aliases);
                browserEntries.Add(new PickupBrowserEntry(entry, aliases, _pickupGameplayNameProvider));
            }

            browserEntries.Sort(ComparePickupBrowserEntries);
            _cachedPickupEntries = browserEntries.ToArray();
            _filteredPickupEntriesCache = null;
            LogPickupBrowserPerformance(
                "Pickup browser data refreshed. CatalogEntries=" + catalogEntries.Length +
                ", AliasEntries=" + aliasesByPickupId.Count +
                ", CachedEntries=" + _cachedPickupEntries.Length +
                ", CatalogProviderMs=" + catalogDurationMs +
                ", AliasBuildMs=" + aliasDurationMs +
                ", TotalMs=" + FormatPickupBrowserMilliseconds(startedAtTimestamp) + ".");
            LogPickupBrowserPerformance(
                "Pickup browser language context. CurrentLanguage=" + GuiText.CurrentLanguageCode
                + ", GameLanguage=" + GuiText.GameLanguageCode
                + ", EntryCount=" + catalogEntries.Length + ".");
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

        private static int ComparePickupBrowserEntries(PickupBrowserEntry left, PickupBrowserEntry right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int categoryComparison = left.CatalogEntry.Category.CompareTo(right.CatalogEntry.Category);
            if (categoryComparison != 0)
            {
                return categoryComparison;
            }

            int displayNameComparison = string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
            if (displayNameComparison != 0)
            {
                return displayNameComparison;
            }

            return left.CatalogEntry.PickupId.CompareTo(right.CatalogEntry.PickupId);
        }

        private void ResetPickupBrowserState()
        {
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

        private void ClosePickupPage()
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
                _currentPage = PanelPage.Command;
                _focusInputField = true;
            }

            _focusPickupSearchField = false;
            RequestGuiFocusRelease();
        }

        private ControllerFocusEntry[] GetPickupPageFocusEntries()
        {
            PickupBrowserEntry[] matches = GetFilteredPickupEntries();
            int extraFilterEntryCount = 8;
            if (_pickupBrowserFilter == PickupBrowserFilter.Gun)
            {
                extraFilterEntryCount += 10;
            }
            else if (_pickupBrowserFilter == PickupBrowserFilter.Passive)
            {
                extraFilterEntryCount += 2;
            }
            else if (_pickupBrowserFilter == PickupBrowserFilter.Active)
            {
                extraFilterEntryCount += 5;
            }

            ControllerFocusEntry[] entries = new ControllerFocusEntry[12 + extraFilterEntryCount + matches.Length];
            entries[0] = new ControllerFocusEntry("pickups.back", 0, 0);
            entries[1] = new ControllerFocusEntry("pickups.target", 0, 1);
            entries[2] = new ControllerFocusEntry("pickups.search", 1, 0);
            entries[3] = new ControllerFocusEntry(PickupSearchClearControlId, 1, 1);
            entries[4] = new ControllerFocusEntry(GetPickupCategoryFilterControlId(PickupBrowserFilter.All), 2, 0);
            entries[5] = new ControllerFocusEntry(GetPickupCategoryFilterControlId(PickupBrowserFilter.Gun), 2, 1);
            entries[6] = new ControllerFocusEntry(GetPickupCategoryFilterControlId(PickupBrowserFilter.Passive), 2, 2);
            entries[7] = new ControllerFocusEntry(GetPickupCategoryFilterControlId(PickupBrowserFilter.Active), 2, 3);
            entries[8] = new ControllerFocusEntry(GetPickupQualityFilterControlId(PickupQualityFilter.S), 3, 0);
            entries[9] = new ControllerFocusEntry(GetPickupQualityFilterControlId(PickupQualityFilter.A), 3, 1);
            entries[10] = new ControllerFocusEntry(GetPickupQualityFilterControlId(PickupQualityFilter.B), 3, 2);
            entries[11] = new ControllerFocusEntry(GetPickupQualityFilterControlId(PickupQualityFilter.C), 3, 3);
            entries[12] = new ControllerFocusEntry(GetPickupQualityFilterControlId(PickupQualityFilter.D), 3, 4);
            entries[13] = new ControllerFocusEntry(GetPickupQualityFilterControlId(PickupQualityFilter.All), 3, 5);
            entries[14] = new ControllerFocusEntry(GetPickupQualityFilterControlId(PickupQualityFilter.Special), 3, 6);
            entries[15] = new ControllerFocusEntry(GetPickupQualityFilterControlId(PickupQualityFilter.Excluded), 3, 7);
            int writeIndex = 16;
            int listStartRow = 4;
            if (_pickupBrowserFilter == PickupBrowserFilter.Gun)
            {
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.All), 4, 0);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.Pistol), 4, 1);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.FullAuto), 4, 2);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.Shotgun), 4, 3);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.Rifle), 4, 4);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.Beam), 4, 5);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.Charge), 4, 6);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.Explosive), 5, 0);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.Elemental), 5, 1);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.Special), 5, 2);
                listStartRow = 6;
            }
            else if (_pickupBrowserFilter == PickupBrowserFilter.Passive)
            {
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupPassiveFilterControlId(PickupPassiveSubcategoryFilter.All), 4, 0);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupPassiveFilterControlId(PickupPassiveSubcategoryFilter.Bullet), 4, 1);
                listStartRow = 5;
            }
            else if (_pickupBrowserFilter == PickupBrowserFilter.Active)
            {
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupActiveCooldownFilterControlId(PickupActiveCooldownFilter.All), 4, 0);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupActiveCooldownFilterControlId(PickupActiveCooldownFilter.Uses), 4, 1);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupActiveCooldownFilterControlId(PickupActiveCooldownFilter.Damage), 4, 2);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupActiveCooldownFilterControlId(PickupActiveCooldownFilter.Time), 4, 3);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupActiveCooldownFilterControlId(PickupActiveCooldownFilter.Room), 4, 4);
                listStartRow = 5;
            }

            for (int index = 0; index < matches.Length; index++)
            {
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupRowActionControlId(matches[index]), listStartRow + index, 0);
            }

            return entries;
        }

        private void ExecutePickupPageFocusedControl(PlayerController player, ManualLogSource logger)
        {
            if (string.Equals(_pickupPageFocusedControlId, "pickups.back", StringComparison.Ordinal))
            {
                ClosePickupPage();
                return;
            }

            if (string.Equals(_pickupPageFocusedControlId, "pickups.target", StringComparison.Ordinal))
            {
                ToggleCharacterSwitchTarget(logger);
                return;
            }

            if (string.Equals(_pickupPageFocusedControlId, "pickups.search", StringComparison.Ordinal))
            {
                _focusPickupSearchField = true;
                return;
            }

            if (string.Equals(_pickupPageFocusedControlId, PickupSearchClearControlId, StringComparison.Ordinal))
            {
                ClearPickupSearchText();
                return;
            }

            if (TryExecutePickupFilterFocusedControl())
            {
                return;
            }

            PickupBrowserEntry[] matches = GetFilteredPickupEntries();
            for (int index = 0; index < matches.Length; index++)
            {
                PickupBrowserEntry entry = matches[index];
                if (!string.Equals(_pickupPageFocusedControlId, GetPickupRowActionControlId(entry), StringComparison.Ordinal))
                {
                    continue;
                }

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

                return;
            }
        }

        private static string GetPickupRowActionControlId(PickupBrowserEntry entry)
        {
            int pickupId = entry != null && entry.CatalogEntry != null ? entry.CatalogEntry.PickupId : -1;
            return "pickups.entry." + pickupId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private bool TryExecutePickupFilterFocusedControl()
        {
            PickupBrowserFilter browserFilter;
            if (TryGetPickupCategoryFilterFromControlId(_pickupPageFocusedControlId, out browserFilter))
            {
                ApplyPickupBrowserFilter(browserFilter);
                return true;
            }

            PickupQualityFilter qualityFilter;
            if (TryGetPickupQualityFilterFromControlId(_pickupPageFocusedControlId, out qualityFilter))
            {
                ApplyPickupQualityFilter(qualityFilter);
                return true;
            }

            PickupGunClassFilter gunClassFilter;
            if (TryGetPickupGunClassFilterFromControlId(_pickupPageFocusedControlId, out gunClassFilter))
            {
                ApplyPickupGunClassFilter(gunClassFilter);
                return true;
            }

            PickupPassiveSubcategoryFilter passiveFilter;
            if (TryGetPickupPassiveFilterFromControlId(_pickupPageFocusedControlId, out passiveFilter))
            {
                ApplyPickupPassiveSubcategoryFilter(passiveFilter);
                return true;
            }

            PickupActiveCooldownFilter cooldownFilter;
            if (TryGetPickupActiveCooldownFilterFromControlId(_pickupPageFocusedControlId, out cooldownFilter))
            {
                ApplyPickupActiveCooldownFilter(cooldownFilter);
                return true;
            }

            return false;
        }

        private void ApplyPickupBrowserFilter(PickupBrowserFilter filter)
        {
            _pickupBrowserFilter = filter;
            if (_pickupBrowserFilter != PickupBrowserFilter.Gun)
            {
                _pickupGunClassFilter = PickupGunClassFilter.All;
            }

            if (_pickupBrowserFilter != PickupBrowserFilter.Passive)
            {
                _pickupPassiveSubcategoryFilter = PickupPassiveSubcategoryFilter.All;
            }

            if (_pickupBrowserFilter != PickupBrowserFilter.Active)
            {
                _pickupActiveCooldownFilter = PickupActiveCooldownFilter.All;
            }

            _pickupScrollPosition = Vector2.zero;
            _focusPickupSearchField = false;
            _pickupPageFocusedControlId = GetPickupCategoryFilterControlId(filter);
            RequestGuiFocusRelease();
        }

        private void ApplyPickupQualityFilter(PickupQualityFilter filter)
        {
            _pickupQualityFilter = filter;
            _pickupScrollPosition = Vector2.zero;
            _focusPickupSearchField = false;
            _pickupPageFocusedControlId = GetPickupQualityFilterControlId(filter);
            RequestGuiFocusRelease();
        }

        private void ApplyPickupGunClassFilter(PickupGunClassFilter filter)
        {
            _pickupGunClassFilter = filter;
            _pickupScrollPosition = Vector2.zero;
            _focusPickupSearchField = false;
            _pickupPageFocusedControlId = GetPickupGunClassFilterControlId(filter);
            RequestGuiFocusRelease();
        }

        private void ApplyPickupPassiveSubcategoryFilter(PickupPassiveSubcategoryFilter filter)
        {
            _pickupPassiveSubcategoryFilter = filter;
            _pickupScrollPosition = Vector2.zero;
            _focusPickupSearchField = false;
            _pickupPageFocusedControlId = GetPickupPassiveFilterControlId(filter);
            RequestGuiFocusRelease();
        }

        private void ApplyPickupActiveCooldownFilter(PickupActiveCooldownFilter filter)
        {
            _pickupActiveCooldownFilter = filter;
            _pickupScrollPosition = Vector2.zero;
            _focusPickupSearchField = false;
            _pickupPageFocusedControlId = GetPickupActiveCooldownFilterControlId(filter);
            RequestGuiFocusRelease();
        }

        private void EnsurePickupBrowserFocusedResultVisible(PickupBrowserEntry[] matches, float listHeight)
        {
            int focusedResultIndex = GetPickupFocusedResultIndex(matches);
            if (focusedResultIndex < 0)
            {
                return;
            }

            float itemTop = 2f + (focusedResultIndex * (PickupBrowserRowHeight + PickupBrowserRowGap));
            float itemBottom = itemTop + (PickupBrowserRowHeight - 4f);
            if (_pickupScrollPosition.y > itemTop)
            {
                _pickupScrollPosition.y = itemTop;
                return;
            }

            float visibleBottom = _pickupScrollPosition.y + listHeight;
            if (itemBottom > visibleBottom)
            {
                _pickupScrollPosition.y = itemBottom - listHeight;
            }
        }

        private int GetPickupFocusedResultIndex(PickupBrowserEntry[] matches)
        {
            if (matches == null)
            {
                return -1;
            }

            for (int index = 0; index < matches.Length; index++)
            {
                if (string.Equals(_pickupPageFocusedControlId, GetPickupRowActionControlId(matches[index]), StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private bool IsPickupFocusOnCategoryFilter(PickupBrowserFilter filter)
        {
            return string.Equals(_pickupPageFocusedControlId, GetPickupCategoryFilterControlId(filter), StringComparison.Ordinal);
        }

        private bool IsPickupFocusOnQualityFilter(PickupQualityFilter filter)
        {
            return string.Equals(_pickupPageFocusedControlId, GetPickupQualityFilterControlId(filter), StringComparison.Ordinal);
        }

        private bool IsPickupFocusOnGunClassFilter(PickupGunClassFilter filter)
        {
            return string.Equals(_pickupPageFocusedControlId, GetPickupGunClassFilterControlId(filter), StringComparison.Ordinal);
        }

        private bool IsPickupFocusOnPassiveFilter(PickupPassiveSubcategoryFilter filter)
        {
            return string.Equals(_pickupPageFocusedControlId, GetPickupPassiveFilterControlId(filter), StringComparison.Ordinal);
        }

        private bool IsPickupFocusOnActiveCooldownFilter(PickupActiveCooldownFilter filter)
        {
            return string.Equals(_pickupPageFocusedControlId, GetPickupActiveCooldownFilterControlId(filter), StringComparison.Ordinal);
        }

        private static string GetPickupCategoryFilterControlId(PickupBrowserFilter filter)
        {
            return "pickups.filter.category." + filter.ToString();
        }

        private static string GetPickupQualityFilterControlId(PickupQualityFilter filter)
        {
            return "pickups.filter.quality." + filter.ToString();
        }

        private static string GetPickupGunClassFilterControlId(PickupGunClassFilter filter)
        {
            return "pickups.filter.gunclass." + filter.ToString();
        }

        private static string GetPickupPassiveFilterControlId(PickupPassiveSubcategoryFilter filter)
        {
            return "pickups.filter.passive." + filter.ToString();
        }

        private static string GetPickupActiveCooldownFilterControlId(PickupActiveCooldownFilter filter)
        {
            return "pickups.filter.activecooldown." + filter.ToString();
        }

        private static bool TryGetPickupCategoryFilterFromControlId(string controlId, out PickupBrowserFilter filter)
        {
            return TryParsePickupFilterControlId(controlId, "pickups.filter.category.", out filter);
        }

        private static bool TryGetPickupQualityFilterFromControlId(string controlId, out PickupQualityFilter filter)
        {
            return TryParsePickupFilterControlId(controlId, "pickups.filter.quality.", out filter);
        }

        private static bool TryGetPickupGunClassFilterFromControlId(string controlId, out PickupGunClassFilter filter)
        {
            return TryParsePickupFilterControlId(controlId, "pickups.filter.gunclass.", out filter);
        }

        private static bool TryGetPickupPassiveFilterFromControlId(string controlId, out PickupPassiveSubcategoryFilter filter)
        {
            return TryParsePickupFilterControlId(controlId, "pickups.filter.passive.", out filter);
        }

        private static bool TryGetPickupActiveCooldownFilterFromControlId(string controlId, out PickupActiveCooldownFilter filter)
        {
            return TryParsePickupFilterControlId(controlId, "pickups.filter.activecooldown.", out filter);
        }

        private static bool TryParsePickupFilterControlId<TEnum>(string controlId, string prefix, out TEnum filter)
            where TEnum : struct
        {
            filter = default(TEnum);
            if (string.IsNullOrEmpty(controlId) || !controlId.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                object parsedValue = Enum.Parse(typeof(TEnum), controlId.Substring(prefix.Length), true);
                if (!(parsedValue is TEnum))
                {
                    return false;
                }

                filter = (TEnum)parsedValue;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryGetPickupIcon(int pickupId, out PickupIconData iconData)
        {
            if (_pickupIconCache.TryGetValue(pickupId, out iconData))
            {
                return iconData.Texture != null;
            }

            PickupObject pickup = PickupObjectDatabase.GetById(pickupId);
            iconData = CreatePickupIconData(pickup);
            _pickupIconCache[pickupId] = iconData;
            return iconData.Texture != null;
        }

        private PickupIconData CreatePickupIconData(PickupObject pickup)
        {
            // Reuse the game's live pickup sprite data so the browser does not need its own icon bundle.
            if ((object)pickup == null || (object)pickup.sprite == null)
            {
                return PickupIconData.Empty;
            }

            // Render the actual tk2d geometry instead of drawing the atlas UV bounding
            // rectangle. The latter loses rotated atlas regions and the original vertex
            // mapping, which can make long guns appear to point in the wrong direction.
            PickupIconData renderedIcon = RenderPickupIconData(pickup.sprite, pickup.PickupObjectId);
            if (renderedIcon.Texture != null)
            {
                return renderedIcon;
            }

            return CreateAtlasPickupIconData(pickup);
        }

        private PickupIconData RenderPickupIconData(tk2dBaseSprite sourceSprite, int pickupId)
        {
            if (sourceSprite == null || sourceSprite.Collection == null || sourceSprite.CurrentSprite == null)
            {
                LogPickupIconDiagnostic(
                    "Icon render skipped. PickupId=" + pickupId
                    + ", SourceSprite=" + (sourceSprite == null ? "null" : "present")
                    + ", Collection=" + (sourceSprite == null || sourceSprite.Collection == null ? "null" : "present")
                    + ", Definition=" + (sourceSprite == null || sourceSprite.CurrentSprite == null ? "null" : "present") + ".");
                return PickupIconData.Empty;
            }
            const int textureWidth = 128;
            const int textureHeight = 80;
            GameObject iconObject = null;
            GameObject cameraObject = null;
            RenderTexture renderTexture = null;
            RenderTexture previousActiveTexture = RenderTexture.active;
            string definitionName = sourceSprite.CurrentSprite.name;
            LogPickupIconDiagnostic(
                "Icon render begin. PickupId=" + pickupId
                + ", Sprite=" + definitionName
                + ", SpriteId=" + sourceSprite.spriteId
                + ", Collection=" + sourceSprite.Collection.name + ".");
            try
            {
                iconObject = new GameObject("EtgGameplayDashboard.PickupIcon");
                iconObject.hideFlags = HideFlags.HideAndDontSave;
                iconObject.layer = 31;
                tk2dSprite iconSprite = tk2dSprite.AddComponent(iconObject, sourceSprite.Collection, sourceSprite.spriteId);
                if (iconSprite == null)
                {
                    LogPickupIconDiagnostic("Icon render failed at tk2dSprite.AddComponent. PickupId=" + pickupId + ".");
                    return PickupIconData.Empty;
                }

                // tk2d applies the collection's render layer while building the sprite.
                // Reapply the private preview layer after AddComponent so the preview
                // camera can see only this temporary icon instead of the gameplay scene.
                iconObject.layer = 31;

                iconSprite.color = sourceSprite.color;
                iconSprite.scale = sourceSprite.scale;
                iconSprite.FlipX = sourceSprite.FlipX;
                iconSprite.FlipY = sourceSprite.FlipY;
                iconObject.transform.localEulerAngles = sourceSprite.transform.localEulerAngles;
                Bounds bounds = iconSprite.GetBounds();
                Renderer iconRenderer = iconObject.GetComponent<Renderer>();
                LogPickupIconDiagnostic(
                    "Icon render setup. PickupId=" + pickupId
                    + ", Renderer=" + (iconRenderer == null ? "null" : "present")
                    + ", RendererEnabled=" + (iconRenderer != null && iconRenderer.enabled)
                    + ", RendererVisible=" + (iconRenderer != null && iconRenderer.isVisible)
                    + ", ObjectLayer=" + iconObject.layer
                    + ", ObjectPosition=" + iconObject.transform.position
                    + ", RendererBounds=" + (iconRenderer == null ? "null" : iconRenderer.bounds.ToString()) + ".");
                float aspect = (float)textureWidth / textureHeight;
                float requiredHeight = Mathf.Max(bounds.size.y, bounds.size.x / aspect);
                if (requiredHeight <= 0.0001f)
                {
                    LogPickupIconDiagnostic(
                        "Icon render failed at bounds. PickupId=" + pickupId
                        + ", Bounds=" + bounds + ".");
                    return PickupIconData.Empty;
                }
                iconObject.transform.localPosition = -bounds.center;

                cameraObject = new GameObject("EtgGameplayDashboard.PickupIconCamera");
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                cameraObject.layer = 31;
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.cullingMask = 1 << 31;
                camera.orthographic = true;
                camera.aspect = aspect;
                camera.orthographicSize = requiredHeight * 0.55f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 20f;

                renderTexture = new RenderTexture(textureWidth, textureHeight, 24, RenderTextureFormat.ARGB32);
                renderTexture.hideFlags = HideFlags.HideAndDontSave;
                renderTexture.filterMode = FilterMode.Bilinear;
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.filterMode = FilterMode.Bilinear;
                texture.ReadPixels(new Rect(0f, 0f, textureWidth, textureHeight), 0, 0, false);
                texture.Apply(false, false);
                Color[] pixels = texture.GetPixels();
                int visiblePixelCount = 0;
                float maximumAlpha = 0f;
                for (int pixelIndex = 0; pixelIndex < pixels.Length; pixelIndex++)
                {
                    maximumAlpha = Mathf.Max(maximumAlpha, pixels[pixelIndex].a);
                    if (pixels[pixelIndex].a > 0.01f)
                    {
                        visiblePixelCount++;
                    }
                }
                LogPickupIconDiagnostic(
                    "Icon render success. PickupId=" + pickupId
                    + ", Sprite=" + definitionName
                    + ", Bounds=" + bounds
                    + ", RequiredHeight=" + requiredHeight
                    + ", VisiblePixels=" + visiblePixelCount
                    + ", MaximumAlpha=" + maximumAlpha + ".");
                return new PickupIconData(texture, new Rect(0f, 0f, 1f, 1f));
            }
            catch (Exception exception)
            {
                LogPickupIconDiagnostic(
                    "Icon render exception. PickupId=" + pickupId
                    + ", Sprite=" + definitionName
                    + ", Type=" + exception.GetType().Name
                    + ", Message=" + exception.Message + ".");
                return PickupIconData.Empty;
            }
            finally
            {
                RenderTexture.active = previousActiveTexture;
                if (renderTexture != null)
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                if (cameraObject != null)
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                if (iconObject != null)
                    UnityEngine.Object.DestroyImmediate(iconObject);
            }
        }

        private void LogPickupIconDiagnostic(string message)
        {
            if (IsPickupBrowserPerformanceLoggingEnabled() && _performanceLogger != null)
            {
                _performanceLogger.LogInfo(EtgGameplayDashboardLog.Performance("PickupBrowserIcon: " + message));
            }
        }

        private static PickupIconData CreateAtlasPickupIconData(PickupObject pickup)
        {
            // Fallback for unusual sprites that cannot be instantiated by tk2d.

            tk2dSpriteDefinition definition = pickup.sprite.CurrentSprite;
            if (definition == null || definition.material == null || definition.uvs == null || definition.uvs.Length == 0)
            {
                return PickupIconData.Empty;
            }

            Texture texture = definition.material.mainTexture;
            if (texture == null)
            {
                return PickupIconData.Empty;
            }

            float minX = definition.uvs[0].x;
            float minY = definition.uvs[0].y;
            float maxX = minX;
            float maxY = minY;
            for (int index = 1; index < definition.uvs.Length; index++)
            {
                Vector2 uv = definition.uvs[index];
                minX = Mathf.Min(minX, uv.x);
                minY = Mathf.Min(minY, uv.y);
                maxX = Mathf.Max(maxX, uv.x);
                maxY = Mathf.Max(maxY, uv.y);
            }

            return new PickupIconData(texture, Rect.MinMaxRect(minX, minY, maxX, maxY));
        }

        private static string NormalizeLookupValue(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue))
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(rawValue.Length);
            for (int index = 0; index < rawValue.Length; index++)
            {
                char current = rawValue[index];
                if (char.IsLetterOrDigit(current))
                {
                    builder.Append(char.ToLowerInvariant(current));
                }
            }

            return builder.ToString();
        }

        private PickupBrowserEntry[] GetFilteredPickupEntries()
        {
            if (_cachedPickupEntries.Length == 0)
            {
                return EmptyPickupBrowserEntries;
            }

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

            string normalizedSearch = NormalizeLookupValue(_pickupSearchText);
            List<PickupBrowserEntry> matches = new List<PickupBrowserEntry>();
            for (int index = 0; index < _cachedPickupEntries.Length; index++)
            {
                PickupBrowserEntry entry = _cachedPickupEntries[index];
                if (!MatchesPickupFilter(entry.CatalogEntry.Category))
                {
                    continue;
                }

                if (!MatchesPickupQualityFilter(entry.CatalogEntry.Quality))
                {
                    continue;
                }

                if (!MatchesPickupGunClassFilter(entry.CatalogEntry))
                {
                    continue;
                }

                if (!MatchesPickupPassiveSubcategoryFilter(entry.CatalogEntry))
                {
                    continue;
                }

                if (!MatchesPickupActiveCooldownFilter(entry.CatalogEntry))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(normalizedSearch) &&
                    entry.SearchText.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                matches.Add(entry);
            }

            _filteredPickupEntriesCache = matches.ToArray();
            _filteredPickupEntriesCacheSearch = _pickupSearchText;
            _filteredPickupEntriesCacheFilter = _pickupBrowserFilter;
            _filteredPickupEntriesCacheQualityFilter = _pickupQualityFilter;
            _filteredPickupEntriesCacheGunClassFilter = _pickupGunClassFilter;
            _filteredPickupEntriesCachePassiveFilter = _pickupPassiveSubcategoryFilter;
            _filteredPickupEntriesCacheActiveCooldownFilter = _pickupActiveCooldownFilter;
            return _filteredPickupEntriesCache;
        }

        private bool MatchesPickupFilter(PickupCategory category)
        {
            switch (_pickupBrowserFilter)
            {
                case PickupBrowserFilter.All:
                    return true;
                case PickupBrowserFilter.Gun:
                    return category == PickupCategory.Gun;
                case PickupBrowserFilter.Passive:
                    return category == PickupCategory.Passive;
                case PickupBrowserFilter.Active:
                    return category == PickupCategory.Active;
                default:
                    return true;
            }
        }

        private bool MatchesPickupQualityFilter(string quality)
        {
            if (_pickupQualityFilter == PickupQualityFilter.All)
            {
                return true;
            }

            string normalizedQuality = (quality ?? string.Empty).Trim();
            switch (_pickupQualityFilter)
            {
                case PickupQualityFilter.D:
                    return string.Equals(normalizedQuality, "D", StringComparison.OrdinalIgnoreCase);
                case PickupQualityFilter.C:
                    return string.Equals(normalizedQuality, "C", StringComparison.OrdinalIgnoreCase);
                case PickupQualityFilter.B:
                    return string.Equals(normalizedQuality, "B", StringComparison.OrdinalIgnoreCase);
                case PickupQualityFilter.A:
                    return string.Equals(normalizedQuality, "A", StringComparison.OrdinalIgnoreCase);
                case PickupQualityFilter.S:
                    return string.Equals(normalizedQuality, "S", StringComparison.OrdinalIgnoreCase);
                case PickupQualityFilter.Special:
                    return string.Equals(normalizedQuality, "SPECIAL", StringComparison.OrdinalIgnoreCase);
                case PickupQualityFilter.Excluded:
                    return string.Equals(normalizedQuality, "EXCLUDED", StringComparison.OrdinalIgnoreCase);
                default:
                    return true;
            }
        }

        private bool MatchesPickupGunClassFilter(EtgPickupCatalogEntry entry)
        {
            if (_pickupGunClassFilter == PickupGunClassFilter.All)
            {
                return true;
            }

            if (entry == null || entry.Category != PickupCategory.Gun)
            {
                return false;
            }

            string gunClass = (entry.GunClass ?? string.Empty).Trim();
            switch (_pickupGunClassFilter)
            {
                case PickupGunClassFilter.Pistol:
                    return string.Equals(gunClass, "PISTOL", StringComparison.OrdinalIgnoreCase);
                case PickupGunClassFilter.FullAuto:
                    return string.Equals(gunClass, "FULLAUTO", StringComparison.OrdinalIgnoreCase);
                case PickupGunClassFilter.Shotgun:
                    return string.Equals(gunClass, "SHOTGUN", StringComparison.OrdinalIgnoreCase);
                case PickupGunClassFilter.Rifle:
                    return string.Equals(gunClass, "RIFLE", StringComparison.OrdinalIgnoreCase);
                case PickupGunClassFilter.Beam:
                    return string.Equals(gunClass, "BEAM", StringComparison.OrdinalIgnoreCase);
                case PickupGunClassFilter.Charge:
                    return string.Equals(gunClass, "CHARGE", StringComparison.OrdinalIgnoreCase);
                case PickupGunClassFilter.Explosive:
                    return string.Equals(gunClass, "EXPLOSIVE", StringComparison.OrdinalIgnoreCase);
                case PickupGunClassFilter.Elemental:
                    return string.Equals(gunClass, "FIRE", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(gunClass, "ICE", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(gunClass, "POISON", StringComparison.OrdinalIgnoreCase);
                case PickupGunClassFilter.Special:
                    return string.Equals(gunClass, "SILLY", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(gunClass, "SHITTY", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(gunClass, "CHARM", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(gunClass, "NONE", StringComparison.OrdinalIgnoreCase);
                default:
                    return true;
            }
        }

        private bool MatchesPickupPassiveSubcategoryFilter(EtgPickupCatalogEntry entry)
        {
            if (_pickupPassiveSubcategoryFilter == PickupPassiveSubcategoryFilter.All)
            {
                return true;
            }

            if (entry == null || entry.Category != PickupCategory.Passive)
            {
                return false;
            }

            switch (_pickupPassiveSubcategoryFilter)
            {
                case PickupPassiveSubcategoryFilter.Bullet:
                    return IsBulletPassive(entry);
                default:
                    return true;
            }
        }

        private static bool IsBulletPassive(EtgPickupCatalogEntry entry)
        {
            return ContainsBulletPassiveToken(entry.DisplayName) ||
                   ContainsBulletPassiveToken(entry.InternalName) ||
                   ContainsBulletPassiveToken(entry.PrimaryDisplayName) ||
                   ContainsBulletPassiveToken(entry.ShortDescription) ||
                   ContainsBulletPassiveToken(entry.LongDescription);
        }

        private static bool ContainsBulletPassiveToken(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            string lowerValue = value.ToLowerInvariant();
            return lowerValue.IndexOf("bullet", StringComparison.Ordinal) >= 0 ||
                   lowerValue.IndexOf("round", StringComparison.Ordinal) >= 0 ||
                   lowerValue.IndexOf("lead", StringComparison.Ordinal) >= 0;
        }

        private bool MatchesPickupActiveCooldownFilter(EtgPickupCatalogEntry entry)
        {
            if (_pickupActiveCooldownFilter == PickupActiveCooldownFilter.All)
            {
                return true;
            }

            if (entry == null || entry.Category != PickupCategory.Active)
            {
                return false;
            }

            switch (_pickupActiveCooldownFilter)
            {
                case PickupActiveCooldownFilter.Uses:
                    return entry.ActiveNumberOfUses > 0;
                case PickupActiveCooldownFilter.Damage:
                    return entry.ActiveDamageCooldown > 0f;
                case PickupActiveCooldownFilter.Time:
                    return entry.ActiveTimeCooldown > 0f;
                case PickupActiveCooldownFilter.Room:
                    return entry.ActiveRoomCooldown > 0;
                default:
                    return true;
            }
        }

        private static Dictionary<int, List<string>> BuildAliasesByPickupId(PickupAliasRegistry aliasRegistry)
        {
            Dictionary<int, List<string>> aliasesByPickupId = new Dictionary<int, List<string>>();
            PickupAliasRegistry effectiveRegistry = aliasRegistry ?? PickupAliasRegistry.Empty;
            for (int index = 0; index < effectiveRegistry.Entries.Length; index++)
            {
                PickupAliasEntry entry = effectiveRegistry.Entries[index];
                List<string> aliases;
                if (!aliasesByPickupId.TryGetValue(entry.PickupId, out aliases))
                {
                    aliases = new List<string>();
                    aliasesByPickupId.Add(entry.PickupId, aliases);
                }

                aliases.Add(entry.Alias);
            }

            return aliasesByPickupId;
        }
    }
}
