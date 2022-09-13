

namespace Surreal.Net.Tests;

public class RpcClientTests
{
    public readonly RpcClient Client = new();
    
    [Fact]
    public async Task Open()
    {
        await Client.Open(ConfigHelper.Default.RpcUrl!);
    }
    
    [Fact]
    public async Task Signin()
    {
        await Open();
        var rsp = await Client.Send(new()
        {
            Method = "signin",
            Params = new()
            {
                new{ user = ConfigHelper.Default.Username, pass = ConfigHelper.Default.Password }
            }
        });
        rsp.Error.Should().BeNull();
        rsp.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task Use()
    {
        await Signin();
        var rsp = await Client.Send(new()
        {
            Method = "use",
            Params = new()
            {
                ConfigHelper.Default.Database!,
                ConfigHelper.Default.Username!
            }
        });
        rsp.Error.Should().BeNull();
        rsp.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task Create()
    {
        await Use();

        var person = new
        {
            title = "Founder & CEO",
            name = new
            {
                first = "Tobie",
                last = "Morgan Hitchcock",
            },
            marketing = true,
            identifier = Random.Shared.Next(),
        };
        
        var rsp = await Client.Send(new()
        {
            Method = "create",
            Params = new()
            {
                "person",
                person
            }
        });
        
        rsp.Error.Should().BeNull();
        rsp.Result.Should().NotBeNull();
    }
}