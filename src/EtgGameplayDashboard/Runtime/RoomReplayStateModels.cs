// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections.Generic;
using Dungeonator;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed class RoomEnemyReplaySnapshot
    {
        public readonly List<List<RoomEnemyReplayEntry>> Waves = new List<List<RoomEnemyReplayEntry>>();
        public List<RoomDecorationState> Decorations;
        public int NextWaveIndex;
        public PlayerRoomSnapshot Player;
        public bool PlayerHasTakenDamageInThisRoom;
        public bool HasGivenMasteryToken;
    }



    internal enum RoomDecorationKind
    {
        Cover,
        Major,
        Minor,
        Grass,
        BreakableObject,
        BreakableSprite
    }



    internal sealed class RoomDecorationState
    {
        private RoomDecorationState(RoomDecorationKind kind, GameObject root)
        {
            Kind = kind;
            WorldPosition = root.transform.position.IntXY();
            WorldTransformPosition = root.transform.position;
            WorldRotation = root.transform.rotation;
            WorldScale = root.transform.localScale;
            Prototype = null;
        }

        public RoomDecorationKind Kind;
        public IntVector2 WorldPosition;
        public PrototypePlacedObjectData Prototype;
        public GameObject Template;
        public Vector3 WorldTransformPosition;
        public Quaternion WorldRotation;
        public Vector3 WorldScale;
        public bool WasBroken;
        public bool WasFlipped;
        public DungeonData.Direction FlipDirection;
        public int NumHits;
        public float HitPoints;
        public readonly List<IntVector2> GrassCells = new List<IntVector2>();

        public static RoomDecorationState ForCover(FlippableCover cover, MajorBreakable breakable)
        {
            RoomDecorationState state = new RoomDecorationState(RoomDecorationKind.Cover, cover.gameObject);
            state.WasFlipped = cover.IsFlipped;
            state.FlipDirection = cover.DirectionFlipped;
            state.WasBroken = cover.IsBroken;
            if (breakable != null)
            {
                state.NumHits = breakable.NumHits;
                state.HitPoints = breakable.HitPoints;
            }
            return state;
        }

        public static RoomDecorationState ForMajor(MajorBreakable major)
        {
            RoomDecorationState state = new RoomDecorationState(RoomDecorationKind.Major, major.gameObject);
            state.WasBroken = major.IsDestroyed;
            state.NumHits = major.NumHits;
            state.HitPoints = major.HitPoints;
            return state;
        }

        public static RoomDecorationState ForMinor(MinorBreakable minor)
        {
            RoomDecorationState state = new RoomDecorationState(RoomDecorationKind.Minor, minor.gameObject);
            state.WasBroken = minor.IsBroken;
            return state;
        }

        public static RoomDecorationState ForGrass(TallGrassPatch grass)
        {
            RoomDecorationState state = new RoomDecorationState(RoomDecorationKind.Grass, grass.gameObject);
            if (grass.cells != null)
            {
                state.GrassCells.AddRange(grass.cells);
                if (state.GrassCells.Count > 0)
                {
                    state.WorldPosition = state.GrassCells[0];
                }
            }
            return state;
        }

        public static RoomDecorationState ForTemplate(RoomDecorationKind kind, GameObject root)
        {
            return new RoomDecorationState(kind, root);
        }
    }

    internal sealed class RoomEnemyReplayEntry
    {
        public RoomEnemyReplayEntry(
            string enemyGuid,
            IntVector2 spawnPosition,
            IntVector2 worldPosition,
            bool ignoreForRoomClear)
        {
            EnemyGuid = enemyGuid;
            SpawnPosition = spawnPosition;
            WorldPosition = worldPosition;
            IgnoreForRoomClear = ignoreForRoomClear;
        }

        public string EnemyGuid { get; private set; }
        public IntVector2 SpawnPosition { get; private set; }
        public IntVector2 WorldPosition { get; private set; }
        public bool IgnoreForRoomClear { get; private set; }
    }


}
