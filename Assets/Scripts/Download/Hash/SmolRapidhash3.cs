// C# port of rapidhash V3 - https://github.com/Nicoshev/rapidhash (rev 92731ee, 2026 Feb).
// rapidhash is MIT license, Copyright (C) 2025 Nicolas De Carli
//
// Only all default options, and the regular "rapidhash" (not "micro", not "nano") is ported.
// Assumes little-endian machine.
// Implements 128 bit multiply via Burst intrinsic, so that's a dependency.
// Uses "unsafe" C# compilation option.

using System;
using System.IO;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Smol.Rapidhash
{

    [BurstCompile]
    public static class Rapidhash3
    {
        // Default secret parameters
        const ulong Secret0 = 0x2d358dccaa6c78a5UL;
        const ulong Secret1 = 0x8bb84b93962eacc9UL;
        const ulong Secret2 = 0x4b33a62ed433d4a3UL;
        const ulong Secret3 = 0x4d5a2da51de1aa47UL;
        const ulong Secret4 = 0xa0761d6478bd642fUL;
        const ulong Secret5 = 0xe7037ed1a0b428dbUL;
        const ulong Secret6 = 0x90ed1765281c388cUL;
        const ulong Secret7 = 0xaaaaaaaaaaaaaaaaUL;

        // 64*64 -> 128bit multiply function
        static void rapid_mum(ref ulong a, ref ulong b)
        {
            a = Common.umul128(a, b, out b);
        }
        // Multiply and xor mix function
        static ulong rapid_mix(ulong a, ulong b)
        {
            rapid_mum(ref a, ref b);
            return a ^ b;
        }
        static unsafe ulong rapid_read64(byte* p)
        {
            ulong value = 0;
            UnsafeUtility.MemCpy(&value, p, sizeof(ulong));
            return value;
        }

        static unsafe ulong rapid_read32(byte* p)
        {
            uint value = 0;
            UnsafeUtility.MemCpy(&value, p, sizeof(uint));
            return value;
        }

        // rapidhash main function.
        [BurstCompile]
        public static unsafe ulong Hash64(void* key, long length, ulong seed = 0)
        {
            ulong len = (ulong)length;
            byte* p = (byte*)key;
            if(seed == 0)
                seed = rapid_mix(Secret2, Secret1);
            ulong a = 0, b = 0;
            ulong i = len;

            if (len <= 16)
            {
                if (len >= 4)
                {
                    seed ^= len;
                    if (len >= 8)
                    {
                        byte* plast = p + len - 8;
                        a = rapid_read64(p);
                        b = rapid_read64(plast);
                    }
                    else
                    {
                        byte* plast = p + len - 4;
                        a = rapid_read32(p);
                        b = rapid_read32(plast);
                    }
                }
                else if (len > 0)
                {
                    a = ((ulong)p[0] << 45) | p[len - 1];
                    b = p[len >> 1];
                }
            }
            else
            {
                if (len > 112)
                {
                    ulong see1 = seed, see2 = seed;
                    ulong see3 = seed, see4 = seed;
                    ulong see5 = seed, see6 = seed;
                    do
                    {
                        seed = rapid_mix(rapid_read64(p) ^ Secret0, rapid_read64(p + 8) ^ seed);
                        see1 = rapid_mix(rapid_read64(p + 16) ^ Secret1, rapid_read64(p + 24) ^ see1);
                        see2 = rapid_mix(rapid_read64(p + 32) ^ Secret2, rapid_read64(p + 40) ^ see2);
                        see3 = rapid_mix(rapid_read64(p + 48) ^ Secret3, rapid_read64(p + 56) ^ see3);
                        see4 = rapid_mix(rapid_read64(p + 64) ^ Secret4, rapid_read64(p + 72) ^ see4);
                        see5 = rapid_mix(rapid_read64(p + 80) ^ Secret5, rapid_read64(p + 88) ^ see5);
                        see6 = rapid_mix(rapid_read64(p + 96) ^ Secret6, rapid_read64(p + 104) ^ see6);
                        p += 112;
                        i -= 112;
                    } while (i > 112);

                    seed ^= see1;
                    see2 ^= see3;
                    see4 ^= see5;
                    seed ^= see6;
                    see2 ^= see4;
                    seed ^= see2;
                }

                if (i > 16)
                {
                    seed = rapid_mix(rapid_read64(p) ^ Secret2, rapid_read64(p + 8) ^ seed);
                    if (i > 32)
                    {
                        seed = rapid_mix(rapid_read64(p + 16) ^ Secret2, rapid_read64(p + 24) ^ seed);
                        if (i > 48)
                        {
                            seed = rapid_mix(rapid_read64(p + 32) ^ Secret1, rapid_read64(p + 40) ^ seed);
                            if (i > 64)
                            {
                                seed = rapid_mix(rapid_read64(p + 48) ^ Secret1, rapid_read64(p + 56) ^ seed);
                                if (i > 80)
                                {
                                    seed = rapid_mix(rapid_read64(p + 64) ^ Secret2, rapid_read64(p + 72) ^ seed);
                                    if (i > 96)
                                    {
                                        seed = rapid_mix(rapid_read64(p + 80) ^ Secret1, rapid_read64(p + 88) ^ seed);
                                    }
                                }
                            }
                        }
                    }
                }

                a = rapid_read64(p + i - 16) ^ i;
                b = rapid_read64(p + i - 8);
            }

            a ^= Secret1;
            b ^= seed;
            rapid_mum(ref a, ref b);
            return rapid_mix(a ^ Secret7, b ^ Secret1 ^ i);
        }

        // compute hash of a single blittable T value
        public static unsafe ulong Hash64<T>(ref T key) where T : unmanaged
        {
            fixed (void* ptr = &key)
            {
                return Hash64(ptr, UnsafeUtility.SizeOf<T>());
            }
        }

        // compute hash of array of blittable T
        public static unsafe ulong Hash64<T>(T[] key) where T : unmanaged
        {
            fixed (void* ptr = key)
            {
                return Hash64(ptr, UnsafeUtility.SizeOf<T>() * key.Length);
            }
        }

        // compute hash of span of blittable T
        public static unsafe ulong Hash64<T>(Span<T> key) where T : unmanaged
        {
            fixed (void* ptr = key)
            {
                return Hash64(ptr, UnsafeUtility.SizeOf<T>() * key.Length);
            }
        }

        // compute hash of native array of T
        public static unsafe ulong Hash64<T>(NativeArray<T> key) where T : unmanaged
        {
            return Hash64(key.GetUnsafeReadOnlyPtr(), UnsafeUtility.SizeOf<T>() * key.Length);
        }

        [BurstCompile]
        static unsafe void Hash64ProcessBlock112(
            byte* p,
            ref ulong seed,
            ref ulong see1,
            ref ulong see2,
            ref ulong see3,
            ref ulong see4,
            ref ulong see5,
            ref ulong see6)
        {
            seed = rapid_mix(rapid_read64(p) ^ Secret0, rapid_read64(p + 8) ^ seed);
            see1 = rapid_mix(rapid_read64(p + 16) ^ Secret1, rapid_read64(p + 24) ^ see1);
            see2 = rapid_mix(rapid_read64(p + 32) ^ Secret2, rapid_read64(p + 40) ^ see2);
            see3 = rapid_mix(rapid_read64(p + 48) ^ Secret3, rapid_read64(p + 56) ^ see3);
            see4 = rapid_mix(rapid_read64(p + 64) ^ Secret4, rapid_read64(p + 72) ^ see4);
            see5 = rapid_mix(rapid_read64(p + 80) ^ Secret5, rapid_read64(p + 88) ^ see5);
            see6 = rapid_mix(rapid_read64(p + 96) ^ Secret6, rapid_read64(p + 104) ^ see6);
        }

        [BurstCompile]
        static void Hash64EnsureLongState(
            ref bool hasLongBlocks,
            ref ulong rollingSeed,
            ref ulong see1,
            ref ulong see2,
            ref ulong see3,
            ref ulong see4,
            ref ulong see5,
            ref ulong see6)
        {
            if (hasLongBlocks)
                return;

            see1 = rollingSeed;
            see2 = rollingSeed;
            see3 = rollingSeed;
            see4 = rollingSeed;
            see5 = rollingSeed;
            see6 = rollingSeed;
            hasLongBlocks = true;
        }

        [BurstCompile]
        static unsafe void Hash64StreamProcessChunk(
            byte* pReadBase,
            int bytesRead,
            byte* pTail,
            ref int tailLength,
            ref bool hasLongBlocks,
            ref ulong rollingSeed,
            ref ulong see1,
            ref ulong see2,
            ref ulong see3,
            ref ulong see4,
            ref ulong see5,
            ref ulong see6)
        {
            byte* pRead = pReadBase;
            int remaining = bytesRead;

            if (tailLength > 0)
            {
                int needed = 112 - tailLength;
                if (remaining < needed)
                {
                    UnsafeUtility.MemCpy(pTail + tailLength, pRead, remaining);
                    tailLength += remaining;
                    return;
                }

                UnsafeUtility.MemCpy(pTail + tailLength, pRead, needed);
                Hash64EnsureLongState(ref hasLongBlocks, ref rollingSeed, ref see1, ref see2, ref see3, ref see4, ref see5, ref see6);
                Hash64ProcessBlock112(pTail, ref rollingSeed, ref see1, ref see2, ref see3, ref see4, ref see5, ref see6);

                pRead += needed;
                remaining -= needed;
                tailLength = 0;
            }

            while (remaining > 112)
            {
                Hash64EnsureLongState(ref hasLongBlocks, ref rollingSeed, ref see1, ref see2, ref see3, ref see4, ref see5, ref see6);
                Hash64ProcessBlock112(pRead, ref rollingSeed, ref see1, ref see2, ref see3, ref see4, ref see5, ref see6);
                pRead += 112;
                remaining -= 112;
            }

            if (remaining > 0)
            {
                UnsafeUtility.MemCpy(pTail, pRead, remaining);
                tailLength = remaining;
            }
        }

        [BurstCompile]
        static void Hash64FinalizeLongState(
            bool hasLongBlocks,
            ref ulong rollingSeed,
            ref ulong see1,
            ref ulong see2,
            ref ulong see3,
            ref ulong see4,
            ref ulong see5,
            ref ulong see6)
        {
            if (!hasLongBlocks)
                return;

            rollingSeed ^= see1;
            see2 ^= see3;
            see4 ^= see5;
            rollingSeed ^= see6;
            see2 ^= see4;
            rollingSeed ^= see2;
        }

        [BurstCompile]
        static unsafe ulong Hash64FinalizeTail(byte* p, ulong i, ulong rollingSeed)
        {
            ulong a = 0;
            ulong b = 0;

            if (i <= 16)
            {
                if (i >= 4)
                {
                    rollingSeed ^= i;
                    if (i >= 8)
                    {
                        byte* plast = p + i - 8;
                        a = rapid_read64(p);
                        b = rapid_read64(plast);
                    }
                    else
                    {
                        byte* plast = p + i - 4;
                        a = rapid_read32(p);
                        b = rapid_read32(plast);
                    }
                }
                else if (i > 0)
                {
                    a = ((ulong)p[0] << 45) | p[i - 1];
                    b = p[i >> 1];
                }
            }
            else
            {
                rollingSeed = rapid_mix(rapid_read64(p) ^ Secret2, rapid_read64(p + 8) ^ rollingSeed);
                if (i > 32)
                {
                    rollingSeed = rapid_mix(rapid_read64(p + 16) ^ Secret2, rapid_read64(p + 24) ^ rollingSeed);
                    if (i > 48)
                    {
                        rollingSeed = rapid_mix(rapid_read64(p + 32) ^ Secret1, rapid_read64(p + 40) ^ rollingSeed);
                        if (i > 64)
                        {
                            rollingSeed = rapid_mix(rapid_read64(p + 48) ^ Secret1, rapid_read64(p + 56) ^ rollingSeed);
                            if (i > 80)
                            {
                                rollingSeed = rapid_mix(rapid_read64(p + 64) ^ Secret2, rapid_read64(p + 72) ^ rollingSeed);
                                if (i > 96)
                                {
                                    rollingSeed = rapid_mix(rapid_read64(p + 80) ^ Secret1, rapid_read64(p + 88) ^ rollingSeed);
                                }
                            }
                        }
                    }
                }

                a = rapid_read64(p + i - 16) ^ i;
                b = rapid_read64(p + i - 8);
            }

            a ^= Secret1;
            b ^= rollingSeed;
            rapid_mum(ref a, ref b);
            return rapid_mix(a ^ Secret7, b ^ Secret1 ^ i);
        }

        public static unsafe ulong Hash64(Stream stream, int bufferSize = 1024, ulong seed = 0)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be positive.");

            ulong rollingSeed = seed == 0 ? rapid_mix(Secret2, Secret1) : seed;
            ulong see1 = 0, see2 = 0, see3 = 0, see4 = 0, see5 = 0, see6 = 0;
            bool hasLongBlocks = false;
            ulong totalLength = 0;

            byte[] readBuffer = new byte[bufferSize];
            byte[] tail = new byte[112];
            int tailLength = 0;

            int bytesRead;
            while ((bytesRead = stream.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
                totalLength += (ulong)bytesRead;
                fixed (byte* pReadBase = readBuffer)
                fixed (byte* pTail = tail)
                {
                    Hash64StreamProcessChunk(
                        pReadBase,
                        bytesRead,
                        pTail,
                        ref tailLength,
                        ref hasLongBlocks,
                        ref rollingSeed,
                        ref see1,
                        ref see2,
                        ref see3,
                        ref see4,
                        ref see5,
                        ref see6);
                }
            }

            if (totalLength == 0)
            {
                byte dummy = 0;
                return Hash64(&dummy, 0, seed);
            }

            Hash64FinalizeLongState(
                hasLongBlocks,
                ref rollingSeed,
                ref see1,
                ref see2,
                ref see3,
                ref see4,
                ref see5,
                ref see6);

            fixed (byte* p = tail)
            {
                return Hash64FinalizeTail(p, (ulong)tailLength, rollingSeed);
            }
        }

        //public static uint Hash32(ulong hash)
        //{
        //    return unchecked((uint)hash);
        //}
    }
} // namespace
