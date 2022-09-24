namespace SurrealDB.Driver.Tests;

[Collection("SurrealDBRequired")]
public class WsClientTests
{

    TestDatabaseFixture? fixture;

    public WsClientTests() {
    }
    
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
            method = "signin",
            parameters = new()
            {
                new{ user = TestHelper.Default.Username, pass = TestHelper.Default.Password }
            }
        });
        
        (rsp.error == default).Should().BeTrue();
        rsp.result.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    [Fact]
    public async Task Use()
    {
        await Signin();
        var rsp = await Client.Send(new()
        {
            method = "use",
            parameters = new()
            {
                TestHelper.Default.Database!,
                TestHelper.Default.Username!
            }
        });
        
        (rsp.error == default).Should().BeTrue();
        rsp.result.ValueKind.Should().NotBe(JsonValueKind.Undefined);
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
            method = "create",
            parameters = new()
            {
                "person",
                person
            }
        });

        (rsp.error == default).Should().BeTrue();
        rsp.result.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    [Fact]
    public async Task Change()
    {
        await Create();
        var rsp = await Client.Send(new()
        {
            method = "change",
            parameters = new()
            {
                "person:jamie",
                new
                {
                    Marketing = true,
                }
            }
        });
        (rsp.error == default).Should().BeTrue();
    }

    [Fact]
    public async Task Select()
    {
        await Change();
        var rsp = await Client.Send(new()
        {
            method = "select",
            parameters = new()
            {
                "person"
            }
        });
        (rsp.error == default).Should().BeTrue();
    }

    [Fact]
    public async Task Query()
    {
        await Select();
        var rsp = await Client.Send(new()
        {
            method = "query",
            parameters = new()
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
