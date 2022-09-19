using SurrealDB.Config;

namespace SurrealDB.Driver.Tests;

public static class ConfigHelper {
    public const string Loopback = "127.0.0.1";
    public const int Port = 8082;
    public const string User = "root";
    public const string Pass = "root";
    public const string Database = "test";
    public const string Namespace = "test";

    public static Config.Config Default => Config.Config.Create()
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
}
