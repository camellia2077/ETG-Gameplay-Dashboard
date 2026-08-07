// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx;

namespace EtgGameplayDashboard
{
    [BepInPlugin(PluginGuid, NAME, VERSION)]
    public sealed partial class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "etg-gameplay-dashboard";
        public const string NAME = "EtgGameplayDashboard";
        public const string VERSION = BuildVersionInfo.Version;
    }
}
