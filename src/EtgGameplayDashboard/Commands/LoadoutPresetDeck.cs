// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Text;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed class LoadoutPresetDeck
    {
        private string[] _presetIds = new string[0];
        private int[] _shuffledIndices = new int[0];

        public int Cursor { get; private set; }

        public int Count
        {
            get { return _shuffledIndices.Length; }
        }

        public void Ensure(string[] presetIds)
        {
            string[] normalizedIds = NormalizeIds(presetIds);
            if (HaveSameIds(_presetIds, normalizedIds))
            {
                return;
            }

            Reset(normalizedIds);
        }

        public void Reset(string[] presetIds)
        {
            _presetIds = NormalizeIds(presetIds);
            _shuffledIndices = new int[_presetIds.Length];
            for (int index = 0; index < _shuffledIndices.Length; index++)
            {
                _shuffledIndices[index] = index;
            }

            Shuffle();
        }

        public int DrawIndex()
        {
            if (_shuffledIndices.Length == 0)
            {
                return -1;
            }

            if (Cursor >= _shuffledIndices.Length)
            {
                Shuffle();
            }

            return _shuffledIndices[Cursor++];
        }

        public string DescribeOrder()
        {
            StringBuilder builder = new StringBuilder("[");
            for (int index = 0; index < _shuffledIndices.Length; index++)
            {
                if (index > 0)
                {
                    builder.Append(",");
                }

                int presetIndex = _shuffledIndices[index];
                builder.Append(presetIndex);
                builder.Append(":");
                builder.Append(presetIndex >= 0 && presetIndex < _presetIds.Length ? _presetIds[presetIndex] : "<null>");
            }

            builder.Append("]");
            return builder.ToString();
        }

        private void Shuffle()
        {
            for (int index = _shuffledIndices.Length - 1; index > 0; index--)
            {
                int swapIndex = UnityEngine.Random.Range(0, index + 1);
                int value = _shuffledIndices[index];
                _shuffledIndices[index] = _shuffledIndices[swapIndex];
                _shuffledIndices[swapIndex] = value;
            }

            Cursor = 0;
        }

        private static string[] NormalizeIds(string[] presetIds)
        {
            if (presetIds == null || presetIds.Length == 0)
            {
                return new string[0];
            }

            string[] normalizedIds = new string[presetIds.Length];
            for (int index = 0; index < presetIds.Length; index++)
            {
                normalizedIds[index] = presetIds[index] ?? string.Empty;
            }

            return normalizedIds;
        }

        private static bool HaveSameIds(string[] left, string[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int index = 0; index < left.Length; index++)
            {
                if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
