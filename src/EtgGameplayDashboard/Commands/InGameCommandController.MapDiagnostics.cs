// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using Dungeonator;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private void LogMapRevealTransitionDiagnostics(string phase)
        {
            if (!ShouldLogMapTeleportVerbose() || (!_revealMapEveryFloor && !_revealMapEnabled))
            {
                return;
            }

            int currentFrame = Time.frameCount;
            string currentSceneName = GetCurrentMapFeatureActivationKey();
            if (!string.Equals(_mapRevealDiagnosticSceneName, currentSceneName, System.StringComparison.Ordinal))
            {
                _mapRevealDiagnosticSceneName = currentSceneName;
                _mapRevealDiagnosticSceneStartFrame = currentFrame;
            }

            int relativeFrame = _mapRevealDiagnosticSceneStartFrame >= 0
                ? currentFrame - _mapRevealDiagnosticSceneStartFrame
                : -1;
            bool isSampleFrame = relativeFrame >= 0 &&
                (relativeFrame <= 30 ||
                 relativeFrame == 60 ||
                 relativeFrame == 90 ||
                 relativeFrame == 120 ||
                 relativeFrame == 180 ||
                 relativeFrame == 300);
            bool isImportantPhase = phase.StartsWith("auto_reveal", System.StringComparison.Ordinal) ||
                phase.StartsWith("floor_scene_changed", System.StringComparison.Ordinal);
            if (!isSampleFrame && !isImportantPhase)
            {
                return;
            }

            GameManager gameManager = GameManager.Instance;
            PlayerController player = gameManager != null ? gameManager.PrimaryPlayer : null;
            RoomHandler currentRoom = player != null ? player.CurrentRoom : null;
            Dungeonator.Dungeon dungeon = gameManager != null ? gameManager.Dungeon : null;
            Minimap minimap = Minimap.HasInstance ? Minimap.Instance : null;
            int roomCount = dungeon != null && dungeon.data != null && dungeon.data.rooms != null
                ? dungeon.data.rooms.Count
                : -1;
            int minimapTeleportEntryCount = minimap != null && minimap.RoomToTeleportMap != null
                ? minimap.RoomToTeleportMap.Count
                : -1;
            string playerPosition = player != null && player.transform != null
                ? player.transform.position.ToString("F3")
                : "<none>";
            string currentRoomLabel = currentRoom != null ? DescribeMapDirectTeleportRoom(currentRoom) : "<none>";
            string currentRoomState = currentRoom != null
                ? "CanFrom=" + currentRoom.CanTeleportFromRoom() +
                  ",CanTo=" + currentRoom.CanTeleportToRoom() +
                  ",TeleportersActive=" + currentRoom.TeleportersActive +
                  ",Revealed=" + currentRoom.RevealedOnMap
                : "CanFrom=<unknown>,CanTo=<unknown>,TeleportersActive=<unknown>,Revealed=<unknown>";
            string playerInputState = player != null ? player.CurrentInputState.ToString() : "<none>";
            string elevatorObjectState = DescribeElevatorTransitionObjects();

            LogGamepadShortcutState(
                "Map reveal transition diagnostic. " +
                "Phase=" + phase +
                ", Frame=" + currentFrame +
                ", RelativeFloorFrame=" + relativeFrame +
                ", Time=" + Time.time.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                ", Realtime=" + Time.realtimeSinceStartup.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                ", UnityScene=" + GetLoadedUnitySceneName() +
                ", LastLoadedDungeonScene=" + GetLastLoadedDungeonSceneName(gameManager) +
                ", ActivationKey=" + currentSceneName +
                ", RevealMapEnabled=" + _revealMapEnabled +
                ", RevealMapEveryFloor=" + _revealMapEveryFloor +
                ", AutoRevealScene=" + _mapFeatureRuntimeCoordinator.AutomaticRevealMapSceneName +
                ", PlayerReady=" + ((object)player != null) +
                ", PlayerActive=" + (player != null ? player.gameObject.activeInHierarchy.ToString() : "<unknown>") +
                ", PlayerInputOverridden=" + (player != null ? player.IsInputOverridden.ToString() : "<unknown>") +
                ", PlayerInputState=" + playerInputState +
                ", PlayerPosition=" + playerPosition +
                ", CurrentRoom=" + currentRoomLabel +
                ", CurrentRoomState=" + currentRoomState +
                ", DungeonReady=" + ((object)dungeon != null && dungeon.data != null) +
                ", DungeonAllRoomsVisited=" + (dungeon != null ? dungeon.AllRoomsVisited.ToString() : "<unknown>") +
                ", DungeonRoomCount=" + roomCount +
                ", MinimapHasInstance=" + Minimap.HasInstance +
                ", MinimapTeleportEntries=" + minimapTeleportEntryCount +
                ", PlayerEverHadMap=" + (player != null ? player.EverHadMap.ToString() : "<unknown>") +
                ", ElevatorTransitionObjects=" + elevatorObjectState +
                ".");
        }

        private static string DescribeElevatorTransitionObjects()
        {
            try
            {
                Component[] components = UnityEngine.Object.FindObjectsOfType<Component>();
                if (components == null || components.Length == 0)
                {
                    return "<none>";
                }

                System.Collections.Generic.List<string> matches = new System.Collections.Generic.List<string>();
                for (int index = 0; index < components.Length; index++)
                {
                    Component component = components[index];
                    if ((object)component == null || (object)component.gameObject == null)
                    {
                        continue;
                    }

                    string objectName = component.gameObject.name ?? string.Empty;
                    string componentType = component.GetType().FullName ?? component.GetType().Name;
                    string searchableText = (objectName + " " + componentType).ToLowerInvariant();
                    if (searchableText.IndexOf("elevator", System.StringComparison.Ordinal) < 0 &&
                        searchableText.IndexOf("stair", System.StringComparison.Ordinal) < 0 &&
                        searchableText.IndexOf("entrance", System.StringComparison.Ordinal) < 0)
                    {
                        continue;
                    }

                    string fsmState = DescribeFsmState(component);
                    string match = objectName +
                        "[type=" + component.GetType().Name +
                        ",active=" + component.gameObject.activeInHierarchy +
                        (fsmState != "<none>" ? ",fsm=" + fsmState : string.Empty) +
                        "]";
                    if (!matches.Contains(match))
                    {
                        matches.Add(match);
                    }

                    if (matches.Count >= 24)
                    {
                        break;
                    }
                }

                return matches.Count > 0 ? string.Join(";", matches.ToArray()) : "<none>";
            }
            catch (System.Exception exception)
            {
                return "<scan-failed:" + exception.GetType().Name + ">";
            }
        }

        private static string DescribeFsmState(Component component)
        {
            if ((object)component == null)
            {
                return "<none>";
            }

            System.Type componentType = component.GetType();
            string typeName = componentType.FullName ?? componentType.Name;
            if (typeName.IndexOf("PlayMakerFSM", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                typeName.IndexOf("FSM", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                return "<none>";
            }

            try
            {
                System.Reflection.PropertyInfo activeStateProperty = componentType.GetProperty("ActiveStateName");
                if (activeStateProperty != null)
                {
                    object activeState = activeStateProperty.GetValue(component, null);
                    if (activeState != null)
                    {
                        return activeState.ToString();
                    }
                }

                System.Reflection.FieldInfo fsmField = componentType.GetField("Fsm");
                object fsm = fsmField != null ? fsmField.GetValue(component) : null;
                if (fsm != null)
                {
                    System.Reflection.PropertyInfo nestedStateProperty = fsm.GetType().GetProperty("ActiveStateName");
                    object nestedState = nestedStateProperty != null ? nestedStateProperty.GetValue(fsm, null) : null;
                    if (nestedState != null)
                    {
                        return nestedState.ToString();
                    }
                }

                return "<fsm-state-unavailable>";
            }
            catch (System.Exception exception)
            {
                return "<fsm-state-failed:" + exception.GetType().Name + ">";
            }
        }

        private void ClearMapFeatureActivationState()
        {
            _mapFeatureRuntimeCoordinator.ClearActivationState();
            ResetMapDirectTeleportDiagnostics();
        }

        private void ResetMapDirectTeleportDiagnostics()
        {
            _lastMapDirectTeleportRoomKey = string.Empty;
            _nextMapDirectTeleportDebugLogAt = 0f;
        }

        private bool IsRevealMapActive()
        {
            return _mapFeatureRuntimeCoordinator.IsRevealMapActive();
        }

        private bool IsRevealMapEnabled()
        {
            return _revealMapEnabled;
        }

        private void MarkRevealMapActivatedForCurrentScene()
        {
            _mapFeatureRuntimeCoordinator.MarkRevealMapActivatedForCurrentScene();
        }

        private bool IsMapDirectTeleportActive()
        {
            return _mapFeatureRuntimeCoordinator.IsMapDirectTeleportActive();
        }

        private void MarkMapDirectTeleportActivatedForCurrentScene()
        {
            _mapFeatureRuntimeCoordinator.MarkMapDirectTeleportActivatedForCurrentScene();
            ResetMapDirectTeleportDiagnostics();
        }

        private static string GetCurrentMapFeatureActivationKey()
        {
            GameManager gameManager = GameManager.Instance;
            string dungeonSceneName = GetLastLoadedDungeonSceneName(gameManager);
            if (!string.IsNullOrEmpty(dungeonSceneName) &&
                !string.Equals(dungeonSceneName, "<unknown>", System.StringComparison.Ordinal) &&
                !string.Equals(dungeonSceneName, "<no_game_manager>", System.StringComparison.Ordinal) &&
                !dungeonSceneName.StartsWith("<exception:", System.StringComparison.Ordinal))
            {
                return dungeonSceneName;
            }

            return GetLoadedUnitySceneName();
        }

        private void LogMapDirectTeleportRoomTransitionIfNeeded()
        {
            if (!IsMapDirectTeleportActive() || !ShouldLogMapTeleportVerbose())
            {
                return;
            }

            GameManager gameManager = GameManager.Instance;
            PlayerController player = gameManager != null ? gameManager.PrimaryPlayer : null;
            RoomHandler currentRoom = player != null ? player.CurrentRoom : null;
            string currentRoomKey = GetMapDirectTeleportRoomKey(currentRoom);
            if (string.Equals(currentRoomKey, _lastMapDirectTeleportRoomKey, System.StringComparison.Ordinal))
            {
                return;
            }

            _lastMapDirectTeleportRoomKey = currentRoomKey;
            Minimap minimap = Minimap.HasInstance ? Minimap.Instance : null;
            int minimapTeleportEntryCount = minimap != null && minimap.RoomToTeleportMap != null ? minimap.RoomToTeleportMap.Count : -1;
            LogGamepadShortcutState(
                "Map direct teleport room transition. " +
                "UnityScene=" +
                GetLoadedUnitySceneName() +
                ", LastLoadedDungeonScene=" +
                GetLastLoadedDungeonSceneName(gameManager) +
                ", CurrentRoom=" +
                DescribeMapDirectTeleportRoom(currentRoom) +
                ", CurrentRoomCanTeleportFrom=" +
                (currentRoom != null ? currentRoom.CanTeleportFromRoom().ToString() : "<unknown>") +
                ", CurrentRoomCanTeleportTo=" +
                (currentRoom != null ? currentRoom.CanTeleportToRoom().ToString() : "<unknown>") +
                ", CurrentRoomTeleportersActive=" +
                (currentRoom != null ? currentRoom.TeleportersActive.ToString() : "<unknown>") +
                ", CurrentRoomRevealedOnMap=" +
                (currentRoom != null ? currentRoom.RevealedOnMap.ToString() : "<unknown>") +
                ", CurrentRoomMinimapTeleportRegistered=" +
                IsMapDirectTeleportRoomRegistered(minimap, currentRoom) +
                ", MinimapTeleportEntries=" +
                minimapTeleportEntryCount +
                ", ConnectedRooms=[" +
                DescribeConnectedMapDirectTeleportRooms(currentRoom, minimap) +
                "].");
        }

        private void LogMapDirectTeleportRuntimeStateIfNeeded()
        {
            if (!IsMapDirectTeleportActive() || !ShouldLogMapTeleportVerbose() || Time.unscaledTime < _nextMapDirectTeleportDebugLogAt)
            {
                return;
            }

            _nextMapDirectTeleportDebugLogAt = Time.unscaledTime + 1f;
            GameManager gameManager = GameManager.Instance;
            PlayerController player = gameManager != null ? gameManager.PrimaryPlayer : null;
            RoomHandler currentRoom = player != null ? player.CurrentRoom : null;
            Minimap minimap = Minimap.HasInstance ? Minimap.Instance : null;
            string currentRoomLabel = currentRoom != null ? DescribeMapDirectTeleportRoom(currentRoom) : "<none>";
            string currentRoomCanTeleportFrom = currentRoom != null ? currentRoom.CanTeleportFromRoom().ToString() : "<unknown>";
            string currentRoomCanTeleportTo = currentRoom != null ? currentRoom.CanTeleportToRoom().ToString() : "<unknown>";
            string currentRoomTeleportersActive = currentRoom != null ? currentRoom.TeleportersActive.ToString() : "<unknown>";
            int minimapTeleportEntryCount = minimap != null && minimap.RoomToTeleportMap != null ? minimap.RoomToTeleportMap.Count : -1;
            string lastLoadedDungeonScene = GetLastLoadedDungeonSceneName(gameManager);
            LogGamepadShortcutState(
                "Map direct teleport runtime sample. " +
                "UnityScene=" +
                GetLoadedUnitySceneName() +
                ", LastLoadedDungeonScene=" +
                lastLoadedDungeonScene +
                ", ActiveSceneBinding=" +
                _mapFeatureRuntimeCoordinator.GetMapDirectTeleportActivationSceneName() +
                ", MinimapHasInstance=" +
                Minimap.HasInstance +
                ", MinimapTeleportEntries=" +
                minimapTeleportEntryCount +
                ", CurrentRoom=" +
                currentRoomLabel +
                ", CurrentRoomCanTeleportFrom=" +
                currentRoomCanTeleportFrom +
                ", CurrentRoomCanTeleportTo=" +
                currentRoomCanTeleportTo +
                ", CurrentRoomTeleportersActive=" +
                currentRoomTeleportersActive +
                ", PlayerReady=" +
                ((object)player != null) +
                ", DungeonReady=" +
                ((object)gameManager != null && (object)gameManager.Dungeon != null && gameManager.Dungeon.data != null) +
                ".");
        }

        private static string DescribeMapDirectTeleportRoom(RoomHandler room)
        {
            if ((object)room == null)
            {
                return "<null>";
            }

            string roomName = room.GetRoomName();
            IntVector2 basePosition = room.area != null ? room.area.basePosition : IntVector2.Zero;
            string category = room.area != null ? room.area.PrototypeRoomCategory.ToString() : "<unknown>";
            return
                (string.IsNullOrEmpty(roomName) ? "<unnamed>" : roomName) +
                "@" +
                basePosition.x +
                "," +
                basePosition.y +
                "#" +
                category;
        }

        private static bool IsMapDirectTeleportRoomRegistered(Minimap minimap, RoomHandler room)
        {
            return minimap != null &&
                minimap.RoomToTeleportMap != null &&
                room != null &&
                minimap.RoomToTeleportMap.ContainsKey(room);
        }

        private static string GetMapDirectTeleportRoomKey(RoomHandler room)
        {
            return room != null ? DescribeMapDirectTeleportRoom(room) : "<none>";
        }

        private static string DescribeConnectedMapDirectTeleportRooms(RoomHandler room, Minimap minimap)
        {
            if (room == null || room.connectedRooms == null || room.connectedRooms.Count == 0)
            {
                return string.Empty;
            }

            System.Collections.Generic.List<string> roomLabels = new System.Collections.Generic.List<string>();
            for (int index = 0; index < room.connectedRooms.Count; index++)
            {
                RoomHandler connectedRoom = room.connectedRooms[index];
                roomLabels.Add(
                    DescribeMapDirectTeleportRoom(connectedRoom) +
                    "{CanTo=" +
                    (connectedRoom != null ? connectedRoom.CanTeleportToRoom().ToString() : "<unknown>") +
                    ", TeleActive=" +
                    (connectedRoom != null ? connectedRoom.TeleportersActive.ToString() : "<unknown>") +
                    ", Revealed=" +
                    (connectedRoom != null ? connectedRoom.RevealedOnMap.ToString() : "<unknown>") +
                    ", Registered=" +
                    IsMapDirectTeleportRoomRegistered(minimap, connectedRoom) +
                    "}");
            }

            return string.Join("; ", roomLabels.ToArray());
        }

        private static string GetLastLoadedDungeonSceneName(GameManager gameManager)
        {
            if ((object)gameManager == null)
            {
                return "<no_game_manager>";
            }

            try
            {
                GameLevelDefinition levelDefinition = gameManager.GetLastLoadedLevelDefinition();
                if (levelDefinition == null || string.IsNullOrEmpty(levelDefinition.dungeonSceneName))
                {
                    return "<unknown>";
                }

                return levelDefinition.dungeonSceneName;
            }
            catch (System.Exception exception)
            {
                return "<exception:" + exception.GetType().Name + ">";
            }
        }

        private bool ShouldLogMapTeleportVerbose()
        {
            return _mapTeleportVerboseLoggingEnabledProvider != null && _mapTeleportVerboseLoggingEnabledProvider();
        }

        private bool ShouldLogFloorTeleportVerbose()
        {
            return _floorTeleportVerboseLoggingEnabledProvider != null && _floorTeleportVerboseLoggingEnabledProvider();
        }
    }
}
