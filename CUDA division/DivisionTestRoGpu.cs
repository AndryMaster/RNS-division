using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;

namespace CUDA_division;

public class DivisionTestRoGpu : Division
{
    public enum TypeDevice
    {
        GPU,
        CPU,
    }
    
    public enum TestType
    {
        Parallel,
        ParallelMem,
        ParallelToken,
        ParallelTokenMem,
    }
    
    public static void Test(int[] newModules, TestType tt=TestType.ParallelMem, int roInit=0)
    {
        initModules(newModules);
        
        using var context = Context.CreateDefault();
        foreach (Device device in context) Console.WriteLine(device);
        
        // var accelerator = context.CreateCudaAccelerator(0);
        var accelerator = context.CreateCPUAccelerator(0);
        accelerator.PrintInformation();

        Console.Write($"\n\nP={P} ro=??? mods=[ ");
        foreach (int i in Modules)
            Console.Write($"{i}, ");
        Console.WriteLine("]\n");
        Console.WriteLine(DateTime.Now.ToLongTimeString());

        Stopwatch sw = new Stopwatch();
        Stopwatch swFull = new Stopwatch();
        swFull.Start();

        
        int ro = roInit;
        BigInteger[] k;

        long countBad = 1;
        while (countBad > 0)
        {
            countBad = 0;
            k = calk_k(++ro);
            sw.Restart();
            
            var memLog = new double[P];
            var mem = new Num.UInt64[P];
            Parallel.For(0, P,
                i =>
                {
                    BigInteger tmp = F(i, ro, k);
                    mem[i] = new Num.UInt64(tmp);
                    memLog[i] = BigInteger.Log(tmp, 2);
                });
            Console.Write("*");
            
            var bufferF = accelerator.Allocate1D<Num.UInt64>(P);
            var bufferSize = accelerator.Allocate1D<double>(P);
            var bufferResult = accelerator.Allocate1D<bool>(P);
            
            // var kernel = accelerator.LoadAutoGroupedStreamKernel<
            //     Index2D,
            //     ArrayView<Num.UInt64>,
            //     ArrayView<double>,
            //     ArrayView<bool>>(KernelMem2D);
            
            bufferF.Dispose();
            bufferSize.Dispose();
            bufferResult.Dispose();
            
            sw.Stop();
            Console.WriteLine($"Ro={ro}\tTime={sw.ElapsedMilliseconds} ms \t" +
                $"Bad={countBad} (All={P * P}) \t" +
                $"Accuracy={1 - (double)countBad / (P * P)} ");
        }
        swFull.Stop();
        Console.WriteLine($"Full time: {swFull.ElapsedMilliseconds} ms");
        Console.WriteLine(DateTime.Now.ToLongTimeString());
    }
    
    // private static void KernelMem2D(
    //     Index2D index,
    //     ArrayView<Num.UInt64> dataF,
    //     ArrayView<double> dataSize,
    //     ArrayView<bool> result)
    // {
    //     Interop.WriteLine("Line {0}: {1}", index.X, data[index.X]);
    //     // output[index] = X2(data[index]);
    //     for (int i = 0; i < 1000; i++)
    //     {
    //         output[index] += data[index];
    //     }
    //     
    //     output[index] = output[index] / 100;
    // }
}