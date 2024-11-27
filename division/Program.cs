namespace division;

class Program
{
    public const int MaxCountModules = 8;
    public const int CurrentCountModules = 4;
    
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        
        // DivisionTestRoInfinium.Test(new uint[]{2, 3, 5, 7});
        // DivisionTestRoInfinium.Test(new uint[]{5, 7, 13, 31});
        // DivisionTestRoInfinium.Test(new uint[]{27, 37, 128});
        // DivisionTestRoInfinium.Test(new uint[]{125, 127, 128});
        
        DivisionTestRoInfinium.Test(new uint[]{125, 127, 128});
        DivisionTestRoInfinium.Test(new uint[]{27, 29, 31, 32});
        DivisionTestRoInfinium.Test(new uint[]{123, 125, 127, 128});
        DivisionTestRoInfinium.Test(new uint[]{507, 509, 511, 512});
        DivisionTestRoInfinium.Test(new uint[]{121, 123, 125, 127, 128});
        DivisionTestRoInfinium.Test(new uint[]{2043, 2045, 2047, 2048});
        
        // DivisionTestRo.Test(new uint[]{2, 3, 5, 7});
        // DivisionTestRo.Test(new uint[]{5, 7, 13, 31}, useFast:true);
        // DivisionTestRo.Test(new uint[]{125, 127, 128}, useFast:true);
        
        // DivisionTestRoOld.Test(new uint[]{125, 127, 128});
    }
}
