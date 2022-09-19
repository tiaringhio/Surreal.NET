using SurrealDB.Config;

namespace SurrealDB.Driver.Tests;

public class ConfigTests {
    [Fact]
    public void Build_with_endpoint() {
        Config.Config cfg = Config.Config.Create().WithEndpoint($"{ConfigHelper.Loopback}:{ConfigHelper.Port}").Build();

        ConfigHelper.ValidateEndpoint(cfg.Endpoint);
    }

    [Fact]
    public void Build_with_address_and_port() {
        Config.Config cfg = Config.Config.Create().WithAddress(ConfigHelper.Loopback).WithPort(ConfigHelper.Port).Build();

        ConfigHelper.ValidateEndpoint(cfg.Endpoint);
    }

    [Fact]
    public void Build_last_option_should_overwrite_prior() {
        Config.Config cfg = Config.Config.Create()
           .WithEndpoint($"0.0.0.0:{ConfigHelper.Port}")
           .WithAddress(ConfigHelper.Loopback)
           .Build();

        ConfigHelper.ValidateEndpoint(cfg.Endpoint);
    }
}
