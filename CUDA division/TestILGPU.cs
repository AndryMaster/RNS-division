using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.ScanReduceOperations;
using ILGPU.Algorithms.Optimization;
using ILGPU.IR.Values;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;

namespace CUDA_division;

public class TestILGPU
{
    public static void Test() {
        var rand = new Random();
        var a = new int[1<<10];
        for (int i = 0; i < a.Length; i++) a[i] = rand.Next() % 100;
        
        var s = Stopwatch.StartNew();
        s.Restart();
        
        using var context = Context.CreateDefault();
        foreach (Device device in context) Console.WriteLine(device);

        // var accelerator = context.CreateCudaAccelerator(0);
        var accelerator = context.CreateCPUAccelerator(0);
        accelerator.PrintInformation();
            
        var buffer = accelerator.Allocate1D<int>(a.Length);
        buffer.CopyFromCPU(a);
        
        St1 st1 = new St1();
        unsafe
        {
            for (int i = 0; i < 5; i++) st1.Values[i] = 100 * (ulong)i;
        }

        var kernel = accelerator.LoadAutoGroupedStreamKernel
            <Index1D, ArrayView<int>, St1>(Kernel_5);
        kernel(a.Length, buffer.View, st1);

        // Reads data from the GPU buffer into a new CPU array.
        // Implicitly calls accelerator.DefaultStream.Synchronize() to ensure
        // that the kernel and memory copy are completed first.
        var data = buffer.GetAsArray1D();
        for (int i = 0; i < 10; ++i)
        {
            Console.Write(data[i] + ", ");
        }
        
        // var kernel = accelerator.LoadAutoGroupedStreamKernel
        //     <Index1D, ArrayView<long>, ArrayView<long>, ArrayView<long>>(Kernel_4);
        //
        // using var bufferA = accelerator.Allocate1D<long>(a.Length);
        // using var bufferConf = accelerator.Allocate1D<long>(10);
        // using var bufferOut = accelerator.Allocate1D<long>(a.Length);
        //
        // bufferA.CopyFromCPU(a);
        // bufferConf.MemSetToZero();
        // kernel(a.Length, bufferA.View, bufferOut.View);
        // accelerator.Synchronize();
        //
        // var data = bufferOut.GetAsArray1D();
        // for (int i = 0; i < 3; i++)
        // {
        //     for (int j = 0; j < 10; j++)
        //     {
        //         if (i == 0) Console.Write(a[j] + " ");
        //         if (i == 1) Console.Write(data[j] + " ");
        //         // if (i == 2) Console.Write(b[j] + " ");
        //     } Console.WriteLine();
        // }
        //
        // var conf = bufferConf.GetAsArray1D();
        // foreach (var elem in conf)
        //     Console.Write(elem + " ");
        // Console.WriteLine();

        Console.WriteLine("IL GPU ms: " + s.ElapsedMilliseconds);
    }
    
    [Benchmark]
    private static void Kernel_1(
        Index1D index,
        ArrayView<long> data,
        ArrayView<long> output)
    {
        Interop.WriteLine("Line {0}: {1}", index.X, data[index.X]);
        // output[index] = X2(data[index]);
        for (int i = 0; i < 1000; i++)
        {
            output[index] += data[index];
        }
        
        output[index] = output[index] / 100;
    }

    private static void Kernel_2(
        Index1D index,
        ArrayView<long> a,
        ArrayView<long> b,
        ArrayView<long> output)
    {
        //Interop.WriteLine("Line {0}: {1} {2}", index, a[index], b[index]);
        Interop.WriteLine("task   {0} {1}", Grid.IdxX, Group.IdxX);

        for (int i = 0; i < 100; i++)
        {
            a[index] += b[index];
        }

        output[index] = a[index] / 10;
    }

    private static void Kernel_3(
        //Index1D index,
        ArrayView<long> a,
        ArrayView<long> b,
        ArrayView<long> output)
    {
        int index = Grid.GlobalIndex.X;
        ref int mem = ref SharedMemory.Allocate<int>();

        if (Group.IsFirstThread) mem = 0;
        Group.Barrier();

        //Interop.WriteLine("Line {0}: {1} {2}", index, a[index], b[index]);
        //Interop.WriteLine("Const {0}", ++mem);
        Interop.WriteLine("task   {0} {1}", Grid.IdxX, Group.IdxX);

        for (int i = 0; i < 100; i++)
        {
            a[index] += b[index];
        }

        output[index] = a[index] / 10;
    }
    
    private static void Kernel_4(
        Index1D index,
        ArrayView<long> a,
        ArrayView<long> conf,
        ArrayView<long> output)
    {
        Interop.WriteLine("grid {0} \tgroup {1} \t{2}", Grid.IdxX, Group.IdxX, conf[1]);
        conf[0] = 777;
        conf[1] += 1;
        conf[2] = Group.DimX;
        conf[3] = Group.DimY;
        conf[4] = Group.DimZ;
        conf[5] = Grid.DimX;
        conf[6] = Grid.DimY;
        conf[7] = Grid.DimZ;
        conf[8] = -1;
        conf[9] = 777;

        if (conf[1] >= 1 && conf[8] == -1)
        {
            conf[9] = 777 * 2;
            conf[8] = Grid.GlobalLinearIndex;
        }
        
        
        output[index] = (a[index] << 5 / 187) + (a[index] / 7) + Group.IdxX;
    }
    
    private static unsafe void Kernel_5(Index1D index, ArrayView<int> data, St1 st1)
    {
        var a = new MInt128(st1.Values[index % St1.MaxValSize],
            st1.Values[(index + 1) % St1.MaxValSize]);
        data[index] = (int)(a.lo + a.hi);
    }

    public unsafe struct St1
    {
        public const int MaxValSize = 5;
        public fixed ulong Values[MaxValSize];
    }

    private static long X2(long x) => x << 1;
}