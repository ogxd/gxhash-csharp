using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GxHash;

public static class UnsafeUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void FlipRandomBit(ReadOnlySpan<byte> input, Span<byte> output)
    {
        unchecked
        {
            input.CopyTo(output);

            int bit = Random.Shared.Next(0, input.Length * 8);

            fixed (byte* p = &MemoryMarshal.GetReference(output))
            {
                // Swap 1 random bit
                p[bit / 8] ^= (byte)(1 << (bit % 8));
            }
        }
    }
}