// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed class EnemyHealthBarToggleService
    {
        private const int ScouterPickupId = 821;

        private readonly HashSet<PlayerController> _attachedPlayers = new HashSet<PlayerController>();
        private readonly Action<bool> _persistEnabledState;
        private bool _isEnabled;
        private GameObject _healthBarPrefab;

        public EnemyHealthBarToggleService(bool initiallyEnabled, Action<bool> persistEnabledState)
        {
            _isEnabled = initiallyEnabled;
            _persistEnabledState = persistEnabledState;
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
        }

        public GrantCommandExecutionResult Toggle(PlayerController player)
        {
            if (_isEnabled)
            {
                DetachAll();
                _isEnabled = false;
                PersistEnabledState();
                return GrantCommandExecutionResult.Localized(true, "result.enemy_health_bars.disable.success");
            }

            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.enemy_health_bars.no_player");
            }

            if (!TryResolveHealthBarPrefab())
            {
                return GrantCommandExecutionResult.Localized(false, "result.enemy_health_bars.unavailable");
            }

            _isEnabled = true;
            Attach(player);
            PersistEnabledState();
            return GrantCommandExecutionResult.Localized(true, "result.enemy_health_bars.enable.success");
        }

        public void Update(PlayerController player)
        {
            if (_isEnabled && (object)player != null)
            {
                if (!TryResolveHealthBarPrefab())
                {
                    return;
                }

                Attach(player);
            }
        }

        public void Reset()
        {
            DetachAll();
            _isEnabled = false;
            _healthBarPrefab = null;
        }

        private void PersistEnabledState()
        {
            if (_persistEnabledState != null)
            {
                _persistEnabledState(_isEnabled);
            }
        }

        private bool TryResolveHealthBarPrefab()
        {
            if ((object)_healthBarPrefab != null)
            {
                return true;
            }

            RatchetScouterItem scouter = PickupObjectDatabase.GetById(ScouterPickupId) as RatchetScouterItem;
            if ((object)scouter == null || (object)scouter.VFXHealthBar == null)
            {
                return false;
            }

            _healthBarPrefab = scouter.VFXHealthBar;
            return true;
        }

        private void Attach(PlayerController player)
        {
            if (!_attachedPlayers.Add(player))
            {
                return;
            }

            player.OnAnyEnemyReceivedDamage = (Action<float, bool, HealthHaver>)Delegate.Combine(
                player.OnAnyEnemyReceivedDamage,
                new Action<float, bool, HealthHaver>(OnAnyEnemyReceivedDamage));
        }

        private void DetachAll()
        {
            foreach (PlayerController player in _attachedPlayers)
            {
                if ((object)player == null)
                {
                    continue;
                }

                player.OnAnyEnemyReceivedDamage = (Action<float, bool, HealthHaver>)Delegate.Remove(
                    player.OnAnyEnemyReceivedDamage,
                    new Action<float, bool, HealthHaver>(OnAnyEnemyReceivedDamage));
            }

            _attachedPlayers.Clear();
        }

        private void OnAnyEnemyReceivedDamage(float damageAmount, bool fatal, HealthHaver target)
        {
            if ((object)target == null || (object)_healthBarPrefab == null)
            {
                return;
            }

            SpeculativeRigidbody body = target.GetComponent<SpeculativeRigidbody>();
            if ((object)body == null ||
                (object)body.healthHaver == null ||
                body.healthHaver.HasHealthBar ||
                body.healthHaver.HasRatchetHealthBar ||
                body.healthHaver.IsBoss)
            {
                return;
            }

            body.healthHaver.HasRatchetHealthBar = true;
            GameObject healthBar = UnityEngine.Object.Instantiate(_healthBarPrefab);
            SimpleHealthBarController controller = healthBar.GetComponent<SimpleHealthBarController>();
            if ((object)controller != null)
            {
                controller.Initialize(body, body.healthHaver);
            }
        }
    }
}
