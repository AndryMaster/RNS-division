// #define MLen 4
#define Mods

namespace CUDA_division;

class Program
{
    public const int MaxCountModules = 8;
    public const int CurrentCountModules = 4;
    
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        // TestNum.Test();
        TestILGPU.Test();

        // DivisionTestRoGpu.Test(new []{2, 3, 5, 7}, roInit: 15);
        
        // DivisionTestRoGpu.Test64(new uint[]{2, 3, 5, 7});
        // DivisionTestRoGpu.Test64(new uint[]{2, 3, 23, 31});
        // DivisionTestRoGpu.Test128(new uint[]{2, 3, 23, 31});
        
        // DivisionTestRoCpu.Test(new []{2, 3, 5, 7});
        // DivisionTestRoCpu.Test(new []{2, 3, 5, 7}, DivisionTestRoCpu.TestType.Simple);
        // DivisionTestRoCpu.Test(new []{29, 32});
    }
}