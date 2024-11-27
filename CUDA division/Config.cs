using System.Runtime.InteropServices;

namespace CUDA_division;

public struct Values8ToGpu32(
    uint m1 = 0, uint m2 = 0, uint m3 = 0, uint m4 = 0,
    uint m5 = 0, uint m6 = 0, uint m7 = 0, uint m8 = 0)
{
    public uint m1 = m1, m2 = m2, m3 = m3, m4 = m4, m5 = m5, m6 = m6, m7 = m7, m8 = m8;
}

public struct Values8ToGpu64(
    ulong m1 = 0, ulong m2 = 0, ulong m3 = 0, ulong m4 = 0,
    ulong m5 = 0, ulong m6 = 0, ulong m7 = 0, ulong m8 = 0)
{
    public ulong m1 = m1, m2 = m2, m3 = m3, m4 = m4, m5 = m5, m6 = m6, m7 = m7, m8 = m8;
}

public struct Values8ToGpu128(
    MInt128 m1, MInt128 m2, MInt128 m3, MInt128 m4,
    MInt128 m5, MInt128 m6, MInt128 m7, MInt128 m8)
{
    public MInt128 m1 = m1, m2 = m2, m3 = m3, m4 = m4, m5 = m5, m6 = m6, m7 = m7, m8 = m8;
}

// [StructLayout(LayoutKind.Explicit)]
// struct Val8Gpu32
// {
//     [FieldOffset(0)]
//     [MarshalAs(UnmanagedType.ByValArray, SizeConst=8)] public required uint[] s1;
//     
// }
// [System.Runtime.CompilerServices.InlineArray(10)]

public unsafe struct Fixed8Struct32
{
    private const int MaxValSize = 8;
    public fixed uint Values[MaxValSize];

    public Fixed8Struct32(uint[] values, uint defaultValue = 0u)
    {
        for (int i = 0; i < Math.Min(MaxValSize, values.Length); i++)
        {
            if (values.Length < i)
                Values[i] = values[i];
            else
                Values[i] = defaultValue;
        }
    }
}
    
public unsafe struct Fixed8Struct64
{
    private const int MaxValSize = 8;
    public fixed ulong Values[MaxValSize];

    public Fixed8Struct64(ulong[] values, uint defaultValue = 0u)
    {
        for (int i = 0; i < Math.Min(MaxValSize, values.Length); i++)
        {
            if (values.Length < i)
                Values[i] = values[i];
            else
                Values[i] = defaultValue;
        }
    }
}
    
public unsafe struct Fixed8Struct128
{
    private const int MaxValSize = 8*2;
    public fixed ulong Values[MaxValSize];

    public Fixed8Struct128(MInt128[] values)
    {
        var defaultValue = new MInt128(0, 0);
        
        for (int i = 0; i < Math.Min(MaxValSize/2, values.Length)/2; i++)
        {
            if (values.Length < i)
            {
                Values[i*2] = values[i].hi;
                Values[i*2+1] = values[i].lo;
            }
            else
            {
                Values[i*2] = defaultValue.hi;
                Values[i*2+1] = defaultValue.lo;
            }
        }
    }
    
    public MInt128 this[int i] => new MInt128(Values[i*2], Values[i*2+1]);
}
