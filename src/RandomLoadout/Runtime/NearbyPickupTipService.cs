using Dungeonator;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed class NearbyPickupTipService
    {
        private const float MaxTipDistance = 2.5f;
        private const float RefreshIntervalSeconds = 0.15f;

        private readonly PickupGameplayRegistry _gameplayRegistry;
        private float _nextRefreshAt;

        public NearbyPickupTipService(PickupGameplayRegistry gameplayRegistry)
        {
            _gameplayRegistry = gameplayRegistry ?? PickupGameplayRegistry.Empty;
        }

        public bool HasVisibleTip
        {
            get { return CurrentPickupId > 0; }
        }

        public int CurrentPickupId { get; private set; }

        public string CurrentDisplayName { get; private set; }

        public void Update(PlayerController player)
        {
            if (Time.unscaledTime < _nextRefreshAt)
            {
                return;
            }

            _nextRefreshAt = Time.unscaledTime + RefreshIntervalSeconds;
            Refresh(player);
        }

        private void Refresh(PlayerController player)
        {
            if (player == null || player.CurrentRoom == null || _gameplayRegistry.Count == 0)
            {
                CurrentPickupId = 0;
                CurrentDisplayName = string.Empty;
                return;
            }

            int nearestPickupId = 0;
            string nearestDisplayName = string.Empty;
            float nearestDistance = MaxTipDistance;
            DebrisObject[] debrisObjects = Object.FindObjectsOfType<DebrisObject>();
            for (int i = 0; i < debrisObjects.Length; i++)
            {
                DebrisObject debris = debrisObjects[i];
                if (!IsValidNearbyPickup(debris, player))
                {
                    continue;
                }

                PickupObject pickup = debris.GetComponentInChildren<PickupObject>();
                if (pickup == null)
                {
                    continue;
                }

                PickupGameplayEntry gameplayEntry;
                bool hasGameplayInfo = _gameplayRegistry.TryGetEntry(pickup.PickupObjectId, out gameplayEntry);
                if (!hasGameplayInfo)
                {
                    continue;
                }

                float distance = Vector2.Distance(player.CenterPosition, GetPickupWorldCenter(debris, pickup));
                if (distance > nearestDistance)
                {
                    continue;
                }

                nearestDistance = distance;
                nearestPickupId = pickup.PickupObjectId;
                nearestDisplayName = GetPickupLabelForGameLanguage(pickup);
            }

            CurrentPickupId = nearestPickupId;
            CurrentDisplayName = nearestDisplayName;
        }

        private static bool IsValidNearbyPickup(DebrisObject debris, PlayerController player)
        {
            if (debris == null ||
                !debris.IsPickupObject ||
                debris.sprite == null ||
                !debris.gameObject.activeInHierarchy)
            {
                return false;
            }

            GameManager gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.Dungeon == null || gameManager.Dungeon.data == null)
            {
                return false;
            }

            RoomHandler debrisRoom = gameManager.Dungeon.data.GetAbsoluteRoomFromPosition(
                debris.transform.position.IntXY(VectorConversions.Floor));
            return debrisRoom == player.CurrentRoom;
        }

        private static Vector2 GetPickupWorldCenter(DebrisObject debris, PickupObject pickup)
        {
            if (debris != null && debris.sprite != null)
            {
                return debris.sprite.WorldCenter;
            }

            if (pickup != null && pickup.sprite != null)
            {
                return pickup.sprite.WorldCenter;
            }

            return pickup != null ? pickup.transform.position.XY() : Vector2.zero;
        }

        private static string GetPickupLabelForGameLanguage(PickupObject pickup)
        {
            if ((object)pickup == null)
            {
                return string.Empty;
            }

            if (pickup.encounterTrackable != null)
            {
                string modifiedDisplayName = pickup.encounterTrackable.GetModifiedDisplayName();
                if (!string.IsNullOrEmpty(modifiedDisplayName))
                {
                    string resolvedModifiedDisplayName = ResolveLocalizedLabelForGameLanguage(modifiedDisplayName);
                    if (!string.IsNullOrEmpty(resolvedModifiedDisplayName))
                    {
                        return resolvedModifiedDisplayName;
                    }
                }

                if (pickup.encounterTrackable.journalData != null &&
                    !string.IsNullOrEmpty(pickup.encounterTrackable.journalData.PrimaryDisplayName))
                {
                    string resolvedPrimaryDisplayName = ResolveLocalizedLabelForGameLanguage(pickup.encounterTrackable.journalData.PrimaryDisplayName);
                    if (!string.IsNullOrEmpty(resolvedPrimaryDisplayName))
                    {
                        return resolvedPrimaryDisplayName;
                    }
                }
            }

            if (!string.IsNullOrEmpty(pickup.DisplayName))
            {
                return ResolveLocalizedLabelForGameLanguage(pickup.DisplayName);
            }

            return ResolveLocalizedLabelForGameLanguage(pickup.name);
        }

        private static string ResolveLocalizedLabelForGameLanguage(string rawLabel)
        {
            if (string.IsNullOrEmpty(rawLabel))
            {
                return string.Empty;
            }

            if (string.Equals(GuiText.GameLanguageCode, "en", System.StringComparison.OrdinalIgnoreCase))
            {
                return ResolveLocalizedLabelFromEnglishBackupTables(rawLabel);
            }

            return ResolveLocalizedLabel(rawLabel);
        }

        private static string ResolveLocalizedLabel(string rawLabel)
        {
            if (string.IsNullOrEmpty(rawLabel))
            {
                return string.Empty;
            }

            if (!rawLabel.StartsWith("#", System.StringComparison.Ordinal))
            {
                return rawLabel;
            }

            string localized = StringTableManager.GetString(rawLabel);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawLabel, System.StringComparison.Ordinal))
            {
                return localized;
            }

            localized = StringTableManager.GetItemsString(rawLabel);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawLabel, System.StringComparison.Ordinal))
            {
                return localized;
            }

            return rawLabel;
        }

        private static string ResolveLocalizedLabelFromEnglishBackupTables(string rawLabel)
        {
            if (string.IsNullOrEmpty(rawLabel) || !rawLabel.StartsWith("#", System.StringComparison.Ordinal))
            {
                return rawLabel ?? string.Empty;
            }

            string localized = ResolveLocalizedLabelFromBackupTable("m_backupCoreTable", rawLabel);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawLabel, System.StringComparison.Ordinal))
            {
                return localized;
            }

            localized = ResolveLocalizedLabelFromBackupTable("m_backupItemsTable", rawLabel);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawLabel, System.StringComparison.Ordinal))
            {
                return localized;
            }

            return rawLabel;
        }

        private static string ResolveLocalizedLabelFromBackupTable(string tableFieldName, string rawLabel)
        {
            System.Collections.IDictionary table = GetStaticMemberValue(typeof(StringTableManager), tableFieldName) as System.Collections.IDictionary;
            if (table == null || !table.Contains(rawLabel))
            {
                return string.Empty;
            }

            object entry = table[rawLabel];
            if (entry == null)
            {
                return string.Empty;
            }

            System.Reflection.MethodInfo getExactString = entry.GetType().GetMethod(
                "GetExactString",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (getExactString == null)
            {
                return string.Empty;
            }

            object resolved = getExactString.Invoke(entry, new object[] { 0 });
            return resolved as string ?? string.Empty;
        }

        private static object GetStaticMemberValue(System.Type type, string memberName)
        {
            System.Reflection.PropertyInfo property = type.GetProperty(
                memberName,
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (property != null)
            {
                return property.GetValue(null, null);
            }

            System.Reflection.FieldInfo field = type.GetField(
                memberName,
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(null);
            }

            return null;
        }
    }
}
