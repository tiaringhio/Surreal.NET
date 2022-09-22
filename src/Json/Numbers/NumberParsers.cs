using System.Globalization;

namespace SurrealDB.Json.Numbers;

internal static class SpecialNumbers {
    public static string NaN { get; } = NumberFormatInfo.InvariantInfo.NaNSymbol;
    public static string PosinfInv { get; } = NumberFormatInfo.InvariantInfo.PositiveInfinitySymbol;
    public static string NeginfInv { get; } = NumberFormatInfo.InvariantInfo.NegativeInfinitySymbol;
    public static string PosinfCur { get; } = NumberFormatInfo.CurrentInfo.PositiveInfinitySymbol;
    public static string NeginfCur { get; } = NumberFormatInfo.CurrentInfo.NegativeInfinitySymbol;
    public static string Posinf => "∞"; // &infin;
    public static string Neginf => "−∞"; // &minus;&infin;

    private static bool IsNegInf(string special) {
        return special.Equals(Neginf, StringComparison.OrdinalIgnoreCase) || special.Equals(NeginfCur, StringComparison.OrdinalIgnoreCase) || special.Equals(NeginfInv, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPosInf(string special) {
        return special.Equals(Posinf, StringComparison.OrdinalIgnoreCase) || special.Equals(PosinfCur, StringComparison.OrdinalIgnoreCase) || special.Equals(PosinfInv, StringComparison.OrdinalIgnoreCase);
    }

    public static float ToSingle(string special) {
        if (special.Equals(NaN, StringComparison.OrdinalIgnoreCase)) {
            return Single.NaN;
        }

        if (IsPosInf(special)) {
            return Single.PositiveInfinity;
        }

        if (IsNegInf(special)) {
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

        if (IsPosInf(special)) {
            return Double.PositiveInfinity;
        }

        if (IsNegInf(special)) {
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