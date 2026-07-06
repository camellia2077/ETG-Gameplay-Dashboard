// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Linq;

namespace RandomLoadout.Core.Tests
{
    internal static class AssertEx
    {
        public static void True(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        public static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new InvalidOperationException(message + " Expected=" + expected + " Actual=" + actual);
            }
        }

        public static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message)
        {
            if (!expected.SequenceEqual(actual))
            {
                throw new InvalidOperationException(
                    message + " Expected=[" + string.Join(", ", expected.Select(value => value.ToString()).ToArray()) + "] Actual=[" +
                    string.Join(", ", actual.Select(value => value.ToString()).ToArray()) + "]");
            }
        }
    }
}
