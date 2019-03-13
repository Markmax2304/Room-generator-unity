using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ParkMillerNumberGenerator
{
    const long m = 2147483647;
    const long a = 16807;
    const double am = 1f / m;
    const long iq = 12773;
    const long ir = 2836;
    
    static long seed = 1;

    public static void InitSeed(long dum)
    {
        seed = dum == 0 ? Time.time.GetHashCode() : dum;
    }

    public static void InitSeed()
    {
        seed = Time.time.GetHashCode();
    }

    public static double GetRandomNumber()
    {
        long k;
        k = seed / iq;
        if ((seed = a * (seed - k * iq) - ir * k) < 0)
            seed += m;
        return (1 - (am * seed)) * 4;
    }

    public static int GetRandomNumber(int max)
    {
        if(max <= 0)
        {
            Debug.Log("Error! value is negative!");
            return -1;
        }
        double temp = GetRandomNumber();
        temp *= max;
        return System.Convert.ToInt32(temp);
    }

    public static int GetRandomNumber(int min, int max)
    {
        if (max - min <= 0)
        {
            Debug.Log("Error! min bigger than max!");
            return -1;
        }
        double temp = GetRandomNumber();
        temp = min + (temp*(max - min));
        return System.Convert.ToInt32(temp);
    }
}
