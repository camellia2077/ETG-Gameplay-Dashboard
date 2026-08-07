// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using EtgGameplayDashboard.Core;

namespace EtgGameplayDashboard
{
    internal static class DefaultLoadoutRuleDefinitionFactory
    {
        private static readonly int[] DefaultGunPoolIds = { 15, 61, 80, 98, 181, 223, 251 };
        private static readonly int[] DefaultPassivePoolIds = { 102, 111, 131, 134, 165, 204, 213 };
        private static readonly int[] DefaultActivePoolIds = { 64, 69, 71, 77, 201, 250 };

        public static LoadoutRuleDefinition[] CreateDefault()
        {
            return new[]
            {
                LoadoutRuleDefinition.Random(
                    PickupCategory.Gun,
                    1,
                    DefaultGunPoolIds),
                LoadoutRuleDefinition.Random(
                    PickupCategory.Passive,
                    1,
                    DefaultPassivePoolIds),
                LoadoutRuleDefinition.Random(
                    PickupCategory.Active,
                    1,
                    DefaultActivePoolIds),
            };
        }

        public static LoadoutRuleDefinition[] CreateMixedExample()
        {
            return new[]
            {
                LoadoutRuleDefinition.Random(
                    PickupCategory.Gun,
                    1,
                    DefaultGunPoolIds),
                LoadoutRuleDefinition.Specific(PickupCategory.Passive, "Scope"),
                LoadoutRuleDefinition.Specific(PickupCategory.Active, "Bullet Time"),
            };
        }
    }
}
