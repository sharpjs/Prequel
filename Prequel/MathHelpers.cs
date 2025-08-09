// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace Prequel;

internal static class MathHelpers
{
    internal static int GetNextPowerOf2Saturating(int value)
    {
        // https://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2
        // but saturating at int.MaxValue instead of overflow

        if (value <= 0)
            return 1;

        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;

        return value == int.MaxValue
            ? value         // edge case: avoid overflow
            : value + 1;    // normal
    }
}
