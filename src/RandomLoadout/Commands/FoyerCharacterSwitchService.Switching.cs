// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Dungeonator;
using RandomLoadout.Core.Input;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class FoyerCharacterSwitchService
    {
        private static bool CanUseForceSwitchFallback(string label)
        {
            string[] prefabSuffixes;
            return TryGetCharacterPrefabSuffixes(label, out prefabSuffixes);
        }

        private bool TryForceSwitchCharacterInBreach(Foyer foyer, string label, bool switchSecondaryPlayer, out string failureMessage)
        {
            failureMessage = string.Empty;
            if ((object)foyer == null)
            {
                failureMessage = GuiText.Get("result.characters.breach_only_switch");
                return false;
            }

            string[] prefabSuffixes;
            if (!TryGetCharacterPrefabSuffixes(label, out prefabSuffixes))
            {
                failureMessage = GuiText.Get("result.characters.force_switch_not_configured", GuiText.GetCharacterLabel(label));
                LogCharacterSwitchDiagnostic("Force switch rejected: no prefab suffix mapping. Label=" + label + ".");
                return false;
            }

            LogCharacterSwitchDiagnostic("Force switch prefab lookup. Label=" + label + ", Candidates=" + DescribePrefabCandidates(prefabSuffixes) + ".");

            GameManager gameManager = GameManager.Instance;
            PlayerController currentPlayer = switchSecondaryPlayer
                ? (object)gameManager != null ? gameManager.SecondaryPlayer : null
                : (object)gameManager != null ? gameManager.PrimaryPlayer : null;
            if ((object)currentPlayer == null)
            {
                failureMessage = GuiText.Get(switchSecondaryPlayer
                    ? "result.characters.select_secondary_character_first"
                    : "result.characters.select_character_first");
                LogCharacterSwitchDiagnostic("Force switch rejected: current target player is null. Target=" + (switchSecondaryPlayer ? "P2" : "P1") + ".");
                return false;
            }

            GameObject prefab = LoadCharacterPrefab(prefabSuffixes);
            if ((object)prefab == null)
            {
                failureMessage = GuiText.Get("result.characters.prefab_not_found", GuiText.GetCharacterLabel(label));
                LogCharacterSwitchDiagnostic("Force switch rejected: prefab not found. Label=" + label + ", Candidates=" + DescribePrefabCandidates(prefabSuffixes) + ".");
                return false;
            }

            LogCharacterSwitchDiagnostic("Force switch prefab loaded. Label=" + label + ", Prefab=" + prefab.name + ", HasController=" + ((object)prefab.GetComponent<PlayerController>() != null) + ".");

            bool usedRandomGuns = currentPlayer.CharacterUsesRandomGuns;
            Vector3 spawnPosition = currentPlayer.transform.position;
            RoomHandler currentRoom = currentPlayer.CurrentRoom;
            LogCharacterSwitchDiagnostic("Replacing " + (switchSecondaryPlayer ? "P2" : "P1") + ". Current=" + DescribePlayer(currentPlayer) + ", State=" + DescribeGameManagerPlayers() + ".");

            if ((object)Pixelator.Instance != null)
            {
                Pixelator.Instance.FadeToBlack(0.25f, false, 0f);
            }

            currentPlayer.SetInputOverride("randomloadout_force_character_switch");
            // Destroy is deferred until the end of the frame. Hide the previous player first so
            // RefreshAllPlayers cannot cache both the pending-destroy player and its replacement.
            // A stale cache containing that destroyed P2 makes GameManager clear SecondaryPlayer
            // on the next frame, which prevents all later P2 character switches.
            currentPlayer.gameObject.SetActive(false);
            if (switchSecondaryPlayer)
            {
                gameManager.ClearSecondaryPlayer();
            }
            else
            {
                gameManager.ClearPrimaryPlayer();
            }
            LogCharacterSwitchDiagnostic("Target cleared. State=" + DescribeGameManagerPlayers() + ".");

            GameManager.PlayerPrefabForNewGame = prefab;
            PlayerController prefabController = prefab.GetComponent<PlayerController>();
            if ((object)prefabController == null)
            {
                GameManager.PlayerPrefabForNewGame = null;
                failureMessage = GuiText.Get("result.characters.prefab_missing_controller", GuiText.GetCharacterLabel(label));
                LogCharacterSwitchDiagnostic("Force switch rejected: loaded prefab has no PlayerController. Label=" + label + ", Prefab=" + prefab.name + ".");
                return false;
            }

            GameStatsManager stats = GameStatsManager.Instance;
            if (!switchSecondaryPlayer && (object)stats != null)
            {
                stats.BeginNewSession(prefabController);
            }

            GameObject playerObject = UnityEngine.Object.Instantiate(prefab, spawnPosition, Quaternion.identity) as GameObject;
            GameManager.PlayerPrefabForNewGame = null;
            if ((object)playerObject == null)
            {
                failureMessage = GuiText.Get("result.characters.instantiate_failed", GuiText.GetCharacterLabel(label));
                LogCharacterSwitchDiagnostic("Force switch rejected: Instantiate returned null. Label=" + label + ".");
                return false;
            }

            playerObject.SetActive(true);
            PlayerController selectedPlayer = playerObject.GetComponent<PlayerController>();
            if ((object)selectedPlayer == null)
            {
                UnityEngine.Object.Destroy(playerObject);
                failureMessage = GuiText.Get("result.characters.controller_init_failed", GuiText.GetCharacterLabel(label));
                LogCharacterSwitchDiagnostic("Force switch rejected: instantiated object has no PlayerController. Label=" + label + ", Object=" + playerObject.name + ".");
                return false;
            }

            if (switchSecondaryPlayer)
            {
                gameManager.SecondaryPlayer = selectedPlayer;
                selectedPlayer.PlayerIDX = 1;
            }
            else
            {
                gameManager.PrimaryPlayer = selectedPlayer;
                selectedPlayer.PlayerIDX = 0;
            }

            if ((object)currentRoom != null)
            {
                selectedPlayer.ForceChangeRoom(currentRoom);
            }
            LogCharacterSwitchDiagnostic("Replacement registered. NewPlayer=" + DescribePlayer(selectedPlayer) + ", State=" + DescribeGameManagerPlayers() + ".");
            if ((object)gameManager.MainCameraController != null)
            {
                gameManager.MainCameraController.ClearPlayerCache();
                gameManager.MainCameraController.SetManualControl(false, true);
            }

            // Skip Breach character-select callbacks in switch-only mode to avoid
            // side effects such as currency costs on hidden-character selections.
            if (switchSecondaryPlayer)
            {
                FinalizeSecondaryCharacterSwitch(selectedPlayer);
            }
            else
            {
                FinalizeCharacterSwitch(foyer, selectedPlayer, false);
            }
            LogCharacterSwitchDiagnostic("Replacement finalized. State=" + DescribeGameManagerPlayers() + ".");

            if (usedRandomGuns)
            {
                selectedPlayer.CharacterUsesRandomGuns = true;
            }

            if ((object)Pixelator.Instance != null)
            {
                Pixelator.Instance.FadeToBlack(0.25f, true, 0f);
            }

            return true;
        }

        private static string DescribePrefabCandidates(string[] prefabSuffixes)
        {
            if (prefabSuffixes == null || prefabSuffixes.Length == 0)
            {
                return "<none>";
            }

            List<string> candidates = new List<string>();
            for (int suffixIndex = 0; suffixIndex < prefabSuffixes.Length; suffixIndex++)
            {
                string suffix = prefabSuffixes[suffixIndex];
                if (string.IsNullOrEmpty(suffix))
                {
                    continue;
                }

                candidates.Add("Player" + suffix);
                candidates.Add("Player" + suffix.ToLowerInvariant());
                candidates.Add("Player" + char.ToUpperInvariant(suffix[0]) + suffix.Substring(1));
            }

            return candidates.Count == 0 ? "<none>" : string.Join("|", candidates.ToArray());
        }

        private void FinalizeSecondaryCharacterSwitch(PlayerController selectedPlayer)
        {
            GameManager gameManager = GameManager.Instance;
            PlayerController primaryPlayer = (object)gameManager != null ? gameManager.PrimaryPlayer : null;
            CleanupExtraPlayers(primaryPlayer, selectedPlayer);

            if ((object)gameManager != null)
            {
                gameManager.RefreshAllPlayers();
            }

            // PlayerController initializes its BraveInput actions during Instantiate/Awake,
            // before the replacement receives its final PlayerIDX. Rebuild the controller
            // assignments after registering both players so a replacement P2 keeps the
            // controller assigned to P2 instead of falling back to keyboard/P1 input.
            _playerInputOwnershipService.RebindAfterCharacterSwitch(
                PlayerSlot.Secondary);
            LogCharacterSwitchDiagnostic("Controller assignments reassigned after replacement. State=" + DescribeGameManagerPlayers() + ".");
        }

        private static string DescribeGameManagerPlayers()
        {
            try
            {
                GameManager gameManager = GameManager.Instance;
                if ((object)gameManager == null)
                {
                    return "GameManager=<null>";
                }

                return "GameType=" + gameManager.CurrentGameType +
                       ", P1=" + DescribePlayer(gameManager.PrimaryPlayer) +
                       ", P2=" + DescribePlayer(gameManager.SecondaryPlayer);
            }
            catch (Exception exception)
            {
                return "StateReadFailed=" + exception.GetType().Name;
            }
        }

        private static string DescribePlayer(PlayerController player)
        {
            if ((object)player == null)
            {
                return "<null>";
            }

            try
            {
                return "Id=" + player.GetInstanceID() +
                       ", Name=" + player.name +
                       ", Index=" + player.PlayerIDX +
                       ", Character=" + player.characterIdentity +
                       ", Active=" + player.gameObject.activeInHierarchy;
            }
            catch (Exception exception)
            {
                return "StateReadFailed=" + exception.GetType().Name;
            }
        }

        private static bool TryGetCharacterPrefabSuffixes(string label, out string[] prefabSuffixes)
        {
            prefabSuffixes = null;
            if (string.IsNullOrEmpty(label))
            {
                return false;
            }

            switch (label)
            {
                case "Marine":
                    // In this environment the Marine character prefab is named "marine".
                    // Do not use "soldier" here or force-switch loading will fail.
                    prefabSuffixes = new[] { "marine" };
                    return true;
                case "Hunter":
                    prefabSuffixes = new[] { "guide" };
                    return true;
                case "Pilot":
                    // The foyer flag identifies Pilot as PlayerRogue in this ETG build.
                    // The display label is Pilot, but the runtime prefab is not PlayerPilot.
                    prefabSuffixes = new[] { "rogue" };
                    return true;
                case "Convict":
                    prefabSuffixes = new[] { "convict" };
                    return true;
                case "Cultist":
                    prefabSuffixes = new[] { "CoopCultist" };
                    return true;
                case "Robot":
                    prefabSuffixes = new[] { "robot" };
                    return true;
                case "Bullet":
                    prefabSuffixes = new[] { "bullet" };
                    return true;
                case "Paradox":
                    prefabSuffixes = new[] { "eevee" };
                    return true;
                case "Gunslinger":
                    prefabSuffixes = new[] { "gunslinger" };
                    return true;
                default:
                    return false;
            }
        }

        private IEnumerator SwitchCharacterRoutine(Foyer foyer, FoyerCharacterSelectFlag flag, HashSet<int> existingPlayerIds)
        {
            yield return foyer.StartCoroutine(foyer.OnSelectedCharacter(0f, flag));

            float deadline = Time.unscaledTime + PendingSelectionTimeoutSeconds;
            PlayerController selectedPlayer = null;
            while (Time.unscaledTime < deadline)
            {
                selectedPlayer = FindNewestPlayer(existingPlayerIds);
                if ((object)selectedPlayer == null)
                {
                    GameManager gameManager = GameManager.Instance;
                    if ((object)gameManager != null)
                    {
                        selectedPlayer = gameManager.PrimaryPlayer;
                    }
                }

                if ((object)selectedPlayer != null && !Foyer.IsCurrentlyPlayingCharacterSelect)
                {
                    break;
                }

                yield return null;
            }

            if ((object)selectedPlayer != null)
            {
                FinalizeCharacterSwitch(foyer, selectedPlayer, true);
            }

            ClearPendingSelection();
        }

        private void RefreshPendingSelectionState(Foyer foyer)
        {
            if ((object)_pendingSelectionFlag == null)
            {
                return;
            }

            if ((object)foyer == null)
            {
                ClearPendingSelection();
                return;
            }

            if ((object)foyer.CurrentSelectedCharacterFlag == (object)_pendingSelectionFlag && !Foyer.IsCurrentlyPlayingCharacterSelect)
            {
                ClearPendingSelection();
                return;
            }

            if (Time.unscaledTime - _pendingSelectionStartedAt >= PendingSelectionTimeoutSeconds)
            {
                ClearPendingSelection();
            }
        }

        private void ClearPendingSelection()
        {
            _pendingSelectionFlag = null;
            _pendingSelectionStartedAt = 0f;
        }

        private static HashSet<int> CaptureCurrentPlayerInstanceIds()
        {
            HashSet<int> instanceIds = new HashSet<int>();
            PlayerController[] players = UnityEngine.Object.FindObjectsOfType(typeof(PlayerController)) as PlayerController[];
            if (players == null)
            {
                return instanceIds;
            }

            for (int i = 0; i < players.Length; i++)
            {
                PlayerController player = players[i];
                if ((object)player != null)
                {
                    instanceIds.Add(player.GetInstanceID());
                }
            }

            return instanceIds;
        }

        private static PlayerController FindNewestPlayer(HashSet<int> existingPlayerIds)
        {
            PlayerController[] players = UnityEngine.Object.FindObjectsOfType(typeof(PlayerController)) as PlayerController[];
            if (players == null || players.Length == 0)
            {
                return null;
            }

            PlayerController newestPlayer = null;
            int newestInstanceId = int.MinValue;
            for (int i = 0; i < players.Length; i++)
            {
                PlayerController player = players[i];
                if ((object)player == null)
                {
                    continue;
                }

                int instanceId = player.GetInstanceID();
                if (existingPlayerIds.Contains(instanceId))
                {
                    continue;
                }

                if ((object)newestPlayer == null || instanceId > newestInstanceId)
                {
                    newestPlayer = player;
                    newestInstanceId = instanceId;
                }
            }

            return newestPlayer;
        }

        private void FinalizeCharacterSwitch(Foyer foyer, PlayerController selectedPlayer, bool notifyFoyerCharacterChanged)
        {
            if (notifyFoyerCharacterChanged && (object)foyer != null)
            {
                foyer.PlayerCharacterChanged(selectedPlayer);
            }

            GameManager gameManager = GameManager.Instance;
            PlayerController coopPlayer = null;
            if ((object)gameManager != null)
            {
                coopPlayer = gameManager.SecondaryPlayer;
                gameManager.PrimaryPlayer = selectedPlayer;
            }

            CleanupExtraPlayers(selectedPlayer, coopPlayer);

            if ((object)gameManager != null)
            {
                gameManager.RefreshAllPlayers();
            }

            // The native foyer replacement also instantiates the new player before the
            // final PlayerIDX/primary-player registration is complete.
            _playerInputOwnershipService.RebindAfterCharacterSwitch(PlayerSlot.Primary);
        }

        private static void CleanupExtraPlayers(PlayerController selectedPlayer, PlayerController coopPlayer)
        {
            PlayerController[] players = UnityEngine.Object.FindObjectsOfType(typeof(PlayerController)) as PlayerController[];
            if (players == null)
            {
                return;
            }

            for (int i = 0; i < players.Length; i++)
            {
                PlayerController player = players[i];
                if ((object)player == null)
                {
                    continue;
                }

                if ((object)player == (object)selectedPlayer || (object)player == (object)coopPlayer)
                {
                    continue;
                }

                UnityEngine.Object.Destroy(player.gameObject);
            }
        }

    }
}
