using System.Globalization;

namespace SurrealDB.Json.Numbers;

internal static class SpecialNumbers {
    public static string NaN { get; } = NumberFormatInfo.InvariantInfo.NaNSymbol;
    public static string PosinfAlt { get; } = NumberFormatInfo.InvariantInfo.PositiveInfinitySymbol;
    public static string NeginfAlt { get; } = NumberFormatInfo.InvariantInfo.NegativeInfinitySymbol;
    public static string Posinf => "∞"; // &infin;
    public static string Neginf => "−∞"; // &minus;&infin;

    public static float ToSingle(string special) {
        if (special.Equals(NaN, StringComparison.OrdinalIgnoreCase)) {
            return Single.NaN;
        }

        if (special.Equals(Posinf, StringComparison.OrdinalIgnoreCase) || special.Equals(PosinfAlt, StringComparison.OrdinalIgnoreCase)) {
            return Single.PositiveInfinity;
        }

        if (special.Equals(Neginf, StringComparison.OrdinalIgnoreCase) || special.Equals(NeginfAlt, StringComparison.OrdinalIgnoreCase)) {
            return Single.NegativeInfinity;
        }

        return default;
    }

    public static string? ToSpecial(in float value) {
        if (Single.IsNaN(value)) {
            return NaN;
        }

        if (Single.IsPositiveInfinity(value)) {
            return Posinf;
        }

        if (Single.IsNegativeInfinity(value)) {
            return Neginf;
        }

        return null;
    }


    public static double ToDouble(string special) {
        if (special.Equals(NaN, StringComparison.OrdinalIgnoreCase)) {
            return Double.NaN;
        }

        if (special.Equals(Posinf, StringComparison.OrdinalIgnoreCase) || special.Equals(PosinfAlt, StringComparison.OrdinalIgnoreCase)) {
            return Double.PositiveInfinity;
        }

        if (special.Equals(Neginf, StringComparison.OrdinalIgnoreCase) || special.Equals(NeginfAlt, StringComparison.OrdinalIgnoreCase)) {
            return Double.NegativeInfinity;
        }

        return default;
    }

    public static string? ToSpecial(in double value) {
        if (Double.IsNaN(value)) {
            return NaN;
        }

        if (Double.IsPositiveInfinity(value)) {
            return Posinf;
        }

        if (Double.IsNegativeInfinity(value)) {
            return Neginf;
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