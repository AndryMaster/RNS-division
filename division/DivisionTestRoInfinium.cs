using System.Diagnostics;
using System.Numerics;

namespace division;

internal sealed class DivisionTestRoInfinium : Division
{
    public static void Test(uint[] newModules)
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

        int ro = 0;
        BigInteger[] k;
        bool useMemory = (P < 1L << 20);
        bool isCorrect = false;
        
        while (!isCorrect)
        {
            k = calk_k(++ro);
            sw.Restart();
            
            if (useMemory)
                isCorrect = TestMem(ro, k);
            else
                isCorrect = TestNoMem(ro, k);
            
            Console.WriteLine($"Ro={ro}\tTime={sw.ElapsedMilliseconds} ms \tisCorrect={isCorrect}");
        }
        Console.WriteLine($"Full time: {swFull.ElapsedMilliseconds} ms");
        Console.WriteLine(DateTime.Now.ToLongTimeString());
    }

    private static bool TestMem(int ro, BigInteger[] k)
    {
        bool isCorrect = true;
        
        CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        CancellationToken token = cancelTokenSource.Token;
        
        var memoryF = new BigInteger[P+1];
        Parallel.For(0, P, i =>
        {
            memoryF[i] = F(i, ro, k);
        });
        memoryF[P] = new BigInteger(1) << 127;
        Console.Write("*");
        
        for (int i = 0; i < P; i++)
        {
            if (!(memoryF[i] < memoryF[i + 1]))
            {
                isCorrect = false;
                break;
            }
        }

        if (isCorrect)
        {
            try
            {
                Parallel.For(1, (P + 1) / 2 + 1, new ParallelOptions { CancellationToken = token }, i =>
                {
                    BigInteger value, valueInit;
                    value = valueInit = memoryF[i];
            
                    for (long j = i * 2; j <= P; j += i)
                    {
                        value += valueInit;
                        if (!(memoryF[j - 1] < value && value <= memoryF[j]))
                        {
                            cancelTokenSource.Cancel();
                        }
                    }
                });
            }
            catch (OperationCanceledException)
            {
                isCorrect = false;
            }
        }
        
        cancelTokenSource.Dispose();
        return isCorrect;
    }

    private static bool TestNoMem(int ro, BigInteger[] k)
    {
        BigInteger GetF(long i)
        {
            if (i != P)
                return F(i, ro, k);
            return new BigInteger(1) << 127;
        }

        bool isCorrect = true;
        
        CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        CancellationToken token = cancelTokenSource.Token;
        
        try
        {
            Parallel.For(0, P, new ParallelOptions { CancellationToken = token }, i =>
            {
                if (!(GetF(i) < GetF(i+1)))
                {
                    cancelTokenSource.Cancel();
                }
            });
        }
        catch (OperationCanceledException)
        {
            isCorrect = false;
        }

        if (isCorrect)
        {
            try
            {
                Parallel.For(1, (P + 1) / 2 + 1, new ParallelOptions { CancellationToken = token }, i =>
                {
                    BigInteger value, valueInit;
                    value = valueInit = GetF(i);
            
                    for (long j = i * 2; j <= P; j += i)
                    {
                        value += valueInit;
                        if (!(GetF(j - 1) < value && value <= GetF(j)))
                        {
                            cancelTokenSource.Cancel();
                        }
                    }
                });
            }
            catch (OperationCanceledException)
            {
                isCorrect = false;
            }
        }
        
        cancelTokenSource.Dispose();
        return isCorrect;
    }
    
    // MEM
    
    // try
    // {
    //     Parallel.For(0, P, new ParallelOptions { CancellationToken = token }, i =>
    //     {
    //         if (!(memoryF[i] < memoryF[i + 1]))
    //         {
    //             cancelTokenSource.Cancel();
    //         }
    //     });
    // }
    // catch (OperationCanceledException)
    // {
    //     isCorrect = false;
    // }
    
    // for (long i = 1; i <= (P + 1) / 2; i++)  // for (long i = (P + 1) / 2; i > 0; i--)
    // {
    //     BigInteger value, valueInit;
    //     value = valueInit = memoryF[i];
    //
    //     for (long j = i * 2; j <= P; j += i)
    //     {
    //         value += valueInit;
    //         if (!(memoryF[j - 1] < value && value <= memoryF[j]))
    //         {
    //             isCorrect = false;
    //             break;
    //         }
    //     }
    //
    //     if (!isCorrect) break;
    // }
    
    
    // NO MEM
    
    // var tmp = GetF(0);
    // for (int i = 0; i < P; i++)
    // {
    //     var nextTmp = GetF(i + 1);
    //     if (!(tmp < nextTmp))
    //     {
    //         isCorrect = false;
    //         break;
    //     }
    //     tmp = nextTmp;
    // }
    
    // for (long i = 1; i <= (P + 1) / 2; i++)
    // {
    //     BigInteger value, valueInit;
    //     value = valueInit = GetF(i);
    //
    //     for (long j = i * 2; j <= P; j += i)
    //     {
    //         value += valueInit;
    //         if (!(GetF(j - 1) < value && value <= GetF(j)))
    //         {
    //             isCorrect = false;
    //             break;
    //         }
    //     }
    //
    //     if (!isCorrect) break;
    // }
}
