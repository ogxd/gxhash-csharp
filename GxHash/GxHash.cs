﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using ArmAes = System.Runtime.Intrinsics.Arm.Aes;
using X86Aes = System.Runtime.Intrinsics.X86.Aes;

namespace GxHash;

public class GxHash
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Hash32(ReadOnlySpan<byte> bytes, long seed) {
        return Finalize(Compress(bytes), seed).AsInt32().GetElement(0);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint HashU32(ReadOnlySpan<byte> bytes, long seed) {
        return Finalize(Compress(bytes), seed).AsUInt32().GetElement(0);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Hash64(ReadOnlySpan<byte> bytes, long seed) {
        return Finalize(Compress(bytes), seed).AsInt64().GetElement(0);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong HashU64(ReadOnlySpan<byte> bytes, long seed) {
        return Finalize(Compress(bytes), seed).AsUInt64().GetElement(0);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid Hash128(ReadOnlySpan<byte> bytes, long seed) {
        Vector128<byte> hash = Finalize(Compress(bytes), seed);
        return Unsafe.As<Vector128<byte>, Guid>(ref hash);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> Finalize(Vector128<byte> input, long seed = 0) {
        var keys1 = Vector128.Create(0x713b01d0, 0x8f2f35db, 0xaf163956, 0x85459f85).AsByte();
        var keys2 = Vector128.Create(0x1de09647, 0x92cfa39c, 0x3dd99aca, 0xb89c054f).AsByte();
        var keys3 = Vector128.Create(0xc78b122b, 0x5544b1b7, 0x689d2b7d, 0xd0012e32).AsByte();

        Vector128<byte> output = input;

        // 3-rounds AES block cipher with hardcoded keys
        if (ArmAes.IsSupported) {
            // For some reasons the ARM Neon intrinsics for AES a very different from the ones for X86,
            // so we need these operations below to achieve the same results as for x86
            // See https://blog.michaelbrase.com/2018/05/08/emulating-x86-aes-intrinsics-on-armv8-a
            output = AdvSimd.Xor(ArmAes.MixColumns(ArmAes.Encrypt(output, Vector128<byte>.Zero)), Vector128.Create(seed).AsByte());
            output = AdvSimd.Xor(ArmAes.MixColumns(ArmAes.Encrypt(output, Vector128<byte>.Zero)), keys1);
            output = AdvSimd.Xor(ArmAes.MixColumns(ArmAes.Encrypt(output, Vector128<byte>.Zero)), keys2);
            output = AdvSimd.Xor(ArmAes.Encrypt(output, Vector128<byte>.Zero), keys3);
        } else if (X86Aes.IsSupported) {
            output = X86Aes.Encrypt(output, Vector128.Create(seed).AsByte());
            output = X86Aes.Encrypt(output, keys1);
            output = X86Aes.Encrypt(output, keys2);
            output = X86Aes.EncryptLast(output, keys3);
        } else {
            throw new PlatformNotSupportedException();
        }

        return output;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Fold(Vector128<byte> input, out int hash) {
        hash = input.AsInt32().GetElement(0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Fold(Vector128<byte> input, out uint hash) {
        hash = input.AsUInt32().GetElement(0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Fold(Vector128<byte> input, out long hash) {
        hash = input.AsInt64().GetElement(0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> ReadAdvance(ref Vector128<byte> ptr)
    {
        Vector128<byte> value = ptr;
        ptr = ref Unsafe.Add(ref ptr, 1);
        return value;
    }

    const int VECTOR_SIZE_SHIFT = 4;
    const int UNROLL_FACTOR_SHIFT = 3;

    const int VECTOR_LENGTH = 1 << VECTOR_SIZE_SHIFT; // 2 ^ 4 = 16
    const int UNROLL_FACTOR = 1 << UNROLL_FACTOR_SHIFT; // 2 ^ 2 = 4
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> Compress(ReadOnlySpan<byte> bytes)
    {
        // Get pointer of SIMD vectors from input span
        ref var ptr = ref Unsafe.As<byte, Vector128<byte>>(ref MemoryMarshal.GetReference(bytes));
        
        int len = bytes.Length;

        if (len < VECTOR_LENGTH + 1) {
            // Input fits on a single SIMD vector, however we might read beyond the input message
            // Thus we need this safe method that checks if it can safely read beyond or must copy
            return GetPartialVector128(ref ptr, len);
        }
        
        int remainingBytes = len & (VECTOR_LENGTH - 1);

        // The input does not fit on a single SIMD vector
        Vector128<byte> hashVector;
        if (remainingBytes == 0) {
            hashVector = Vector128<byte>.Zero;
        } else {
            // If the input length does not match the length of a whole number of SIMD vectors,
            // it means we'll need to read a partial vector. We can start with the partial vector first,
            // so that we can safely read beyond since we expect the following bytes to still be part of
            // the input
            hashVector = GetPartialVectorUnsafe(ref ptr, remainingBytes);
            ptr = ref Unsafe.AddByteOffset(ref ptr, remainingBytes);
        }

        if (len < VECTOR_LENGTH * 2 + 1) {
            // Fast path when input length > 16 and <= 32
            hashVector = Compress(hashVector, ReadAdvance(ref ptr));
        } else if (len < VECTOR_LENGTH * 3 + 1) {
            // Fast path when input length > 32 and <= 48
            hashVector = Compress(hashVector, Compress(ReadAdvance(ref ptr), ReadAdvance(ref ptr)));
        } else {
            // Input message is large and we can use the high ILP loop
            hashVector = CompressMany(ref ptr, hashVector, len);
        }
        
        return hashVector;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> CompressMany(ref Vector128<byte> start, Vector128<byte> hashVector, int len)
    {
        int unrollableBlocksCount = (len >> (VECTOR_SIZE_SHIFT + UNROLL_FACTOR_SHIFT)) << UNROLL_FACTOR_SHIFT;
        ref var end2 = ref Unsafe.Add(ref start, unrollableBlocksCount);

        while (Unsafe.IsAddressLessThan(ref start, ref end2)) {

            Vector128<byte> blockHash = start;
            blockHash = CompressFast(blockHash, Unsafe.Add(ref start, 1));
            blockHash = CompressFast(blockHash, Unsafe.Add(ref start, 2));
            blockHash = CompressFast(blockHash, Unsafe.Add(ref start, 3));
            blockHash = CompressFast(blockHash, Unsafe.Add(ref start, 4));
            blockHash = CompressFast(blockHash, Unsafe.Add(ref start, 5));
            blockHash = CompressFast(blockHash, Unsafe.Add(ref start, 6));
            blockHash = CompressFast(blockHash, Unsafe.Add(ref start, 7));
            start = ref Unsafe.Add(ref start, UNROLL_FACTOR);
                
            hashVector = Compress(hashVector, blockHash);
        }

        int remainingBlocksCount = (len >> VECTOR_SIZE_SHIFT) - unrollableBlocksCount;
        
        ref var end = ref Unsafe.Add(ref start, remainingBlocksCount);

        while (Unsafe.IsAddressLessThan(ref start, ref end))
        {
            hashVector = Compress(hashVector, start);
            start = ref Unsafe.Add(ref start, 1);
        }
        
        return hashVector;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> GetPartialVector128(ref Vector128<byte> start, int remainingBytes)
    {
        if (IsReadBeyondSafe(ref start))
        {
            return GetPartialVectorUnsafe(ref start, remainingBytes);
        }

        Vector128<byte> input = Vector128<byte>.Zero;
        ref byte source = ref Unsafe.As<Vector128<byte>, byte>(ref start);
        ref byte dest = ref Unsafe.As<Vector128<byte>, byte>(ref input);
        Unsafe.CopyBlockUnaligned(ref dest, ref source, (uint)remainingBytes);
        return Vector128.Add(input, Vector128.Create((byte)remainingBytes));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> GetPartialVectorUnsafe(ref Vector128<byte> start, int remainingBytes)
    {
        var indices = Vector128.Create(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
        var mask = Vector128.GreaterThan(Vector128.Create((sbyte)remainingBytes), indices).AsByte();
        Vector128<byte> hashVector = Vector128.BitwiseAnd(mask, start);
        return Vector128.Add(hashVector, Vector128.Create((byte)remainingBytes));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> Compress(Vector128<byte> a, Vector128<byte> b)
    {
        var keys1 = Vector128.Create(0xFC3BC28E, 0x89C222E5, 0xB09D3E21, 0xF2784542).AsByte();
        var keys2 = Vector128.Create(0x03FCE279, 0xCB6B2E9B, 0xB361DC58, 0x39136BD9).AsByte();
        
        if (ArmAes.IsSupported)
        {
            b = AdvSimd.Xor(ArmAes.MixColumns(ArmAes.Encrypt(b, Vector128<byte>.Zero)), keys1);
            b = AdvSimd.Xor(ArmAes.MixColumns(ArmAes.Encrypt(b, Vector128<byte>.Zero)), keys2);
            return AdvSimd.Xor(ArmAes.Encrypt(a, Vector128<byte>.Zero), b);
        }
        if (X86Aes.IsSupported)
        {
            b = X86Aes.Encrypt(b, keys1);
            b = X86Aes.Encrypt(b, keys2);
            return X86Aes.EncryptLast(a, b);
        }
        
        throw new PlatformNotSupportedException();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> CompressFast(Vector128<byte> a, Vector128<byte> b)
    {
        if (ArmAes.IsSupported)
        {
            return AdvSimd.Xor(ArmAes.MixColumns(ArmAes.Encrypt(a, Vector128<byte>.Zero)), b);
        }
        if (X86Aes.IsSupported)
        {
            return X86Aes.Encrypt(a, b);
        }
        
        throw new PlatformNotSupportedException();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe bool IsReadBeyondSafe(ref Vector128<byte> reference)
    {
        const int PAGE_SIZE = 0x1000;
        IntPtr address = (IntPtr)Unsafe.AsPointer(ref reference);
        IntPtr offsetWithinPage = address & (PAGE_SIZE - 1);
        return offsetWithinPage < PAGE_SIZE - VECTOR_LENGTH;
    }
}