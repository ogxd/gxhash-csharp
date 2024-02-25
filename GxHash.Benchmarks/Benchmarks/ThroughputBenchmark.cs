using System;
using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Order;
using System.Security.Cryptography;
using GxHash.Utils;

namespace GxHash.Benchmarks;

[Orderer(SummaryOrderPolicy.Declared)]
[Throughput]
public class ThroughputBenchmark
{
    [Benchmark(Baseline = true)]
    public int Marvin() => string.GetHashCode(Value);
    
    [Benchmark]
    public int XxH3() => (int)(long)XxHash3.HashToUInt64(MemoryMarshal.AsBytes(Value.AsSpan()), _seed);

    [Benchmark]
    public int GxHash32() => GxHash.Hash32(MemoryMarshal.AsBytes(Value.AsSpan()), _useed);
    
    private static readonly ulong _useed = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));
    private static readonly long _seed = BitConverter.ToInt64(RandomNumberGenerator.GetBytes(8));

    [ParamsSource(nameof(GetData))]
    public string Value;
    
    public IEnumerable<string> GetData()
    {
        // Using similar values as what https://github.com/rurban/smhasher uses
        for (int i = 1; i < 14; i++)
        {
            // Testing on aligned buffers (favorable)
            int chars = (int)Math.Pow(2, i);
            yield return RandomObjectUtils.CreateRandomString(
                minSize: chars,
                maxSize: chars,
                seed: chars,
                charSet: "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
        }
    }
}