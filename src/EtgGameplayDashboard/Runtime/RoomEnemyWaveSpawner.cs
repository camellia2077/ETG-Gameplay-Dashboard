// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using Dungeonator;
using UnityEngine;

namespace EtgGameplayDashboard
{
    /// <summary>
    /// Spawns recorded enemy waves and restores the runtime state required by replayed Bosses.
    /// </summary>
    internal sealed class RoomEnemyWaveSpawner
    {
        private readonly Func<RoomHandler, string> _roomLabelProvider;
        private readonly Action<string> _log;
        private readonly Action<string> _logAlways;
        private readonly Action<string> _logWarning;

        public RoomEnemyWaveSpawner(
            Func<RoomHandler, string> roomLabelProvider,
            Action<string> log,
            Action<string> logAlways,
            Action<string> logWarning)
        {
            _roomLabelProvider = roomLabelProvider;
            _log = log;
            _logAlways = logAlways;
            _logWarning = logWarning;
        }

        public int SpawnWave(
            RoomHandler room,
            List<RoomEnemyReplayEntry> wave,
            out List<RoomEnemyReplayEntry> actualWave)
        {
            actualWave = new List<RoomEnemyReplayEntry>();
            int spawned = 0;
            for (int index = 0; index < wave.Count; index++)
            {
                RoomEnemyReplayEntry entry = wave[index];
                AIActor prefab = EnemyDatabase.GetOrLoadByGuid(entry.EnemyGuid);
                if ((object)prefab == null)
                {
                    LogWarning("Recorded enemy prefab is unavailable. Guid=" + entry.EnemyGuid + ", Room=" + RoomLabel(room) + ".");
                    continue;
                }

                // Use the same DungeonPlaceableBehaviour path as RoomHandler uses for a
                // direct enemyBehaviourGuid. AIActor.Spawn has different anchor behavior
                // for several enemy prefabs.
                GameObject spawnedObject = prefab.InstantiateObject(room, entry.SpawnPosition - room.area.basePosition);
                AIActor enemy = spawnedObject != null ? spawnedObject.GetComponent<AIActor>() : null;
                if ((object)enemy == null)
                {
                    LogWarning("Recorded enemy spawn returned null. Guid=" + entry.EnemyGuid + ", Room=" + RoomLabel(room) + ".");
                    continue;
                }

                enemy.PlacedPosition = entry.SpawnPosition;
                if ((object)enemy.specRigidbody != null)
                {
                    enemy.specRigidbody.Initialize();
                }

                enemy.IgnoreForRoomClear = entry.IgnoreForRoomClear;
                enemy.HasDonePlayerEnterCheck = true;
                enemy.HasBeenEngaged = true;
                if (enemy.healthHaver != null && enemy.healthHaver.IsBoss)
                {
                    RestoreReplayedBossVisibility(enemy);
                    BossAudioDiagnosticsHooks.StartReplayedTankTreaderIdle(enemy);
                }

                actualWave.Add(new RoomEnemyReplayEntry(
                    enemy.EnemyGuid,
                    entry.SpawnPosition,
                    enemy.transform.position.IntXY(),
                    enemy.IgnoreForRoomClear));
                Log(
                    "Recorded replay spawn state. ExpectedRoom=" + RoomLabel(room) +
                    ", Enemy=" + DescribeActiveEnemy(enemy) +
                    ", ParentRoomMatchesExpected=" + (enemy.ParentRoom == room) +
                    ", SpawnInsideExpectedRoom=" + room.ContainsPosition(enemy.transform.position.IntXY()) + ".");
                spawned++;
            }

            return spawned;
        }

