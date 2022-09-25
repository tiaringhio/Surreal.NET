using SurrealDB.Shared.Tests;

namespace SurrealDB.Core.Tests;

public class ConfigTests {
    [Fact]
    public void Build_with_endpoint() {
        Config cfg = Config.Create().WithEndpoint($"{TestHelper.Loopback}:{TestHelper.Port}").Build();

        TestHelper.ValidateEndpoint(cfg.Endpoint);
    }

    [Fact]
    public void Build_with_address_and_port() {
        Config cfg = Config.Create().WithAddress(TestHelper.Loopback).WithPort(TestHelper.Port).Build();

        TestHelper.ValidateEndpoint(cfg.Endpoint);
    }

    [Fact]
    public void Build_last_option_should_overwrite_prior() {
        Config cfg = Config.Create()
           .WithEndpoint($"0.0.0.0:{TestHelper.Port}")
           .WithAddress(TestHelper.Loopback)
           .Build();

        TestHelper.ValidateEndpoint(cfg.Endpoint);
    }
}
