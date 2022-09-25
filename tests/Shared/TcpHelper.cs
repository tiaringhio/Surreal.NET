using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SurrealDB.Shared.Tests;

public static class TcpHelper {
    public static void SpinUntilListening(string remote, CancellationToken ct = default) {
        SpinUntilListening(IPEndPoint.Parse(remote), ct);
    }

    public static void SpinUntilListening(IPEndPoint remote, CancellationToken ct = default) {
        SpinWait s = new();
        while (!IsListening(remote) && !ct.IsCancellationRequested) {
            s.SpinOnce();
            ct.ThrowIfCancellationRequested();
        }
        // Now we have a listener, attempt to connect until we do now get connection refused
        bool open = false;
        while (!open && !ct.IsCancellationRequested) {
            try {
                using TcpClient c = new();
                c.Connect(remote);
                open = true;
            } catch (HttpRequestException ex) {
                // spin until we can connect
                open = ex.Message != "Connection refused";
            }
            ct.ThrowIfCancellationRequested();
        }
    }

    public static void SpinUntilClosed(string remote, CancellationToken ct = default) {
        SpinUntilClosed(IPEndPoint.Parse(remote), ct);
    }

    public static void SpinUntilClosed(IPEndPoint remote, CancellationToken ct = default) {
        SpinWait s = new();
        while (IsListening(remote) && !ct.IsCancellationRequested) {
            s.SpinOnce();
            ct.ThrowIfCancellationRequested();
        }
    }

    public static bool IsListening(IPEndPoint remote)
    {
        var properties = IPGlobalProperties.GetIPGlobalProperties();

        IPEndPoint[] tcpListeners = properties.GetActiveTcpListeners();
        return tcpListeners.Contains(remote);
    }
}
