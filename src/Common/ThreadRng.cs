namespace SurrealDB.Common;

public class ThreadRng {
    [ThreadStatic]
    private static Random? s_shared;

    public static Random Shared => s_shared ?? new();
}
