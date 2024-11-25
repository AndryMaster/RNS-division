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
    public static void Test64(uint[] newModules)
    {
        initModules(newModules);
        using var context = Context.CreateDefault();
        foreach (Device device in context) Console.WriteLine(device);
        
        var accelerator = context.CreateCudaAccelerator(0);
        // var accelerator = context.CreateCPUAccelerator(0);
        accelerator.PrintInformation();

        Console.Write($"\n\nP={P} ro=??? mods=[ ");
        foreach (int i in Modules)
            Console.Write($"{i}, ");
        Console.WriteLine("]\n");
        Console.WriteLine(DateTime.Now.ToLongTimeString());

        Stopwatch sw = new Stopwatch();
        Stopwatch swFull = new Stopwatch();
        swFull.Start();
        
        for (int ro = 0; ro < 64; ro++)
        {
            BigInteger[] k = calk_k(ro);
            sw.Restart();
            
            var memLog = new int[P];
            var mem = new ulong[P];
            Parallel.For(0, P,
                i =>
                {
                    BigInteger tmp = F(i, ro, k);
                    mem[i] = (ulong)tmp;
                    // memLog[i] = Math.Max(0, (int)BigInteger.Log(tmp, 2) + 1);
                    if (tmp == 0) memLog[i] = 0;
                    else memLog[i] = (int)BigInteger.Log2(tmp) + 1;
                });
            
            var tmp = new List<ulong>();
            foreach (ulong i in k) tmp.Add(i);
            var longK = tmp.ToArray();

            var bufferRes = accelerator.Allocate1D<ulong>(P);
            bufferRes.MemSetToZero();

            var mg = new Values8ToGpu32(Modules[0], Modules[1], Modules[2], Modules[3]);
            var kg = new Values8ToGpu64(longK[0], longK[1], longK[2], longK[3]);
            
            var kernel = accelerator.LoadAutoGroupedStreamKernel<
                             Index1D, 
                             long, int, int, Values8ToGpu32, Values8ToGpu64,
                             ArrayView<ulong>>(Kernel64);
            kernel((int)P, P, ro, Modules.Length, mg, kg, bufferRes.View);
            accelerator.Synchronize();

            var res = bufferRes.GetAsArray1D();
            long countBad = res.Cast<long>().Sum();
            bufferRes.Dispose();
            // for (int i = 0; i < 3; i++)
            // {
            //     for (int j = 0; j < 20; j++)
            //     {
            //         if      (i == 0) Console.Write($"{mem[j]}, ");
            //         else if (i == 1) Console.Write($"{res[j]}, ");
            //         else             Console.Write($"{memLog[j]}, ");
            //     }   Console.WriteLine();
            // }
            
            sw.Stop();
            // Console.WriteLine($"Ro={ro} \tTime={sw.ElapsedMilliseconds} ms \tAll={P}");
            Console.WriteLine($"Ro={ro}\tTime={sw.ElapsedMilliseconds} ms \t" +
                              $"Bad={countBad} (All={P * P}) \t" +
                              $"Accuracy={1 - (double)countBad / (P * P)} ");
        }
        swFull.Stop();
        Console.WriteLine($"Full time: {swFull.ElapsedMilliseconds} ms");
        Console.WriteLine(DateTime.Now.ToLongTimeString());
    }
    
    public static void Test128(uint[] newModules)
    {
        initModules(newModules);
        using var context = Context.CreateDefault();
        foreach (Device device in context) Console.WriteLine(device);
        
        var accelerator = context.CreateCudaAccelerator(0);
        // var accelerator = context.CreateCPUAccelerator(0);
        accelerator.PrintInformation();

        Console.Write($"\n\nP={P} ro=??? mods=[ ");
        foreach (int i in Modules)
            Console.Write($"{i}, ");
        Console.WriteLine("]\n");
        Console.WriteLine(DateTime.Now.ToLongTimeString());

        Stopwatch sw = new Stopwatch();
        Stopwatch swFull = new Stopwatch();
        swFull.Start();
        
        for (int ro = 0; ro < 35; ro++)
        {
            BigInteger[] k = calk_k(ro);
            sw.Restart();
            
            var memLog = new int[P];
            var mem = new ulong[P];
            Parallel.For(0, P,
                i =>
                {
                    BigInteger tmp = F(i, ro, k);
                    mem[i] = (ulong)tmp;
                    // memLog[i] = Math.Max(0, (int)BigInteger.Log(tmp, 2) + 1);
                    if (tmp == 0) memLog[i] = 0;
                    else memLog[i] = (int)BigInteger.Log2(tmp) + 1;
                });
            
            var tmp = new List<MInt128>();
            foreach (var i in k) tmp.Add(new MInt128(i));
            var longK = tmp.ToArray();

            var bufferRes = accelerator.Allocate1D<ulong>(P);
            bufferRes.MemSetToZero();

            var mg = new Values8ToGpu32(Modules[0], Modules[1], Modules[2], Modules[3]);
            var kg = new Values8ToGpu128(longK[0], longK[1], longK[2], longK[3],
                longK[0], longK[1], longK[2], longK[3]);
            
            var kernel = accelerator.LoadAutoGroupedStreamKernel<
                             Index1D, 
                             long, int, int, Values8ToGpu32, Values8ToGpu128,
                             ArrayView<ulong>>(Kernel128);
            kernel((int)P, P, ro, Modules.Length, mg, kg, bufferRes.View);
            accelerator.Synchronize();

            var res = bufferRes.GetAsArray1D();
            long countBad = res.Cast<long>().Sum();
            bufferRes.Dispose();
            
            sw.Stop();
            // Console.WriteLine($"Ro={ro} \tTime={sw.ElapsedMilliseconds} ms \tAll={P}");
            Console.WriteLine($"Ro={ro}\tTime={sw.ElapsedMilliseconds} ms \t" +
                              $"Bad={countBad} (All={P * P}) \t" +
                              $"Accuracy={1 - (double)countBad / (P * P)} ");
        }
        swFull.Stop();
        Console.WriteLine($"Full time: {swFull.ElapsedMilliseconds} ms");
        Console.WriteLine(DateTime.Now.ToLongTimeString());
    }
    
    private static void Kernel128(
        Index1D index, long pval, int ro, int modLen,
        Values8ToGpu32 mg, Values8ToGpu128 kg, ArrayView<ulong> result)
    {
        ulong failsDiv = 0;
        
        MInt128 Fa = Fval128(index, ro, modLen, mg, kg);
        int aLen = 128 - MInt128.LeadingZeroCount(Fa);

        for (long b = 1; b < pval; b++)
        {
            MInt128 Fb = Fval128(index, ro, modLen, mg, kg);
            int bLen = 128 - MInt128.LeadingZeroCount(Fb);

            long res = 0;
            MInt128 delta = Fa;
            
            for (int i = aLen - bLen; i >= 0; i--)
            {
                MInt128 deltaTmp = delta;
                delta -= Fb << i;
            
                if (delta > deltaTmp)  // < 0
                    delta = deltaTmp;
                else
                    res += 1 << i;
            }

            if (res != index / b) failsDiv++;
            // Group.Barrier();
        }
        
        result[index] = failsDiv;
    }
    
    private static void Kernel64(
        Index1D index, long pval, int ro, int modLen,
        Values8ToGpu32 mg, Values8ToGpu64 kg, ArrayView<ulong> result)
    {
        ulong failsDiv = 0;
        
        ulong Fa = Fval64(index, ro, modLen, mg, kg);
        int aLen = 64 - IntrinsicMath.BitOperations.LeadingZeroCount(Fa);

        for (long b = 1; b < pval; b++)
        {
            ulong Fb = Fval64(b, ro, modLen, mg, kg);
            int bLen = 64 - IntrinsicMath.BitOperations.LeadingZeroCount(Fb);

            long res = 0;
            ulong delta = Fa;
            
            for (int i = aLen - bLen; i >= 0; i--)
            {
                ulong deltaTmp = delta;
                delta -= Fb << i;
            
                if (delta > deltaTmp)  // < 0
                    delta = deltaTmp;
                else
                    res += 1 << i;
            }

            if (res != index / b) failsDiv++;
            // Group.Barrier();
        }
        
        result[index] = failsDiv;
    }
    
    private static MInt128 Fval128(long num, int ro, int len, Values8ToGpu32 mg, Values8ToGpu128 kg)
    {
        MInt128 s = new();

        if (len >= 1) s += kg.m1 * (uint)(num % mg.m1);
        if (len >= 2) s += kg.m2 * (uint)(num % mg.m2);
        if (len >= 3) s += kg.m3 * (uint)(num % mg.m3);
        if (len >= 4) s += kg.m4 * (uint)(num % mg.m4);
        if (len >= 5) s += kg.m5 * (uint)(num % mg.m5);
        if (len >= 6) s += kg.m6 * (uint)(num % mg.m6);
        if (len >= 7) s += kg.m7 * (uint)(num % mg.m7);
        if (len >= 8) s += kg.m8 * (uint)(num % mg.m8);
        
        return s - ((s >> ro) << ro);
    }

    private static ulong Fval64(long num, int ro, int len, Values8ToGpu32 mg, Values8ToGpu64 kg)
    {
        ulong s = 0;

        if (len >= 1) s += (ulong)(num % mg.m1) * kg.m1;
        if (len >= 2) s += (ulong)(num % mg.m2) * kg.m2;
        if (len >= 3) s += (ulong)(num % mg.m3) * kg.m3;
        if (len >= 4) s += (ulong)(num % mg.m4) * kg.m4;
        if (len >= 5) s += (ulong)(num % mg.m5) * kg.m5;
        if (len >= 6) s += (ulong)(num % mg.m6) * kg.m6;
        if (len >= 7) s += (ulong)(num % mg.m7) * kg.m7;
        if (len >= 8) s += (ulong)(num % mg.m8) * kg.m8;
        
        return s - ((s >> ro) << ro);
    }

    // private static void KernelF64(
    //     Index1D index, ulong pval, int ro, int modLen,
    //     Values8ToGpu32 mg, Values8ToGpu64 kg, ArrayView<ulong> result)
    // {
    //     ulong res = Fval64(index, ro, modLen, mg, kg);
    //     int a = 64 - IntrinsicMath.BitOperations.LeadingZeroCount(res);
    //     result[index] = (ulong)a;
    // }
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
