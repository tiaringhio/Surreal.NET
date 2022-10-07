using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

using SurrealDB.Common;
using SurrealDB.Json;
using SurrealDB.Models;

namespace SurrealDB.Driver.Rest;

internal static class RestClientExtensions {
    internal static async Task<RestResponse> ToSurreal(this HttpResponseMessage msg, CancellationToken ct = default) {
#if NET6_0_OR_GREATER
        Stream stream = await msg.Content.ReadAsStreamAsync(ct);
#else
        Stream stream = await msg.Content.ReadAsStreamAsync();
#endif
        if (!msg.IsSuccessStatusCode) {
            RestError restError = await JsonSerializer.DeserializeAsync<RestError>(stream, SerializerOptions.Shared, ct);
            ErrorResult errorResult = restError.ToErrorResult();
            return new RestResponse(errorResult);
        }

        if (await PeekIsEmpty(stream, ct)) {
            // Success and empty message -> invalid json
            return new RestResponse();
        }
        
        List<RawResult>? docs = await JsonSerializer.DeserializeAsync<List<RawResult>>(stream, SerializerOptions.Shared, ct);

        if (docs == null) {
            return new RestResponse();
        }

        var results = docs.Select(e => e.ToResult()).ToList();

        return new RestResponse(results);
    }
    
    internal static async Task<RestResponse> ToSurrealFromAuthResponse(this HttpResponseMessage msg, CancellationToken ct = default) {
        
            // Signin and Signup returns a different object to the other response
            // And for that reason needs it's on deserialization path
            // The whole response is ultimately shoved into the RestResponse.Success.result field
            // {"code":200,"details":"Authentication succeeded","token":"a.jwt.token"}

#if NET6_0_OR_GREATER
        Stream stream = await msg.Content.ReadAsStreamAsync(ct);
#else
        Stream stream = await msg.Content.ReadAsStreamAsync();
#endif
        if (msg.StatusCode != HttpStatusCode.OK) {
            RestError restError = await JsonSerializer.DeserializeAsync<RestError>(stream, SerializerOptions.Shared, ct);
            ErrorResult errorResult = restError.ToErrorResult();
            return new RestResponse(errorResult);
        }

        AuthResult result = await JsonSerializer.DeserializeAsync<AuthResult>(stream, SerializerOptions.Shared, ct);

        return new RestResponse(result.ToResult());
    }

    /// <summary>
    /// Attempts to peek the next byte of the stream.
    /// </summary>
    /// <remarks>
    /// Resets the stream to the original position.
    /// </remarks>
    private static async Task<bool> PeekIsEmpty(
        Stream stream,
        CancellationToken ct) {
        Debug.Assert(stream.CanSeek && stream.CanRead);
        using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(1);
        // This is more efficient, then ReadByte.
        // Async, because this is the first request to the networkstream, thus no readahead is possible.
        int read = await stream.ReadAsync(buffer.Memory.Slice(0, 1), ct);
        stream.Seek(-read, SeekOrigin.Current);
        return read <= 0;
    }

    private readonly record struct RawResult(string time,
        string status,
        string detail,
        JsonElement result) {
        internal IResult ToResult() {
            if (status == "OK") {
                return OkResult.From(result.IntoSingle());
            } else {
                return new ErrorResult(-1, status, detail);
            }
        }
    }

    private readonly record struct RestError(int code,
        string details,
        string description,
        string information) {
        internal ErrorResult ToErrorResult() {
            ErrorResult errorResult = new (code, details, $"{description}\n{information}");
            return errorResult;
        }
    }

    public readonly record struct AuthResult(
        HttpStatusCode code,
        string details,
        JsonElement token) {
        internal IResult ToResult() {
            return OkResult.From(token);
        }
    }
}
