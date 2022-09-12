namespace Surreal.Net;

public static class Id
{
    public static string GetRandom(in int length)
    {
        Span<byte> buf = stackalloc byte[length];
        Random.Shared.NextBytes(buf);
        return Convert.ToHexString(buf);
    }
}