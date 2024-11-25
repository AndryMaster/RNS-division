#define DEBUG
#undef DEBUG

namespace division;

class Program
{
    public const int MaxCountModules = 8;
    public const int CurrentCountModules = 3;
    
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        
        // DivisionTestRo.Test(new uint[]{2, 3, 5, 7});
        // DivisionTestRo.Test(new uint[]{5, 7, 13, 31}, useFast:false);
        DivisionTestRo.Test(new uint[]{125, 127, 128}, useFast:true);
        
        // DivisionTestRoOld.Test(new uint[]{125, 127, 128});
    }
}