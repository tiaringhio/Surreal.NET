namespace SurrealDB.Json.Numbers;

internal static class SpecialNumbers {
    public const string NUM_NAN = "nan";
    public const string NUM_POSINF = "inf";
    public const string NUM_NEGINF = "-inf";
    public const string NUM_POSINF_ALT = "∞";
    public const string NUM_NEGINF_ALT = "-∞";

    public static float ToSingle(string special) {
        return special switch {
            NUM_NAN => Single.NaN,
            NUM_POSINF => Single.PositiveInfinity,
            NUM_NEGINF => Single.NegativeInfinity,
            NUM_POSINF_ALT => Single.PositiveInfinity,
            NUM_NEGINF_ALT => Single.NegativeInfinity,
            _ => default,
        };
    }

    public static string? ToSpecial(in float value) {
        if (Single.IsNaN(value)) {
            return NUM_NAN;
        }

        if (Single.IsPositiveInfinity(value)) {
            return NUM_POSINF;
        }

        if (Single.IsNegativeInfinity(value)) {
            return NUM_NEGINF;
        }

        return null;
    }


    public static double ToDouble(string special) {
        return special switch {
            NUM_NAN => Double.NaN,
            NUM_POSINF => Double.PositiveInfinity,
            NUM_NEGINF => Double.NegativeInfinity,
            NUM_POSINF_ALT => Double.PositiveInfinity,
            NUM_NEGINF_ALT => Double.NegativeInfinity,
            _ => default,
        };
    }

    public static string? ToSpecial(in double value) {
        if (Double.IsNaN(value)) {
            return NUM_NAN;
        }

        if (Double.IsPositiveInfinity(value)) {
            return NUM_POSINF;
        }

        if (Double.IsNegativeInfinity(value)) {
            return NUM_NEGINF;
        }

        return null;
    }

    public static decimal ToDecimal(string str) {

        return default;
    }
    public static string? ToSpecial(in decimal value) {

        return null;
    }

}