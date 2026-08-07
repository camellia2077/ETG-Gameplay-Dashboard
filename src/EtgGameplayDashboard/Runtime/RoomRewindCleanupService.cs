// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using Dungeonator;
using UnityEngine;

namespace EtgGameplayDashboard
{
    /// <summary>
    /// Removes room-local replay artifacts without owning replay state or spawning enemies.
    /// </summary>
    internal sealed class RoomRewindCleanupService
    {
        private readonly Func<bool> _cleanupEnabledProvider;
        private readonly Func<RoomHandler, string> _roomLabelProvider;
        private readonly Func<RoomHandler, bool> _bossRoomProvider;
        private readonly Action<string> _log;

        public RoomRewindCleanupService(
            Func<bool> cleanupEnabledProvider,
            Func<RoomHandler, string> roomLabelProvider,
            Func<RoomHandler, bool> bossRoomProvider,
            Action<string> log)
        {
            _cleanupEnabledProvider = cleanupEnabledProvider;
            _roomLabelProvider = roomLabelProvider;
            _bossRoomProvider = bossRoomProvider;
            _log = log;
        }

        public int ClearRoomRewindObjects(RoomHandler room)
        {
            if ((object)room == null || !_cleanupEnabledProvider())
            {
                return 0;
            }

            int removedCount = 0;
            int removedProjectiles = 0;
            int removedDecalsAndDebris = 0;
            int removedCorpses = 0;
            int removedRoomPersistentVfx = 0;
            int removedPedestals = 0;
            HashSet<GameObject> removedObjects = new HashSet<GameObject>();
            List<Projectile> projectiles = room.GetComponentsAbsoluteInRoom<Projectile>();
            List<EphemeralObject> ephemeralObjects = room.GetComponentsAbsoluteInRoom<EphemeralObject>();
            List<PersistentVFXBehaviour> persistentVfx = room.GetComponentsAbsoluteInRoom<PersistentVFXBehaviour>();
            List<DebrisObject> corpseDebris = room.GetComponentsAbsoluteInRoom<DebrisObject>();
            List<CorpseSpawnController> corpseControllers = room.GetComponentsAbsoluteInRoom<CorpseSpawnController>();
            List<GameObject> corpses = StaticReferenceManager.AllCorpses != null
                ? new List<GameObject>(StaticReferenceManager.AllCorpses)
                : null;
            Log(
                "Rewind cleanup scan. Room=" + RoomLabel(room) +
                ", IsBossRoom=" + IsBossRoom(room) +
                ", ProjectilesFound=" + CountValidObjects(projectiles) +
                ", EphemeralObjectsFound=" + CountValidObjects(ephemeralObjects) +
                ", PersistentVfxFound=" + CountValidObjects(persistentVfx) +
                ", CorpsesFound=" + CountValidObjects(corpses) +
                ", CorpseDebrisFound=" + CountCorpseDebris(corpseDebris) +
                ", CorpseControllersFound=" + CountValidObjects(corpseControllers) + ".");

            if (projectiles != null)
            {
                for (int index = 0; index < projectiles.Count; index++)
                {
                    Projectile projectile = projectiles[index];
                    if ((object)projectile == null || (object)projectile.gameObject == null || !removedObjects.Add(projectile.gameObject))
                    {
                        continue;
                    }

                    projectile.OnDespawned();
                    SpawnManager.Despawn(projectile.gameObject);
                    removedProjectiles++;
                    removedCount++;
                }
            }

            // AIActor.ForceDeath registers spawned corpse prefabs in AllCorpses. Some
            // small-enemy corpse prefabs do not expose DebrisObject, so the generic
            // EphemeralObject cleanup above cannot find them. Filter by room before
            // despawning so cleanup never touches corpses from another room.
            if (corpses != null)
            {
                for (int index = 0; index < corpses.Count; index++)
                {
                    GameObject corpse = corpses[index];
                    if ((object)corpse == null || !IsObjectInRoom(room, corpse) || !removedObjects.Add(corpse))
                    {
                        continue;
                    }

                    corpse.SetActive(false);
                    SpawnManager.Despawn(corpse);
                    StaticReferenceManager.AllCorpses.Remove(corpse);
                    removedCorpses++;
                    removedCount++;
                }
            }

            if (corpseDebris != null)
            {
                for (int index = 0; index < corpseDebris.Count; index++)
                {
                    DebrisObject debris = corpseDebris[index];
                    if ((object)debris == null || !debris.IsCorpse || (object)debris.gameObject == null ||
                        !removedObjects.Add(debris.gameObject))
                    {
                        continue;
                    }

                    debris.gameObject.SetActive(false);
                    SpawnManager.Despawn(debris.gameObject);
                    removedCorpses++;
                    removedCount++;
                }
            }

            if (corpseControllers != null)
            {
                for (int index = 0; index < corpseControllers.Count; index++)
                {
                    CorpseSpawnController controller = corpseControllers[index];
                    if ((object)controller == null || (object)controller.gameObject == null ||
                        !removedObjects.Add(controller.gameObject))
                    {
                        continue;
                    }

                    controller.gameObject.SetActive(false);
                    SpawnManager.Despawn(controller.gameObject);
                    removedCorpses++;
                    removedCount++;
                }
            }

            if (persistentVfx != null)
            {
                for (int index = 0; index < persistentVfx.Count; index++)
                {
                    PersistentVFXBehaviour vfx = persistentVfx[index];
                    if ((object)vfx == null || (object)vfx.gameObject == null ||
                        IsPlayerOwnedVfx(vfx.gameObject) || IsRoomDecorationOwnedVfx(vfx.gameObject) ||
                        !removedObjects.Add(vfx.gameObject))
                    {
                        continue;
                    }

                    vfx.gameObject.SetActive(false);
                    SpawnManager.Despawn(vfx.gameObject);
                    removedRoomPersistentVfx++;
                    removedCount++;
                }
            }

            if (ephemeralObjects != null)
            {
                for (int index = 0; index < ephemeralObjects.Count; index++)
                {
                    EphemeralObject ephemeralObject = ephemeralObjects[index];
                    if ((object)ephemeralObject == null || (object)ephemeralObject.gameObject == null ||
                        !removedObjects.Add(ephemeralObject.gameObject))
                    {
                        continue;
                    }

                    if (!(ephemeralObject is DecalObject) && !(ephemeralObject is DebrisObject))
                    {
                        removedObjects.Remove(ephemeralObject.gameObject);
                        continue;
                    }

                    DebrisObject debrisObject = ephemeralObject as DebrisObject;
                    if (debrisObject != null && debrisObject.IsCorpse)
                    {
                        ephemeralObject.gameObject.SetActive(false);
                        SpawnManager.Despawn(ephemeralObject.gameObject);
                        StaticReferenceManager.AllCorpses.Remove(ephemeralObject.gameObject);
                        removedCorpses++;
                        removedCount++;
                        continue;
                    }

                    ephemeralObject.TriggerDestruction(true);
                    removedDecalsAndDebris++;
                    removedCount++;
                }
            }

            if (IsBossRoom(room))
            {
                List<RewardPedestal> pedestals = room.GetComponentsAbsoluteInRoom<RewardPedestal>();
                if (pedestals != null)
                {
                    for (int index = 0; index < pedestals.Count; index++)
                    {
                        RewardPedestal pedestal = pedestals[index];
                        if ((object)pedestal == null || (object)pedestal.gameObject == null ||
                            !removedObjects.Add(pedestal.gameObject))
                        {
                            continue;
                        }

                        UnityEngine.Object.Destroy(pedestal.gameObject);
                        removedPedestals++;
                        removedCount++;
                    }
                }
            }

            List<PickupObject> pickups = room.GetComponentsAbsoluteInRoom<PickupObject>();
            if (pickups != null)
            {
                for (int index = 0; index < pickups.Count; index++)
                {
                    PickupObject pickup = pickups[index];
                    if ((object)pickup == null || (object)pickup.gameObject == null ||
                        ((object)pickup.GetComponent<DebrisObject>() == null && !(pickup is CurrencyPickup)) ||
                        !removedObjects.Add(pickup.gameObject))
                    {
                        continue;
                    }

                    UnityEngine.Object.Destroy(pickup.gameObject);
                    removedCount++;
                }
            }

            Log(
                "Cleared rewind-room objects before replay. Room=" + RoomLabel(room) +
                ", IsBossRoom=" + IsBossRoom(room) +
                ", RemovedProjectiles=" + removedProjectiles +
                ", RemovedDecalsAndDebris=" + removedDecalsAndDebris +
                ", RemovedCorpses=" + removedCorpses +
                ", RemovedRoomPersistentVfx=" + removedRoomPersistentVfx +
                ", PersistentVfxSkipped=" + Math.Max(0, CountValidObjects(persistentVfx) - removedRoomPersistentVfx) +
                ", RemovedPedestals=" + removedPedestals +
                ", RemovedObjects=" + removedCount + ".");
            return removedCount;
        }

