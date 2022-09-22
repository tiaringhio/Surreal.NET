namespace SurrealDB.Json;

public static class TimeZoneHelper {
    private static Dictionary<TimeSpan, TimeZoneInfo>? s_cacheTz;
    private static readonly object s_cacheTzLock = new();

    private static IReadOnlyDictionary<TimeSpan, TimeZoneInfo> GetTimeZones() {
        Dictionary<TimeSpan, TimeZoneInfo> cache;
        lock (s_cacheTzLock) {
            cache = EnsureTzCacheUnchecked();
        }

        return cache;
    }

    private static Dictionary<TimeSpan, TimeZoneInfo> EnsureTzCacheUnchecked() {
        Dictionary<TimeSpan, TimeZoneInfo>? cache = s_cacheTz;
        if (cache is null || cache.Count <= 0) {
            cache = TimeZoneInfo.GetSystemTimeZones().ToDictionary(static tz => tz.BaseUtcOffset);
            s_cacheTz = cache;
        }

        return cache;
    }

    private static TimeZoneInfo AddTimeZone(string name, in TimeSpan off) {
        lock (s_cacheTzLock) {
            Dictionary<TimeSpan, TimeZoneInfo> cache = EnsureTzCacheUnchecked();
            if (cache.TryGetValue(off, out TimeZoneInfo? tz)) {
                return tz;
            }
            tz = TimeZoneInfo.CreateCustomTimeZone(name, off, null, null, null, null, false);
            cache[off] = tz;
            return tz;
        }
    }

    public static TimeZoneInfo FromOffset(in TimeSpan offset) {
        return GetTimeZones().TryGetValue(offset, out TimeZoneInfo? tz) ? tz : AddTimeZone(offset.ToString(), in offset);
    }

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
