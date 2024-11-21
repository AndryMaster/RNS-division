using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;

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
            b[i] = rand.Next();
        }

        var s = Stopwatch.StartNew();
        
        s.Restart();
        using var context = Context.CreateDefault();
        foreach (Device device in context) Console.WriteLine(device);
        
        var accelerator = context.CreateCudaAccelerator(0);
        // var accelerator = context.CreateCPUAccelerator(0);
        accelerator.PrintInformation();
        
        var kernel = accelerator.LoadAutoGroupedStreamKernel<
            Index1D,
            ArrayView<long>,
            ArrayView<long>>(Kernel_1);
        
        using var buffer = accelerator.Allocate1D<long>(a.Length);
        using var bufferOut = accelerator.Allocate1D<long>(a.Length);
        
        buffer.CopyFromCPU(a);
        kernel((int)buffer.Length, buffer.View, bufferOut.View);
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

        Console.WriteLine("IL GPU: " + s.ElapsedMilliseconds);
    }
    
    [Benchmark]
    private static void Kernel_1(
        Index1D index,
        ArrayView<long> data,
        ArrayView<long> output)
    {
        if (index == 5) return;
        Interop.WriteLine("Line {0}: {1}", index, data[index]);
        output[index] = X2(data[index]);
    }

    private static long X2(long x)
    {
        return x << 1;
    }

}