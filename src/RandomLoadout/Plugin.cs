// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx;

namespace RandomLoadout
{
    [BepInDependency("etgmodding.etg.mtgapi")]
    [BepInPlugin(GUID, NAME, VERSION)]
    public sealed partial class Plugin : BaseUnityPlugin
    {
        public const string GUID = "randomgun.randomloadout";
        public const string NAME = "RandomLoadout";
        public const string VERSION = BuildVersionInfo.Version;
    }
}
