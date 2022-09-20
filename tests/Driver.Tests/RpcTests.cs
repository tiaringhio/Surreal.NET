namespace SurrealDB.Driver.Tests.Ws;


public class WsClientTests
{
    public readonly WsClient Client = new();

    [Fact]
    public async Task Open()
    {
        await Client.Open(TestHelper.Default.RpcEndpoint!);
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
                new{ user = TestHelper.Default.Username, pass = TestHelper.Default.Password }
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
                TestHelper.Default.Database!,
                TestHelper.Default.Username!
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
            Title = "Founder & CEO",
            Name = new
            {
                First = "Tobie",
                Last = "Morgan Hitchcock",
            },
            Marketing = true,
            Identifier = Random.Shared.Next(),
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

    [Fact]
    public async Task Change()
    {
        await Create();
        var rsp = await Client.Send(new()
        {
            Method = "change",
            Params = new()
            {
                "person:jamie",
                new
                {
                    Marketing = true,
                }
            }
        });
        rsp.Error.Should().BeNull();
    }

    [Fact]
    public async Task Select()
    {
        await Change();
        var rsp = await Client.Send(new()
        {
            Method = "select",
            Params = new()
            {
                "person"
            }
        });
        rsp.Error.Should().BeNull();
    }

    [Fact]
    public async Task Query()
    {
        await Select();
        var rsp = await Client.Send(new()
        {
            Method = "query",
            Params = new()
            {
                "SELECT marketing, count() FROM type::table($tb) GROUP BY marketing",
                new
                {
                    tb = "person"
                }
            }
        });
    }
}
