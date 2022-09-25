namespace SurrealDB.Driver.Tests;

public sealed class RpcDatabaseTest : DatabaseTestDriver<DatabaseRpc> {

}

public sealed class RestDatabaseTest : DatabaseTestDriver<DatabaseRest> {

}

[Collection("SurrealDBRequired")]
public abstract class DatabaseTestDriver<T>
    : DriverBase<T>
    where T : IDatabase, new() {

    [Fact]
    protected override async Task TestSuite() {
        await Database.Open(TestHelper.Default);
        Database.GetConfig().Should().BeEquivalentTo(TestHelper.Default);

        IResponse useResp = await Database.Use(TestHelper.Database, TestHelper.Namespace);
        AssertOk(useResp);
        IResponse infoResp = await Database.Info();
        AssertOk(infoResp);

        IResponse signInStatus = await Database.Signin(new() { Username = TestHelper.User, Password = TestHelper.Pass, });

        AssertOk(signInStatus);
        //AssertOk(await Database.Invalidate());

        (string id1, string id2) = ("id1", "id2");
        IResponse res1 = await Database.Create(
            "person",
            new {
                Title = "Founder & CEO",
                Name = new { First = "Tobie", Last = "Morgan Hitchcock", },
                Marketing = true,
                Identifier = Random.Shared.Next(),
            }
        );

        AssertOk(res1);

        IResponse res2 = await Database.Create(
            "person",
            new {
                Title = "Contributor",
                Name = new { First = "Prophet", Last = "Lamb", },
                Marketing = false,
                Identifier = Random.Shared.Next(),
            }
        );

        AssertOk(res2);

        Thing thing2 = Thing.From("person", id2);
        AssertOk(await Database.Update(thing2, new { Marketing = false, }));

        AssertOk(await Database.Select(thing2));

        AssertOk(await Database.Delete(thing2));

        Thing thing1 = Thing.From("person", id1);
        AssertOk(
            await Database.Change(
                thing1,
                new {
                    Title = "Founder & CEO",
                    Name = new { First = "Tobie", Last = "Hitchcock Morgan", },
                    Marketing = false,
                    Identifier = Random.Shared.Next(),
                }
            )
        );

        string newTitle = "Founder & CEO & Ruler of the known free World";
        IResponse modifyResp = await Database.Modify(thing1, new object[] { new { op = "replace", path = "/Title", value = newTitle, }, });
        AssertOk(modifyResp);

        AssertOk(await Database.Let("tbl", "person"));

        IResponse queryResp = await Database.Query(
            "SELECT $props FROM $tbl WHERE title = $title",
            new Dictionary<string, object?> { ["props"] = "title, identifier", ["tbl"] = "person", ["title"] = newTitle, }
        );

        AssertOk(queryResp);

        await Database.Close();
    }
}

/// <summary>
///     The test driver executes the testsuite on the client.
/// </summary>
[Collection("SurrealDBRequired")]
public abstract class DriverBase<T>
    where T : new() {

    TestDatabaseFixture? fixture;

    public DriverBase() {
        Database = new();
    }

    public T Database { get; }

    public async Task Execute() {
        await TestSuite();
    }

    protected abstract Task TestSuite();

    [DebuggerStepThrough]
    protected void AssertOk(
        in IResponse rpcResponse,
        [CallerArgumentExpression("rpcResponse")]
        string caller = "") {
        if (!rpcResponse.TryGetError(out Error err)) {
            return;
        }

        Exception ex = new($"Expected Ok, got {err.Code} ({err.Message}) in {caller}");
        throw ex;
    }
}
