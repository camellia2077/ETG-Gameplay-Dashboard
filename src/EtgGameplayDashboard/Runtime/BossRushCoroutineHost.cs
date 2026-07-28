// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed class BossRushCoroutineHost : MonoBehaviour
    {
        public Coroutine StartRoutine(IEnumerator routine)
        {
            return routine != null ? StartCoroutine(routine) : null;
        }

        public void StopRoutine(Coroutine routine)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }
    }
}
