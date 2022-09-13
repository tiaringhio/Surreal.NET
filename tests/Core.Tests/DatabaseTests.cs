using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Surreal.Net.Database;

namespace Surreal.Net.Tests;

public class DatabaseTests
{
    [Fact]
    public async Task DatabaseTestSuite()
    {
        await DatabaseTestDriver.Run<DbRpc>();
    }
}

/// <summary>
/// The test driver executes the testsuite on the client.
/// </summary>
public sealed class DatabaseTestDriver
{
    private ISurrealDatabase _database;

    public DatabaseTestDriver(ISurrealDatabase database)
    {
        _database = database;
    }

    public static async Task Run<T>()
        where T : ISurrealDatabase, new()
    {
        DatabaseTestDriver driver = new(new T());
        await driver.Run();
    }

    private async Task Run()
    {
        await _database.Open(ConfigHelper.Default);
        _database.GetConfig().Should().BeEquivalentTo(ConfigHelper.Default);

        AssertOk(await _database.Use(ConfigHelper.Database, ConfigHelper.Namespace));
        AssertOk(await _database.Info());

        AssertOk(await _database.Signin(new()
        {
            Namespace = ConfigHelper.Namespace,
            Username = ConfigHelper.User,
            Password = ConfigHelper.Pass,
        }));
        AssertOk(await _database.Invalidate());

        var (id1, id2) = ("", "");
        SurrealResponse res1 = await _database.Create("person", new
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
        (res1.TryGetResult(out SurrealResult rsp1) && rsp1.TryGetDocument(out id1, out JsonElement _)).Should()
            .BeTrue();

        SurrealResponse res2 = await _database.Create("person", new
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
        (res2.TryGetResult(out SurrealResult rsp2) && rsp2.TryGetDocument(out id2, out JsonElement _)).Should()
            .BeTrue();

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

        string newTitle = "Founder & CEO & Ruler of the known free World";
        AssertOk(await _database.Modify(thing1, new
        {
            op = "replace", path = "/Title", value = newTitle
        }));

        AssertOk(await _database.Let("tbl", "person"));

        AssertOk(await _database.Query("SELECT $props FROM $tbl WHERE title = $title", new
        {
            Props = "title, identifier",
            Title = newTitle,
        }));

        await _database.Close();
    }

    [DebuggerStepThrough]
    private static void AssertOk(in SurrealResponse response, [CallerArgumentExpression("response")] string caller = "")
    {
        if (response.TryGetError(out var err))
        {
            throw new($"Expected Ok, got {err.Code} ({err.Message}) in {caller}");
        }
    }
}