        private bool IsBossRoom(RoomHandler room)
        {
            return _bossRoomProvider(room);
        }

        private string RoomLabel(RoomHandler room)
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

        private static bool IsObjectInRoom(RoomHandler room, GameObject gameObject)
        {
            try
            {
                GameManager gameManager = GameManager.Instance;
                Dungeon dungeon = gameManager != null ? gameManager.Dungeon : null;
                DungeonData dungeonData = dungeon != null ? dungeon.data : null;
                Transform transform = (object)gameObject != null ? gameObject.transform : null;
                if ((object)room == null || (object)gameObject == null || dungeonData == null || (object)transform == null)
                {
                    return false;
                }

                return dungeonData.GetAbsoluteRoomFromPosition(transform.position.IntXY()) == room;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsPlayerOwnedVfx(GameObject gameObject)
        {
            return (object)gameObject != null &&
                   (gameObject.GetComponentInParent<PlayerController>() != null ||
                    gameObject.GetComponentInParent<Gun>() != null ||
                    gameObject.GetComponentInParent<PickupObject>() != null);
        }

        private static bool IsRoomDecorationOwnedVfx(GameObject gameObject)
        {
            return (object)gameObject != null &&
                   (gameObject.GetComponentInParent<FlippableCover>() != null ||
                    gameObject.GetComponentInParent<TallGrassPatch>() != null ||
                    gameObject.GetComponentInParent<MajorBreakable>() != null ||
                    gameObject.GetComponentInParent<MinorBreakable>() != null ||
                    gameObject.GetComponentInParent<BreakableColumn>() != null ||
                    gameObject.GetComponentInParent<BreakableObject>() != null ||
                    gameObject.GetComponentInParent<BreakableSprite>() != null);
        }

        private static int CountValidObjects<T>(List<T> objects) where T : UnityEngine.Object
        {
            if (objects == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < objects.Count; index++)
            {
                if ((object)objects[index] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountCorpseDebris(List<DebrisObject> debrisObjects)
        {
            if (debrisObjects == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < debrisObjects.Count; index++)
            {
                DebrisObject debris = debrisObjects[index];
                if ((object)debris != null && debris.IsCorpse)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
