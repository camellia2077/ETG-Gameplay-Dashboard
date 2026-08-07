// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;

namespace EtgGameplayDashboard
{
    internal enum ControllerNavDirection
    {
        Left,
        Right,
        Up,
        Down,
    }

    internal struct ControllerFocusEntry
    {
        public ControllerFocusEntry(string controlId, int row, int column)
        {
            ControlId = controlId ?? string.Empty;
            Row = row;
            Column = column;
        }

        public string ControlId;
        public int Row;
        public int Column;
    }

    internal static class ControllerFocusNavigator
    {
        public static string Move(ControllerFocusEntry[] entries, string currentControlId, ControllerNavDirection direction)
        {
            if (entries == null || entries.Length == 0) return string.Empty;

            int currentIndex = FindEntryIndex(entries, currentControlId);
            if (currentIndex < 0) return entries[0].ControlId;

            ControllerFocusEntry currentEntry = entries[currentIndex];
            int bestIndex = currentIndex;
            int bestScore = int.MaxValue;
            for (int index = 0; index < entries.Length; index++)
            {
                if (index == currentIndex) continue;
                ControllerFocusEntry candidate = entries[index];
                int score = GetScore(direction, candidate.Row - currentEntry.Row, candidate.Column - currentEntry.Column);
                if (score >= bestScore) continue;
                bestScore = score;
                bestIndex = index;
            }

            return entries[bestIndex].ControlId;
        }

        private static int FindEntryIndex(ControllerFocusEntry[] entries, string controlId)
        {
            for (int index = 0; index < entries.Length; index++)
            {
                if (string.Equals(entries[index].ControlId, controlId, System.StringComparison.Ordinal)) return index;
            }

            return -1;
        }

        private static int GetScore(ControllerNavDirection direction, int rowDelta, int columnDelta)
        {
            switch (direction)
            {
                case ControllerNavDirection.Left:
                    return columnDelta < 0 ? (Mathf.Abs(columnDelta) * 10) + Mathf.Abs(rowDelta) : int.MaxValue;
                case ControllerNavDirection.Right:
                    return columnDelta > 0 ? (columnDelta * 10) + Mathf.Abs(rowDelta) : int.MaxValue;
                case ControllerNavDirection.Up:
                    return rowDelta < 0 ? (Mathf.Abs(rowDelta) * 10) + Mathf.Abs(columnDelta) : int.MaxValue;
                case ControllerNavDirection.Down:
                    return rowDelta > 0 ? (rowDelta * 10) + Mathf.Abs(columnDelta) : int.MaxValue;
                default:
                    return int.MaxValue;
            }
        }
    }
}
