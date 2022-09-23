namespace SurrealDB.Json.Time; 

public static class TimeExtensions {
    /// <summary>
    /// Computes the fraction of a second part of the <see cref="TimeOnly"/>.
    /// </summary>
    public static long Fraction(in this TimeOnly value) {
        return value.Ticks % TimeSpan.TicksPerSecond;
    }

    /// <summary>
    /// Returns the ticks as a 7 digit string
    /// </summary>
    public static string FractionString(in this TimeOnly value) {
        return value.Fraction().ToString("D7");
    }
}
