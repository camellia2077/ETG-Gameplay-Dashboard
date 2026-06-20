using System;

namespace RandomLoadout.Core
{
    public sealed class LoadoutPoolEntryConfig
    {
        public LoadoutPoolEntryConfig(PickupCategory category, int pickupId)
        {
            if (pickupId <= 0)
            {
                throw new ArgumentOutOfRangeException("pickupId");
            }

            Category = category;
            PickupId = pickupId;
        }

        public PickupCategory Category { get; private set; }

        public int PickupId { get; private set; }
    }
}
