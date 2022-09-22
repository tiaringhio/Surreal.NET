using System.Globalization;

using Superpower;
using Superpower.Parsers;

namespace SurrealDB.Json; 

public static class TimeParsers {
    private static TextParser<char> Dot { get; } = Character.EqualTo('.');
    private static TextParser<char> Dash { get; } = Character.EqualTo('-');
    private static TextParser<char> Colon { get; } = Character.EqualTo(':');
    private static TextParser<char> TimeSeparator { get; } = Character.In('t', 'T', ' ');
    private static TextParser<int> IntDigits { get; } =
        Character.Digit.AtLeastOnce().Select(static c => Int32.Parse(c));

    public static TextParser<double> Float { get; } =
        from sign in Character.EqualTo('-').Value(-1.0).OptionalOrDefault(1.0)
        from whole in Numerics.Natural.Select(n => double.Parse(n.ToStringValue()))
        from frac in Character.EqualTo('.')
           .IgnoreThen(Numerics.Natural)
           .Select(n => double.Parse(n.ToStringValue()) * Math.Pow(10, -n.Length))
           .OptionalOrDefault()
        from exp in Character.EqualToIgnoreCase('e')
           .IgnoreThen(Character.EqualTo('+').Value(1.0)
               .Or(Character.EqualTo('-').Value(-1.0))
               .OptionalOrDefault(1.0))
           .Then(expsign => Numerics.Natural.Select(n => double.Parse(n.ToStringValue()) * expsign))
           .OptionalOrDefault()
        select (whole + frac) * sign * Math.Pow(10, exp);

    /// <summary>
    /// Parses any ISO8601 like <see cref="DateOnly"/> `{year}-{month}-{day}`
    /// </summary>
    public static TextParser<DateOnly> IsoDate { get; } =
        from year in IntDigits
        from month in Dash.IgnoreThen(IntDigits)
        from day in Dash.IgnoreThen(IntDigits)
        select new DateOnly(year, month, day);

    /// <summary>
    /// Parses any ISo8601 like time fraction into ticks.
    /// A tick is 10us, that is a 7 digit fraction.
    /// The fraction is multiplied by 7 then divided by the number of digits.
    /// </summary>
    public static TextParser<long> IsoTimeFraction { get; } =
        Character.Digit.AtLeastOnce().Select(static c => (long)(Double.Parse(c, NumberStyles.None) * 7d / c.Length));
    
    /// <summary>
    /// Parses any ISO8601 like <see cref="TimeOnly"/> `{hour}:{minute}:{second}.{fraction}`
    /// </summary>
    public static TextParser<TimeOnly> IsoTime { get; } =
        from hour in IntDigits
        from minute in Colon.IgnoreThen(IntDigits)
        from second in Colon
           .IgnoreThen(IntDigits)
           .OptionalOrDefault()
        from fraction in Dot.IgnoreThen(IsoTimeFraction)
        select new TimeOnly(new TimeOnly(hour, minute, second).Ticks + fraction);

    /// <summary>
    /// Parses the ISO8601 UTC timezone `Z`
    /// </summary>
    private static TextParser<TimeSpan> IsoTimezoneUtc => 
        Character.EqualToIgnoreCase('Z').Value(TimeSpan.Zero);

    /// <summary>
    /// Parses any ISO8601 offset `[+-]\d+(:\d+)?`
    /// </summary>
    private static TextParser<TimeSpan> IsoTimezone { get; } =
        from sign in Character.In('+', '-').Optional()
        from hours in IntDigits
        from minutes in Colon.IgnoreThen(IntDigits).OptionalOrDefault()
        select new TimeSpan(hours, minutes, 0) * (sign.GetValueOrDefault('+') == '+' ? 1 : -1);

    private static TextParser<DateTime> IsoDateTimeUtc { get; } =
        from date in IsoDate
        from time in TimeSeparator.OptionalOrDefault()
           .IgnoreThen(IsoTime)
           .OptionalOrDefault()
        select date.ToDateTime(time, DateTimeKind.Unspecified);

    /// <summary>
    /// Parses any ISO8601 <see cref="DateTimeOffset"/> `{year}-{month}-{day}T{hour}:{minute}:{second}{offset}`, 
    /// where `{offset}` is `(Z)|([+-]\d+(\:?\d+)?)`
    /// </summary>
    public static TextParser<DateTimeOffset> IsoDateTimeOffset { get; } =
        from dt in IsoDateTimeUtc
        from tz in IsoTimezoneUtc.Or(IsoTimezone)
        select dt.WithOffset(tz);

    private static TextParser<TimeSpan> SpecificTimeSegment { get; } =
        from value in Float
        from unit in Character.EqualToIgnoreCase('d').Value((double)TimeSpan.TicksPerDay)
           .Or(Span.EqualToIgnoreCase('d').Value((double)TimeSpan.TicksPerDay))
           .Or(Span.EqualToIgnoreCase("h").Value((double)TimeSpan.TicksPerHour))
           .Or(Span.EqualToIgnoreCase("m").Value((double)TimeSpan.TicksPerMinute))
           .Or(Span.EqualToIgnoreCase("s").Value((double)TimeSpan.TicksPerSecond))
           .Or(Span.EqualToIgnoreCase("ms").Value((double)TimeSpan.TicksPerMillisecond))
           .Or(Span.EqualToIgnoreCase("us").Or(Span.EqualToIgnoreCase("µs")).Value(10d)) // yea we 10 us ftw
           .Or(Span.EqualToIgnoreCase("ns").Value(10d / 1000d))
           .Or(Span.EqualToIgnoreCase("ticks").OptionalOrDefault().Value(1d))
        select new TimeSpan(unchecked((long)(value * unit)));

    /// <summary>
    /// Parses a time span from a set of specific time unit tuples, e.g. `1h`, `59m`, `19s`, `1ms`
    /// Valid units are: day := `d`, hour := `h`, minute := `m`, second := `s`,
    /// milliseconds := `ms`, microseconds := `us` |`µs`, nanoseconds := `ns`,
    /// ticks := `ticks` | &lt;empty&gt;
    /// </summary>
    public static TextParser<TimeSpan> SpecificTimeSpan { get; } =
        from unit in SpecificTimeSegment.AtLeastOnce()
        select unit.Aggregate(static (lhs, rhs) => lhs + rhs);
}
