using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Pure copy of the installed Unity build's integer RNG. Native calibration is the oracle.
    public readonly struct UnityRng : IEquatable<UnityRng>
    {
        public uint S0 { get; }
        public uint S1 { get; }
        public uint S2 { get; }
        public uint S3 { get; }
        public UnityRng(uint s0, uint s1, uint s2, uint s3)
        { S0 = s0; S1 = s1; S2 = s2; S3 = s3; }

        public static UnityRng Seed(int seed)
        {
            unchecked
            {
                uint s0 = (uint)seed;
                uint s1 = 1812433253u * s0 + 1;
                uint s2 = 1812433253u * s1 + 1;
                uint s3 = 1812433253u * s2 + 1;
                return new UnityRng(s0, s1, s2, s3);
            }
        }

        public UnityRng Next()
        {
            unchecked
            {
                uint t = S0 ^ (S0 << 11);
                return new UnityRng(S1, S2, S3, S3 ^ (S3 >> 19) ^ t ^ (t >> 8));
            }
        }

        public RngDraw Range(int min, int max)
        {
            if (min == max) return new RngDraw(min, this);
            UnityRng next = Next();
            long span = (long)max - min;
            long offset = (long)(next.S3 % (ulong)Math.Abs(span));
            return new RngDraw((int)(min + (span > 0 ? offset : -offset)), next);
        }

        public ShuffleResult<T> Shuffle<T>(IReadOnlyList<T> source)
        {
            T[] items = source.ToArray();
            UnityRng state = this;
            for (int count = items.Length - 1; count > 0; count--)
            {
                RngDraw draw = state.Range(0, count + 1);
                state = draw.State;
                T saved = items[draw.Value]; items[draw.Value] = items[count]; items[count] = saved;
            }
            return new ShuffleResult<T>(Array.AsReadOnly(items), state);
        }

        public bool Equals(UnityRng other) => S0 == other.S0 && S1 == other.S1 && S2 == other.S2 && S3 == other.S3;
        public override bool Equals(object? other) => other is UnityRng state && Equals(state);
        public override int GetHashCode() => HashCode.Combine(S0, S1, S2, S3);
    }

    public readonly struct RngDraw
    {
        public int Value { get; }
        public UnityRng State { get; }
        internal RngDraw(int value, UnityRng state) { Value = value; State = state; }
    }

    public sealed class ShuffleResult<T>
    {
        public IReadOnlyList<T> Items { get; }
        public UnityRng State { get; }
        internal ShuffleResult(IReadOnlyList<T> items, UnityRng state) { Items = items; State = state; }
    }
}
