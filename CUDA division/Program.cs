namespace CUDA_division;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        TestNum.Test();
        //TestILGPU.Test();

        //DivisionTestRoGpu.Test(new []{2, 3, 5, 7});
        // DivisionTestRoCpu.Test(new []{29, 31, 32}, DivisionTestRoCpu.TestType.ParallelTokenMem);
    }
}