using System.Text;

namespace SurrealDB.Driver.Tests;
public static class TestHelper {
    
    public const string Loopback = "127.0.0.1";
    public const int Port = 8082;
    public const string User = "root";
    public const string Pass = "root";
    public const string Database = "test";
    public const string Namespace = "test";

    public static Config Default => Config.Create()
       .WithAddress(Loopback)
       .WithPort(Port)
       .WithNamespace(Namespace)
       .WithDatabase(Database)
       .WithRpc(true)
       .WithRest(true)
       .WithBasicAuth(User, Pass)
       .Build();

    public static void ValidateEndpoint(IPEndPoint? endpoint) {
        endpoint.Should().NotBeNull();
        endpoint!.Address.ToString().Should().Be(Loopback);
        endpoint.Port.Should().Be(Port);
    }

    public static void AssertOk(
        in IResponse rpcResponse,
        [CallerArgumentExpression("rpcResponse")]
        string caller = "") {
        if (!rpcResponse.TryGetError(out SurrealError err)) {
            return;
        }

        Exception ex = new($"Expected Ok, got error code {err.Code} ({err.Message}) in {caller}");
        throw ex;
    }

    public static void EnsureDB() {
        return;
        string? path = GetFullPath("surreal");
        // Assume we have surreal as a command in PATH
        Debug.Assert(path is not null);
        // Kill running surrealdb instances
        Process.Start(new ProcessStartInfo("killall", "surreal"))!.WaitForExit();
        // Start new instances
        Process.Start(new ProcessStartInfo(path!, $"start -b 0.0.0.0:{Port} -u {User} -p {Pass} --log debug"));
        Thread.Sleep(150); // wait for surrealdb to start
    }

    public static string? GetFullPath(string file)
    {
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
