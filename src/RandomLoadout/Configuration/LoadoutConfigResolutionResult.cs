// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed class LoadoutConfigResolutionResult
    {
        public LoadoutConfigResolutionResult(LoadoutConfig config, SelectionWarning[] warnings)
        {
            Config = config;
            Warnings = warnings;
        }

        public LoadoutConfig Config { get; private set; }

        public SelectionWarning[] Warnings { get; private set; }
    }
}
