using System;
using System.Collections;
using System.Reflection;

namespace RandomLoadout
{
    internal sealed partial class EtgPickupResolver
    {
        private const int FallbackScanLimit = 2048;

        private static object GetStaticMemberValue(Type type, string memberName)
        {
            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                return property.GetValue(null, null);
            }

            FieldInfo field = type.GetField(memberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(null);
            }

            return null;
        }

        private static object GetInstanceMemberValue(object target, string memberName)
        {
            if (target == null)
            {
                return null;
            }

            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                return property.GetValue(target, null);
            }

            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(target);
            }

            return null;
        }

        private static string GetPickupLabel(PickupObject pickup)
        {
            if ((object)pickup == null)
            {
                return "<null>";
            }

            if (pickup.encounterTrackable != null)
            {
                string modifiedDisplayName = pickup.encounterTrackable.GetModifiedDisplayName();
                if (!string.IsNullOrEmpty(modifiedDisplayName))
                {
                    return ResolveLocalizedLabelForCurrentUiLanguage(modifiedDisplayName);
                }

                if (pickup.encounterTrackable.journalData != null &&
                    !string.IsNullOrEmpty(pickup.encounterTrackable.journalData.PrimaryDisplayName))
                {
                    return ResolveLocalizedLabelForCurrentUiLanguage(pickup.encounterTrackable.journalData.PrimaryDisplayName);
                }
            }

            if (!string.IsNullOrEmpty(pickup.DisplayName))
            {
                return ResolveLocalizedLabelForCurrentUiLanguage(pickup.DisplayName);
            }

            return ResolveLocalizedLabelForCurrentUiLanguage(pickup.name);
        }

        private static string GetPickupLabelForGameLanguage(PickupObject pickup)
        {
            if ((object)pickup == null)
            {
                return "<null>";
            }

            if (pickup.encounterTrackable != null)
            {
                string modifiedDisplayName = pickup.encounterTrackable.GetModifiedDisplayName();
                if (!string.IsNullOrEmpty(modifiedDisplayName))
                {
                    return ResolveLocalizedLabelForGameLanguage(modifiedDisplayName);
                }

                if (pickup.encounterTrackable.journalData != null &&
                    !string.IsNullOrEmpty(pickup.encounterTrackable.journalData.PrimaryDisplayName))
                {
                    return ResolveLocalizedLabelForGameLanguage(pickup.encounterTrackable.journalData.PrimaryDisplayName);
                }
            }

            if (!string.IsNullOrEmpty(pickup.DisplayName))
            {
                return ResolveLocalizedLabelForGameLanguage(pickup.DisplayName);
            }

            return ResolveLocalizedLabelForGameLanguage(pickup.name);
        }

        private static string GetEnglishPickupLabel(PickupObject pickup)
        {
            if ((object)pickup == null)
            {
                return string.Empty;
            }

            string itemName = GetInstanceMemberValue(pickup, "itemName") as string;
            if (!string.IsNullOrEmpty(itemName))
            {
                return itemName;
            }

            if (pickup.encounterTrackable != null && pickup.encounterTrackable.journalData != null)
            {
                string englishPrimaryDisplayName = ResolveLocalizedLabelFromBackupTables(pickup.encounterTrackable.journalData.PrimaryDisplayName);
                if (!string.IsNullOrEmpty(englishPrimaryDisplayName) &&
                    !string.Equals(englishPrimaryDisplayName, pickup.encounterTrackable.journalData.PrimaryDisplayName, StringComparison.Ordinal))
                {
                    return englishPrimaryDisplayName;
                }
            }

            return ResolveLocalizedLabelFromBackupTables(pickup.name);
        }

        private static string ResolveLocalizedLabelForCurrentUiLanguage(string rawLabel)
        {
            return ResolveLocalizedLabelForLanguage(rawLabel, GuiText.CurrentLanguageCode);
        }

        private static string ResolveLocalizedLabelForGameLanguage(string rawLabel)
        {
            return ResolveLocalizedLabelForLanguage(rawLabel, GuiText.GameLanguageCode);
        }

        private static string ResolveLocalizedLabelForLanguage(string rawLabel, string languageCode)
        {
            if (string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase))
            {
                string englishLabel = ResolveLocalizedLabelFromBackupTables(rawLabel);
                if (!string.IsNullOrEmpty(englishLabel) && !string.Equals(englishLabel, rawLabel, StringComparison.Ordinal))
                {
                    return englishLabel;
                }
            }

            return ResolveLocalizedLabel(rawLabel);
        }

        private static string ResolveLocalizedLabel(string rawLabel)
        {
            if (string.IsNullOrEmpty(rawLabel))
            {
                return string.Empty;
            }

            if (!rawLabel.StartsWith("#", StringComparison.Ordinal))
            {
                return rawLabel;
            }

            string localized = StringTableManager.GetString(rawLabel);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawLabel, StringComparison.Ordinal))
            {
                return localized;
            }

            localized = StringTableManager.GetItemsString(rawLabel);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawLabel, StringComparison.Ordinal))
            {
                return localized;
            }

            return rawLabel;
        }

        private static string ResolveLocalizedLabelFromBackupTables(string rawLabel)
        {
            if (string.IsNullOrEmpty(rawLabel) || !rawLabel.StartsWith("#", StringComparison.Ordinal))
            {
                return rawLabel ?? string.Empty;
            }

            string localized = ResolveLocalizedLabelFromTable("m_backupCoreTable", rawLabel);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawLabel, StringComparison.Ordinal))
            {
                return localized;
            }

            localized = ResolveLocalizedLabelFromTable("m_backupItemsTable", rawLabel);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawLabel, StringComparison.Ordinal))
            {
                return localized;
            }

            return rawLabel;
        }

        private static string ResolveLocalizedLabelFromTable(string tableFieldName, string rawLabel)
        {
            IDictionary table = GetStaticMemberValue(typeof(StringTableManager), tableFieldName) as IDictionary;
            if (table == null || !table.Contains(rawLabel))
            {
                return string.Empty;
            }

            object entry = table[rawLabel];
            if (entry == null)
            {
                return string.Empty;
            }

            MethodInfo getExactString = entry.GetType().GetMethod("GetExactString", BindingFlags.Instance | BindingFlags.Public);
            if (getExactString == null)
            {
                return string.Empty;
            }

            object resolved = getExactString.Invoke(entry, new object[] { 0 });
            return resolved as string ?? string.Empty;
        }
    }
}
