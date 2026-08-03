// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed class PickupShortcutRegistry
    {
        private readonly Dictionary<string, KeyCode> _keysByTargetId;

        private PickupShortcutRegistry(Dictionary<string, KeyCode> keysByTargetId)
        {
            _keysByTargetId = keysByTargetId ?? new Dictionary<string, KeyCode>(StringComparer.Ordinal);
        }

        public static PickupShortcutRegistry Parse(string serialized)
        {
            Dictionary<string, KeyCode> keysByTargetId = new Dictionary<string, KeyCode>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(serialized))
            {
                return new PickupShortcutRegistry(keysByTargetId);
            }

            string[] bindings = serialized.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < bindings.Length; index++)
            {
                string[] parts = bindings[index].Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    continue;
                }

                string targetId = parts[0].Trim();
                if (string.IsNullOrEmpty(targetId))
                {
                    continue;
                }

                KeyCode keyCode;
                if (!TryParseKeyCode(parts[1].Trim(), out keyCode) || !IsKeyboardKey(keyCode))
                {
                    continue;
                }

                string previousTargetId;
                if (TryFindTargetId(keysByTargetId, keyCode, out previousTargetId) &&
                    !string.Equals(previousTargetId, targetId, StringComparison.Ordinal))
                {
                    keysByTargetId.Remove(previousTargetId);
                }

                keysByTargetId[targetId] = keyCode;
            }

            return new PickupShortcutRegistry(keysByTargetId);
        }

        public bool TryGetKey(string targetId, out KeyCode keyCode)
        {
            keyCode = KeyCode.None;
            return !string.IsNullOrEmpty(targetId) && _keysByTargetId.TryGetValue(targetId, out keyCode);
        }

        public bool TryGetTargetId(KeyCode keyCode, out string targetId)
        {
            foreach (KeyValuePair<string, KeyCode> binding in _keysByTargetId)
            {
                if (binding.Value == keyCode)
                {
                    targetId = binding.Key;
                    return true;
                }
            }

            targetId = string.Empty;
            return false;
        }

        public KeyValuePair<string, KeyCode>[] GetBindings()
        {
            KeyValuePair<string, KeyCode>[] bindings = new KeyValuePair<string, KeyCode>[_keysByTargetId.Count];
            int writeIndex = 0;
            foreach (KeyValuePair<string, KeyCode> binding in _keysByTargetId)
            {
                bindings[writeIndex++] = binding;
            }

            return bindings;
        }

        public bool Set(string targetId, KeyCode keyCode, out string replacedTargetId)
        {
            replacedTargetId = string.Empty;
            if (string.IsNullOrEmpty(targetId) || !IsKeyboardKey(keyCode))
            {
                return false;
            }

            string existingTargetId;
            if (TryGetTargetId(keyCode, out existingTargetId) &&
                !string.Equals(existingTargetId, targetId, StringComparison.Ordinal))
            {
                replacedTargetId = existingTargetId;
                _keysByTargetId.Remove(existingTargetId);
            }

            _keysByTargetId[targetId] = keyCode;
            return true;
        }

        public bool Clear(string targetId)
        {
            return !string.IsNullOrEmpty(targetId) && _keysByTargetId.Remove(targetId);
        }

        public string Serialize()
        {
            List<string> targetIds = new List<string>(_keysByTargetId.Keys);
            targetIds.Sort(StringComparer.Ordinal);
            List<string> bindings = new List<string>(targetIds.Count);
            for (int index = 0; index < targetIds.Count; index++)
            {
                string targetId = targetIds[index];
                bindings.Add(targetId + "=" + _keysByTargetId[targetId]);
            }

            return string.Join(",", bindings.ToArray());
        }

        private static bool TryParseKeyCode(string value, out KeyCode keyCode)
        {
            keyCode = KeyCode.None;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            try
            {
                object parsed = Enum.Parse(typeof(KeyCode), value, true);
                if (!(parsed is KeyCode) || !Enum.IsDefined(typeof(KeyCode), parsed))
                {
                    return false;
                }

                keyCode = (KeyCode)parsed;
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static bool TryFindTargetId(Dictionary<string, KeyCode> keysByTargetId, KeyCode keyCode, out string targetId)
        {
            foreach (KeyValuePair<string, KeyCode> binding in keysByTargetId)
            {
                if (binding.Value == keyCode)
                {
                    targetId = binding.Key;
                    return true;
                }
            }

            targetId = string.Empty;
            return false;
        }

        private static bool IsKeyboardKey(KeyCode keyCode)
        {
            return keyCode != KeyCode.None &&
                keyCode < KeyCode.Mouse0 &&
                keyCode < KeyCode.JoystickButton0;
        }
    }
}
