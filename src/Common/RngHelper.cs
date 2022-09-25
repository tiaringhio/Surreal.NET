namespace SurrealDB.Common;

public static class RngHelper {
    #if NET6_0_OR_GREATER
    public static Random Shared => ThreadRng.Shared;
    #else
    [ThreadStatic]
    private static Random? s_random;
    public static Random Shared => s_random ??= new();

    public static long NextInt64(this Random rng) {
        Span<byte> buf = stackalloc byte[8];
        rng.NextBytes(buf);
        return BitConverter.ToInt64(buf);
    }

    public static long NextInt64(this Random rng, long min, long max) {
        return rng.NextInt64() % (max - min) + min;
    }

    public static float NextSingle(this Random rng) {
        return (float)rng.NextDouble();
    }
#endif
}
