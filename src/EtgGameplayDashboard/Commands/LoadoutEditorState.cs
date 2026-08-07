// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;

namespace EtgGameplayDashboard
{
    internal enum LoadoutEditorMode
    {
        PresetList,
        PresetDetail,
        RandomPoolDetail,
        PresetPickupsDetail,
    }

    /// <summary>
    /// Owns the mutable workflow state for the loadout editor pages.
    /// </summary>
    internal sealed class LoadoutEditorState
    {
        public LoadoutEditorMode Mode = LoadoutEditorMode.PresetList;
        public string PresetRenameText = string.Empty;
        public string RandomPoolRenameText = string.Empty;
        public string PickupCountEditText = string.Empty;
        public int RandomPoolRuleIndex = -1;
        public int PickupCountEditIndex = -1;
        public Vector2 EditorScrollPosition = Vector2.zero;
        public Vector2 PresetScrollPosition = Vector2.zero;
    }
}
