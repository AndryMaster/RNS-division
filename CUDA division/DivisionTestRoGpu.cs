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
            var mem = new ulong[P];
            Parallel.For(0, P,
                i =>
                {
                    BigInteger tmp = F(i, ro, k);
                    mem[i] = (ulong)tmp;
                    memLog[i] = BigInteger.Log(tmp, 2);
                });
            Console.Write("*");
            
            var bufferF = accelerator.Allocate1D<ulong>(P);
            var bufferSize = accelerator.Allocate1D<double>(P);
            var bufferResult = accelerator.Allocate1D<double>(P);

            var kernel = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView<ulong>,
                ArrayView<double>,
                ArrayView<double>>(KernelMem2D);
            kernel(new Index2D((int)P, (int)P), bufferF.View, bufferSize.View, bufferResult.View);

            var res = bufferResult.GetAsArray1D();
            int r = 0;
            foreach ( var i in res )
            {
                if ( i == 1 ) r++;
            }
            Console.Write($" {res.Length} {(double)r/res.Length} ");
            countBad = res.Length - r;
            
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

    private static void KernelMem2D(
        Index2D index,
        ArrayView<ulong> dataF,
        ArrayView<double> dataSize,
        ArrayView<double> result)
    {
        // Interop.WriteLine("Line {0}: {1}", index.X, data[index.X]);
        if (index.Y == 0) return;

        ulong Fa = dataF[index.X];
        ulong Fb = dataF[index.Y];
        int numIters = (int)(dataSize[index.X] - dataSize[index.Y]);

        long res = 0;
        ulong delta = Fa;
        ulong deltaTmp;


        for (int i = numIters; i >= 0; i--)
        {
            deltaTmp = delta;
            delta -= Fb << i;

            if (delta > deltaTmp)
            {
                delta = deltaTmp;
            } 
            else
            {
                res += 1L << i;
            }
        }

        result[index.X] = (res == index.X / index.Y) ? 1:0;
    }
}