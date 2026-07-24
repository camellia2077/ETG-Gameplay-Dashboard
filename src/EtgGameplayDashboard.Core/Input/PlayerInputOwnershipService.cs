// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;

namespace EtgGameplayDashboard.Core.Input
{
    public enum PlayerSlot
    {
        Primary = 0,
        Secondary = 1,
    }

    public sealed class PlayerInputOwnershipService
    {
        private readonly Action _reassignControllers;
        private int _reassignmentCount;

        public PlayerInputOwnershipService(Action reassignControllers)
        {
            _reassignControllers = reassignControllers;
        }

        public int ReassignmentCount
        {
            get { return _reassignmentCount; }
        }

        public void RebindAfterCharacterSwitch(PlayerSlot replacedPlayer)
        {
            if (replacedPlayer != PlayerSlot.Primary && replacedPlayer != PlayerSlot.Secondary)
            {
                throw new ArgumentOutOfRangeException("replacedPlayer");
            }

            _reassignmentCount++;
            if (_reassignControllers != null)
            {
                _reassignControllers();
            }
        }
    }
}
