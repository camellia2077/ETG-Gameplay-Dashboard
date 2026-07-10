// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;

namespace RandomLoadout
{
    internal static class GuiText
    {
        private const string EnglishLanguageCode = "en";
        private const string SimplifiedChineseLanguageCode = "zh-CN";
        private static readonly Dictionary<string, string> EmptyTable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, string> _englishTable = EmptyTable;
        private static Dictionary<string, string> _simplifiedChineseTable = EmptyTable;
        private static string _languageOverride = string.Empty;

        public static string CurrentLanguageCode
        {
            get { return DetectLanguageCode(); }
        }

        public static string GameLanguageCode
        {
            get { return DetectLanguageCode(ignoreOverride: true); }
        }

        public static void Initialize(string configDirectory)
        {
            _englishTable = LoadTable(DashboardFileLayout.GetEnglishLocalizationFilePath(configDirectory));
            _simplifiedChineseTable = LoadTable(DashboardFileLayout.GetSimplifiedChineseLocalizationFilePath(configDirectory));
        }

        public static void SetLanguageOverride(string languageCode)
        {
            _languageOverride = NormalizeLanguageOverride(languageCode);
        }

        public static string NormalizeLanguageOverride(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                return "auto";
            }

            string normalized = languageCode.Trim();
            if (string.Equals(normalized, "auto", StringComparison.OrdinalIgnoreCase))
            {
                return "auto";
            }

            return NormalizeLanguageCode(normalized);
        }

        public static string Get(string key)
        {
            return Get(key, new object[0]);
        }

        public static string Get(string key, params object[] args)
        {
            return Format(Resolve(key, false), args);
        }

        public static string GetEnglish(string key)
        {
            return GetEnglish(key, new object[0]);
        }

        public static string GetEnglish(string key, params object[] args)
        {
            return Format(Resolve(key, true), args);
        }

        public static string GetCharacterLabel(string characterLabel)
        {
            return GetCharacterLabelInternal(characterLabel, false);
        }

        public static string GetEnglishCharacterLabel(string characterLabel)
        {
            return GetCharacterLabelInternal(characterLabel, true);
        }

        public static string GetCategoryLabel(RandomLoadout.Core.PickupCategory category)
        {
            return GetCategoryLabelInternal(category, false);
        }

        public static string GetEnglishCategoryLabel(RandomLoadout.Core.PickupCategory category)
        {
            return GetCategoryLabelInternal(category, true);
        }

        private static string GetCharacterLabelInternal(string characterLabel, bool forceEnglish)
        {
            string key = "label.character." + (characterLabel ?? string.Empty).ToLowerInvariant();
            string fallback = !string.IsNullOrEmpty(characterLabel) ? characterLabel : string.Empty;
            return ResolveWithFallback(key, fallback, forceEnglish);
        }

        private static string GetCategoryLabelInternal(RandomLoadout.Core.PickupCategory category, bool forceEnglish)
        {
            switch (category)
            {
                case RandomLoadout.Core.PickupCategory.Gun:
                    return ResolveWithFallback("label.category.gun", "Gun", forceEnglish);
                case RandomLoadout.Core.PickupCategory.Passive:
                    return ResolveWithFallback("label.category.passive", "Passive", forceEnglish);
                case RandomLoadout.Core.PickupCategory.Active:
                    return ResolveWithFallback("label.category.active", "Active", forceEnglish);
                default:
                    return ResolveWithFallback("label.category.unknown", "Unknown", forceEnglish);
            }
        }

        private static string Resolve(string key, bool forceEnglish)
        {
            return ResolveWithFallback(key, key, forceEnglish);
        }

