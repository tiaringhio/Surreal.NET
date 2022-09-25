namespace SurrealDB.Common;

public class ThreadRng {
#if NET6_0_OR_GREATER
    public static Random Shared => Random.Shared;
#else
    [ThreadStatic]
    private static Random? s_shared;

    public static Random Shared => s_shared ?? new();
#endif
}
