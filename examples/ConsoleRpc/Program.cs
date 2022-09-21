using SurrealDB.Configuration;
using SurrealDB.Driver.Rpc;
using SurrealDB.Models;

// start server: surreal start -b 0.0.0.0:8082 -u root -p root --log debug
Config cfg = Config.Create()
    .WithEndpoint("127.0.0.1:8082")
    .WithDatabase("test")
    .WithNamespace("test")
    .WithBasicAuth("root", "root")
    .WithRpc(insecure: true).Build();

DatabaseRpc db = new(cfg);
await db.Open();

Person you = new("Max Mustermann", 39, new("Musterstraße 1", 12345, "Musterstadt"), "0123456789", "max@mustermann.de");

RpcResponse create = await db.Create("test:maxmustermann", you);

RpcResponse select = await db.Select("test:maxmustermann");
if (select.TryGetResult(out Result result)) {
    // Prints: {"address":{"city":"Musterstadt","street":"Musterstraße 1","zip":12345},"age":39,"email":"max@mustermann.de","id":"test:maxmustermann","name":"Max Mustermann","phone":"0123456789"}
    Console.WriteLine(result.Inner);
    Person alsoYou = result.GetObject<Person>();
    // Prints: Yes we equals? True
    Console.WriteLine($"Yes we equals? {you == alsoYou}");
}


record struct Person(string name, int age, Address address, string phone, string email);
record struct Address(string street, int zip, string city);