        private static string ResolveWithFallback(string key, string fallback, bool forceEnglish)
        {
            if (string.IsNullOrEmpty(key))
            {
                return fallback ?? string.Empty;
            }

            string resolved;
            if (!forceEnglish && string.Equals(CurrentLanguageCode, SimplifiedChineseLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                if (_simplifiedChineseTable.TryGetValue(key, out resolved) && !string.IsNullOrEmpty(resolved))
                {
                    return resolved;
                }
            }

            if (_englishTable.TryGetValue(key, out resolved) && !string.IsNullOrEmpty(resolved))
            {
                return resolved;
            }

            return fallback ?? key;
        }

        private static string Format(string template, object[] args)
        {
            if (string.IsNullOrEmpty(template) || args == null || args.Length == 0)
            {
                return template ?? string.Empty;
            }

            try
            {
                return string.Format(CultureInfo.InvariantCulture, template, args);
            }
            catch
            {
                return template;
            }
        }

        private static Dictionary<string, string> LoadTable(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            string rawText = Json5TextNormalizer.Normalize(File.ReadAllText(filePath, Encoding.UTF8));
            return ParseFlatStringObject(rawText);
        }

        private static Dictionary<string, string> ParseFlatStringObject(string rawJson)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(rawJson))
            {
                return values;
            }

            JObject root = JObject.Parse(rawJson);
            foreach (JProperty property in root.Properties())
            {
                string key = property.Name;
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                JToken token = property.Value;
                if (token != null && token.Type == JTokenType.String)
                {
                    values[key] = token.Value<string>();
                }
            }

            return values;
        }

        private static string DetectLanguageCode()
        {
            return DetectLanguageCode(false);
        }

        private static string DetectLanguageCode(bool ignoreOverride)
        {
            if (!ignoreOverride &&
                !string.IsNullOrEmpty(_languageOverride) &&
                !string.Equals(_languageOverride, "auto", StringComparison.OrdinalIgnoreCase))
            {
                return NormalizeLanguageCode(_languageOverride);
            }

            string token = ReadLanguageToken();
            if (string.IsNullOrEmpty(token))
            {
                return EnglishLanguageCode;
            }

            return NormalizeLanguageCode(token);
        }

        private static string NormalizeLanguageCode(string token)
        {
            string normalized = token.Trim().ToLowerInvariant();
            if (normalized.Contains("zh") ||
                normalized.Contains("chinese") ||
                normalized.Contains("simplified") ||
                normalized.Contains("china") ||
                normalized.Contains("cn") ||
                normalized.Contains("中文") ||
                normalized.Contains("简体"))
            {
                return SimplifiedChineseLanguageCode;
            }

            return EnglishLanguageCode;
        }

        private static string ReadLanguageToken()
        {
            try
            {
                if ((object)GameManager.Instance == null)
                {
                    return string.Empty;
                }

                object options = ReadMemberValue(GameManager.Instance, "Options");
                if (options != null)
                {
                    string token = FindLanguageMemberValue(options);
                    if (!string.IsNullOrEmpty(token))
                    {
                        return token;
                    }
                }

                return FindLanguageMemberValue(GameManager.Instance);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string FindLanguageMemberValue(object target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            string[] preferredNames =
            {
                "CurrentLanguage",
                "Language",
                "CurrentLang",
                "m_currentLanguage",
                "m_language",
            };

            for (int i = 0; i < preferredNames.Length; i++)
            {
                object value = ReadMemberValue(target, preferredNames[i]);
                string resolved = ConvertLanguageValueToString(value);
                if (!string.IsNullOrEmpty(resolved))
                {
                    return resolved;
                }
            }

            MemberInfo[] members = target.GetType().GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < members.Length; i++)
            {
                string memberName = members[i].Name;
                if (string.IsNullOrEmpty(memberName) ||
                    memberName.IndexOf("lang", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                object value = ReadMemberValue(target, memberName);
                string resolved = ConvertLanguageValueToString(value);
                if (!string.IsNullOrEmpty(resolved))
                {
                    return resolved;
                }
            }

            return string.Empty;
        }

        private static object ReadMemberValue(object target, string memberName)
        {
            if (target == null || string.IsNullOrEmpty(memberName))
            {
                return null;
            }

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            PropertyInfo property = target.GetType().GetProperty(memberName, flags);
            if (property != null && property.GetIndexParameters().Length == 0)
            {
                return property.GetValue(target, null);
            }

            FieldInfo field = target.GetType().GetField(memberName, flags);
            if (field != null)
            {
                return field.GetValue(target);
            }

            return null;
        }

        private static string ConvertLanguageValueToString(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            string text = value as string;
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }

            return value.ToString();
        }
    }
}
