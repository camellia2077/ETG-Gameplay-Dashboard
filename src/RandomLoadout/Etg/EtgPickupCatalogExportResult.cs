// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace RandomLoadout
{
    internal sealed class EtgPickupCatalogExportResult
    {
        public EtgPickupCatalogExportResult(
            bool succeeded,
            string textOutputPath,
            string jsonOutputPath,
            string groupedJsonOutputPath,
            string namesJsonOutputPath,
            string rulePoolOutputPath,
            int entryCount,
            string failureReason)
        {
            Succeeded = succeeded;
            TextOutputPath = textOutputPath ?? string.Empty;
            JsonOutputPath = jsonOutputPath ?? string.Empty;
            GroupedJsonOutputPath = groupedJsonOutputPath ?? string.Empty;
            NamesJsonOutputPath = namesJsonOutputPath ?? string.Empty;
            RulePoolOutputPath = rulePoolOutputPath ?? string.Empty;
            EntryCount = entryCount;
            FailureReason = failureReason ?? string.Empty;
        }

        public bool Succeeded { get; private set; }

        public string TextOutputPath { get; private set; }

        public string JsonOutputPath { get; private set; }

        public string GroupedJsonOutputPath { get; private set; }

        public string NamesJsonOutputPath { get; private set; }

        public string RulePoolOutputPath { get; private set; }

        public int EntryCount { get; private set; }

        public string FailureReason { get; private set; }
    }
}
