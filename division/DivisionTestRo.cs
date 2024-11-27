using System.Diagnostics;
using System.Runtime.CompilerServices;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;

namespace division;

public class DivisionTestRo : Division
{
    private const int MyGroupSize = 768 * 2;
    
    public static void Test(uint[] newModules, bool useFast=false)
    {
        initModules(newModules);
        using var context = Context.CreateDefault();
        foreach (Device device in context) Console.WriteLine(device);
        
        // using var accelerator = context.CreateCPUAccelerator(0);
        using var accelerator = context.CreateCudaAccelerator(0);
        
        accelerator.PrintInformation();
        Console.Write($"\nP={P} ro=??? mods=[ ");
        foreach (int i in Modules) Console.Write($"{i}, ");
        Console.WriteLine($"]\n{DateTime.Now.ToLongTimeString()}");

        Stopwatch sw = new Stopwatch();
        Stopwatch swFull = new Stopwatch();
        swFull.Start();
        
        var kernel64 = accelerator.LoadAutoGroupedStreamKernel<
            Index1D, long, long, int, int, FixedArr32, FixedArr64,
            ArrayView<ulong>>(Kernel64);
        
        var kernel128 = accelerator.LoadAutoGroupedStreamKernel<
            Index1D, long, long, int, int, FixedArr32, FixedArr64, FixedArr64,
            ArrayView<ulong>>(Kernel128);
        
        var bufferRes = accelerator.Allocate1D<ulong>(MyGroupSize);
        var modulesGpu = new FixedArr32(Modules);
        
        int ro = 0;
        ulong failsDiv = 1;
        while (failsDiv > 0)
        {
            ro++;
            failsDiv = 0;
            sw.Restart();
            
            // Console.WriteLine($"Group={MyGroupSize}");
            for (long offset = 0; offset < P; offset += MyGroupSize)
            {
                bufferRes.MemSetToZero();
                var runSize = (int)Math.Min(P - offset, MyGroupSize);
                // Console.WriteLine($"Size={runSize}\t Offset={offset}");
                
                if (ro < 64)
                {
                    var k = calk_k64(ro);
                    var kGpu = new FixedArr64(k);
                    kernel64(runSize, offset, P, ro, Modules.Length, modulesGpu, kGpu, bufferRes.View);
                    // for (int i = 0; i < Program.MaxCountModules; i++) 
                    //     Console.Write($"{modulesGpu[i]} ({kGpu[i]}), ");
                    // Console.WriteLine();
                }
                else if (ro < 128)
                {
                    var k = calk_k128(ro);
                    var kHiGpu = new FixedArr64(k.Select(elem => elem.hi).ToArray());
                    var kLoGpu = new FixedArr64(k.Select(elem => elem.lo).ToArray());
                    kernel128(runSize, offset, P, ro, Modules.Length, modulesGpu, kHiGpu, kLoGpu, bufferRes.View);
                }
                accelerator.Synchronize();
                
                var res = bufferRes.GetAsArray1D();
                failsDiv += (ulong)res.Cast<long>().Sum();
            
                if (useFast && failsDiv > 0)
                {
                    failsDiv = 1;
                    break;
                }
            }
            // var buffer = accelerator.Allocate1D<ulong>(P);
            // buffer.MemSetToZero();
            // if (ro < 64)
            // {
            //     var k = calk_k64(ro);
            //     var kGpu = new FixedArr64(k);
            //     kernel64((int)P, 0, P, ro, Modules.Length, modulesGpu, kGpu, buffer.View);
            // }
            // accelerator.Synchronize();
            // var res = buffer.GetAsArray1D();
            // failsDiv += (ulong)res.Cast<long>().Sum();
            // buffer.Dispose();
            
            Console.WriteLine($"Ro={ro}\tTime={sw.ElapsedMilliseconds} ms \t" +
                              $"Bad={failsDiv} (All={P * P}) \t" +
                              $"Accuracy={1 - (double)failsDiv / (P * P)} ");
        }
        bufferRes.Dispose();
        Console.WriteLine($"Full time: {swFull.ElapsedMilliseconds} ms");
        Console.WriteLine(DateTime.Now.ToLongTimeString());
    }
    
    private static void Kernel64(
        Index1D index, long offset, long pval, int ro, int modLen,
        FixedArr32 modulesGpu, FixedArr64 kGpu, ArrayView<ulong> result)
    {
        ulong failsDiv = 0;

        long a = index + offset;
        ulong Fa = FCalc(a, ro, modLen, modulesGpu, kGpu);
        int aLen = 64 - IntrinsicMath.BitOperations.LeadingZeroCount(Fa);

        for (long b = 1; b < pval; b++)
        {
            ulong Fb = FCalc(b, ro, modLen, modulesGpu, kGpu);
            int bLen = 64 - IntrinsicMath.BitOperations.LeadingZeroCount(Fb);
            
            long res = 0;
            ulong delta = Fa;
            
            for (int i = aLen - bLen; i >= 0; i--)
            {
                ulong deltaTmp = delta;
                delta -= Fb << i;
            
                if (delta > deltaTmp)
                    delta = deltaTmp;
                else
                    res += 1L << i;
            }

            if (res != a / b) failsDiv++;
            // if (Fb != 0 && ((long)Fa / (long)Fb) != a / b) failsDiv++;  // !!!!!!!!!!!!!!!!!!!
        }
        
        result[index] = failsDiv;
    }
    
    private static void Kernel128(
        Index1D index, long offset, long pval, int ro, int modLen,
        FixedArr32 modulesGpu, FixedArr64 kHiGpu, FixedArr64 kLoGpu, ArrayView<ulong> result)
    {
        ulong failsDiv = 0;
        
        var kGpu = new MInt128[Program.CurrentCountModules];
        for (int i = 0; i < kGpu.Length; i++)
            kGpu[i] = new MInt128(kHiGpu[i], kLoGpu[i]);
        
        long a = index + offset;
        MInt128 Fa = FCalc(a, ro, modLen, modulesGpu, kGpu);
        int aLen = 128 - MInt128.LeadingZeroCount(Fa);

        for (long b = 1; b < pval; b++)
        {
            MInt128 Fb = FCalc(b, ro, modLen, modulesGpu, kGpu);
            int bLen = 128 - MInt128.LeadingZeroCount(Fb);

            long res = 0;
            MInt128 delta = Fa;
            
            for (int i = aLen - bLen; i >= 0; i--)
            {
                MInt128 deltaTmp = delta;
                delta -= Fb << i;
            
                if (delta > deltaTmp)
                    delta = deltaTmp;
                else
                    res += 1 << i;
            }

            if (res != a / b) failsDiv++;
            // Group.Barrier();
        }
        
        result[index] = failsDiv;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FCalc(long num, int ro, int modLen, FixedArr32 modulesGpu, FixedArr64 kGpu)
    {
        ulong s = 0;
        for (int i = 0; i < Program.CurrentCountModules; i++)
            s += kGpu[i] * ((ulong)num % modulesGpu[i]);
        
        return s - ((s >> ro) << ro);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static MInt128 FCalc(long num, int ro, int modLen, FixedArr32 modulesGpu, MInt128[] kGpu)
    {
        MInt128 s = new MInt128();
        for (int i = 0; i < Program.CurrentCountModules; i++)
            s += kGpu[i] * (uint)((ulong)num % modulesGpu[i]);
        
        return s - ((s >> ro) << ro);
    }
}
