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
// unsafe struct headerUnion                  // 2048 bytes in header
// {
//     [FieldOffset(0)]
//     public fixed byte headerBytes[2048];      
//     [FieldOffset(0)]
//     public headerLayout header; 
// }
