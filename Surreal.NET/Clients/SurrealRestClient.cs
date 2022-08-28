using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using Surreal.NET.Configs;
using Surreal.NET.Models;

namespace Surreal.NET.Clients;

public class SurrealRestClient : ISurrealRestClient, IDisposable
{
    private readonly RestClient _client;

    public SurrealRestClient(SurrealConfig config)
    {
        var options = new RestClientOptions(config.BaseUrl);
        _client = new RestClient(options)
        {
            Authenticator = new HttpBasicAuthenticator(config.Username, config.Password)
        };
        
        _client.AddDefaultHeader("NS", config.Namespace);
        _client.AddDefaultHeader("DB", config.Database);
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }


    /// <summary>
    /// Gets all item for a specific set
    /// </summary>
    /// <param name="set">Set to return</param>
    /// <typeparam name="T">Type of items in the set</typeparam>
    /// <returns>Items in the set or empty array if no item is present</returns>
    public async Task<SurrealResult<T>> GetAll<T>(string set)
        where T : class
    {
        var request = CreateRequest<T>($"key/{set}", Method.Get);
        
        var response = await _client.ExecuteAsync<IEnumerable<SurrealResult<T>>>(request);
        
        return JsonConvert
            .DeserializeObject<IEnumerable<SurrealResult<T>>>(response.Content)
            .FirstOrDefault() ?? null;
    }

    /// <summary>
    /// Returns a single item from a set given its ID
    /// </summary>
    /// <param name="set">Set in which the item is present</param>
    /// <param name="id">ID of the item</param>
    /// <typeparam name="T">Type of the item</typeparam>
    /// <returns>Expected item or empty array if not present</returns>
    public async Task<SurrealResult<T>> Get<T>(string set, string id) where T : class
    {
        var request = CreateRequest<T>($"key/{set}/{id}", Method.Get);
        
        var response = await _client.ExecuteAsync<IEnumerable<SurrealResult<T>>>(request);
        
        return JsonConvert
            .DeserializeObject<IEnumerable<SurrealResult<T>>>(response.Content)
            .FirstOrDefault() ?? null;
    }

    /// <summary>
    /// Creates a new item in the set
    /// </summary>
    /// <param name="set">Set in which the item will be added</param>
    /// <param name="item">Item to add</param>
    /// <typeparam name="T">Type of the item</typeparam>
    /// <returns>The new item created</returns>
    public async Task<SurrealResult<T>> Create<T>(string set, T item)
        where T : class
    {
        var request = CreateRequest($"key/{set}", Method.Post, item);
        
        var response = await _client.ExecuteAsync<IEnumerable<SurrealResult<T>>>(request);

        return JsonConvert
            .DeserializeObject<IEnumerable<SurrealResult<T>>>(response.Content)
            .FirstOrDefault();
    }

    /// <summary>
    /// Updates an existing item in the set
    /// </summary>
    /// <param name="set">Set in which the item is present</param>
    /// <param name="id">ID of the item to update</param>
    /// <param name="item">Item to update</param>
    /// <typeparam name="T">Type of the Item</typeparam>
    /// <returns>Updated item</returns>
    public async Task<SurrealResult<T>> Update<T>(string set, string id, T item) where T : class
    {
        var request = CreateRequest($"key/{set}/{id}", Method.Put, item);
        
        var response = await _client.ExecuteAsync<IEnumerable<SurrealResult<T>>>(request);

        return JsonConvert
            .DeserializeObject<IEnumerable<SurrealResult<T>>>(response.Content)
            .FirstOrDefault();
    }

    /// <summary>
    /// Deletes an item from the set given its ID
    /// </summary>
    /// <param name="set">Set in which the item is present</param>
    /// <param name="id">ID of the item to delete</param>
    /// <typeparam name="T">Type of the item to delete</typeparam>
    /// <returns>Result</returns>
    public async Task<SurrealResult<T>> Delete<T>(string set, string id) where T : class
    {
        var request = CreateRequest<T>($"key/{set}/{id}", Method.Delete);
        
        var response = await _client.ExecuteAsync<IEnumerable<SurrealResult<T>>>(request);
        
        return JsonConvert
            .DeserializeObject<IEnumerable<SurrealResult<T>>>(response.Content)
            .FirstOrDefault();
    }

    public async Task<SurrealResult<T>> Sql<T>(string query) where T : class
    {
        var request = new RestRequest("/sql", Method.Post);
        request.AddHeader("Content-Type", "application/json");
        request.AddParameter("application/json", query, ParameterType.RequestBody);
        
        var response = await _client.ExecuteAsync<IEnumerable<SurrealResult<T>>>(request);
        return JsonConvert
            .DeserializeObject<IEnumerable<SurrealResult<T>>>(response.Content)
            .FirstOrDefault();
    }

    /// <summary>
    /// Creates a SurrealRequest for the given URL and method.
    /// </summary>
    /// <param name="url">URL to which the request will be made</param>
    /// <param name="method">HTTP Methos</param>
    /// <param name="body">Body of the request (if present)</param>
    /// <typeparam name="T">Type of the item</typeparam>
    /// <returns>New RestRequest</returns>
    private static RestRequest CreateRequest<T>(string url, Method method, T? body = null)
        where T : class
    {
        var request = new RestRequest($"/{url}", method);
        request.AddHeader("Content-Type", "application/json");
        if (body is not null)
            request.AddParameter("application/json", body, ParameterType.RequestBody);
        return request;
    }
}