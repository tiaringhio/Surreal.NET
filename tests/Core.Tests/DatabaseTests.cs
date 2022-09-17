using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Surreal.Net.Database;

namespace Surreal.Net.Tests;

public class DatabaseTests
{
    [Fact]
    public async Task RpcTestSuite()
    {
        await new DatabaseTestDriver<DbRpc, SurrealRpcResponse>().Run();
    }

    [Fact]
    public async Task RestTestSuite()
    {
        await new DatabaseTestDriver<DbRest, SurrealRestResponse>().Run();
    }
}

/// <summary>
/// The test driver executes the testsuite on the client.
/// </summary>
public sealed class DatabaseTestDriver<T, U>
    where T : ISurrealDatabase<U>, new()
    where U : ISurrealResponse
{
    private T _database;

    public DatabaseTestDriver()
    {
        _database = new();
    }

    public async Task Run()
    {
        await _database.Open(ConfigHelper.Default);
        _database.GetConfig().Should().BeEquivalentTo(ConfigHelper.Default);

        var useResp = await _database.Use(ConfigHelper.Database, ConfigHelper.Namespace);
        AssertOk(useResp);
        var infoResp = await _database.Info();
        AssertOk(infoResp);

        var signInStatus = await _database.Signin(new()
        {
            Username = ConfigHelper.User,
            Password = ConfigHelper.Pass,
        });

        AssertOk(signInStatus);
        //AssertOk(await _database.Invalidate());

        var (id1, id2) = ("", "");
        ISurrealResponse res1 = await _database.Create("person", new
        {
            Title = "Founder & CEO",
            Name = new
            {
                First = "Tobie",
                Last = "Morgan Hitchcock",
            },
            Marketing = true,
            Identifier = Random.Shared.Next(),
        });

        AssertOk(res1);

        ISurrealResponse res2 = await _database.Create("person", new
        {
            Title = "Contributor",
            Name = new
            {
                First = "Prophet",
                Last = "Lamb",
            },
            Marketing = false,
            Identifier = Random.Shared.Next(),
        });
        AssertOk(res2);

        var thing2 = SurrealThing.From("person", id2);
        AssertOk(await _database.Update(thing2, new
        {
            Marketing = false,
        }));

        AssertOk(await _database.Select(thing2));

        AssertOk(await _database.Delete(thing2));

        var thing1 = SurrealThing.From("person", id1);
        AssertOk(await _database.Change(thing1, new
        {
            Title = "Founder & CEO",
            Name = new
            {
                First = "Tobie",
                Last = "Hitchcock Morgan",
            },
            Marketing = false,
            Identifier = Random.Shared.Next(),
        }));

        /*
         * Modify doesn't work but the Go lib doesn't update it so I don't
         * have a reference for how to use Modify
         *
        string newTitle = "Founder & CEO & Ruler of the known free World";
        var modifyResp = await _database.Modify(thing1, new
        {
            op = "replace", path = "/Title", value = newTitle
        });
        */

        AssertOk(await _database.Let("tbl", "person"));

        string newTitle = "Founder & CEO & Ruler of the known free World";
        var queryResp = await _database.Query("SELECT $props FROM $tbl WHERE title = $title", new Dictionary<string, object?>
        {
            ["props"] = "title, identifier",
            ["title"] = newTitle,
        });

        await _database.Close();
    }

    [DebuggerStepThrough]
    private static void AssertOk(in ISurrealResponse rpcResponse, [CallerArgumentExpression("rpcResponse")] string caller = "")
    {
        if (rpcResponse.TryGetError(out var err))
        {
            throw new($"Expected Ok, got {err.Code} ({err.Message}) in {caller}");
        }
    }
}