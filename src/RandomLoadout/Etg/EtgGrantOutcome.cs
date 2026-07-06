// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed class EtgGrantOutcome
    {
        public EtgGrantOutcome(
            PickupCategory category,
            int pickupId,
            string pickupLabel,
            bool succeeded,
            string failureReason,
            string grantPath,
            string grantDetail)
        {
            Category = category;
            PickupId = pickupId;
            PickupLabel = pickupLabel;
            Succeeded = succeeded;
            FailureReason = failureReason;
            GrantPath = grantPath ?? string.Empty;
            GrantDetail = grantDetail ?? string.Empty;
        }

        public PickupCategory Category { get; private set; }

        public int PickupId { get; private set; }

        public string PickupLabel { get; private set; }

        public bool Succeeded { get; private set; }

        public string FailureReason { get; private set; }

        public string GrantPath { get; private set; }

        public string GrantDetail { get; private set; }
    }
}
