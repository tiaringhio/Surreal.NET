using System.Text.Json.Serialization;

namespace Surreal.Net.Tests;

public class RpcClientTests
{
    [Fact]
    public async Task Open()
    {
        RpcClient client = new();
        await client.Open(ConfigHelper.Default.RpcUrl!);
    }
    
    [Fact]
    public async Task Signin()
    {
        RpcClient client = new();
        await client.Open(ConfigHelper.Default.RpcUrl!);
        var rsp = await client.Send<SigninReq, string>(new()
        {
            Method = "signin",
            Params = new()
            {
                new(ConfigHelper.Default.Username, ConfigHelper.Default.Password)
            }
        });
    }

    record SigninReq(string? user, string? pass);
}