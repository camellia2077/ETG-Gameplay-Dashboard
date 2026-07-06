// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace RandomLoadout
{
    internal sealed class AliasLoadResult
    {
        public AliasLoadResult(PickupAliasRegistry registry, string[] messages, string[] warnings)
        {
            Registry = registry;
            Messages = messages ?? new string[0];
            Warnings = warnings ?? new string[0];
        }

        public PickupAliasRegistry Registry { get; private set; }

        public string[] Messages { get; private set; }

        public string[] Warnings { get; private set; }
    }
}
