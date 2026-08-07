// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Reflection;
using Dungeonator;
using UnityEngine;

namespace EtgGameplayDashboard
{
    /// <summary>
    /// Captures and restores Boss-room destructibles without owning replay orchestration.
    /// </summary>
    internal sealed class BossRoomDecorationRestorer
    {
        private static readonly BindingFlags InstancePrivateFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly Func<RoomHandler, string> _roomLabelProvider;
        private readonly Func<bool> _verboseLoggingEnabledProvider;
        private readonly Action<string> _log;
        private readonly Action<string> _logWarning;

        public BossRoomDecorationRestorer(
            Func<RoomHandler, string> roomLabelProvider,
            Func<bool> verboseLoggingEnabledProvider,
            Action<string> log,
            Action<string> logWarning)
        {
            _roomLabelProvider = roomLabelProvider;
            _verboseLoggingEnabledProvider = verboseLoggingEnabledProvider;
            _log = log;
            _logWarning = logWarning;
        }

        private string GetRoomLabel(RoomHandler room)
        {
            return _roomLabelProvider(room);
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log(message);
            }
        }

        private void LogWarning(string message)
        {
            if (_logWarning != null)
            {
                _logWarning(message);
            }
        }
        private static void ClearTallGrassFireState(TallGrassPatch grass)
        {
            FieldInfo field = grass != null ? grass.GetType().GetField("m_fireData", InstancePrivateFlags) : null;
            if (field != null)
            {
                field.SetValue(grass, Activator.CreateInstance(field.FieldType));
            }

            FieldInfo stripPool = grass != null ? grass.GetType().GetField("m_tiledSpritePool", InstancePrivateFlags) : null;
            if (stripPool != null)
            {
                stripPool.SetValue(grass, Activator.CreateInstance(stripPool.FieldType));
            }
        }



