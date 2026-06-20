using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
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
            _currentPage = PanelPage.Pickups;
            _pickupBrowserMode = mode;
            _focusInputField = false;
            _focusPickupSearchField = true;
            RefreshPickupBrowserData();

            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command("Pickup browser opened. Mode=" + _pickupBrowserMode + "."));
            }
        }

        private void DrawPickupPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), _buttonStyle))
            {
                if (_pickupBrowserMode == PickupBrowserMode.AddToStartItems)
                {
                    _currentPage = PanelPage.LoadoutEditor;
                    RefreshLoadoutEditorEntries();
                }
                else if (_pickupBrowserMode == PickupBrowserMode.AddToRandomPool)
                {
                    _currentPage = PanelPage.LoadoutEditor;
                    _loadoutEditorMode = LoadoutEditorMode.RandomPoolDetail;
                    RefreshLoadoutEditorEntries();
                    RefreshLoadoutRandomPoolEntries();
                }
                else
                {
                    _currentPage = PanelPage.Command;
                    _focusInputField = true;
                }

                _focusPickupSearchField = false;
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, panelRect.width - ButtonWidth - 32f, 24f),
                GetPickupBrowserTitle(),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.pickups.hint.search"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 58f, panelRect.width - 28f, 20f),
                GetPickupBrowserActionHint(),
                _hintStyle);

            GUI.SetNextControlName(PickupSearchControlName);
            Rect searchRect = new Rect(panelRect.x + 14f, panelRect.y + 86f, panelRect.width - 28f, 32f);
            _pickupSearchText = GUI.TextField(searchRect, _pickupSearchText, 128, _textFieldStyle);
            if (_focusPickupSearchField)
            {
                GUI.FocusControl(PickupSearchControlName);
                _focusPickupSearchField = false;
            }

            float filtersTop = searchRect.yMax + 10f;
            DrawPickupFilterButtons(panelRect.x + 14f, filtersTop);

            float listTop = filtersTop + GetPickupFilterAreaHeight();
            Rect listRect = new Rect(panelRect.x + 14f, listTop, panelRect.width - 28f, panelRect.height - (listTop - panelRect.y) - 14f);
            DrawPickupResults(listRect, player, logger);
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
            GUIStyle style = _pickupBrowserFilter == filter ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle;
            if (GUI.Button(rect, label, style))
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
                _focusPickupSearchField = true;
            }

            return rect.xMax + ButtonGap;
        }

        private float DrawPickupQualityFilterButton(Rect rect, PickupQualityFilter filter, string label)
        {
            GUIStyle style = _pickupQualityFilter == filter ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle;
            if (GUI.Button(rect, label, style))
            {
                _pickupQualityFilter = filter;
                _pickupScrollPosition = Vector2.zero;
                _focusPickupSearchField = true;
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
            GUIStyle style = _pickupGunClassFilter == filter ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle;
            if (GUI.Button(rect, label, style))
            {
                _pickupGunClassFilter = filter;
                _pickupScrollPosition = Vector2.zero;
                _focusPickupSearchField = true;
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
            GUIStyle style = _pickupPassiveSubcategoryFilter == filter ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle;
            if (GUI.Button(rect, label, style))
            {
                _pickupPassiveSubcategoryFilter = filter;
                _pickupScrollPosition = Vector2.zero;
                _focusPickupSearchField = true;
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
            GUIStyle style = _pickupActiveCooldownFilter == filter ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle;
            if (GUI.Button(rect, label, style))
            {
                _pickupActiveCooldownFilter = filter;
                _pickupScrollPosition = Vector2.zero;
                _focusPickupSearchField = true;
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

            Rect viewRect = new Rect(0f, 0f, listRect.width - 18f, (matches.Length * PickupRowHeight) + 4f);
            _pickupScrollPosition = GUI.BeginScrollView(listRect, _pickupScrollPosition, viewRect);
            for (int i = 0; i < matches.Length; i++)
            {
                DrawPickupRow(new Rect(0f, 2f + (i * PickupRowHeight), viewRect.width, PickupRowHeight - 4f), matches[i], player, logger);
            }

            GUI.EndScrollView();
        }

        private void DrawPickupRow(Rect rowRect, PickupBrowserEntry entry, PlayerController player, ManualLogSource logger)
        {
            GUI.Box(rowRect, GUIContent.none, _pickupRowStyle);

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

            Rect iconRect = new Rect(rowRect.x + 8f, rowRect.y + ((rowRect.height - PickupIconSize) * 0.5f), PickupIconSize, PickupIconSize);
            DrawPickupIcon(iconRect, entry);

            float textLeft = iconRect.xMax + 8f;
            float textWidth = rowRect.width - actionButtonsWidth - 32f - PickupIconSize - 24f;
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
                if (GUI.Button(addButtonRect, GuiText.Get("gui.pickups.button.add_loadout"), _buttonStyle))
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
            if (GUI.Button(grantButtonRect, GuiText.Get("gui.command.button.grant"), _buttonStyle))
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
                GUI.DrawTextureWithTexCoords(iconRect, iconData.Texture, iconData.TextureCoords, true);
                return;
            }

            GUI.Box(iconRect, entry.IconFallbackLabel, _pickupIconFallbackStyle);
        }

        private void ExecutePickupBrowserGrant(PickupBrowserEntry entry, PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _commandService.ExecuteCatalogEntry(player, entry.CatalogEntry);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);
            _inputText = entry.CommandText;

            if (executionResult.Succeeded)
            {
                if (logger != null)
                {
                    logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                }

                _focusPickupSearchField = true;
            }
            else if (logger != null)
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void RefreshPickupBrowserData()
        {
            if (_cachedPickupEntries.Length > 0 || _pickupCatalogProvider == null)
            {
                return;
            }

            EtgPickupCatalogEntry[] catalogEntries = _pickupCatalogProvider() ?? new EtgPickupCatalogEntry[0];
            PickupAliasRegistry aliasRegistry = _aliasRegistryProvider != null ? _aliasRegistryProvider() : PickupAliasRegistry.Empty;
            Dictionary<int, List<string>> aliasesByPickupId = BuildAliasesByPickupId(aliasRegistry);
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
                browserEntries.Add(new PickupBrowserEntry(entry, aliases));
            }

            _cachedPickupEntries = browserEntries.ToArray();
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
            _pickupIconCache.Clear();
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

        private static PickupIconData CreatePickupIconData(PickupObject pickup)
        {
            // Reuse the game's live pickup sprite data so the browser does not need its own icon bundle.
            if ((object)pickup == null || (object)pickup.sprite == null)
            {
                return PickupIconData.Empty;
            }

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

            return matches.ToArray();
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
