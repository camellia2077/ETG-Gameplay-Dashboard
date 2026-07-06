// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace RandomLoadout.Core
{
    public sealed class GrantCommandRequest
    {
        public GrantCommandRequest(GrantCommandTarget target, string pickupName)
        {
            Target = target;
            PickupName = pickupName;
        }

        public GrantCommandTarget Target { get; private set; }

        public string PickupName { get; private set; }
    }
}
