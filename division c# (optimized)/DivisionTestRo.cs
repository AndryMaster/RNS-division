using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;

namespace division;

internal class DivisionTestRo : Division
{
    private const long randIters = 100_000_000;

    public enum TestType
    {
        Simple,
        Parallel,
        ParallelToken,
        ParallelTokenRand,
        ParallelSIMD,
        ParallelMem,
    }

    public static void initModules(int[] newModules)
    {
        modules = newModules;
        P = 1;
        foreach (int i in modules)
            P *= i;
    }

    public static void Test(int[] newModules, TestType tt=TestType.Simple, int roInit=0)
    {
        initModules(newModules);

        Console.Write($"\n\nP={P} ro=??? mods=[ ");  // {(int)Math.Pow(P, 0.5)}
        foreach (int i in modules)
            Console.Write($"{i}, ");
        Console.WriteLine("]\n");
        Console.WriteLine(DateTime.Now.ToLongTimeString());

        Stopwatch sw = new Stopwatch();
        Stopwatch swFull = new Stopwatch();
        swFull.Start();

        //int ro = (int)Math.Pow(P, 0.4);
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


                case TestType.Parallel:

                    ParallelLoopResult resultParallel =
                        Parallel.For(0, P, a =>
                        {
                            long threadBad = 0;
                            for (long b = 1; b < P; b++)
                                if (divide(a, b, ro, k) != (a / b)) threadBad++;
                            resultCollection.Add(threadBad);
                        });
                    countBad = resultCollection.Sum();
                    break;


                case TestType.ParallelToken:

                    try
                    {
                        Parallel.For(0, P,
                            new ParallelOptions { CancellationToken = token },
                            a =>
                            {
                                long threadBad = 0;
                                for (long b = 1; b < P; b++)
                                    if (divide(a, b, ro, k) != (a / b))
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


                case TestType.ParallelTokenRand:

                    if (randIters > P*P / 10) {
                        Console.WriteLine("AAAAAAAAAAAAAAAAAAAAAAA");
                        break; }
                    try
                    {
                        Parallel.For(0, randIters / 10_000,
                            new ParallelOptions { CancellationToken = token },
                            c =>
                            {
                                long a, b;
                                for (int i = 0; i < 10_000; i++)
                                {
                                    a = randNum.NextInt64(1, P);
                                    b = randNum.NextInt64(1, P);
                                    if (divide(a, b, ro, k) != (a / b))
                                    {
                                        cancelTokenSource.Cancel();
                                        break;
                                    }
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


                case TestType.ParallelMem:
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
                    long threadBad = 0;
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

    protected static int divide_mem(in BigInteger Fa, in BigInteger Fb, int numIters)
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
}
