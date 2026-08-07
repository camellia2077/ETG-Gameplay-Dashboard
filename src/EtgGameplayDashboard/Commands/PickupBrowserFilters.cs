// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace EtgGameplayDashboard
{
    internal enum PickupBrowserFilter
    {
        All,
        Gun,
        Passive,
        Active,
    }

    internal enum PickupQualityFilter
    {
        All,
        D,
        C,
        B,
        A,
        S,
        Special,
        Excluded,
    }

    internal enum PickupGunClassFilter
    {
        All,
        Pistol,
        FullAuto,
        Shotgun,
        Rifle,
        Beam,
        Charge,
        Explosive,
        Elemental,
        Special,
    }

    internal enum PickupPassiveSubcategoryFilter
    {
        All,
        Bullet,
    }

    internal enum PickupActiveCooldownFilter
    {
        All,
        Uses,
        Damage,
        Time,
        Room,
    }
}
