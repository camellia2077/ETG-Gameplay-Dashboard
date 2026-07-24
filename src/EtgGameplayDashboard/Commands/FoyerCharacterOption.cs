// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace EtgGameplayDashboard
{
    internal sealed class FoyerCharacterOption
    {
        public FoyerCharacterOption(string label, bool isSelectable, bool isSelected, bool isPending, FoyerCharacterSelectFlag flag, bool canUnlock)
        {
            Label = label ?? string.Empty;
            IsSelectable = isSelectable;
            IsSelected = isSelected;
            IsPending = isPending;
            Flag = flag;
            CanUnlock = canUnlock;
        }

        public string Label { get; private set; }

        public bool IsSelectable { get; private set; }

        public bool IsSelected { get; private set; }

        public bool IsPending { get; private set; }

        public FoyerCharacterSelectFlag Flag { get; private set; }

        public bool CanUnlock { get; private set; }

        public bool IsLocked
        {
            get { return !IsSelected && !IsSelectable; }
        }
    }
}