        public List<RoomDecorationState> Capture(RoomHandler room)
        {
            List<RoomDecorationState> decorations = new List<RoomDecorationState>();
            HashSet<GameObject> capturedRoots = new HashSet<GameObject>();

            List<FlippableCover> covers = room.GetComponentsAbsoluteInRoom<FlippableCover>();
            if (covers != null)
            {
                for (int index = 0; index < covers.Count; index++)
                {
                    FlippableCover cover = covers[index];
                    if ((object)cover == null || (object)cover.gameObject == null || !capturedRoots.Add(cover.gameObject))
                    {
                        continue;
                    }

                    MajorBreakable breakable = cover.GetComponentInChildren<MajorBreakable>();
                    RoomDecorationState state = RoomDecorationState.ForCover(cover, breakable);
                    state.Prototype = FindDecorationPrototype(room, state.WorldPosition, state.Kind);
                    decorations.Add(state);
                }
            }

            List<MajorBreakable> majors = room.GetComponentsAbsoluteInRoom<MajorBreakable>();
            if (majors != null)
            {
                for (int index = 0; index < majors.Count; index++)
                {
                    MajorBreakable major = majors[index];
                    if ((object)major == null || (object)major.gameObject == null ||
                        major.GetComponentInParent<FlippableCover>() != null || !capturedRoots.Add(major.gameObject))
                    {
                        continue;
                    }

                    RoomDecorationState state = RoomDecorationState.ForMajor(major);
                    state.Prototype = FindDecorationPrototype(room, state.WorldPosition, state.Kind);
                    decorations.Add(state);
                }
            }

            List<MinorBreakable> minors = GetRoomMinorBreakables(room);
            if (minors != null)
            {
                for (int index = 0; index < minors.Count; index++)
                {
                    MinorBreakable minor = minors[index];
                    if ((object)minor == null || (object)minor.gameObject == null || !capturedRoots.Add(minor.gameObject))
                    {
                        continue;
                    }

                    RoomDecorationState state = RoomDecorationState.ForMinor(minor);
                    state.Prototype = FindDecorationPrototype(room, state.WorldPosition, state.Kind);
                    // MinorBreakable keeps private broken state, disabled rigidbodies,
                    // and break-animation sprite state. Keep an intact runtime template
                    // even when a room prototype is available so rewind can restore the
                    // actual captured visual instead of only toggling m_isBroken.
                    state.Template = CreateDecorationTemplate(minor.gameObject);
                    if (IsVerboseLoggingEnabled() &&
                        (minor.gameObject.name.IndexOf("grass", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         minor.gameObject.name.IndexOf("bush", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        Log("Captured Boss-room named minor. Room=" + GetRoomLabel(room) +
                            ", Name=" + minor.gameObject.name +
                            ", Position=" + state.WorldPosition + ".");
                    }
                    decorations.Add(state);
                }
            }

            List<TallGrassPatch> grassPatches = GetRoomGrassPatches(room);
            if (IsVerboseLoggingEnabled())
            {
                TallGrassPatch[] visibleGrass = UnityEngine.Object.FindObjectsOfType<TallGrassPatch>();
                int globalGrassCount = StaticReferenceManager.AllGrasses != null
                    ? StaticReferenceManager.AllGrasses.Count
                    : 0;
                Log("Boss-room grass scan. Room=" + GetRoomLabel(room) +
                    ", VisibleComponents=" + (visibleGrass != null ? visibleGrass.Length : 0) +
                    ", GlobalRegistry=" + globalGrassCount +
                    ", RoomGrass=" + (grassPatches != null ? grassPatches.Count : 0) + ".");
            }
            if (grassPatches != null)
            {
                for (int index = 0; index < grassPatches.Count; index++)
                {
                    TallGrassPatch grass = grassPatches[index];
                    if ((object)grass == null || (object)grass.gameObject == null || !capturedRoots.Add(grass.gameObject))
                    {
                        continue;
                    }

                    decorations.Add(RoomDecorationState.ForGrass(grass));
                }
            }

            List<BreakableObject> breakableObjects = room.GetComponentsAbsoluteInRoom<BreakableObject>();
            if (breakableObjects != null)
            {
                for (int index = 0; index < breakableObjects.Count; index++)
                {
                    BreakableObject breakable = breakableObjects[index];
                    if ((object)breakable == null || (object)breakable.gameObject == null || !capturedRoots.Add(breakable.gameObject))
                    {
                        continue;
                    }
                    RoomDecorationState state = RoomDecorationState.ForTemplate(RoomDecorationKind.BreakableObject, breakable.gameObject);
                    state.Template = CreateDecorationTemplate(breakable.gameObject);
                    decorations.Add(state);
                }
            }

            List<BreakableSprite> breakableSprites = room.GetComponentsAbsoluteInRoom<BreakableSprite>();
            if (breakableSprites != null)
            {
                for (int index = 0; index < breakableSprites.Count; index++)
                {
                    BreakableSprite breakable = breakableSprites[index];
                    if ((object)breakable == null || (object)breakable.gameObject == null || !capturedRoots.Add(breakable.gameObject))
                    {
                        continue;
                    }
                    RoomDecorationState state = RoomDecorationState.ForTemplate(RoomDecorationKind.BreakableSprite, breakable.gameObject);
                    state.Template = CreateDecorationTemplate(breakable.gameObject);
                    decorations.Add(state);
                }
            }

            Log("Captured Boss-room destructible state. Room=" + GetRoomLabel(room) +
                ", Decorations=" + decorations.Count +
                ", Covers=" + CountDecorationKind(decorations, RoomDecorationKind.Cover) +
                ", Majors=" + CountDecorationKind(decorations, RoomDecorationKind.Major) +
                ", Minors=" + CountDecorationKind(decorations, RoomDecorationKind.Minor) +
                ", Grass=" + CountDecorationKind(decorations, RoomDecorationKind.Grass) +
                ", BreakableObjects=" + CountDecorationKind(decorations, RoomDecorationKind.BreakableObject) +
                ", BreakableSprites=" + CountDecorationKind(decorations, RoomDecorationKind.BreakableSprite) + ".");
            return decorations;
        }



        private static int CountDecorationKind(List<RoomDecorationState> decorations, RoomDecorationKind kind)
        {
            int count = 0;
            if (decorations == null)
            {
                return count;
            }

            for (int index = 0; index < decorations.Count; index++)
            {
                if (decorations[index] != null && decorations[index].Kind == kind)
                {
                    count++;
                }
            }
            return count;
        }



        private static List<TallGrassPatch> GetRoomGrassPatches(RoomHandler room)
        {
            List<TallGrassPatch> result = new List<TallGrassPatch>();
            HashSet<TallGrassPatch> seen = new HashSet<TallGrassPatch>();
            List<TallGrassPatch> roomGrass = room != null
                ? room.GetComponentsAbsoluteInRoom<TallGrassPatch>()
                : null;
            AddRoomGrassPatches(room, roomGrass, result, seen);
            List<TallGrassPatch> allGrasses = StaticReferenceManager.AllGrasses;
            AddRoomGrassPatches(room, allGrasses, result, seen);
            return result;
        }



        private static void AddRoomGrassPatches(
            RoomHandler room,
            List<TallGrassPatch> candidates,
            List<TallGrassPatch> result,
            HashSet<TallGrassPatch> seen)
        {
            if (candidates == null)
            {
                return;
            }

            for (int index = 0; index < candidates.Count; index++)
            {
                TallGrassPatch grass = candidates[index];
                if ((object)grass == null || (object)grass.gameObject == null ||
                    !seen.Add(grass) || grass.cells == null)
                {
                    continue;
                }

                for (int cellIndex = 0; cellIndex < grass.cells.Count; cellIndex++)
                {
                    if (IsCellInRoom(room, grass.cells[cellIndex]))
                    {
                        result.Add(grass);
                        break;
                    }
                }
            }

        }



        private static List<MinorBreakable> GetRoomMinorBreakables(RoomHandler room)
        {
            List<MinorBreakable> result = new List<MinorBreakable>();
            HashSet<MinorBreakable> seen = new HashSet<MinorBreakable>();
            List<MinorBreakable> roomMinors = room != null
                ? room.GetComponentsAbsoluteInRoom<MinorBreakable>()
                : null;
            AddRoomMinorBreakables(room, roomMinors, result, seen);
            AddRoomMinorBreakables(room, StaticReferenceManager.AllMinorBreakables, result, seen);
            return result;
        }



        private static void AddRoomMinorBreakables(
            RoomHandler room,
            List<MinorBreakable> candidates,
            List<MinorBreakable> result,
            HashSet<MinorBreakable> seen)
        {
            if (candidates == null)
            {
                return;
            }

            for (int index = 0; index < candidates.Count; index++)
            {
                MinorBreakable minor = candidates[index];
                if ((object)minor == null || (object)minor.gameObject == null || !seen.Add(minor) ||
                    !IsCellInRoom(room, minor.transform.position.IntXY(VectorConversions.Floor)))
                {
                    continue;
                }
                result.Add(minor);
            }
        }



        private static bool IsCellInRoom(RoomHandler room, IntVector2 cell)
        {
            if (room == null)
            {
                return false;
            }
            if ((room.Cells != null && room.Cells.Contains(cell)) ||
                (room.RawCells != null && room.RawCells.Contains(cell)))
            {
                return true;
            }

            return GameManager.Instance != null && GameManager.Instance.Dungeon != null &&
                   GameManager.Instance.Dungeon.data != null &&
                   GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(cell) == room;
        }



        public void Restore(RoomHandler room, RoomEnemyReplaySnapshot snapshot)
        {
            if (snapshot == null || snapshot.Decorations == null)
            {
                LogWarning("Boss-room destructible restore skipped because no decoration snapshot exists. Room=" + GetRoomLabel(room) + ".");
                return;
            }

            int restored = 0;
            int respawned = 0;
            int missing = 0;
            int failed = 0;
            Dictionary<DecorationLookupKey, GameObject> currentObjects = BuildDecorationLookup(room);
            for (int index = 0; index < snapshot.Decorations.Count; index++)
            {
                RoomDecorationState state = snapshot.Decorations[index];
                GameObject current = FindDecorationObject(currentObjects, state);
                if (current != null && state.Template != null &&
                    (state.Kind == RoomDecorationKind.BreakableObject || state.Kind == RoomDecorationKind.BreakableSprite))
                {
                    UnityEngine.Object.Destroy(current);
                    current = null;
                }
                if (current == null)
                {
                    current = RespawnDecoration(room, state);
                    if (current != null)
                    {
                        respawned++;
                    }
                }

                if (current == null)
                {
                    if (state.Prototype == null)
                    {
                        missing++;
                    }
                    else
                    {
                        failed++;
                    }
                    Log("Boss-room destructible restore missing object. Room=" + GetRoomLabel(room) +
                        ", Kind=" + state.Kind +
                        ", Position=" + state.WorldPosition.x + "," + state.WorldPosition.y +
                        ", Prototype=" + (state.Prototype != null) +
                        ", Template=" + (state.Template != null) + ".");
                    continue;
                }

                RestoreDecorationState(room, state, current);
                restored++;
            }

            Log(
                "Restored Boss-room destructible state. Room=" + GetRoomLabel(room) +
                ", SnapshotCount=" + snapshot.Decorations.Count +
                ", Restored=" + restored +
                ", Respawned=" + respawned +
                ", Missing=" + missing +
                ", Failed=" + failed + ".");
            if (missing > 0 || failed > 0)
            {
                LogWarning("Boss-room destructible restore was incomplete. Room=" + GetRoomLabel(room) +
                    ", Missing=" + missing + ", Failed=" + failed + ".");
            }
        }



        private static Dictionary<DecorationLookupKey, GameObject> BuildDecorationLookup(RoomHandler room)
        {
            Dictionary<DecorationLookupKey, GameObject> result = new Dictionary<DecorationLookupKey, GameObject>();
            if (room == null)
            {
                return result;
            }

            AddDecorationObjects(result, room.GetComponentsAbsoluteInRoom<FlippableCover>(), RoomDecorationKind.Cover, false);
            AddDecorationObjects(result, room.GetComponentsAbsoluteInRoom<MajorBreakable>(), RoomDecorationKind.Major, true);
            AddDecorationObjects(result, room.GetComponentsAbsoluteInRoom<BreakableObject>(), RoomDecorationKind.BreakableObject, false);
            AddDecorationObjects(result, room.GetComponentsAbsoluteInRoom<BreakableSprite>(), RoomDecorationKind.BreakableSprite, false);
            AddDecorationObjects(result, GetRoomMinorBreakables(room), RoomDecorationKind.Minor, false);

            List<TallGrassPatch> grassPatches = GetRoomGrassPatches(room);
            if (grassPatches != null)
            {
                for (int index = 0; index < grassPatches.Count; index++)
                {
                    TallGrassPatch grass = grassPatches[index];
                    if (grass == null || grass.gameObject == null || grass.cells == null)
                    {
                        continue;
                    }

                    for (int cellIndex = 0; cellIndex < grass.cells.Count; cellIndex++)
                    {
                        AddDecorationObject(
                            result,
                            RoomDecorationKind.Grass,
                            grass.cells[cellIndex],
                            grass.gameObject);
                    }
                }
            }

            return result;
        }



        private static void AddDecorationObjects<T>(
            Dictionary<DecorationLookupKey, GameObject> lookup,
            List<T> objects,
            RoomDecorationKind kind,
            bool skipCovers) where T : Component
        {
            if (objects == null)
            {
                return;
            }

            for (int index = 0; index < objects.Count; index++)
            {
                T component = objects[index];
                if (component == null || component.gameObject == null ||
                    (skipCovers && component.GetComponentInParent<FlippableCover>() != null))
                {
                    continue;
                }

                AddDecorationObject(lookup, kind, component.transform.position.IntXY(), component.gameObject);
            }
        }



        private static void AddDecorationObject(
            Dictionary<DecorationLookupKey, GameObject> lookup,
            RoomDecorationKind kind,
            IntVector2 position,
            GameObject gameObject)
        {
            if (lookup == null || gameObject == null)
            {
                return;
            }

            DecorationLookupKey key = new DecorationLookupKey(kind, position);
            if (!lookup.ContainsKey(key))
            {
                lookup.Add(key, gameObject);
            }
        }



        private static GameObject FindDecorationObject(
            Dictionary<DecorationLookupKey, GameObject> lookup,
            RoomDecorationState state)
        {
            if (lookup == null || state == null)
            {
                return null;
            }

            GameObject gameObject;
            return lookup.TryGetValue(new DecorationLookupKey(state.Kind, state.WorldPosition), out gameObject)
                ? gameObject
                : null;
        }



        private struct DecorationLookupKey : IEquatable<DecorationLookupKey>
        {
            public DecorationLookupKey(RoomDecorationKind kind, IntVector2 position)
            {
                Kind = kind;
                X = position.x;
                Y = position.y;
            }

            private readonly RoomDecorationKind Kind;
            private readonly int X;
            private readonly int Y;

            public bool Equals(DecorationLookupKey other)
            {
                return Kind == other.Kind && X == other.X && Y == other.Y;
            }

            public override bool Equals(object obj)
            {
                return obj is DecorationLookupKey && Equals((DecorationLookupKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (int)Kind;
                    hash = (hash * 397) ^ X;
                    hash = (hash * 397) ^ Y;
                    return hash;
                }
            }
        }



        private GameObject RespawnDecoration(RoomHandler room, RoomDecorationState state)
        {
            PrototypePlacedObjectData data = state.Prototype;
            if (data == null && state.Template == null)
            {
                return null;
            }

            IntVector2 location = state.WorldPosition - room.area.basePosition;
            GameObject spawned = null;
            if (data == null && state.Template != null)
            {
                spawned = UnityEngine.Object.Instantiate(state.Template);
                spawned.transform.position = state.WorldTransformPosition;
                spawned.transform.rotation = state.WorldRotation;
                spawned.transform.localScale = state.WorldScale;
                spawned.SetActive(true);
                MinorBreakable templateMinor = spawned.GetComponentInChildren<MinorBreakable>();
                if (templateMinor != null)
                {
                    templateMinor.ConfigureOnPlacement(room);
                    if (templateMinor.specRigidbody != null)
                    {
                        templateMinor.specRigidbody.Reinitialize();
                    }
                }
                Log("Respawned Boss-room destructible from captured template. Room=" + GetRoomLabel(room) +
                    ", Kind=" + state.Kind + ", Position=" + state.WorldPosition.x + "," + state.WorldPosition.y + ".");
            }
            else if (data != null && data.nonenemyBehaviour != null)
            {
                spawned = data.nonenemyBehaviour.InstantiateObject(room, location);
            }
            else if (data != null && data.placeableContents != null && !data.placeableContents.ContainsEnemy)
            {
                spawned = data.placeableContents.InstantiateObject(room, location);
            }

            if (spawned != null)
            {
                if (data != null)
                {
                    room.HandleFields(data, spawned);
                }
                Log("Respawned Boss-room destructible. Room=" + GetRoomLabel(room) +
                    ", Kind=" + state.Kind + ", Position=" + state.WorldPosition.x + "," + state.WorldPosition.y + ".");
            }
            return spawned;
        }



        private static GameObject CreateDecorationTemplate(GameObject source)
        {
            if (source == null)
            {
                return null;
            }

            GameObject template = UnityEngine.Object.Instantiate(source);
            template.name = source.name + "__RoomRewindTemplate";
            template.transform.position = new Vector3(-10000f, -10000f, -10000f);
            template.SetActive(false);
            template.hideFlags = HideFlags.HideAndDontSave;
            MinorBreakable minor = template.GetComponentInChildren<MinorBreakable>();
            if (minor != null && StaticReferenceManager.AllMinorBreakables != null)
            {
                StaticReferenceManager.AllMinorBreakables.Remove(minor);
            }
            return template;
        }



        public void DestroyTemplates(IEnumerable<RoomEnemyReplaySnapshot> snapshots)
        {
            if (snapshots == null)
            {
                return;
            }

            foreach (RoomEnemyReplaySnapshot snapshot in snapshots)
            {
                if (snapshot == null || snapshot.Decorations == null)
                {
                    continue;
                }

                for (int index = 0; index < snapshot.Decorations.Count; index++)
                {
                    GameObject template = snapshot.Decorations[index].Template;
                    if (template != null)
                    {
                        UnityEngine.Object.Destroy(template);
                    }
                }
            }
        }

        private bool IsVerboseLoggingEnabled()
        {
            return _verboseLoggingEnabledProvider != null && _verboseLoggingEnabledProvider();
        }



        private static PrototypePlacedObjectData FindDecorationPrototype(RoomHandler room, IntVector2 worldPosition, RoomDecorationKind kind)
        {
            if (room == null || room.area == null || room.area.prototypeRoom == null)
            {
                return null;
            }

            PrototypeDungeonRoom prototypeRoom = room.area.prototypeRoom;
            PrototypePlacedObjectData result = FindDecorationPrototypeInList(
                prototypeRoom.placedObjects,
                prototypeRoom.placedObjectPositions,
                room.area.basePosition,
                worldPosition,
                kind);
            if (result != null)
            {
                return result;
            }

            if (prototypeRoom.runtimeAdditionalObjectLayers != null)
            {
                for (int layerIndex = 0; layerIndex < prototypeRoom.runtimeAdditionalObjectLayers.Count; layerIndex++)
                {
                    PrototypeRoomObjectLayer layer = prototypeRoom.runtimeAdditionalObjectLayers[layerIndex];
                    if (layer == null || layer.layerIsReinforcementLayer)
                    {
                        continue;
                    }

                    result = FindDecorationPrototypeInList(
                        layer.placedObjects,
                        layer.placedObjectBasePositions,
                        room.area.basePosition,
                        worldPosition,
                        kind);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            if (prototypeRoom.additionalObjectLayers != null)
            {
                for (int layerIndex = 0; layerIndex < prototypeRoom.additionalObjectLayers.Count; layerIndex++)
                {
                    PrototypeRoomObjectLayer layer = prototypeRoom.additionalObjectLayers[layerIndex];
                    if (layer == null || layer.layerIsReinforcementLayer)
                    {
                        continue;
                    }

                    result = FindDecorationPrototypeInList(
                        layer.placedObjects,
                        layer.placedObjectBasePositions,
                        room.area.basePosition,
                        worldPosition,
                        kind);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            // A few predefined-room objects carry pixel offsets or are authored
            // through a layer whose runtime base position differs by one cell.
            // If exact matching fails, use the nearest same-kind prototype within
            // a small room-local radius and still instantiate at the captured cell.
            result = FindNearestDecorationPrototypeInList(
                prototypeRoom.placedObjects,
                prototypeRoom.placedObjectPositions,
                room.area.basePosition,
                worldPosition,
                kind);
            if (result != null)
            {
                return result;
            }

            result = FindNearestDecorationPrototypeInLayers(
                prototypeRoom.runtimeAdditionalObjectLayers,
                room.area.basePosition,
                worldPosition,
                kind);
            if (result != null)
            {
                return result;
            }

            return FindNearestDecorationPrototypeInLayers(
                prototypeRoom.additionalObjectLayers,
                room.area.basePosition,
                worldPosition,
                kind);
        }



        private static PrototypePlacedObjectData FindNearestDecorationPrototypeInLayers(
            List<PrototypeRoomObjectLayer> layers,
            IntVector2 roomBase,
            IntVector2 worldPosition,
            RoomDecorationKind kind)
        {
            if (layers == null)
            {
                return null;
            }

            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                PrototypeRoomObjectLayer layer = layers[layerIndex];
                if (layer == null || layer.layerIsReinforcementLayer)
                {
                    continue;
                }

                PrototypePlacedObjectData result = FindNearestDecorationPrototypeInList(
                    layer.placedObjects,
                    layer.placedObjectBasePositions,
                    roomBase,
                    worldPosition,
                    kind);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }



        private static PrototypePlacedObjectData FindNearestDecorationPrototypeInList(
            List<PrototypePlacedObjectData> objects,
            List<Vector2> positions,
            IntVector2 roomBase,
            IntVector2 worldPosition,
            RoomDecorationKind kind)
        {
            if (objects == null)
            {
                return null;
            }

            PrototypePlacedObjectData nearest = null;
            int nearestDistance = 3;
            for (int index = 0; index < objects.Count; index++)
            {
                PrototypePlacedObjectData data = objects[index];
                if (data == null || !IsDecorationPrototype(data, kind))
                {
                    continue;
                }

                IntVector2 candidate = positions != null && index < positions.Count
                    ? positions[index].ToIntVector2() + roomBase
                    : data.contentsBasePosition.ToIntVector2() + roomBase;
                int distance = Math.Abs(candidate.x - worldPosition.x) + Math.Abs(candidate.y - worldPosition.y);
                if (distance < nearestDistance)
                {
                    nearest = data;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }



        private static PrototypePlacedObjectData FindDecorationPrototypeInList(
            List<PrototypePlacedObjectData> objects,
            List<Vector2> positions,
            IntVector2 roomBase,
            IntVector2 worldPosition,
            RoomDecorationKind kind)
        {
            if (objects == null)
            {
                return null;
            }

            for (int index = 0; index < objects.Count; index++)
            {
                PrototypePlacedObjectData data = objects[index];
                if (data == null || !IsDecorationPrototype(data, kind))
                {
                    continue;
                }

                IntVector2 candidate = positions != null && index < positions.Count
                    ? positions[index].ToIntVector2() + roomBase
                    : data.contentsBasePosition.ToIntVector2() + roomBase;
                if (candidate.x == worldPosition.x && candidate.y == worldPosition.y)
                {
                    return data;
                }
            }

            return null;
        }



        private static bool IsDecorationPrototype(PrototypePlacedObjectData data, RoomDecorationKind kind)
        {
            if (data == null || !string.IsNullOrEmpty(data.enemyBehaviourGuid))
            {
                return false;
            }

            if (data.nonenemyBehaviour != null)
            {
                return ContainsDecorationType(data.nonenemyBehaviour.gameObject, kind);
            }

            if (data.placeableContents == null || data.placeableContents.variantTiers == null)
            {
                return false;
            }

            for (int index = 0; index < data.placeableContents.variantTiers.Count; index++)
            {
                Dungeonator.DungeonPlaceableVariant variant = data.placeableContents.variantTiers[index];
                if (variant != null && variant.nonDatabasePlaceable != null &&
                    ContainsDecorationType(variant.nonDatabasePlaceable, kind))
                {
                    return true;
                }
            }

            return false;
        }



        private static bool ContainsDecorationType(GameObject prefab, RoomDecorationKind kind)
        {
            if (prefab == null)
            {
                return false;
            }

            if (kind == RoomDecorationKind.Cover)
            {
                return prefab.GetComponentInChildren<FlippableCover>() != null;
            }
            if (kind == RoomDecorationKind.Major)
            {
                return prefab.GetComponentInChildren<MajorBreakable>() != null &&
                    prefab.GetComponentInChildren<FlippableCover>() == null;
            }
            return prefab.GetComponentInChildren<MinorBreakable>() != null;
        }



        private static void RestoreDecorationState(RoomHandler room, RoomDecorationState state, GameObject root)
        {
            root.SetActive(true);
            if (root.transform.parent == null && room.hierarchyParent != null)
            {
                root.transform.SetParent(room.hierarchyParent, true);
            }

            if (state.Kind == RoomDecorationKind.Grass)
            {
                TallGrassPatch grass = root.GetComponentInChildren<TallGrassPatch>();
                if (grass != null)
                {
                    grass.cells = new List<IntVector2>(state.GrassCells);
                    ClearTallGrassFireState(grass);
                    grass.BuildPatch();
                }
                return;
            }

            if (state.Kind == RoomDecorationKind.Cover)
            {
                FlippableCover cover = root.GetComponentInChildren<FlippableCover>();
                if (cover != null)
                {
                    PrivateFieldAccessor.SetPrivateBool(cover, "m_flipped", state.WasFlipped);
                    PrivateFieldAccessor.SetPrivateEnum(cover, "m_flipDirection", state.FlipDirection);
                    if (!room.IsRegistered(cover))
                    {
                        room.RegisterInteractable(cover);
                    }
                }

                MajorBreakable breakable = root.GetComponentInChildren<MajorBreakable>();
                RestoreMajorBreakable(breakable, state);
                return;
            }

            if (state.Kind == RoomDecorationKind.Major)
            {
                RestoreMajorBreakable(root.GetComponentInChildren<MajorBreakable>(), state);
            }
            else
            {
                MinorBreakable minor = root.GetComponentInChildren<MinorBreakable>();
                if (minor != null)
                {
                    if (!state.WasBroken && minor.IsBroken && state.Template != null)
                    {
                        ReplaceBrokenMinorWithTemplate(room, state, root);
                        return;
                    }
                    PrivateFieldAccessor.SetPrivateBool(minor, "m_isBroken", state.WasBroken);
                    minor.enabled = !state.WasBroken;
                    if (minor.specRigidbody != null)
                    {
                        minor.specRigidbody.enabled = !state.WasBroken;
                        if (!state.WasBroken)
                        {
                            minor.specRigidbody.Reinitialize();
                        }
                    }
                }
            }
        }



        private static void ReplaceBrokenMinorWithTemplate(
            RoomHandler room,
            RoomDecorationState state,
            GameObject brokenRoot)
        {
            // Vanilla MinorBreakable.Break() changes more than IsBroken: it disables
            // the SpeculativeRigidbody and may leave the break animation/sprite active.
            // Replacing the live object from the intact captured template restores all
            // of that native state together, including serialized prefab components.
            if (state == null || state.Template == null || brokenRoot == null)
            {
                return;
            }

            Transform parent = brokenRoot.transform.parent;
            UnityEngine.Object.Destroy(brokenRoot);

            GameObject restored = UnityEngine.Object.Instantiate(state.Template);
            restored.name = state.Template.name.Replace("__RoomRewindTemplate", string.Empty);
            if (parent != null)
            {
                restored.transform.SetParent(parent, true);
            }
            restored.transform.position = state.WorldTransformPosition;
            restored.transform.rotation = state.WorldRotation;
            restored.transform.localScale = state.WorldScale;
            restored.SetActive(true);

            MinorBreakable minor = restored.GetComponentInChildren<MinorBreakable>();
            if (minor != null)
            {
                minor.ConfigureOnPlacement(room);
                if (minor.specRigidbody != null)
                {
                    minor.specRigidbody.enabled = true;
                    minor.specRigidbody.Reinitialize();
                }
            }
        }



        private static void RestoreMajorBreakable(MajorBreakable breakable, RoomDecorationState state)
        {
            if (breakable == null)
            {
                return;
            }

            PrivateFieldAccessor.SetPrivateBool(breakable, "m_isBroken", state.WasBroken);
            PrivateFieldAccessor.SetPrivateBool(breakable, "m_inZeroHPState", false);
            PrivateFieldAccessor.SetPrivateInt(breakable, "m_numHits", state.NumHits);
            breakable.HitPoints = state.HitPoints;
            breakable.enabled = !state.WasBroken;
            if (breakable.specRigidbody != null)
            {
                breakable.specRigidbody.enabled = !state.WasBroken;
                if (!state.WasBroken)
                {
                    breakable.specRigidbody.Reinitialize();
                }
            }
        }

    }
}
