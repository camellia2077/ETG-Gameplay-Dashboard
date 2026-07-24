// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;

namespace EtgGameplayDashboard.Core
{
    public sealed class SelectionWarning
    {
        public SelectionWarning(PickupCategory? category, string code, string message)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("A warning code is required.", "code");
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("A warning message is required.", "message");
            }

            Category = category;
            Code = code;
            Message = message;
        }

        public PickupCategory? Category { get; private set; }

        public string Code { get; private set; }

        public string Message { get; private set; }
    }
}
