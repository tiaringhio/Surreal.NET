using SurrealDB.Configuration;
using SurrealDB.Driver.Rpc;
using SurrealDB.Models;

// start server: surreal start -b 0.0.0.0:8082 -u root -p root --log debug
// Create a configuration for the sever specified above.
Config cfg = Config.Create()
    .WithEndpoint("127.0.0.1:8082")
    .WithDatabase("test")
    .WithNamespace("test")
    .WithBasicAuth("root", "root")
    // Tell the configuration to connect to the server using RPC, and without TLS.
    .WithRpc(insecure: true).Build();

// Create a RPC database connection with the configuration.
DatabaseRpc db = new(cfg);
// Connect using the defined connection.
await db.Open();
// Create a struct with the fields we want to insert, nesting is supported.
Person you = new("Max Mustermann", 39, new("Musterstraße 1", 12345, "Musterstadt"), "0123456789", "max@mustermann.de");
// Insert the struct into the database, table = person, id = maxmustermann.
// If id` is not specified it will be random-generated, the id can be read from the response.
RpcResponse create = await db.Create("person:maxmustermann", you);
// Read the struct from the database to verify it was inserted correctly.
RpcResponse select = await db.Select("person:maxmustermann");
if (select.TryGetResult(out Result result)) {
    // Prints: {"address":{"city":"Musterstadt","street":"Musterstraße 1","zip":12345},"age":39,"email":"max@mustermann.de","id":"test:maxmustermann","name":"Max Mustermann","phone":"0123456789"}
    Console.WriteLine(result.Inner);
    Person alsoYou = result.GetObject<Person>();
    // Prints: Yes we equals? True
    Console.WriteLine($"Yes we equals? {you == alsoYou}");
}


/// <summary>
/// A Person.
/// </summary>
record struct Person(string name, int age, Address address, string phone, string email);

/// <summary>
/// The address of one or more people.
/// </summary>
record struct Address(string street, int zip, string city);