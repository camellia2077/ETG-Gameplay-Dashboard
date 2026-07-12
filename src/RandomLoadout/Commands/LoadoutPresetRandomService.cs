// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;

namespace RandomLoadout
{
    internal sealed class LoadoutPresetRandomService
    {
        private readonly LoadoutRuleEditorService _editorService;
        private readonly LoadoutPresetDeck _deck = new LoadoutPresetDeck();
        // The toggle immediately draws a preset so the UI can show the upcoming
        // loadout. Keep that draw for the next run; drawing again at run start
        // would make the displayed preset differ from the one actually granted.
        private bool _selectionPendingForGrant;
        // This is intentionally kept when Random is toggled off so re-enabling it
        // cannot immediately select the same preset again.
        private string _lastSelectedPresetId = string.Empty;

        public LoadoutPresetRandomService(LoadoutRuleEditorService editorService)
        {
            _editorService = editorService;
        }

        public bool IsEnabled { get; private set; }

        public bool Toggle(ManualLogSource logger)
        {
            IsEnabled = !IsEnabled;
            _selectionPendingForGrant = false;
            if (IsEnabled)
            {
                LoadoutPresetEditorEntry[] entries = GetPresetEntries();
                _deck.Reset(GetPresetIds(entries));
                SelectNextPreset(logger, _lastSelectedPresetId, entries);
            }

            return IsEnabled;
        }

        public GrantCommandExecutionResult SelectNextPreset(ManualLogSource logger)
        {
            return SelectNextPreset(logger, string.Empty, null);
        }

        private GrantCommandExecutionResult SelectNextPreset(
            ManualLogSource logger,
            string excludedPresetId,
            LoadoutPresetEditorEntry[] preparedEntries)
        {
            if (!IsEnabled || _editorService == null)
            {
                return null;
            }

            LoadoutPresetEditorEntry[] entries = preparedEntries ?? GetPresetEntries();
            _deck.Ensure(GetPresetIds(entries));
            int presetIndex = DrawIndexExcluding(entries, excludedPresetId);
            if (presetIndex < 0 || presetIndex >= entries.Length || entries[presetIndex] == null)
            {
                GrantCommandExecutionResult result = new GrantCommandExecutionResult(
                    false,
                    GuiText.Get("gui.loadout_editor.empty_presets"),
                    GuiText.GetEnglish("gui.loadout_editor.empty_presets"));
                LogResult(result, logger);
                return result;
            }

            LoadoutPresetEditorEntry entry = entries[presetIndex];
            if (logger != null)
            {
                logger.LogInfo(
                    RandomLoadoutLog.Command(
                        "Random preset draw: index=" + presetIndex +
                        ", presetId=" + entry.Id +
                        ", deckCursor=" + _deck.Cursor + "/" + _deck.Count +
                        ", deck=" + _deck.DescribeOrder() + "."));
            }

            GrantCommandExecutionResult selectionResult = _editorService.SelectPreset(entry.Id);
            _selectionPendingForGrant = selectionResult != null && selectionResult.Succeeded;
            if (_selectionPendingForGrant)
            {
                _lastSelectedPresetId = entry.Id ?? string.Empty;
            }
            LogResult(selectionResult, logger);
            return selectionResult;
        }

        private int DrawIndexExcluding(LoadoutPresetEditorEntry[] entries, string excludedPresetId)
        {
            if (entries == null || entries.Length == 0)
            {
                return -1;
            }

            bool shouldExclude = entries.Length > 1 && !string.IsNullOrEmpty(excludedPresetId);
            int fallbackIndex = -1;
            for (int attempt = 0; attempt < entries.Length; attempt++)
            {
                int candidateIndex = _deck.DrawIndex();
                if (candidateIndex < 0 || candidateIndex >= entries.Length || entries[candidateIndex] == null)
                {
                    continue;
                }

                fallbackIndex = candidateIndex;
                if (!shouldExclude || !string.Equals(entries[candidateIndex].Id, excludedPresetId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return candidateIndex;
                }
            }

            // This is only reachable when the preset list has no valid alternative;
            // with a single preset, reusing it is the only valid behavior.
            return fallbackIndex;
        }

        public GrantCommandExecutionResult SelectNextPresetForGrant(ManualLogSource logger)
        {
            if (!IsEnabled)
            {
                return null;
            }

            if (_selectionPendingForGrant)
            {
                _selectionPendingForGrant = false;
                if (logger != null)
                {
                    logger.LogInfo(
                        RandomLoadoutLog.Grant(
                            "Using the preset already displayed by Random mode for this run; no second draw was performed."));
                }

                return null;
            }

            GrantCommandExecutionResult result = SelectNextPreset(logger);
            _selectionPendingForGrant = false;
            return result;
        }

        public string GetDiagnostics()
        {
            LoadoutPresetEditorEntry[] entries = GetPresetEntries();
            _deck.Ensure(GetPresetIds(entries));

            string activePresetId = "<none>";
            string activePresetName = "<none>";
            for (int index = 0; index < entries.Length; index++)
            {
                if (entries[index] != null && entries[index].IsActive)
                {
                    activePresetId = entries[index].Id;
                    activePresetName = entries[index].DisplayName;
                    break;
                }
            }

            return "Random preset diagnostics: enabled=" + IsEnabled +
                ", activePresetId=" + activePresetId +
                ", activePresetName=" + activePresetName +
                ", lastSelectedPresetId=" + _lastSelectedPresetId +
                ", selectionPendingForGrant=" + _selectionPendingForGrant +
                ", deckCursor=" + _deck.Cursor + "/" + _deck.Count +
                ", deck=" + _deck.DescribeOrder() + ".";
        }

        private LoadoutPresetEditorEntry[] GetPresetEntries()
        {
            return _editorService != null
                ? _editorService.GetPresetEntries()
                : new LoadoutPresetEditorEntry[0];
        }

        private static string[] GetPresetIds(LoadoutPresetEditorEntry[] entries)
        {
            string[] presetIds = new string[entries != null ? entries.Length : 0];
            for (int index = 0; index < presetIds.Length; index++)
            {
                presetIds[index] = entries[index] != null ? entries[index].Id : string.Empty;
            }

            return presetIds;
        }

        private static void LogResult(GrantCommandExecutionResult result, ManualLogSource logger)
        {
            if (result == null || logger == null)
            {
                return;
            }

            if (result.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(result.LogMessage));
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(result.LogMessage));
            }
        }
    }
}
