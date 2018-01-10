using System;
using System.Collections.Generic;
using System.Linq;

public class GlobalRandom
{
    [ThreadStatic]
    private static Random random = null;

    [ThreadStatic]
    private static object syncLock = null;

    public static int GetRandomNumber(int min, int max) {
        if (syncLock == null || random == null) {
            init();
        }

        int num = 0;
        lock (syncLock) {
            num = random.Next(min, max);
        }
        return num;
    }

    private static void init() {
        if (syncLock == null) {
            syncLock = new object();
        }

        if (random == null) {
            random = new Random();
        }
    }
}
