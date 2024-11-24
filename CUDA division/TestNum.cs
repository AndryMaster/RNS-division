namespace CUDA_division;

public class TestNum
{
    public static void Test()
    {
        MInt128 a = new MInt128(0, 256);
        MInt128 b = new MInt128(0, 64);

        Console.WriteLine(a + b);
        Console.WriteLine(a - b);
        Console.WriteLine(b - a);
        Console.WriteLine(a > b);
        Console.WriteLine(a << 64);

        a = new MInt128(23, 1ul << 63);
        b = new MInt128(3, 1ul << 63);
        var c = a + b;
        Console.WriteLine(c - a == b);
        Console.WriteLine(c.hi);
        
        a = new MInt128(123, (127ul<<60) + 123);
        Console.WriteLine(a * 2);
        Console.WriteLine(a * 2 == a + a);
        Console.WriteLine(a * 100 - a * 97 == a + a + a);


        //ushort a1 = (ushort)45;
        //Console.WriteLine(a1);
        //Console.WriteLine(~a1);
        //Console.WriteLine((ushort)~a1);
        //Console.WriteLine(ushort.MaxValue - a1);
        //Console.WriteLine(ushort.MaxValue - 13+12);
    }
}