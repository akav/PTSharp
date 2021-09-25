using System;

namespace PTSharpCore
{
    ///
    /// ThreadSafeRandom.cs - Thread safe random number generation
    /// Code from https://devblogs.microsoft.com/pfxteam/getting-random-numbers-in-a-thread-safe-way/ article by Stephen Toub
    /// 

    class ThreadSafeRandom
    {
        private static Random _global = new Random();
        [ThreadStatic]
        private static Random _local;

        public static int Next()
        {
            Random inst = _local;
            if (inst is null)
            {
                int seed;
                lock (_global) seed = _global.Next();
                _local = inst = new Random(seed);
            }
            return inst.Next();
        }

        public static int Next(int i)
        {
            Random inst = _local;
            if (inst is null)
            {
                int seed;
                lock (_global) seed = _global.Next();
                _local = inst = new Random(seed);
            }
            return inst.Next(i);
        }

        public static double NextDouble()
        {
            Random inst = _local;
            if (inst is null)
            {
                int seed;
                lock (_global) seed = _global.Next();
                _local = inst = new Random(seed);
            }
            return inst.NextDouble();
        }

        public static double NextDouble(Random rand)
        {
            Random inst = rand;
            if (inst is null)
            {
                int seed;
                lock (_global) seed = _global.Next();
                _local = inst = new Random(seed);
            }
            return inst.NextDouble();
        }
    }
}
