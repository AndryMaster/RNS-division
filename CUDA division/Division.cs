using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;

namespace CUDA_division;

public class Division
{
    public static int[] Modules;
    public static long P = 1;
    
    public static void initModules(int[] newModules)
    {
        Modules = newModules;
        P = 1;
        foreach (int i in Modules)
            P *= i;
    }

    public static BigInteger[] calk_k(in int ro)
    {
        long[] P_i = new long[Modules.Length];
        BigInteger[] m_i = new BigInteger[Modules.Length];

        for (int i = 0; i < Modules.Length; i++)
        {
            P_i[i] = P / Modules[i];
        }

        for (int i = 0; i < Modules.Length; i++)
        {
            int m = 1;
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
        for (int i = 0; i < Modules.Length; i++)  // SIMD
            rns[i] = (int)(n % Modules[i]);
        return rns;
    }

    public static BigInteger F(long num, in int ro, in BigInteger[] k)
    {
        BigInteger s = 0;
        int[] rns = mod(num);

        for (int i = 0; i < Modules.Length; i++)  // SIMD
            s += rns[i] * k[i];

        return s - ((s >> ro) << ro);
    }
}
