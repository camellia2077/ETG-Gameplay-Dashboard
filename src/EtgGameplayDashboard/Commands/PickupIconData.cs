// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;

namespace EtgGameplayDashboard
{
    internal struct PickupIconData
    {
        public static readonly PickupIconData Empty = new PickupIconData(null, Rect.zero);

        public PickupIconData(Texture texture, Rect textureCoords)
        {
            Texture = texture;
            TextureCoords = textureCoords;
        }

        public Texture Texture;
        public Rect TextureCoords;
    }
}
