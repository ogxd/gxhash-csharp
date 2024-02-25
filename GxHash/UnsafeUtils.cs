using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GxHash;

public static class UnsafeUtils
{
    public static unsafe bool IsAligned<T>(ref T t)
        where T : unmanaged
    {
        UIntPtr ptr = new UIntPtr(Unsafe.AsPointer(ref t));
        uint length = (uint)Unsafe.SizeOf<T>();
        return ptr % length == 0;
    }
    
    public static unsafe void ThrowIfUnaligned<T>(this ref T t)
        where T : unmanaged
    {
        UIntPtr ptr = new UIntPtr(Unsafe.AsPointer(ref t));
        uint length = (uint)Unsafe.SizeOf<T>();
        if (ptr % length != 0)
        {
            throw new DataMisalignedException($"{t} is unaligned");
        }
    }

    public static unsafe uint GetMostSignificantBit(int value)
    {
        double ff = value | 1;
        return (*(1 + (uint*) & ff) >> 20) - 1023;  // assumes x86 endianness
    }

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