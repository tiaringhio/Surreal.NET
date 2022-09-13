namespace Surreal.Net.Tests;

public static class ConfigHelper
{
    public const string Loopback = "127.0.0.1";
    public const int Port = 8082;
    public const string User = "root";
    public const string Pass = "root";
    public const string Database = "test";
    public const string Namespace = "test";

    public static void ValidateEndpoint(IPEndPoint? endpoint)
    {
        endpoint.Should().NotBeNull();
        endpoint!.Address.ToString().Should().Be(Loopback);
        endpoint.Port.Should().Be(Port);
    }

    public static SurrealConfig Default => SurrealConfig.Create()
        .WithAddress(Loopback)
        .WithPort(Port)
        .WithNamespace(Namespace)
        .WithDatabase(Database)
        .WithBasicAuth(User, Pass)
        .Build();
}