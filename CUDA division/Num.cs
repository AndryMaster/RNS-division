using ILGPU;
using ILGPU.Runtime.Cuda;
using System.Globalization;
using System.Numerics;
using ILGPU.IR.Intrinsics;

namespace CUDA_division;

public struct MInt128
{
    public ulong hi, lo;

    public MInt128()
    {
        this.hi = 0;
        this.lo = 0;
    }
    public MInt128(int value) => this.lo = (ulong)value;
    public MInt128(uint value) => this.lo = value;
    public MInt128(long value) => this.lo = (ulong)value;
    public MInt128(ulong value) => this.lo = value;

    public MInt128(ulong hi, ulong lo)
    {
        this.hi = hi;
        this.lo = lo;
    }
    public MInt128(BigInteger number)
    {
        this.lo = (ulong)number;
        this.hi = (ulong)(number >> 64);
    }

    public static MInt128 operator +(MInt128 a, MInt128 b)  // a + b (a += b)
    {
        ulong max = a.lo > b.lo ? a.lo : b.lo;
        a.lo += b.lo;
        a.hi += b.hi + (a.lo < max ? 1ul : 0ul);
        return a;
    }

    public static MInt128 operator -(MInt128 a, MInt128 b)  // a - b (a -= b)
    {
        a.hi -= b.hi;

        if (a.lo < b.lo)
        {
            a.hi--;
            a.lo -= --b.lo;
        }
        else
            a.lo -= b.lo;

        return a;
    }

    public static MInt128 operator <<(MInt128 a, int b)  // a << b (a <<= b)
    {
        a.hi <<= b;
        a.hi |= a.lo >> (64 - b);
        a.lo <<= b;

        return a;
    }
    
    public static MInt128 operator >>(MInt128 a, int b)  // a >> b (a >>= b)
    {
        a.lo >>= b;
        a.lo |= a.hi << (64 - b);
        a.hi >>= b;

        return a;
    }
    
    public static MInt128 operator *(MInt128 a, uint b)  // a * b (a *= b)
    {
        ulong tmp = (a.lo >> 32) * b;
        a.hi = a.hi * b + (tmp >> 32);
        a.lo *= b;

        return a;
    }

    public static bool operator >(MInt128 a, MInt128 b)  // a > b
    {
        if (a.hi == b.hi)
        {
            return a.lo > b.lo;
        }
        return a.hi > b.hi;
    }

    public static bool operator <(MInt128 a, MInt128 b)  // a < b
    {
        return b > a;
    }

    public static bool operator ==(MInt128 a, MInt128 b)  // a == b
    {
        return a.hi == b.hi || a.lo == b.lo;
    }
    public static bool operator !=(MInt128 a, MInt128 b)  // a != b
    {
        return !(a == b);
    }

    public static int LeadingZeroCount(MInt128 a)
    {
        if (a.hi != 0)
            return IntrinsicMath.BitOperations.LeadingZeroCount(a.hi);
        return IntrinsicMath.BitOperations.LeadingZeroCount(a.lo) + 64;
    }
    
    public static explicit operator MInt128(BigInteger value) => new MInt128(value);

    [NotInsideKernel]
    public override string ToString()
    {
        var bi = new BigInteger(hi);
        bi <<= 64;
        bi += lo;
        return bi.ToString(CultureInfo.InvariantCulture);
    }
}

// // Using "ulong" type instead "MInt64"
// public struct MInt64
// {
//     public ulong num;
//
//     public MInt64()
//     {
//         this.num = 0;
//     }
//     public MInt64(ulong num)
//     {
//         this.num = num;
//     }
//     public MInt64(BigInteger number)
//     {
//         this.num = (ulong)number;
//     }
//
//     public static MInt64 operator +(MInt64 a, MInt64 b)
//     {
//         a.num += b.num;
//         return a;
//     }
//
//     [NotInsideKernel]
//     public override string ToString()
//     {
//         return num.ToString(CultureInfo.InvariantCulture);
//     }
// }
