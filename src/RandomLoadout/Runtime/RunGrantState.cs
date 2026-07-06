// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace RandomLoadout
{
    internal sealed class RunGrantState
    {
        public bool HasGrantedThisRun { get; private set; }

        public int CurrentSeed { get; private set; }

        public float GrantReadyAtTime { get; private set; }

        public void ScheduleGrant(float currentTime, float delaySeconds)
        {
            GrantReadyAtTime = currentTime + delaySeconds;
        }

        public bool IsGrantReady(float currentTime)
        {
            return currentTime >= GrantReadyAtTime;
        }

        public void MarkGranted(int seed)
        {
            HasGrantedThisRun = true;
            CurrentSeed = seed;
        }

        public void Reset()
        {
            HasGrantedThisRun = false;
            CurrentSeed = 0;
            GrantReadyAtTime = 0f;
        }
    }
}
