using NUnit.Framework;
using System;
using System.Runtime.Intrinsics;

namespace GxHash.Tests;

public class GxHashTests
{
    [TestCase(1)]
    [TestCase(4)]
    [TestCase(15)]
    [TestCase(16)]
    [TestCase(17)]
    [TestCase(255)]
    [TestCase(256)]
    [TestCase(1024)]
    [TestCase(3000)]
    [TestCase(56789)]
    [Repeat(100)]
    public void AllBytesAreRead(int payloadSize)
    {
        Random random = new Random(0);

        byte[] bytes = new byte[payloadSize];
        byte[] bytesTweaked = new byte[payloadSize];
        random.NextBytes(bytes);

        UnsafeUtils.FlipRandomBit(bytes, bytesTweaked);

        int hash = GxHash.Hash32(bytes, 42);
        int hashTweaked = GxHash.Hash32(bytesTweaked, 42);

        Assert.AreNotEqual(hash, hashTweaked);
    }

    [TestCase(1, 0, 1)]
    [TestCase(1, 0, 16)]
    [TestCase(1, 0, 32)]
    [TestCase(16, 0, 16)]
    [TestCase(16, 0, 32)]
    [TestCase(16, 16, 32)]
    [TestCase(16, 32, 48)]
    [TestCase(32, 0, 32)]
    [TestCase(32, 0, 64)]
    [TestCase(32, 32, 64)]
    [TestCase(32, 64, 96)]
    public void BytesOrderMatters(int swapSize, int swapPositionA, int swapPositionB)
    {
        Random rnd = new Random(123);
        byte[] bytes = new byte[255];
        rnd.NextBytes(bytes);

        int hash = GxHash.Hash32(bytes, 0);

        SwapBytes(bytes, swapPositionA, swapPositionB, swapSize);

        int hashAfterSwap = GxHash.Hash32(bytes, 0);

        Assert.AreNotEqual(hash, hashAfterSwap);
    }

    private static void SwapBytes(Span<byte> span, int pos1, int pos2, int n)
    {
        // Check if the input parameters are valid
        if (pos1 < 0 || pos2 < 0 || n < 0)
        {
            throw new ArgumentOutOfRangeException("Positions and length must be non-negative.");
        }
        if (pos1 + n > span.Length || pos2 + n > span.Length)
        {
            throw new ArgumentOutOfRangeException("Positions and length must be within the span's length.");
        }

        // Perform the swap
        Span<byte> temp = stackalloc byte[n];
        span.Slice(pos1, n).CopyTo(temp);
        span.Slice(pos2, n).CopyTo(span.Slice(pos1, n));
        temp.CopyTo(span.Slice(pos2, n));
    }
}
