// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Linq;

namespace EtgGameplayDashboard.Core
{
    public sealed class LoadoutConfig
    {
        public LoadoutConfig(IEnumerable<LoadoutRuleConfig> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException("rules");
            }

            Rules = rules.ToArray();
        }

        public LoadoutRuleConfig[] Rules { get; private set; }
    }
}
