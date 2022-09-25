namespace SurrealDB.Common;

internal static class TimeHelper {
    /// <summary>
    /// Converts the <see cref="DateTime"/> to a <see cref="DateTimeOffset"/> by adding a offset.
    /// </summary>
    /// <remarks>
    /// This method ignores the <see cref="DateTimeKind"/> property and assumes UTC time by setting <see cref="DateTimeKind.Unspecified"/>!
    /// </remarks>
    public static DateTimeOffset WithOffset(in this DateTime dt, in TimeSpan offset) {
        if (dt.Kind != DateTimeKind.Unspecified) {
            return new(new DateTime(dt.Ticks), offset);
        }
        return new(dt, offset);
    }
}
