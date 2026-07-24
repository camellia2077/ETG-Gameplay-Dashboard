// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;
using Dungeonator;
using System.Collections.Generic;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class RoomDebugCommandService
    {
        public List<RoomBossOption> GetBossSelectionOptions()
        {
            long startedAt = System.Diagnostics.Stopwatch.GetTimestamp();
            List<RoomBossOption> bossOptions = new List<RoomBossOption>();
            GameManager gameManager = GameManager.Instance;
            Dungeon targetDungeon = GetCachedBossSelectionDungeon(gameManager);
            LogBossSelectionOptionsDiagnostic(gameManager, targetDungeon, "resolved");
            if ((object)gameManager == null || (object)targetDungeon == null || gameManager.BossManager == null)
            {
                LogBossSelectionDiagnostic(
                    null,
                    "Boss selection options unavailable. GameManager=" + ((object)gameManager != null) +
                    ", TargetDungeon=" + ((object)targetDungeon != null) +
                    ", BossManager=" + ((object)gameManager != null && gameManager.BossManager != null) + ".");
                LogBossSelectionOptionsPerformance(startedAt, bossOptions.Count, targetDungeon, false);
                ClearDerivedBossSelectionCaches();
                return bossOptions;
            }

            // GetCompiledList() performs vanilla room-table compilation on its
            // first call. GUI.OnGUI can run hundreds of times per half-second,
            // so cache the resolved options by target dungeon and tileset and
            // never repeat that cold path during panel repaint.
            GlobalDungeonData.ValidTilesets targetTileset = targetDungeon.tileIndices.tilesetId;
            if (_cachedBossSelectionOptions != null &&
                _cachedBossSelectionManager == gameManager.BossManager &&
                _cachedBossSelectionDungeon == targetDungeon &&
                _cachedBossSelectionTileset == targetTileset)
            {
                RefreshBossOptionDisplayNames(_cachedBossSelectionOptions);
                LogBossSelectionOptionsPerformance(startedAt, _cachedBossSelectionOptions.Count, targetDungeon, true);
                return _cachedBossSelectionOptions;
            }

            List<PrototypeDungeonRoom> bossRoomPrototypes = GetBossRoomPrototypesForTileset(
                gameManager.BossManager,
                targetTileset);
            for (int index = 0; index < bossRoomPrototypes.Count; index++)
            {
                PrototypeDungeonRoom bossRoomPrototype = bossRoomPrototypes[index];
                bossOptions.Add(new RoomBossOption(bossRoomPrototype, ResolveBossName(bossRoomPrototype)));
            }

            string optionsDiagnostic =
                "Boss selection options built. Count=" + bossOptions.Count +
                ", Options=" + DescribeBossOptions(bossOptions) + ".";
            if (!string.Equals(_lastBossSelectionOptionsBuiltDiagnostic, optionsDiagnostic, System.StringComparison.Ordinal))
            {
                _lastBossSelectionOptionsBuiltDiagnostic = optionsDiagnostic;
                LogBossSelectionDiagnostic(null, optionsDiagnostic);
            }

            _cachedBossSelectionManager = gameManager.BossManager;
            _cachedBossSelectionDungeon = targetDungeon;
            _cachedBossSelectionTileset = targetTileset;
            _cachedBossSelectionOptions = bossOptions;
            ClearDerivedBossSelectionCaches();
            LogBossSelectionOptionsPerformance(startedAt, bossOptions.Count, targetDungeon, false);

            return bossOptions;
        }

        private void RefreshBossOptionDisplayNames(List<RoomBossOption> options)
        {
            if (options == null)
            {
                return;
            }

            for (int index = 0; index < options.Count; index++)
            {
                RoomBossOption option = options[index];
                if (option != null && option.BossRoomPrototype != null)
                {
                    // The room-table cache is language-independent, but the
                    // resolved BossName is not. Refresh only the cheap catalog
                    // text when the foyer changes language; do not rebuild the
                    // vanilla room table just to update button labels.
                    option.BossName = ResolveBossName(option.BossRoomPrototype);
                }
            }
        }

        public List<RoomBossOption> GetBossSelectionBossOptions()
        {
            List<RoomBossOption> cachedOptions;
            if (TryGetCurrentCachedBossSelectionOptions(out cachedOptions) &&
                _cachedUniqueBossSelectionSource == cachedOptions &&
                _cachedUniqueBossSelectionOptions != null)
            {
                return _cachedUniqueBossSelectionOptions;
            }

            List<RoomBossOption> allOptions = GetBossSelectionOptions();
            if (_cachedUniqueBossSelectionSource == allOptions && _cachedUniqueBossSelectionOptions != null)
            {
                return _cachedUniqueBossSelectionOptions;
            }

            List<RoomBossOption> bossOptions = new List<RoomBossOption>();
            for (int index = 0; index < allOptions.Count; index++)
            {
                RoomBossOption option = allOptions[index];
                if (option == null || ContainsBossName(bossOptions, option.BossName))
                {
                    continue;
                }

                bossOptions.Add(option);
            }

            _cachedUniqueBossSelectionSource = allOptions;
            _cachedUniqueBossSelectionOptions = bossOptions;
            return _cachedUniqueBossSelectionOptions;
        }

        public List<RoomBossOption> GetBossRoomOptions(string bossName)
        {
            List<RoomBossOption> cachedOptions;
            if (TryGetCurrentCachedBossSelectionOptions(out cachedOptions) &&
                _cachedBossRoomOptionsSource == cachedOptions &&
                string.Equals(_cachedBossRoomOptionsName, bossName, System.StringComparison.Ordinal) &&
                _cachedBossRoomOptions != null)
            {
                return _cachedBossRoomOptions;
            }

            List<RoomBossOption> allOptions = GetBossSelectionOptions();
            if (_cachedBossRoomOptionsSource == allOptions &&
                string.Equals(_cachedBossRoomOptionsName, bossName, System.StringComparison.Ordinal) &&
                _cachedBossRoomOptions != null)
            {
                return _cachedBossRoomOptions;
            }

            List<RoomBossOption> roomOptions = new List<RoomBossOption>();
            for (int index = 0; index < allOptions.Count; index++)
            {
                RoomBossOption option = allOptions[index];
                if (option != null && string.Equals(option.BossName, bossName, System.StringComparison.Ordinal))
                {
                    roomOptions.Add(option);
                }
            }

            _cachedBossRoomOptionsSource = allOptions;
            _cachedBossRoomOptionsName = bossName ?? string.Empty;
            _cachedBossRoomOptions = roomOptions;
            return _cachedBossRoomOptions;
        }

        private bool TryGetCurrentCachedBossSelectionOptions(out List<RoomBossOption> options)
        {
            options = null;
            GameManager gameManager = GameManager.Instance;
            if (_cachedBossSelectionOptions == null ||
                (object)gameManager == null ||
                gameManager.BossManager == null)
            {
                return false;
            }

            Dungeon targetDungeon = GetCachedBossSelectionDungeon(gameManager);
            if ((object)targetDungeon == null || targetDungeon.tileIndices == null)
            {
                return false;
            }

            if (_cachedBossSelectionManager != gameManager.BossManager ||
                _cachedBossSelectionDungeon != targetDungeon ||
                _cachedBossSelectionTileset != targetDungeon.tileIndices.tilesetId)
            {
                return false;
            }

            RefreshBossOptionDisplayNames(_cachedBossSelectionOptions);
            options = _cachedBossSelectionOptions;
            return true;
        }

        private void ClearDerivedBossSelectionCaches()
        {
            _cachedUniqueBossSelectionOptions = null;
            _cachedUniqueBossSelectionSource = null;
            _cachedBossRoomOptions = null;
            _cachedBossRoomOptionsSource = null;
            _cachedBossRoomOptionsName = string.Empty;
        }

        private static bool ContainsBossName(List<RoomBossOption> options, string bossName)
        {
            for (int index = 0; index < options.Count; index++)
            {
                if (options[index] != null && string.Equals(options[index].BossName, bossName, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public string GetSelectedBossName()
        {
            return BossManager.PriorFloorSelectedBossRoom != null
                ? ResolveBossName(BossManager.PriorFloorSelectedBossRoom)
                : "Random";
        }

        public string GetCurrentFloorBossName()
        {
            GameManager gameManager = GameManager.Instance;
            Dungeon dungeon = gameManager != null ? gameManager.Dungeon : null;
            List<RoomHandler> rooms = dungeon != null && dungeon.data != null ? dungeon.data.rooms : null;
            if (rooms == null)
            {
                return "None";
            }

            for (int index = 0; index < rooms.Count; index++)
            {
                RoomHandler room = rooms[index];
                if ((object)room == null || room.area == null ||
                    room.area.PrototypeRoomCategory != PrototypeDungeonRoom.RoomCategory.BOSS)
                {
                    continue;
                }

                return ResolveCurrentFloorBossName(room);
            }

            return "None";
        }

        private string ResolveCurrentFloorBossName(RoomHandler room)
        {
            string displayName;
            string runtimeRoomName = room.GetRoomName();
            if (_bossNameCatalog != null &&
                _bossNameCatalog.TryGetDisplayName(runtimeRoomName, out displayName))
            {
                return displayName;
            }

            PrototypeDungeonRoom prototype = ResolvePrototypeRoom(room);
            if (prototype != null && _bossNameCatalog != null &&
                _bossNameCatalog.TryGetDisplayName(prototype.name, out displayName))
            {
                return displayName;
            }

            // Keep the existing live-actor fallback for custom rooms, while
            // ensuring vanilla rooms use the static bilingual JSON catalog
            // whenever either runtime name resolves to a catalog key.
            return prototype != null ? ResolveBossName(prototype) : "Unknown Boss";
        }

        public string GetBossRoomDisplayName(RoomBossOption option)
        {
            string displayName;
            if (option != null && option.BossRoomPrototype != null && _bossNameCatalog != null &&
                _bossNameCatalog.TryGetRoomDisplayName(option.BossRoomPrototype.name, out displayName))
            {
                return displayName;
            }

            return option != null && option.BossRoomPrototype != null
                ? option.BossRoomPrototype.name
                : "Unknown Room";
        }

        public GrantCommandExecutionResult SelectBoss(RoomBossOption selectedOption, ManualLogSource logger)
        {
            GameManager gameManager = GameManager.Instance;
            string currentSceneName = GetCurrentSceneName();
            string runtimeSceneName = GetRuntimeSceneName();
            bool isFoyer = (object)gameManager != null &&
                (gameManager.IsFoyer ||
                 string.Equals(currentSceneName, "foyer", System.StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(runtimeSceneName, "foyer", System.StringComparison.OrdinalIgnoreCase));
            LogBossSelectionDiagnostic(
                logger,
                "Boss selection request. Selected=" +
                (selectedOption != null ? DescribeBossPrototype(selectedOption.BossRoomPrototype) : "<null>") +
                ", CurrentPriorFloorSelectedBossRoom=" + DescribeBossPrototype(BossManager.PriorFloorSelectedBossRoom) +
                ", TargetDungeon=" + DescribeBossSelectionDungeon(gameManager) +
                ", IsFoyer=" + isFoyer +
                ", IsLoadingLevel=" + ((object)gameManager != null && gameManager.IsLoadingLevel) +
                ", Scene=" + currentSceneName +
                ", RuntimeScene=" + runtimeSceneName + ".");
            // ETG keeps the Unity scene name as LoadingDungeon while the foyer is
            // being rebuilt after a dungeon return. IsFoyer=True plus
            // IsLoadingLevel=False is therefore the authoritative ready state;
            // checking RuntimeScene alone would reject the second foyer choice.
            // LoadingDungeon is ETG's persistent gameplay scene name after a
            // floor is ready, including direct load_level teleports. It is not
            // a reliable loading indicator; IsLoadingLevel is the authoritative
            // flag for whether pre-generation Boss selection is still too late.
            bool isActuallyLoadingFloor = (object)gameManager != null && gameManager.IsLoadingLevel;
            if ((object)gameManager == null || isActuallyLoadingFloor)
            {
                LogBossSelectionDiagnostic(
                    logger,
                    "Boss selection rejected before validation. IsActuallyLoadingFloor=" + isActuallyLoadingFloor +
                    ", IsFoyer=" + isFoyer + ".");
                return GrantCommandExecutionResult.Localized(false, "result.room.boss_room.unavailable");
            }

            if (selectedOption == null || selectedOption.BossRoomPrototype == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.room.boss_room.not_found");
            }

            List<RoomBossOption> currentOptions = GetBossSelectionOptions();
            bool isCurrentOption = false;
            for (int index = 0; index < currentOptions.Count; index++)
            {
                if (currentOptions[index] != null && currentOptions[index].BossRoomPrototype == selectedOption.BossRoomPrototype)
                {
                    isCurrentOption = true;
                    break;
                }
            }

            if (!isCurrentOption)
            {
                LogBossSelectionDiagnostic(
                    logger,
                    "Boss selection rejected because the clicked prototype is not in the freshly resolved option list. Clicked=" +
                    DescribeBossPrototype(selectedOption.BossRoomPrototype) +
                    ", CurrentOptions=" + DescribeBossOptions(currentOptions) + ".");
                return GrantCommandExecutionResult.Localized(false, "result.room.boss_room.not_found");
            }

            if (BossManager.PriorFloorSelectedBossRoom == selectedOption.BossRoomPrototype)
            {
                return GrantCommandExecutionResult.Localized(true, "result.room.boss_room.already_here", selectedOption.BossName);
            }

            BossManager.PriorFloorSelectedBossRoom = selectedOption.BossRoomPrototype;
            LogBossSelectionDiagnostic(
                logger,
                "Boss selection written. Boss=" + selectedOption.BossName +
                ", Prototype=" + DescribeBossPrototype(selectedOption.BossRoomPrototype) +
                ", TargetDungeon=" + DescribeBossSelectionDungeon(gameManager) +
                ", PriorFloorSelectedBossRoom=" + DescribeBossPrototype(BossManager.PriorFloorSelectedBossRoom) + ".");
            return GrantCommandExecutionResult.Localized(true, "result.room.boss_room.success", selectedOption.BossName);
        }

        private static Dungeon GetBossSelectionDungeon(GameManager gameManager)
        {
            if ((object)gameManager == null)
            {
                return null;
            }

            if (gameManager.IsLoadingLevel && gameManager.CurrentlyGeneratingDungeonPrefab != null)
            {
                return gameManager.CurrentlyGeneratingDungeonPrefab;
            }

            List<GameLevelDefinition> dungeonFloors = gameManager.dungeonFloors;
            if (dungeonFloors == null || dungeonFloors.Count == 0)
            {
                return null;
            }

            string currentSceneName = GetCurrentSceneName();
            bool isFoyer = gameManager.IsFoyer || string.Equals(currentSceneName, "foyer", System.StringComparison.OrdinalIgnoreCase);
            int nextLevelIndex = isFoyer ? 1 : gameManager.CurrentFloor + 1;
            if (GameManagerNextLevelIndexField != null)
            {
                object nextLevelIndexValue = GameManagerNextLevelIndexField.GetValue(gameManager);
                if (nextLevelIndexValue is int)
                {
                    nextLevelIndex = (int)nextLevelIndexValue;
                }
            }

            for (int offset = 0; offset < dungeonFloors.Count; offset++)
            {
                int index = (nextLevelIndex + offset) % dungeonFloors.Count;
                GameLevelDefinition floor = dungeonFloors[index];
                if (floor == null || string.IsNullOrEmpty(floor.dungeonPrefabPath) ||
                    string.Equals(floor.dungeonSceneName, "Foyer", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(floor.dungeonSceneName, "tt_foyer", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Dungeon dungeon = DungeonDatabase.GetOrLoadByName(floor.dungeonPrefabPath);
                if ((object)dungeon != null)
                {
                    return dungeon;
                }
            }

            return null;
        }

        private Dungeon GetCachedBossSelectionDungeon(GameManager gameManager)
        {
            if ((object)gameManager == null)
            {
                return null;
            }

            string currentSceneName = GetCurrentSceneName();
            bool isFoyer = gameManager.IsFoyer ||
                string.Equals(currentSceneName, "foyer", System.StringComparison.OrdinalIgnoreCase);
            int nextLevelIndex = gameManager.CurrentFloor + 1;
            if (isFoyer)
            {
                nextLevelIndex = 1;
            }

            if (GameManagerNextLevelIndexField != null)
            {
                object nextLevelIndexValue = GameManagerNextLevelIndexField.GetValue(gameManager);
                if (nextLevelIndexValue is int)
                {
                    nextLevelIndex = (int)nextLevelIndexValue;
                }
            }

            Dungeon generatingDungeon = gameManager.IsLoadingLevel
                ? gameManager.CurrentlyGeneratingDungeonPrefab
                : null;
            if (_cachedTargetResolutionGameManager == gameManager &&
                _cachedTargetResolutionCurrentFloor == gameManager.CurrentFloor &&
                _cachedTargetResolutionNextLevelIndex == nextLevelIndex &&
                _cachedTargetResolutionIsFoyer == isFoyer &&
                _cachedTargetResolutionIsLoading == gameManager.IsLoadingLevel &&
                string.Equals(_cachedTargetResolutionScene, currentSceneName, System.StringComparison.Ordinal) &&
                _cachedTargetResolutionGeneratingDungeon == generatingDungeon)
            {
                return _cachedTargetResolutionDungeon;
            }

            Dungeon resolvedDungeon = GetBossSelectionDungeon(gameManager);
            _cachedTargetResolutionGameManager = gameManager;
            _cachedTargetResolutionCurrentFloor = gameManager.CurrentFloor;
            _cachedTargetResolutionNextLevelIndex = nextLevelIndex;
            _cachedTargetResolutionIsFoyer = isFoyer;
            _cachedTargetResolutionIsLoading = gameManager.IsLoadingLevel;
            _cachedTargetResolutionScene = currentSceneName;
            _cachedTargetResolutionGeneratingDungeon = generatingDungeon;
            _cachedTargetResolutionDungeon = resolvedDungeon;
            return resolvedDungeon;
        }

        private static List<PrototypeDungeonRoom> GetBossRoomPrototypesForTileset(
            BossManager bossManager,
            GlobalDungeonData.ValidTilesets tileset)
        {
            List<PrototypeDungeonRoom> prototypes = new List<PrototypeDungeonRoom>();
            if (bossManager == null || bossManager.BossFloorData == null || bossManager.BossFloorData.Count == 0)
            {
                return prototypes;
            }

            BossFloorEntry floorData = null;
            for (int index = 0; index < bossManager.BossFloorData.Count; index++)
            {
                BossFloorEntry candidate = bossManager.BossFloorData[index];
                if (candidate != null && (candidate.AssociatedTilesets | tileset) == candidate.AssociatedTilesets)
                {
                    floorData = candidate;
                }
            }

            if (floorData == null)
            {
                floorData = bossManager.BossFloorData[0];
            }

            if (floorData == null || floorData.Bosses == null)
            {
                return prototypes;
            }

            for (int index = 0; index < floorData.Bosses.Count; index++)
            {
                IndividualBossFloorEntry bossEntry = floorData.Bosses[index];
                if (bossEntry == null || !bossEntry.GlobalPrereqsValid() || bossEntry.TargetRoomTable == null)
                {
                    continue;
                }

                List<WeightedRoom> candidateRooms = bossEntry.TargetRoomTable.GetCompiledList();
                // Some vanilla Bosses intentionally have multiple room layouts
                // (for example Mine Flayer and Gatling Gull). Keep every valid
                // prototype so the UI can let the user choose the layout too;
                // selecting only the first entry makes the Boss look selectable
                // while silently locking its map shape.
                for (int roomIndex = 0; roomIndex < candidateRooms.Count; roomIndex++)
                {
                    WeightedRoom weightedRoom = candidateRooms[roomIndex];
                    PrototypeDungeonRoom prototype = weightedRoom != null ? weightedRoom.room : null;
                    if (prototype == null || prototype.category != PrototypeDungeonRoom.RoomCategory.BOSS ||
                        prototype.subCategoryBoss != PrototypeDungeonRoom.RoomBossSubCategory.FLOOR_BOSS || prototypes.Contains(prototype))
                    {
                        continue;
                    }

                    prototypes.Add(prototype);
                }
            }

            return prototypes;
        }

        private string ResolveBossName(PrototypeDungeonRoom prototype)
        {
            string catalogName;
            if (prototype != null && _bossNameCatalog != null && _bossNameCatalog.TryGetDisplayName(prototype.name, out catalogName))
            {
                return catalogName;
            }

            return prototype != null ? ResolveBossName(prototype.placedObjects, prototype.additionalObjectLayers) : "Unknown Boss";
        }

        private static string ResolveBossName(
            List<PrototypePlacedObjectData> placedObjects,
            List<PrototypeRoomObjectLayer> additionalObjectLayers)
        {
            List<string> bossNames = new List<string>();
            AddBossNamesFromPlacedObjects(placedObjects, bossNames);
            AddBossNamesFromLayers(additionalObjectLayers, bossNames);
            return bossNames.Count > 0 ? string.Join(" + ", bossNames.ToArray()) : "Unknown Boss";
        }

        private static void AddBossNamesFromLayers(List<PrototypeRoomObjectLayer> layers, List<string> bossNames)
        {
            if (layers == null)
            {
                return;
            }

            for (int index = 0; index < layers.Count; index++)
            {
                PrototypeRoomObjectLayer layer = layers[index];
                if (layer != null)
                {
                    AddBossNamesFromPlacedObjects(layer.placedObjects, bossNames);
                }
            }
        }

        private static void AddBossNamesFromPlacedObjects(List<PrototypePlacedObjectData> placedObjects, List<string> bossNames)
        {
            if (placedObjects == null)
            {
                return;
            }

            for (int index = 0; index < placedObjects.Count; index++)
            {
                PrototypePlacedObjectData placedObject = placedObjects[index];
                if (placedObject == null || string.IsNullOrEmpty(placedObject.enemyBehaviourGuid))
                {
                    continue;
                }

                EnemyDatabaseEntry databaseEntry = EnemyDatabase.GetEntry(placedObject.enemyBehaviourGuid);
                if (databaseEntry == null || !databaseEntry.isInBossTab)
                {
                    continue;
                }

                string bossName = string.Empty;
                AIActor enemyPrefab = EnemyDatabase.GetOrLoadByGuid(placedObject.enemyBehaviourGuid);
                if ((object)enemyPrefab != null)
                {
                    bossName = enemyPrefab.GetActorName();
                }

                if (string.IsNullOrEmpty(bossName) && databaseEntry != null)
                {
                    bossName = databaseEntry.name;
                }

                AddBossName(bossNames, bossName);
            }
        }

        private static void AddBossName(List<string> bossNames, string bossName)
        {
            if (bossNames == null || string.IsNullOrEmpty(bossName) || bossNames.Contains(bossName))
            {
                return;
            }

            bossNames.Add(bossName);
        }

    }
}
