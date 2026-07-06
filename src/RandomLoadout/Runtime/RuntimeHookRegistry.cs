// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;

namespace RandomLoadout
{
    internal sealed class RuntimeHookRegistry
    {
        private readonly List<RuntimeHookRegistration> _registrations = new List<RuntimeHookRegistration>();
        private readonly string _guidPrefix;
        private readonly ManualLogSource _logger;

        public RuntimeHookRegistry(string guidPrefix, ManualLogSource logger)
        {
            _guidPrefix = guidPrefix ?? string.Empty;
            _logger = logger;
        }

        public void Register(string suffix, Action<Harmony, ManualLogSource> installAction, Action uninstallAction)
        {
            if (installAction == null)
            {
                return;
            }

            string normalizedSuffix = suffix ?? string.Empty;
            Harmony harmony = new Harmony(_guidPrefix + normalizedSuffix);
            _registrations.Add(new RuntimeHookRegistration(harmony, installAction, uninstallAction));
        }

        public void InstallAll()
        {
            for (int index = 0; index < _registrations.Count; index++)
            {
                RuntimeHookRegistration registration = _registrations[index];
                registration.InstallAction(registration.Harmony, _logger);
            }
        }

        public void UninstallAll()
        {
            for (int index = 0; index < _registrations.Count; index++)
            {
                RuntimeHookRegistration registration = _registrations[index];
                if (registration.UninstallAction != null)
                {
                    registration.UninstallAction();
                }

                if (registration.Harmony != null)
                {
                    registration.Harmony.UnpatchSelf();
                }
            }
        }

        private struct RuntimeHookRegistration
        {
            public RuntimeHookRegistration(Harmony harmony, Action<Harmony, ManualLogSource> installAction, Action uninstallAction)
            {
                Harmony = harmony;
                InstallAction = installAction;
                UninstallAction = uninstallAction;
            }

            public readonly Harmony Harmony;
            public readonly Action<Harmony, ManualLogSource> InstallAction;
            public readonly Action UninstallAction;
        }
    }
}
