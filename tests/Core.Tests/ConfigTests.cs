namespace Surreal.Net.Tests;

public class ConfigTests
{
    [Fact]
    public void Build_with_endpoint()
    {
        SurrealConfig cfg = SurrealConfig.Create().WithEndpoint($"{Helper.Loopback}:{Helper.Port}").Build();

        Helper.ValidateEndpoint(cfg.Endpoint);
    }

    [Fact]
    public void Build_with_address_and_port()
    {
        SurrealConfig cfg = SurrealConfig.Create().WithAddress(Helper.Loopback).WithPort(Helper.Port).Build();

        Helper.ValidateEndpoint(cfg.Endpoint);
    }
    
    [Fact]
    public void Build_last_option_should_overwrite_prior()
    {
        SurrealConfig cfg = SurrealConfig.Create()
            .WithEndpoint($"0.0.0.0:{Helper.Port}")
            .WithAddress(Helper.Loopback)
            .Build();

        Helper.ValidateEndpoint(cfg.Endpoint);
    }
}
