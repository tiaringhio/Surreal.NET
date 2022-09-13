namespace Surreal.Net.Tests;

public class ConfigTests
{
    [Fact]
    public void Build_with_endpoint()
    {
        SurrealConfig cfg = SurrealConfig.Create().WithEndpoint($"{ConfigHelper.Loopback}:{ConfigHelper.Port}").Build();

        ConfigHelper.ValidateEndpoint(cfg.Endpoint);
    }

    [Fact]
    public void Build_with_address_and_port()
    {
        SurrealConfig cfg = SurrealConfig.Create().WithAddress(ConfigHelper.Loopback).WithPort(ConfigHelper.Port).Build();

        ConfigHelper.ValidateEndpoint(cfg.Endpoint);
    }
    
    [Fact]
    public void Build_last_option_should_overwrite_prior()
    {
        SurrealConfig cfg = SurrealConfig.Create()
            .WithEndpoint($"0.0.0.0:{ConfigHelper.Port}")
            .WithAddress(ConfigHelper.Loopback)
            .Build();

        ConfigHelper.ValidateEndpoint(cfg.Endpoint);
    }
}
