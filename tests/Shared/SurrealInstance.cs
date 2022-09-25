namespace SurrealDB.Common;

public static class SurrealInstance {
    public static Process Create() {
        // Wait until port is closed
        TcpHelper.SpinUntilClosed($"0.0.0.0:{TestHelper.Port}");
        // Assume we have surreal as a command in PATH
        string? path = GetFullPath("surreal");
        Debug.Assert(path is not null);

        // Start new instances
        var p = Process.Start(path!, $"start memory -b 0.0.0.0:{TestHelper.Port} -u {TestHelper.User} -p {TestHelper.Pass} --log debug");
        Debug.Assert(p is not null);
        TcpHelper.SpinUntilListening($"0.0.0.0:{TestHelper.Port}");
        Debug.Assert(!p.HasExited);
        return p;
    }

    public static string? GetFullPath(string file)
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
            file += ".exe";
        }

        if (File.Exists(file)) {
            return Path.GetFullPath(file);
        }

        var values = Environment.GetEnvironmentVariable("PATH");
        if (values is null) {
            return null;
        }
        foreach (var path in values.Split(Path.PathSeparator))
        {
            var fullPath = Path.Combine(path, file);
            if (File.Exists(fullPath))
                return fullPath;
        }
        return null;
    }

}
