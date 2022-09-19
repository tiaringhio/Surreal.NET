namespace SurrealDB.Json;

internal static class SpecialNumbers {
    public const string NUM_NAN = "nan";
    public const string NUM_POSINF = "inf";
    public const string NUM_NEGINF = "-inf";
    public const string NUM_POSEPS = "eps";
    public const string NUM_NEGEPS = "-eps";
    public const string NUM_MAX = "max";
    public const string NUM_MIN = "min";

    public static float ToSingle(string special) {
        return special switch {
            NUM_NAN => Single.NaN,
            NUM_POSINF => Single.PositiveInfinity,
            NUM_NEGINF => Single.NegativeInfinity,
            NUM_POSEPS => Single.Epsilon,
            NUM_NEGEPS => -Single.Epsilon,
            NUM_MAX => Single.MaxValue,
            NUM_MIN => Single.MinValue,
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

        if (Math.Abs(value) <= Single.Epsilon) {
            return value > 0 ? NUM_POSEPS : NUM_NEGEPS;
        }

        if (value >= Single.MaxValue) {
            return NUM_MAX;
        }

        if (value <= Single.MinValue) {
            return NUM_MIN;
        }

        return null;
    }


    public static double ToDouble(string special) {
        return special switch {
            NUM_NAN => Double.NaN,
            NUM_POSINF => Double.PositiveInfinity,
            NUM_NEGINF => Double.NegativeInfinity,
            NUM_POSEPS => Double.Epsilon,
            NUM_NEGEPS => -Double.Epsilon,
            NUM_MAX => Double.MaxValue,
            NUM_MIN => Double.MinValue,
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

        if (Math.Abs(value) <= Double.Epsilon) {
            return value > 0 ? NUM_POSEPS : NUM_NEGEPS;
        }

        if (value >= Double.MaxValue) {
            return NUM_MAX;
        }

        if (value <= Double.MinValue) {
            return NUM_MIN;
        }

        return null;
    }

    public static decimal ToDecimal(string str) {
        if (str == NUM_MAX) {
            return Decimal.MaxValue;
        }

        if (str == NUM_MIN) {
            return Decimal.MinValue;
        }

        return default;
    }
    public static string? ToSpecial(in decimal value) {
        if (value >= Decimal.MaxValue) {
            return NUM_MAX;
        }

        if (value <= Decimal.MinValue) {
            return NUM_MIN;
        }

        return null;
    }

}