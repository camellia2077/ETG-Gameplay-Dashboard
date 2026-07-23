// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using RandomLoadout.Core.Input;

namespace RandomLoadout.Core.Tests
{
    internal static class PlayerInputOwnershipServiceTests
    {
        public static void RebindsPrimaryAfterPrimaryCharacterSwitch()
        {
            int calls = 0;
            PlayerInputOwnershipService service = new PlayerInputOwnershipService(delegate { calls++; });

            service.RebindAfterCharacterSwitch(PlayerSlot.Primary);

            AssertEx.Equal(1, calls, "Primary replacement should reassign controllers once.");
            AssertEx.Equal(1, service.ReassignmentCount, "Primary replacement should update the reassignment count.");
        }

        public static void RebindsSecondaryAfterSecondaryCharacterSwitch()
        {
            int calls = 0;
            PlayerInputOwnershipService service = new PlayerInputOwnershipService(delegate { calls++; });

            service.RebindAfterCharacterSwitch(PlayerSlot.Secondary);

            AssertEx.Equal(1, calls, "Secondary replacement should reassign controllers once.");
            AssertEx.Equal(1, service.ReassignmentCount, "Secondary replacement should update the reassignment count.");
        }

        public static void RebindsBothPlayersIndependently()
        {
            int calls = 0;
            PlayerInputOwnershipService service = new PlayerInputOwnershipService(delegate { calls++; });

            service.RebindAfterCharacterSwitch(PlayerSlot.Primary);
            service.RebindAfterCharacterSwitch(PlayerSlot.Secondary);

            AssertEx.Equal(2, calls, "P1/P2 replacements should each reassign controllers.");
            AssertEx.Equal(2, service.ReassignmentCount, "P1/P2 replacements should remain independent.");
        }
    }
}
