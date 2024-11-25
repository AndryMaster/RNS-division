using System.Diagnostics;
using System.Numerics;

namespace division;

internal sealed class DivisionTestRoOld : Division
{
    public enum TestType
    {
        Simple,
        ParallelToken,
        ParallelTokenMem,
    }
    
    public static void Test(uint[] newModules, TestType tt=TestType.ParallelTokenMem, int roInit=0)
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
            
            sw.Restart();
            switch (tt)
            {
                case TestType.Simple:
                    
                    for (long a = 0; a < P; a++)
                        for (long b = 1; b < P; b++)
                            if (divide(a, b, ro, k) != (a / b))
                                countBad++;
                    break;

                
                case TestType.ParallelToken:
                    CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
                    CancellationToken token = cancelTokenSource.Token;
                    try
                    {
                        Parallel.For(0, P,
                            new ParallelOptions { CancellationToken = token },
                            a =>
                            {
                                long threadBad = 0;
                                BigInteger Fa = F(a, ro, k);
                                int aLog = 0;
                                if (Fa != 0) aLog = (int)BigInteger.Log2(Fa) + 1;
                                for (long b = 1; b < P; b++)
                                    if (divide_half(Fa, b, aLog, ro, k) != (a / b))
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
            Console.WriteLine($"Ro={ro}\tTime={sw.ElapsedMilliseconds} ms \t" +
                $"Bad={countBad} (All={P * P}) \t" +
                $"Accuracy={1 - (double)countBad / (P * P)} ");
        }
        Console.WriteLine($"Full time: {swFull.ElapsedMilliseconds} ms");
        Console.WriteLine(DateTime.Now.ToLongTimeString());
    }

    private static long TestParallelMem(int ro, BigInteger[] k)
    {
        var memLog = new int[P];
        var mem = new BigInteger[P];
        Parallel.For(0, P,
            i =>
            {
                mem[i] = F(i, ro, k);
                if (mem[i] == 0) memLog[i] = 0;
                else memLog[i] = (int)BigInteger.Log2(mem[i]) + 1;
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
                        if (divide_mem(mem[a], mem[b], memLog[a] - memLog[b]) != (a / b))
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

    private static long divide_mem(in BigInteger Fa, in BigInteger Fb, int numIters)
    {
        long result = 0;
        BigInteger delta = Fa;

        for (int i = numIters; i >= 0; i--)
        {
            BigInteger deltaTmp = delta;
            delta -= Fb << i;

            if (delta < 0)
                delta = deltaTmp;
            else
                result += 1l << i;
        }

        return result;
    }
    
    private static long divide_half(in BigInteger Fa, long b, int logA, in int ro, in BigInteger[] k)
    {
        BigInteger Fb = F(b, ro, k);
        int logB = (int)BigInteger.Log2(Fb) + 1,
            numIters = logA - logB;
        
        long result = 0;
        BigInteger delta = Fa;
    
        for (int i = numIters - 1; i >= 0; i--)
        {
            BigInteger oldDelta = delta;
    
            delta -= Fb << i;
            if (delta < 0)
                delta = oldDelta;
            else
                result += 1l << i;
        }
    
        return result;
    }
    
    public static long divide(long divisible, long quotient, in int ro, in BigInteger[] k)
    {
        BigInteger Fa = F(divisible, ro, k);
        BigInteger Fb = F(quotient, ro, k);

        if (Fa <= 0 || Fb <= 0)
            return 0;
        
        int logA = (int)BigInteger.Log2(Fa) + 1,
            logB = (int)BigInteger.Log2(Fb) + 1,
            numIters = logA - logB;

        long result = 0;
        BigInteger delta = Fa;

        for (int i = numIters; i >= 0; i--)
        {
            BigInteger oldDelta = delta;

            delta -= Fb << i;
            if (delta < 0)
                delta = oldDelta;
            else
                result += 1l << i;
        }
        return result;
    }
}
