using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;

namespace CUDA_division;

internal class DivisionTestRoCpu : Division
{
    public enum TestType
    {
        Simple,
        ParallelToken,
        ParallelTokenMem,
    }
    
    public static void Test(int[] newModules, TestType tt=TestType.ParallelTokenMem, int roInit=0)
    {
        initModules(newModules);

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

            sw.Reset();
            sw.Start();

            // Vars
            Random randNum = new Random();
            var resultCollection = new ConcurrentBag<long>();
            CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
            CancellationToken token = cancelTokenSource.Token;

            switch (tt)
            {
                case TestType.Simple:
                    
                    for (long a = 0; a < P; a++)
                        for (long b = 1; b < P; b++)
                            if (divide(a, b, ro, k) != (a / b))
                                countBad++;
                    break;

                
                case TestType.ParallelToken:

                    try
                    {
                        Parallel.For(0, P,
                            new ParallelOptions { CancellationToken = token },
                            a =>
                            {
                                long threadBad = 0;
                                BigInteger Fa = F(a, ro, k);
                                double aIters = BigInteger.Log(Fa, 2);
                                for (long b = 1; b < P; b++)
                                    if (divide_half(Fa, b, aIters, ro, k) != (a / b))
                                    {
                                        cancelTokenSource.Cancel();
                                        break;
                                    }
                            });
                    }
                    catch (OperationCanceledException) 
                    {
                        countBad = P * P;
                    }
                    finally
                    {
                        cancelTokenSource.Dispose();
                    }
                    break;

                
                case TestType.ParallelTokenMem:
                    countBad = TestParallelMem(ro, k);
                    break;
            }

            sw.Stop();
            Console.WriteLine($"Ro={ro}\tTime={sw.ElapsedMilliseconds} ms \t" +
                $"Bad={countBad} (All={P * P}) \t" +
                $"Accuracy={1 - (double)countBad / (P * P)} ");
        }
        swFull.Stop();
        Console.WriteLine($"Full time: {swFull.ElapsedMilliseconds} ms");
        Console.WriteLine(DateTime.Now.ToLongTimeString());
    }

    protected static long TestParallelMem(int ro, BigInteger[] k)
    {
        var memLog = new double[P];
        var mem = new BigInteger[P];
        Parallel.For(0, P,
            i =>
            {
                mem[i] = F(i, ro, k);
                memLog[i] = BigInteger.Log(mem[i], 2);
            });
        Console.Write("*");

        long countBad = 0;
        CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        CancellationToken token = cancelTokenSource.Token;

        try
        {
            Parallel.For(0, P,
                new ParallelOptions { CancellationToken = token },
                a =>
                {
                    for (long b = 1; b < P; b++)
                        if (divide_mem(mem[a], mem[b], (int)(memLog[a] - memLog[b]) + 1) != (a / b))
                        {
                            cancelTokenSource.Cancel();
                            break;
                        }
                });
        }
        catch (OperationCanceledException)
        {
            countBad = P * P;
        }
        finally
        {
            cancelTokenSource.Dispose();
        }

        return countBad;
    }

    private static int divide_mem(in BigInteger Fa, in BigInteger Fb, int numIters)
    {
        int result = 0;
        BigInteger delta = Fa;

        for (int i = numIters - 1; i >= 0; i--)
        {
            BigInteger deltaTmp = delta;
            delta -= Fb << i;

            if (delta < 0)
                delta = deltaTmp;
            else
                result += 1 << i;
        }

        return result;
    }
    
    private static int divide_half(in BigInteger Fa, long b, double aIters, in int ro, in BigInteger[] k)
    {
        BigInteger Fb = F(b, ro, k);
        int numIters = (int)(aIters - BigInteger.Log(Fb, 2.0)) + 1;

        int result = 0;
        BigInteger delta = Fa;

        for (int i = numIters - 1; i >= 0; i--)
        {
            BigInteger oldDelta = delta;

            delta -= Fb << i;
            if (delta < 0)
                delta = oldDelta;
            else
                result += 1 << i;
        }

        return result;
    }
    
    public static int divide(long divisible, long quotient, in int ro, in BigInteger[] k)
    {
        BigInteger Fa = F(divisible, ro, k);
        BigInteger Fb = F(quotient, ro, k);

        if (Fa <= 0 || Fb <= 0)
            return 0;

        int numIters = (int)(BigInteger.Log(Fa, 2.0) - BigInteger.Log(Fb, 2.0)) + 1;

        int result = 0;
        BigInteger delta = Fa;

        for (int i = numIters - 1; i >= 0; i--)
        {
            BigInteger oldDelta = delta;

            delta -= Fb << i;
            if (delta < 0)
                delta = oldDelta;
            else
                result += 1 << i;
        }

        return result;  // int[] resultRNS = mod(result);
    }
}
