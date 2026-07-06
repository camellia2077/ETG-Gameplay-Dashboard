// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace RandomLoadout
{
    internal sealed class AliasFileModel
    {
        public AliasFileModel()
        {
            Aliases = new AliasEntryModel[0];
        }

        public AliasEntryModel[] Aliases { get; set; }
    }

    internal sealed class AliasEntryModel
    {
        public string Alias { get; set; }

        public int Id { get; set; }
    }
}
