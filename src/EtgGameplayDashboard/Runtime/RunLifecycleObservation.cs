// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace EtgGameplayDashboard
{
    internal enum RunLifecycleResetKind
    {
        None,
        EnteredCharacterSelectHub,
        PrimaryPlayerChanged
    }

    internal sealed class RunLifecycleObservation
    {
        public RunLifecycleObservation(
            string sceneName,
            string previousSceneName,
            bool sceneChanged,
            bool playerChanged,
            bool isGrantableDungeonScene,
            RunLifecycleResetKind resetKind)
        {
            SceneName = sceneName;
            PreviousSceneName = previousSceneName;
            SceneChanged = sceneChanged;
            PlayerChanged = playerChanged;
            IsGrantableDungeonScene = isGrantableDungeonScene;
            ResetKind = resetKind;
        }

        public string SceneName { get; private set; }

        public string PreviousSceneName { get; private set; }

        public bool SceneChanged { get; private set; }

        public bool PlayerChanged { get; private set; }

        public bool IsGrantableDungeonScene { get; private set; }

        public RunLifecycleResetKind ResetKind { get; private set; }

        public bool ShouldScheduleGrant
        {
            get { return IsGrantableDungeonScene && (SceneChanged || PlayerChanged); }
        }
    }
}
