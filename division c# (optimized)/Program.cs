using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;

using simd;

namespace division;



internal class Program
{
    static void Main(string[] args)
    {
        // Test1();
        // Test2();
        // Test3();
        Test4();
        // Test5();


        //var test = new SIMD();
        //test.Test2();
    }

    public static void Test1()
    {
        DivisionTestRo.Test(new int[] { 2, 3, 5, 7 },
            DivisionTestRo.TestType.Simple);

        DivisionTestRo.Test(new int[] { 29, 32 },
            DivisionTestRo.TestType.Parallel);

        DivisionTestRo.Test(new int[] { 29, 32 },
            DivisionTestRo.TestType.ParallelToken);

        DivisionTestRo.Test(new int[] { 7, 23, 31 },
            DivisionTestRo.TestType.ParallelToken);

        DivisionTestRo.Test(new int[] { 125, 127, 128 },
            DivisionTestRo.TestType.ParallelToken);
    }

    public static void Test2()
    {
        DivisionTestRo.Test(new int[] { 5, 7, 23, 31 },
            DivisionTestRo.TestType.Simple);

        DivisionTestRo.Test(new int[] { 5, 7, 23, 31 },
            DivisionTestRo.TestType.Parallel);

        DivisionTestRo.Test(new int[] { 5, 7, 23, 31 },
            DivisionTestRo.TestType.ParallelToken);

        DivisionTestRo.Test(new int[] { 5, 7, 23, 31 },
            DivisionTestRo.TestType.ParallelMem);
    }

    public static void Test3()
    {
        DivisionTestRo.Test(new int[] { 7, 23, 31 },
            DivisionTestRo.TestType.ParallelToken);

        DivisionTestRo.Test(new int[] { 7, 23, 31, 5 },
            DivisionTestRo.TestType.ParallelMem);

        DivisionTestRo.Test(new int[] { 125, 127, 128, 7 },
            DivisionTestRo.TestType.ParallelMem);
        
        //DivisionTestRo.Test(new int[] { 125, 127, 128 },
        //    DivisionTestRo.TestType.ParallelMem);

        //DivisionTestRo.Test(new int[] { 123, 125, 127, 128 },
        //    DivisionTestRo.TestType.ParallelMem);
    }

    public static void Test4()
    {
        // DivisionTestRo.Test(new int[] { 27, 29, 31, 32 },
        //     DivisionTestRo.TestType.ParallelMem);
        
        // DivisionTestRo.Test(new int[] { 125, 127, 128 },
        //     DivisionTestRo.TestType.ParallelMem);
        
        DivisionTestRo.Test(new int[] { 7, 125, 127, 128 },
            DivisionTestRo.TestType.ParallelMem);
        
        // DivisionTestRo.Test(new int[] { 123, 125, 127, 128 },
        //     DivisionTestRo.TestType.ParallelMem);
    }
    
    public static void Test5()
    {
        DivisionTestRo.ShowFValues(
            new int[] { 125, 127, 128 },
            new long[] { 0, 1, 10, 12, 1234, 123456 },
            51);
    }
}
