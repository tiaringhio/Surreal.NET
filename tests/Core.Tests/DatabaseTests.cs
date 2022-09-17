using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Surreal.Net.Database;

namespace Surreal.Net.Tests;

public class DatabaseTests
{
    [Fact]
    public async Task RpcTestSuite()
    {
        await new DatabaseTestDriver<DbRpc, SurrealRpcResponse>().Execute();
    }

    [Fact]
    public async Task RestTestSuite()
    {
        await new DatabaseTestDriver<DbRest, SurrealRestResponse>().Execute();
    }
}

public sealed class DatabaseTestDriver<T, U>
    : DriverBase<T>
    where T : ISurrealDatabase<U>, new()
    where U : ISurrealResponse
{
    protected override async Task Run()
    {
        await Database.Open(ConfigHelper.Default);
        Database.GetConfig().Should().BeEquivalentTo(ConfigHelper.Default);

        var useResp = await Database.Use(ConfigHelper.Database, ConfigHelper.Namespace);
        AssertOk(useResp);
        var infoResp = await Database.Info();
        AssertOk(infoResp);

        var signInStatus = await Database.Signin(new()
        {
            Username = ConfigHelper.User,
            Password = ConfigHelper.Pass,
        });

        AssertOk(signInStatus);
        //AssertOk(await Database.Invalidate());

        var (id1, id2) = ("", "");
        ISurrealResponse res1 = await Database.Create("person", new
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

        ISurrealResponse res2 = await Database.Create("person", new
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
        AssertOk(await Database.Update(thing2, new
        {
            Marketing = false,
        }));

        AssertOk(await Database.Select(thing2));

        AssertOk(await Database.Delete(thing2));

        var thing1 = SurrealThing.From("person", id1);
        AssertOk(await Database.Change(thing1, new
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
        var modifyResp = await Database.Modify(thing1, new object[]
        {
            new {
                op = "replace",
                path = "/Title",
                value = newTitle
            }
        });
        AssertOk(modifyResp);

        AssertOk(await Database.Let("tbl", "person"));

        var queryResp = await Database.Query("SELECT $props FROM $tbl WHERE title = $title", new Dictionary<string, object?>
        {
            ["props"] = "title, identifier",
            ["title"] = newTitle,
        });
        AssertOk(queryResp);

        await Database.Close();
    }
}

/// <summary>
/// The test driver executes the testsuite on the client.
/// </summary>
public abstract class DriverBase<T>
    where T : new()
{
    private readonly List<Exception> _ex = new();

    public DriverBase()
    {
        Database = new();
    }

    public T Database { get; }

    public async Task Execute()
    {
        await Run();
        if (_ex.Count > 0)
        {
            throw new AggregateException(_ex);
        }
    }

    protected abstract Task Run();

    [DebuggerStepThrough]
    protected void AssertOk(in ISurrealResponse rpcResponse, [DoesNotReturnIf(true)] bool fatal = true, [CallerArgumentExpression("rpcResponse")] string caller = "")
    {
        if (!rpcResponse.TryGetError(out var err))
        {
            return;
        }

        Exception ex = new($"Expected Ok, got {err.Code} ({err.Message}) in {caller}");
        if (fatal)
        {
            throw ex;
        }

        _ex.Add(ex);
    }
}

public sealed class AggregateException : Exception
{
    public List<Exception> Exceptions { get; } = new();

    public AggregateException(List<Exception> exceptions) : base("Multiple exceptions occured")
    {
        Exceptions = exceptions;
    }

    public AggregateException() : base()
    {
    }

    public AggregateException(string? message) : base(message)
    {
    }

    public AggregateException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public override string ToString()
    {
        return Message + Environment.NewLine + string.Join(Environment.NewLine, Exceptions.Select(x => x.ToString()));
    }
}