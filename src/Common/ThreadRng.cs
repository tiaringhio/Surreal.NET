using System.Runtime.CompilerServices;

namespace SurrealDB.Common;

internal static class ThreadRng {
#if NET6_0_OR_GREATER
    public static Random Shared => Random.Shared;
#else
    [ThreadStatic]
    private static Random? t_shared;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Random Create() => t_shared = new();

    public static Random Shared => t_shared ?? Create();

    public static long NextInt64(this Random rng, long min, long max) {
        return rng.NextInt64() % (max - min) + min;
    }

    public static long NextInt64(this Random rng) {
        unsafe {
            Span<byte> buf = stackalloc byte[sizeof(long)];
            rng.NextBytes(buf);
            return BitConverter.ToInt64(buf);
        }
    }

    public static float NextSingle(this Random rng) {
        return (float)rng.NextDouble();
    }
#endif
}
