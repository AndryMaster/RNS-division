namespace division;

class Program
{
    public const int MaxCountModules = 8;      // Old
    public const int CurrentCountModules = 4;  // Old
    
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        
        DivisionTestRoInfinium.useFileSaving = true;
        CalcAllRo();  // Auto tests
        
        // DivisionTestRoInfinium.Test(new uint[]{2, 3});
        // DivisionTestRoInfinium.Test(new uint[]{2, 3, 5, 7});
        // DivisionTestRoInfinium.Test(new uint[]{27, 29, 31, 32});
        // DivisionTestRoInfinium.Test(new uint[]{125, 127, 128});
        // DivisionTestRoInfinium.Test(new uint[]{125, 127, 128, 3});
        // DivisionTestRoInfinium.Test(new uint[]{507, 509, 511, 512});
        
        // DivisionTestRo.Test(new uint[]{2, 3, 5, 7});
        // DivisionTestRo.Test(new uint[]{5, 7, 13, 31}, useFast:true);
        // DivisionTestRo.Test(new uint[]{125, 127, 128}, useFast:true);
        // DivisionTestRoOld.Test(new uint[]{125, 127, 128});
    }

    static void CalcAllRo()
    {
        List<uint[]> listMods = new List<uint[]>()
        {
            new uint[]{27, 29, 31, 32},
            // new uint[]{123, 125, 127, 128},
            new uint[]{507, 509, 511, 512},
            new uint[]{2043, 2045, 2047, 2048},
            new uint[]{8187, 8189, 8191, 8192},
            
            new uint[]{125, 127, 128},
            new uint[]{123, 125, 127, 128},
            new uint[]{121, 123, 125, 127, 128},
            new uint[]{119, 121, 123, 125, 127, 128},
            new uint[]{113, 119, 121, 123, 125, 127, 128},
            new uint[]{109, 113, 119, 121, 123, 125, 127, 128},
        };

        Dictionary<long, uint[]> tests = new();
        foreach (uint[] mods in listMods)
        {
            long P = 1;
            foreach (var i in mods) P *= i;
            tests.Add(P, mods);
        }
        
        Console.WriteLine("\nAll tests:");
        foreach (var key in tests.Keys.Order())
        {
            Console.Write($"{key}: [");
            foreach (int i in tests[key]) Console.Write($"{i}, ");
            Console.WriteLine("]");
        }
        foreach (var key in tests.Keys.Order())
        {
            DivisionTestRoInfinium.Test(tests[key]);
        }
    }
}
