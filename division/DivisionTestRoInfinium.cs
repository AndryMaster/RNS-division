using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace division;

internal sealed class DivisionTestRoInfinium : Division
{
    public static bool useFileSaving = false;
    
    private class SaveState
    {
        [JsonPropertyName("isCorrect")]
        public bool isCorrect { get; set; }
        
        [JsonPropertyName("lastRo")]
        public int lastRo { get; set; }
        
        [JsonPropertyName("Modules")]
        public uint[] mods { get; set; }
        
        [JsonPropertyName("P")]
        public long P { get; set; }
        
        [JsonPropertyName("allTimeMs")]
        public long allTime { get; set; }
    }

    private static void SaveHistory(Dictionary<long, SaveState> saveStates)
    {
        if (useFileSaving)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            using (FileStream fs = new FileStream("save.json", FileMode.Create, FileAccess.Write))
            {
                JsonSerializer.Serialize(fs, saveStates, options);
            }
        }
    }
    
    private static Dictionary<long, SaveState> LoadHistory()
    {
        if (useFileSaving && File.Exists(@"save.json"))
        {
            using (FileStream fs = new FileStream("save.json", FileMode.Open, FileAccess.Read))
            {
                Dictionary<long, SaveState>? saveStates = JsonSerializer.Deserialize<Dictionary<long, SaveState>>(fs);
                return saveStates ?? new();
            }
        }
        return new();
    }

    private static void ClearHistory()
    {
        if (File.Exists(@"save.json"))
            File.Delete(@"save.json");
    }
    
    public static void Test(uint[] newModules)
    {
        initModules(newModules);
        
        long needIters = 0;
        for (long i = 1; i <= (P + 1) / 2; i++) needIters += P / i - 1;
        Console.Write($"\n\nP={P} ({needIters}) ro=<???> mods=[ ");
        foreach (int i in Modules)
            Console.Write($"{i}, ");
        Console.WriteLine($"]\nNeed iters={needIters}\n");
        Console.WriteLine(DateTime.Now.ToLongTimeString());

        Stopwatch sw = new Stopwatch();
        Stopwatch swFull = new Stopwatch();
        swFull.Start();
        
        var history = LoadHistory();
        if (!history.ContainsKey(P))
            history.Add(P, new SaveState { isCorrect = false, lastRo = 0, mods = Modules, P = P, allTime = 0 });
        
        int ro = history[P].lastRo;  // 0
        if (ro != 0) Console.WriteLine($"Restoring last Ro value (={ro})");
        
        bool useMemory = (P < 1L << 20); useMemory = false;
        if (useMemory) Console.WriteLine("Using memory");
        else Console.WriteLine("Not using memory");

        bool isCorrect = history[P].isCorrect;  // false
        while (!isCorrect)
        {
            BigInteger[] k = calk_k(++ro);
            sw.Restart();
            
            if (useMemory)
                isCorrect = TestMem(ro, k);
            else if (ro < 128)
                isCorrect = TestNoMem128(ro, k);  // TestNoMem(ro, k);
            else
                isCorrect = TestNoMem(ro, k);
            
            history[P].lastRo = ro;
            history[P].isCorrect = isCorrect;
            history[P].allTime += sw.ElapsedMilliseconds;
            SaveHistory(history);
            
            Console.WriteLine($"Ro={ro}\tTime={sw.ElapsedMilliseconds} " +
                              $"({history[P].allTime}) ms \tisCorrect={isCorrect}");
        }
        Console.WriteLine($"Result: Ro={ro}\nFull time: {swFull.ElapsedMilliseconds} ms");
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
            if (i == P) return BigInteger.One << 255;
            
            BigInteger s = 0;
            
            for (int j = 0; j < Modules.Length; j++)
                s += k[j] * (i % Modules[j]);    

            return s - ((s >> ro) << ro);
        }

        bool isCorrect = true;

        using CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        CancellationToken token = cancelTokenSource.Token;
        
        try
        {
            Parallel.For(1, (P + 1) / 2 + 1, new ParallelOptions { CancellationToken = token }, i =>
            {
                BigInteger value, valueInit = GetF(i);
                value = valueInit;
        
                for (long j = i * 2; j <= P; j += i)
                {
                    value += valueInit;
                    if (!(GetF(j - 1) < value && value <= GetF(j))) cancelTokenSource.Cancel(); // break;
                    if (token.IsCancellationRequested) break;
                }
            });
        }
        catch (OperationCanceledException)
        {
            isCorrect = false;
        }
        
        return isCorrect;
    }
    
    private static bool TestNoMem128(int ro, BigInteger[] k)
    {
        bool isCorrect = true;

        UInt128[] k128 = k.Select(el => (UInt128)el).ToArray();
        UInt128 GetF(long i)
        {
            if (i == P) return UInt128.MaxValue;
            
            UInt128 s = 0;
            
            for (int j = 0; j < Modules.Length; j++)
                s += k128[j] * new UInt128(0, (ulong)i % Modules[j]);;    

            return s - ((s >> ro) << ro);
        }
        
        using CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        CancellationToken token = cancelTokenSource.Token;
        
        try
        {
            Parallel.For(1, (P + 1) / 2 + 1, new ParallelOptions { CancellationToken = token }, i =>
            {
                UInt128 value, valueInit = GetF(i);
                value = valueInit;
        
                for (long j = i * 2; j <= P; j += i)
                {
                    // Console.WriteLine($"({i}; {j}): \t[{GetF(j - 1)} < {value} <= {GetF(j)}]\t" +
                    //                   $"({GetF(j - 1) < value && value <= GetF(j)}) ({j/i-1}, {P/i-1})");
                    value += valueInit;
                    if (!(GetF(j - 1) < value && value <= GetF(j))) cancelTokenSource.Cancel(); // break;
                    if (token.IsCancellationRequested) break;
                }
            });
        }
        catch (OperationCanceledException)
        {
            isCorrect = false;
        }
        
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
    
    
    // NO MEM 128
    // try
    // {
    //     Parallel.For(0, P, new ParallelOptions { CancellationToken = token }, i =>
    //     {
    //         if (!(GetF(i) < GetF(i+1)))
    //             cancelTokenSource.Cancel();
    //     });
    // }
    // catch (OperationCanceledException)
    // {
    //     isCorrect = false;
    // }
    // Console.Write("*");
}
