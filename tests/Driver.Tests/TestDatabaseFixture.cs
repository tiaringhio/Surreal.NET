namespace SurrealDB.Common;

[CollectionDefinition("SurrealDBRequired")]
public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

/// <summary>
/// Sets up the Database proccess for all tests that are in the 'SurrealDBRequired' collection
///
/// To use:
/// - Add `[Collection("SurrealDBRequired")]` to the test class
/// - Add `TestDatabaseFixture? fixture;` field to the test class
/// </summary>
public class TestDatabaseFixture : IDisposable
{
    private static readonly object _lock = new();
    private static Process? _databaseProcess = null;
    private static bool _databaseInitialized = false;

    public TestDatabaseFixture() {
        lock (_lock)
        {
            if (!_databaseInitialized)
            {
                EnsureDB();
                _databaseInitialized = true;
            }
        }
    }

    public static void EnsureDB() {
        // Assume we have surreal as a command in PATH
        string? path = GetFullPath("surreal");
        Debug.Assert(path is not null);

        // Start new instances
        _databaseProcess = Process.Start(path!, $"start memory -b 0.0.0.0:{TestHelper.Port} -u {TestHelper.User} -p {TestHelper.Pass} --log debug");
        Debug.Assert(_databaseProcess is not null);
        Thread.Sleep(150); // wait for surrealdb to start
        Debug.Assert(!_databaseProcess.HasExited);
    }

    public static void KillDB() {
        if (_databaseProcess != null) {
            _databaseProcess.Kill();
            _databaseProcess = null;
        }
    }

    public void Dispose() {
        KillDB();
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
