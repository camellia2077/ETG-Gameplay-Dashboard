// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections.Generic;
using System.Reflection;

namespace EtgGameplayDashboard
{
    /// <summary>
    /// Centralizes the small amount of private-field access required by ETG state restoration.
    /// </summary>
    internal static class PrivateFieldAccessor
    {
        private static readonly BindingFlags InstancePrivateFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        public static List<T> GetPrivateList<T>(object target, string fieldName)
        {
            FieldInfo field = FindField(target, fieldName);
            return field != null ? field.GetValue(target) as List<T> : null;
        }

        public static void SetPrivateList<T>(object target, string fieldName, List<T> value)
        {
            FieldInfo field = FindField(target, fieldName);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        public static int GetPrivateInt(object target, string fieldName)
        {
            FieldInfo field = FindField(target, fieldName);
            return field != null ? (int)field.GetValue(target) : -1;
        }

        public static void SetPrivateInt(object target, string fieldName, int value)
        {
            FieldInfo field = FindField(target, fieldName);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        public static void SetPrivateEnum<T>(object target, string fieldName, T value) where T : struct
        {
            FieldInfo field = FindField(target, fieldName);
            if (field != null && field.FieldType.IsEnum)
            {
                field.SetValue(target, value);
            }
        }

        public static float GetPrivateFloat(object target, string fieldName)
        {
            FieldInfo field = FindField(target, fieldName);
            return field != null ? (float)field.GetValue(target) : 0f;
        }

        public static void SetPrivateFloat(object target, string fieldName, float value)
        {
            FieldInfo field = FindField(target, fieldName);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        public static bool GetPrivateBool(object target, string fieldName)
        {
            FieldInfo field = FindField(target, fieldName);
            return field != null && (bool)field.GetValue(target);
        }

        public static T GetPrivateObject<T>(object target, string fieldName) where T : class
        {
            FieldInfo field = FindField(target, fieldName);
            return field != null ? field.GetValue(target) as T : null;
        }

        public static void SetPrivateBool(object target, string fieldName, bool value)
        {
            FieldInfo field = FindField(target, fieldName);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        private static FieldInfo FindField(object target, string fieldName)
        {
            System.Type currentType = target != null ? target.GetType() : null;
            while (currentType != null)
            {
                FieldInfo field = currentType.GetField(fieldName, InstancePrivateFlags);
                if (field != null)
                {
                    return field;
                }

                currentType = currentType.BaseType;
            }

            return null;
        }
    }
}
