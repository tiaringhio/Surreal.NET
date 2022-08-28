using Surreal.NET.Models;

namespace Surreal.NET.Clients;

public interface ISurrealRestClient
{
    Task<SurrealResult<T>> GetAll<T>(string set) where T : class;
    Task<SurrealResult<T>> Get<T>(string set, string id) where T : class;
    Task<SurrealResult<T>> Create<T>(string set, T item) where T : class;
    Task<SurrealResult<T>> Update<T>(string set, string id, T item) where T : class;
    Task<SurrealResult<T>> Delete<T>(string set, string id) where T : class;
    Task<SurrealResult<T>> Sql<T>(string query) where T : class;
}