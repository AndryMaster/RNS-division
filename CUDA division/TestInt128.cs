namespace CUDA_division;

public class TestInt128
{
    public static void Test()
    {
        Int128 a = new Int128(123L, 456L);
        Int128 b = new Int128(123L, 456L);
        var c = a + b;
        
        Console.WriteLine(c);
    }
}