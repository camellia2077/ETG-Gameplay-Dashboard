// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace EtgGameplayDashboard
{
    /// <summary>
    /// Static names extracted from the game's enemies text assets. Room geometry
    /// is still resolved from the live BossManager; only the display name avoids
    /// loading an AIActor and asking the game StringTableManager on every redraw.
    /// </summary>
    internal sealed class BossNameCatalog
    {
        private sealed class Entry
        {
            public string EnglishName;
            public string SimplifiedChineseName;
        }

        private sealed class RoomEntry
        {
            public string EnglishName;
            public string SimplifiedChineseName;
        }

        private readonly Dictionary<string, Entry> _entries =
            new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, RoomEntry> _roomEntries =
            new Dictionary<string, RoomEntry>(StringComparer.OrdinalIgnoreCase);

        public static BossNameCatalog Load(string filePath)
        {
            BossNameCatalog catalog = new BossNameCatalog();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return catalog;
            }

            try
            {
                JObject root = JObject.Parse(File.ReadAllText(filePath, Encoding.UTF8));
                JArray bosses = root["bosses"] as JArray;
                if (bosses == null)
                {
                    return catalog;
                }

                for (int index = 0; index < bosses.Count; index++)
                {
                    JObject boss = bosses[index] as JObject;
                    JObject displayNames = boss != null ? boss["display_name"] as JObject : null;
                    JArray roomNames = boss != null ? boss["room_names"] as JArray : null;
                    if (displayNames == null || roomNames == null)
                    {
                        continue;
                    }

                    Entry entry = new Entry
                    {
                        EnglishName = (string)displayNames["en"] ?? string.Empty,
                        SimplifiedChineseName = (string)displayNames["zh-CN"] ?? string.Empty,
                    };
                    JObject roomDisplayNames = boss != null ? boss["room_display_names"] as JObject : null;
                    for (int roomIndex = 0; roomIndex < roomNames.Count; roomIndex++)
                    {
                        string roomName = (string)roomNames[roomIndex];
                        if (!string.IsNullOrEmpty(roomName))
                        {
                            catalog._entries[roomName] = entry;
                            JObject roomDisplayName = roomDisplayNames != null ? roomDisplayNames[roomName] as JObject : null;
                            if (roomDisplayName != null)
                            {
                                catalog._roomEntries[roomName] = new RoomEntry
                                {
                                    EnglishName = (string)roomDisplayName["en"] ?? string.Empty,
                                    SimplifiedChineseName = (string)roomDisplayName["zh-CN"] ?? string.Empty,
                                };
                            }
                        }
                    }
                }
            }
            catch
            {
                // The live AIActor/StringTableManager path remains the safe fallback.
            }

            return catalog;
        }

        public bool TryGetDisplayName(string roomName, out string displayName)
        {
            Entry entry;
            if (!_entries.TryGetValue(roomName ?? string.Empty, out entry))
            {
                displayName = string.Empty;
                return false;
            }

            if (string.Equals(GuiText.CurrentLanguageCode, "zh-CN", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(entry.SimplifiedChineseName))
            {
                displayName = entry.SimplifiedChineseName;
                return true;
            }

            displayName = entry.EnglishName;
            return !string.IsNullOrEmpty(displayName);
        }

        public bool TryGetRoomDisplayName(string roomName, out string displayName)
        {
            RoomEntry entry;
            if (!_roomEntries.TryGetValue(roomName ?? string.Empty, out entry))
            {
                displayName = string.Empty;
                return false;
            }

            // Room layout labels intentionally use the same text in both
            // languages: the game has no localized names for room prototypes.
            if (string.Equals(GuiText.CurrentLanguageCode, "zh-CN", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(entry.SimplifiedChineseName))
            {
                displayName = entry.SimplifiedChineseName;
                return true;
            }

            displayName = entry.EnglishName;
            return !string.IsNullOrEmpty(displayName);
        }
    }
}
