# GxHash

![CI](https://github.com/ogxd/gxhash-csharp/actions/workflows/.github-ci.yml/badge.svg)

GxHash is a blazingly fast and robust non-cryptographic hashing algorithm.

# Features

## Blazingly Fast 🚀
Up to this date, GxHash is the fastest non-cryptographic hashing algorithm of its class, for all input sizes. This performance is possible mostly thanks to heavy usage of SIMD intrinsics, high ILP construction and a small bytecode (easily inlined and cached).
See the benchmarks.

## Highly Robust 🗿
GxHash uses several rounds of hardware-accelerated AES block cipher for efficient bit mixing.
Thanks to this, GxHash passes all SMHasher tests, which is the de facto quality benchmark for non-cryptographic hash functions, gathering most of the existing algorithms. GxHash has low collisions, uniform distribution and high avalanche properties.

# Portability

## Architecture Compatibility
GxHash is compatible with:

- X86 processors with AES-NI & SSE2 intrinsics
- ARM processors with AES & NEON intrinsics
Warning: Other platforms are currently not supported (there is no fallback). GxHash will not build on these platforms.

## Hashes Stability
All generated hashes for a given version of GxHash are stable, meaning that for a given input the output hash will be the same across all supported platforms.

# Benchmarks

This library is a C# port of [gxhash](https://github.com/ogxd/gxhash). Despite the language difference, performance is really close to the Rust version of the algorithm for a given version of the algorithm. You can run the benchmarks in GxHash.Benchmarks to see for yourself.