using System.Diagnostics;
using System.Numerics;

namespace simd;

internal class SIMD
{
    private void printArr(in int[] arr, int start=0, int end=0)
    {
        end = (end == 0) ? arr.Length : end;
        arr.Skip(start).Take(end - start).ToList().ForEach(s => Console.Write($"{s}, "));
        Console.WriteLine();
    }

    public void Test1()
    {
        var arr1 = new int[] { 2, 3, 5, 7 };
        var arr2 = new int[] { 45, 23, 55, 74, };

        var a = new Vector<int>(arr1);
        var b = new Vector<int>(arr2);
        var s = Vector<int>.One;

        var c = Vector.Add(a, s);
        Console.WriteLine(c);
    }

    public void Test2()
    {
        Random randNum = new Random();
        int[] arr1 = Enumerable.Repeat(0, 101).Select(i => randNum.Next(0, 100)).ToArray();
        int[] arr2 = Enumerable.Repeat(0, 50).Select(i => randNum.Next(0, 100)).ToArray();

        //printArr(arr1, 0, 10);
        printArr(arr1);
        Console.WriteLine($"as {Vector<int>.Count} {Vector<long>.Count} {Vector<ushort>.Count}");

        int vectorSize = Vector<int>.Count;
        var accVector = Vector<int>.Zero;
        for (int i = 0; i < arr1.Length - vectorSize; i += vectorSize)
        {
            var v = new Vector<int>(arr1, i);
            accVector = Vector.Add(accVector, v);
        }
        int result = 0;
        for (int i = 0; i < arr1.Length; i++) result += arr1[i];
        Console.WriteLine(result);

        result = 0;
        foreach (int i in arr1) result += i;
        Console.WriteLine(result);
    }

    public void TestSpeed()
    {
        int count = 10_000;
        Stopwatch sw = new Stopwatch();


    }
}