        public void LogBossSpriteMaterialState(AIActor boss, tk2dSprite[] sprites, string phase)
        {
            if (boss == null || sprites == null)
            {
                return;
            }

            for (int index = 0; index < sprites.Length; index++)
            {
                tk2dSprite sprite = sprites[index];
                if (sprite == null)
                {
                    continue;
                }

                try
                {
                    tk2dSpriteCollectionData collection = sprite.Collection;
                    tk2dSpriteDefinition definition = sprite.CurrentSprite;
                    Renderer spriteRenderer = sprite.renderer;
                    Material sharedMaterial = spriteRenderer != null ? spriteRenderer.sharedMaterial : null;
                    Texture mainTexture = sharedMaterial != null ? sharedMaterial.mainTexture : null;
                    string shaderName = sharedMaterial != null && sharedMaterial.shader != null
                        ? sharedMaterial.shader.name
                        : "<none>";
                    string animationClip = "<none>";
                    tk2dSpriteAnimator animator = sprite.GetComponent<tk2dSpriteAnimator>();
                    if (animator != null && animator.CurrentClip != null)
                    {
                        animationClip = animator.CurrentClip.name;
                    }

                    LogAlways(
                        "Boss sprite material state. Phase=" + phase + ", Enemy=" + boss.EnemyGuid +
                        ", Index=" + index +
                        ", Object=" + sprite.gameObject.name +
                        ", ActiveSelf=" + sprite.gameObject.activeSelf +
                        ", ActiveInHierarchy=" + sprite.gameObject.activeInHierarchy +
                        ", Enabled=" + sprite.enabled +
                        ", RendererPresent=" + (spriteRenderer != null) +
                        ", RendererEnabled=" + (spriteRenderer != null && spriteRenderer.enabled) +
                        ", SpriteId=" + sprite.spriteId +
                        ", SpriteDefinition=" + (definition != null ? definition.name : "<null>") +
                        ", DefinitionMaterial=" + (definition != null && definition.material != null ? definition.material.name : "<null>") +
                        ", DefinitionMaterialInst=" + (definition != null && definition.materialInst != null ? definition.materialInst.name : "<null>") +
                        ", Collection=" + (collection != null ? collection.name : "<null>") +
                        ", CollectionAsset=" + (collection != null ? collection.assetName : "<null>") +
                        ", CollectionName=" + (collection != null ? collection.spriteCollectionName : "<null>") +
                        ", CollectionDefinitions=" + (collection != null && collection.spriteDefinitions != null ? collection.spriteDefinitions.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-1") +
                        ", CollectionMaterials=" + (collection != null && collection.materials != null ? collection.materials.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-1") +
                        ", CollectionMaterialInsts=" + (collection != null && collection.materialInsts != null ? collection.materialInsts.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-1") +
                        ", CollectionTextures=" + (collection != null && collection.textures != null ? collection.textures.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-1") +
                        ", SharedMaterial=" + (sharedMaterial != null ? sharedMaterial.name : "<null>") +
                        ", Shader=" + shaderName +
                        ", MainTexture=" + (mainTexture != null ? mainTexture.name : "<null>") +
                        ", AnimationClip=" + animationClip + ".");
                }
                catch (Exception exception)
                {
                    LogAlways(
                        "Boss sprite material state failed. Phase=" + phase + ", Enemy=" + boss.EnemyGuid +
                        ", Index=" + index +
                        ", Object=" + sprite.gameObject.name +
                        ", Exception=" + exception.GetType().Name + ":" + exception.Message + ".");
                }
            }
        }

        private void RestoreReplayedBossVisibility(AIActor boss)
        {
            if ((object)boss == null)
            {
                return;
            }

            tk2dSprite[] spritesBefore = boss.GetComponentsInChildren<tk2dSprite>(true);
            int disabledBefore = CountDisabledSprites(spritesBefore);
            boss.State = AIActor.ActorState.Normal;
            boss.invisibleUntilAwaken = false;
            boss.ToggleRenderers(true);
            boss.IsGone = false;
            if (boss.specRigidbody != null)
            {
                boss.specRigidbody.CollideWithOthers = true;
                boss.specRigidbody.Reinitialize();
            }

            if (boss.healthHaver != null)
            {
                boss.healthHaver.IsVulnerable = true;
            }

            tk2dSprite[] allSprites = boss.GetComponentsInChildren<tk2dSprite>(true);
            int activatedSpriteObjects = 0;
            for (int index = 0; index < allSprites.Length; index++)
            {
                tk2dSprite sprite = allSprites[index];
                if (sprite == null)
                {
                    continue;
                }

                if (!sprite.gameObject.activeSelf)
                {
                    sprite.gameObject.SetActive(true);
                    activatedSpriteObjects++;
                }

                sprite.enabled = true;
            }

            tk2dSprite[] spritesAfter = boss.GetComponentsInChildren<tk2dSprite>(true);
            LogBossSpriteMaterialState(boss, spritesAfter, "AfterSpawn");
            Log(
                "Restored replayed Boss visibility. Enemy=" + boss.EnemyGuid +
                ", State=" + boss.State +
                ", InvisibleUntilAwaken=" + boss.invisibleUntilAwaken +
                ", IsGone=" + boss.IsGone +
                ", RendererEnabled=" + (boss.renderer != null && boss.renderer.enabled) +
                ", Sprites=" + spritesAfter.Length +
                ", ActivatedSpriteObjects=" + activatedSpriteObjects +
                ", DisabledSpritesBefore=" + disabledBefore +
                ", DisabledSpritesAfter=" + CountDisabledSprites(spritesAfter) +
                ", CollideWithOthers=" + (boss.specRigidbody != null && boss.specRigidbody.CollideWithOthers) +
                ", IsVulnerable=" + (boss.healthHaver != null && boss.healthHaver.IsVulnerable) + ".");
        }

        private static int CountDisabledSprites(tk2dSprite[] sprites)
        {
            if (sprites == null)
            {
                return 0;
            }

            int disabled = 0;
            for (int index = 0; index < sprites.Length; index++)
            {
                if (sprites[index] != null && !sprites[index].enabled)
                {
                    disabled++;
                }
            }

            return disabled;
        }

        private string RoomLabel(RoomHandler room)
        {
            return _roomLabelProvider(room);
        }

        private string DescribeActiveEnemy(AIActor enemy)
        {
            if ((object)enemy == null)
            {
                return "<null>";
            }

            IntVector2 worldPosition = enemy.transform.position.IntXY();
            RoomHandler parentRoom = enemy.ParentRoom;
            return
                "Guid=" + enemy.EnemyGuid +
                " Placed=" + enemy.PlacedPosition.x + "," + enemy.PlacedPosition.y +
                " World=" + worldPosition.x + "," + worldPosition.y +
                " ParentRoom=" + RoomLabel(parentRoom) +
                " IgnoreForRoomClear=" + enemy.IgnoreForRoomClear;
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log(message);
            }
        }

        private void LogAlways(string message)
        {
            if (_logAlways != null)
            {
                _logAlways(message);
            }
        }

        private void LogWarning(string message)
        {
            if (_logWarning != null)
            {
                _logWarning(message);
            }
        }
    }
}
