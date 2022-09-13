using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Surreal.Net.Tests;

public class ClientTests
{
}

/// <summary>
/// The test driver executes the testsuite on the client.
/// </summary>
public sealed class ClientTestDriver
{
    private ISurrealClient _client;

    public ClientTestDriver(ISurrealClient client)
    {
        _client = client;
    }

    public async Task Run()
    {
        await _client.Open(ConfigHelper.Default);
        _client.GetConfig().Should().BeEquivalentTo(ConfigHelper.Default);
        
        AssertOk(await _client.Use(ConfigHelper.Database, ConfigHelper.Namespace));
        AssertOk(await _client.Info());
        
        SurrealAuthentication auth = ConfigHelper.GetAuthentication("user", "user");
        AssertOk(await _client.Signup(auth));
        AssertOk(await _client.Invalidate());
        AssertOk(await _client.Signin(auth));

        var (id1, id2) = ("", "");
        SurrealResult res1 = await _client.Create("person", new
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
        (res1.TryGetResult(out SurrealResponse rsp1) && rsp1.TryGetDocumentWithId(out id1, out JsonElement _)).Should().BeTrue();

        SurrealResult res2 = await _client.Create("person", new
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
        (res2.TryGetResult(out SurrealResponse rsp2) && rsp2.TryGetDocumentWithId(out id2, out JsonElement _)).Should().BeTrue();

        var thing2 = SurrealThing.From("person", id2);
        AssertOk(await _client.Update(thing2, new
        {
            Marketing = false,
        }));
        
        AssertOk(await _client.Select(thing2));

        AssertOk(await _client.Delete(thing2));
        
        var thing1 = SurrealThing.From("person", id1);
        AssertOk(await _client.Change(thing1, new
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
        AssertOk(await _client.Modify(thing1, new
        {
            op = "replace", path = "/Title", value = newTitle
        }));

        AssertOk(await _client.Let("tbl", "person"));
        
        AssertOk(await _client.Query("SELECT $props FROM $tbl WHERE title = $title", new
        {
            Props = "title, identifier",
            Title = newTitle,
        }));

        _client.Close();
    }

    private static void AssertOk(in SurrealResult rsp, [CallerArgumentExpression("rsp")] string caller = "")
    {
        if (rsp.TryGetError(out var err))
        {
            throw new($"Expected Ok, got {err.Code} ({err.Message}) in {caller}");
        }
    }
}