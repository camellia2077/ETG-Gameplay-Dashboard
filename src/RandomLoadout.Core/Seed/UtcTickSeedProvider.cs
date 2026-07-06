// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;

namespace RandomLoadout.Core
{
    public sealed class UtcTickSeedProvider : ISeedProvider
    {
        public int CreateSeed()
        {
            return unchecked((int)DateTime.UtcNow.Ticks);
        }
    }
}
