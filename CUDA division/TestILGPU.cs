using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using ILGPU;
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
        var a = new long[1<<10];
        var b = new long[1<<10];

        for (int i = 0; i < a.Length; i++)
        {
            a[i] = rand.Next() % 100;
            b[i] = rand.Next() % 10;
        }

        var s = Stopwatch.StartNew();
        
        s.Restart();
        using var context = Context.CreateDefault();
        foreach (Device device in context) Console.WriteLine(device);

        // var accelerator = context.CreateCudaAccelerator(0);
        //var accelerator = context.CreateCLAccelerator(0);
        var accelerator = context.CreateCPUAccelerator(0);
        accelerator.PrintInformation();

        //var kernel = accelerator.LoadAutoGroupedStreamKernel<
        //    Index1D,
        //    ArrayView<long>,
        //    ArrayView<long>>(Kernel_1);

        //using var buffer = accelerator.Allocate1D<long>(a.Length);
        //using var bufferOut = accelerator.Allocate1D<long>(a.Length);

        //buffer.CopyFromCPU(a);
        //kernel(a.Length, buffer.View, bufferOut.View);
        //accelerator.Synchronize();

        //var groupSize = Math.Min(accelerator.MaxNumThreadsPerGroup, 128);
        //KernelConfig config = ((a.Length + groupSize - 1) / groupSize, groupSize);

        //var kernel = accelerator.LoadStreamKernel
        //    <ArrayView<long>, ArrayView<long>, ArrayView<long>> (Kernel_3);
        //kernel(config, bufferA.View, bufferB.View, bufferOut.View);

        var kernel = accelerator.LoadAutoGroupedStreamKernel
            <Index1D, ArrayView<long>, ArrayView<long>, ArrayView<long>>(Kernel_2);

        using var bufferA = accelerator.Allocate1D<long>(a.Length);
        using var bufferB = accelerator.Allocate1D<long>(a.Length);
        using var bufferOut = accelerator.Allocate1D<long>(a.Length);

        bufferA.CopyFromCPU(a);
        bufferB.CopyFromCPU(b);
        kernel(a.Length, bufferA.View, bufferB.View, bufferOut.View);
        accelerator.Synchronize();

        var data = bufferOut.GetAsArray1D();
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if (i == 0) Console.Write(a[j] + " ");
                if (i == 1) Console.Write(data[j] + " ");
                if (i == 2) Console.Write(b[j] + " ");
            } Console.WriteLine();
        }

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

    private static long X2(long x)
    {
        return x << 1;
    }

}