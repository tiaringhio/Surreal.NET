using SurrealDB.Configuration;
using SurrealDB.Extensions.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSurrealDB(static b => b
   .WithEndpoint("0.0.0.0:8082")
   .WithDatabase("test")
   .WithNamespace("test")
   .WithBasicAuth("root", "root")
   .WithRpc(insecure: true));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
