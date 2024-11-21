namespace CUDA_division;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        TestILGPU.Test();
        
        // DivisionTestRoCpu.Test(new []{29, 31, 32}, DivisionTestRoCpu.TestType.ParallelTokenMem);
    }
}