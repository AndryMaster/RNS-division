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
    
    public enum TypeTest
    {
        Parallel,
        ParallelMem,
        ParallelToken,
        ParallelTokenMem,
    }
    
    public static void Test(int[] newModules, TypeTest tt=TypeTest.ParallelMem, int roInit=0)
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
        
        // int ro = roInit;
        int ro = 0;
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

            // for (int i = 0; i < 20; i++)
            // {
            //     Console.Write($"{mem[i]}, ");
            // } Console.WriteLine();
            // return;
            
            // var bufferF = accelerator.Allocate1D<ulong>(P);
            // var bufferSize = accelerator.Allocate1D<double>(P);
            // var bufferResult = accelerator.Allocate1D<double>(P);
            //
            // var kernel = accelerator.LoadAutoGroupedStreamKernel<
            //     Index1D, long,
            //     ArrayView<ulong>,
            //     ArrayView<double>,
            //     ArrayView<double>>(KernelMem1D);
            // kernel((int)P, P, bufferF.View, bufferSize.View, bufferResult.View);
            //
            // var res = bufferResult.GetAsArray1D();
            // double r = 0;
            // foreach ( var i in res )
            // {
            //     if (i > 0) r += i;
            // }
            // // Console.Write($" {res.Length} {(double)r/res.Length} ");
            // countBad = (long)((double)P * P - r);
            //
            // bufferF.Dispose();
            // bufferSize.Dispose();
            // bufferResult.Dispose();
            
            var tmp = new List<long>();
            foreach (long i in k) tmp.Add(i);
            var longK = tmp.ToArray();
            
            var bufferMods = accelerator.Allocate1D<int>(Modules.Length);
            var bufferKVal = accelerator.Allocate1D<long>(Modules.Length);
            var bufferRes = accelerator.Allocate1D<ulong>(P);
            bufferMods.CopyFromCPU(Modules);
            bufferKVal.CopyFromCPU(longK);
            
            var kernel = accelerator.LoadAutoGroupedStreamKernel<
                             Index1D, 
                             long, ArrayView<int>,
                             ArrayView<long>, int,
                             ArrayView<ulong>>(KernelF);
            kernel((int)P, P, bufferMods.View, bufferKVal.View, ro, bufferRes.View);

            bufferRes.Dispose();
            bufferKVal.Dispose();
            bufferMods.Dispose();

            sw.Stop();
            Console.WriteLine($"Ro={ro}\tTime={sw.ElapsedMilliseconds} ms \t" +
                $"Bad={countBad} (All={P * P}) \t" +
                $"Accuracy={1 - (double)countBad / (P * P)} ");
        }
        swFull.Stop();
        Console.WriteLine($"Full time: {swFull.ElapsedMilliseconds} ms");
        Console.WriteLine(DateTime.Now.ToLongTimeString());
    }
    
    // private static void KernelBest1D(
    //     Index1D index, long pval, long offset,
    //     int[] mods, long[] k, int ro,
    //     ArrayView<int> result)
    // {
    //     long indexX = index.X + offset;
    //     
    //     long Fa = Fval(indexX, ro, mods, k);
    //     long delta, deltaTmp;
    //     long res;
    //
    //     for (int indexY = 1; indexY < pval; indexY++)
    //     {
    //         long Fb = Fval(indexY, ro, mods, k);
    //         int numIters = 5;  ///////////////////////////////
    //
    //         res = 0;
    //         delta = Fa;
    //         for (int i = numIters; i >= 0; i--)
    //         {
    //             deltaTmp = delta;
    //             delta -= Fb << i;
    //
    //             if (delta > deltaTmp)
    //             {
    //                 delta = deltaTmp;
    //             } 
    //             else
    //             {
    //                 res += 1L << i;
    //             }
    //         }
    //         if ((indexX / indexY) != res)
    //         {
    //             result[0] = 1;
    //             Interop.WriteLine("x {0}  x {1}  A {2}  B {3}", indexX, indexY, Fa, Fb);
    //         }
    //     }
    //     // result[index.X] = (double)countBad / pval;
    // }
    
    private static void KernelF(
        Index1D index, long pval,
        ArrayView<int> mods, ArrayView<long> k, int ro,
        ArrayView<ulong> result)
    {
        result[index] = (ulong)Fval(index, ro, mods, k);
    }

    static long Fval(long num, int ro, ArrayView<int> mods, ArrayView<long> k)
    {
        int[] rns = new int[mods.Length];
        for (int i = 0; i < mods.Length; i++)
            rns[i] = (int)(num % mods[i]);
        
        long s = 0;
        for (int i = 0; i < Modules.Length; i++)
            s += rns[i] * k[i];

        return s - ((s >> ro) << ro);
    }

    // private static void KernelMem1D(
    //     Index1D index, long pval,
    //     ArrayView<ulong> dataF,
    //     ArrayView<double> dataSize,
    //     ArrayView<double> result)
    // {
    //     ulong Fa = dataF[index.X];
    //     ulong delta, deltaTmp;
    //     long res, countBad = 0;
    //
    //     for (int indexxY = 1; indexxY < pval; indexxY++)
    //     {
    //         ulong Fb = dataF[indexxY];
    //         int numIters = (int)(dataSize[index.X] - dataSize[indexxY]);
    //
    //         res = 0;
    //         delta = Fa;
    //         for (int i = numIters; i >= 0; i--)
    //         {
    //             deltaTmp = delta;
    //             delta -= Fb << i;
    //
    //             if (delta > deltaTmp)
    //             {
    //                 delta = deltaTmp;
    //             } 
    //             else
    //             {
    //                 res += 1L << i;
    //             }
    //         }
    //         // Group.Barrier();  //
    //         if ((index.X / indexxY) != res) countBad++;
    //     }
    //     
    //     result[index.X] = (double)countBad / pval;
    // }
}