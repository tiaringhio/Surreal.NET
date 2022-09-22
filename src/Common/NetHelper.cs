using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;

namespace SurrealDB.Common; 

public class NetHelper {
    /// <summary>
    /// Specifies the maximum acceptable value for the <see cref='System.Net.IPEndPoint.Port'/> property.
    /// </summary>
    public const int MaxPort = 0x0000FFFF;
    
    public static bool TryParseEndpoint(ReadOnlySpan<char> s, [NotNullWhen(true)] out IPEndPoint? result)
    {
#if NET6_0_OR_GREATER
        return IPEndPoint.TryParse(s, out result);
#else
        int addressLength = s.Length;  // If there's no port then send the entire string to the address parser
        int lastColonPos = s.LastIndexOf(':');

        // Look to see if this is an IPv6 address with a port.
        if (lastColonPos > 0)
        {
            if (s[lastColonPos - 1] == ']')
            {
                addressLength = lastColonPos;
            }
            // Look to see if this is IPv4 with a port (IPv6 will have another colon)
            else if (s.Slice(0, lastColonPos).LastIndexOf(':') == -1)
            {
                addressLength = lastColonPos;
            }
        }

        if (IPAddress.TryParse(s.Slice(0, addressLength), out IPAddress? address))
        {
            uint port = 0;
            if (addressLength == s.Length ||
                (uint.TryParse(s.Slice(addressLength + 1), NumberStyles.None, CultureInfo.InvariantCulture, out port) && port <= MaxPort))

            {
                result = new IPEndPoint(address, (int)port);
                return true;
            }
        }

        result = null;
        return false;
#endif
    }
    
    public static IPEndPoint ParseEndpoint(ReadOnlySpan<char> s)
    {
#if NET6_0_OR_GREATER
        return IPEndPoint.Parse(s);
#else
        if (TryParseEndpoint(s, out IPEndPoint? result))
        {
            return result;
        }

        throw new FormatException("String cannot be parsed as an ip-endpoint.");
#endif
    }

}
