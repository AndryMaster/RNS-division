using System.Numerics;

namespace division;

public class Division
{
    public static uint[] Modules;
    public static long P = 1;
    // public static ulong Pulong = 1;
    
    public static void initModules(uint[] newModules)
    {
        Modules = newModules.Order().ToArray();
        P = 1;
        foreach (var i in Modules)
            P *= i;
        // Pulong = (ulong)P;
    }

    public static ulong[] calk_k64(int ro)
    {
        BigInteger[] kBig = calk_k(ro);
        ulong[] k = new ulong[Modules.Length];
        for (int i = 0; i < Modules.Length; i++) k[i] = (ulong)kBig[i];
        return k;
    }
    
    public static MInt128[] calk_k128(int ro)
    {
        BigInteger[] kBig = calk_k(ro);
        MInt128[] k = new MInt128[Modules.Length];
        for (int i = 0; i < Modules.Length; i++) k[i] = (MInt128)kBig[i];
        return k;
    }
    
    
    public static BigInteger[] calk_k(int ro)
    {
        ulong[] P_i = new ulong[Modules.Length];
        BigInteger[] m_i = new BigInteger[Modules.Length];

        for (int i = 0; i < Modules.Length; i++)
        {
            P_i[i] = (ulong)P / Modules[i];
        }

        for (int i = 0; i < Modules.Length; i++)
        {
            uint m = 1;
            while (true)
            {
                if (m * P_i[i] % Modules[i] == 1)
                {
                    m_i[i] = m * BigInteger.Pow(2, ro) / Modules[i];
                    break;
                }
                m++;
            }
        }
        return m_i;
    }

    public static int[] mod(long n)
    {
        int[] rns = new int[Modules.Length];
        n = Math.Abs(n);
        for (int i = 0; i < Modules.Length; i++)
            rns[i] = (int)(n % Modules[i]);
        return rns;
    }

    public static BigInteger F(long num, in int ro, in BigInteger[] k)
    {
        BigInteger s = 0;
        int[] rns = mod(num);

        for (int i = 0; i < Modules.Length; i++)
            s += rns[i] * k[i];

        return s - ((s >> ro) << ro);
    }
}