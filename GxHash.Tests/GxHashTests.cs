using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GxHash.Tests;

public class GxHashTests
{
    [Test]
    public void ValuesTest()
    {
        Assert.AreEqual(456576800, GxHash.Hash32(Array.Empty<byte>().AsSpan(), 0), "Unexpected hash value");
        Assert.AreEqual(978957914, GxHash.Hash32(new byte[1].AsSpan(), 0), "Unexpected hash value");
        Assert.AreEqual(-969081598, GxHash.Hash32(new byte[1000].AsSpan(), 0), "Unexpected hash value");
        Assert.AreEqual(1827274036, GxHash.Hash32(MemoryMarshal.AsBytes("hello world".AsSpan()), 123), "Unexpected hash value");
    }
    
    [Test]
    public void SanityChecks()
    {
        HashSet<long> hashes = new HashSet<long>();

        // Check that zero filled inputs are hashes differently depending on their size
        byte[] bytes = new byte[1000];
        for (int i = 0; i < bytes.Length; i++)
        {
            ReadOnlySpan<byte> slice = bytes.AsSpan().Slice(0, i);
            long hash = GxHash.Hash64(slice, 42);
            Assert.AreNotEqual(0L, hash, "Zero hash!");
            Assert.IsTrue(hashes.Add(hash), "Collision!");
        }
        
        // Check that zero padding affects output hash
        hashes.Clear();
        bytes[0] = 123;
        for (int i = 0; i < bytes.Length; i++)
        {
            ReadOnlySpan<byte> slice = bytes.AsSpan().Slice(0, i);
            long hash = GxHash.Hash64(slice, 42);
            Assert.AreNotEqual(0L, hash, "Zero hash!");
            Assert.IsTrue(hashes.Add(hash), "Collision!");
        }
    }
    
    [Test]
    public void AllBytesAreRead()
    {
        for (int s = 0; s < 1200; s++) {
            byte[] bytes = new byte[s];
            int hash = GxHash.Hash32(bytes, 42);

            for (int i = 0; i < s; i++) {
                byte swap = bytes[i];
                bytes[i] = 82;
                int newHash = GxHash.Hash32(bytes, 42);
                bytes[i] = swap;
                
                Assert.AreNotEqual(hash, newHash, $"byte {i} not processed for input of size {s}");
            }
        }
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
