using System.Numerics;

namespace CUDA_division;

internal static class Num
{
    public struct UInt128
    {
        public ulong hi, lo;

        public UInt128(ulong hi, ulong lo)
        {
            this.hi = hi;
            this.lo = lo;
        }
        public UInt128(BigInteger number)
        {
            this.lo = (ulong)number;
            this.hi = (ulong)(number >> 64);
        }
    }
    
    public struct UInt64
    {
        public ulong num;

        public UInt64(ulong num)
        {
            this.num = num;
        }
        public UInt64(BigInteger number)
        {
            this.num = (ulong)number;
        }
    }

    public static UInt128 Add(UInt128 a, UInt128 b)
    {
        return new UInt128(a.hi + b.hi, a.lo + b.lo);
    }
}