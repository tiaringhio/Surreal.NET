using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;

using SurrealDB.Common;

namespace SurrealDB.Models;

public readonly record struct RawResult {
    private RawResult(Kind type, int code, string? time, string? status, string? detail, JsonElement result) {
        Type = type;
        Code = code;
        Time = time;
        Status = status;
        Detail = detail;
        Result = result;
    }

    public const string OK = "OK";

    public static RawResult Unknown(JsonElement inner) => new(Kind.Unknown, default, null, null, null, inner);
    public static RawResult Ok(string time, string status, string detail, JsonElement inner) => new(Kind.Ok, default, time, status, detail, inner);
    public static RawResult Error(string time, string status, string detail, JsonElement inner) => new(Kind.Error, default, time, status, detail, inner);
    public static RawResult TransportError(int code, string status, string detail) => new(Kind.TransportError, code, default, status, detail, default);
    public static RawResult Auth(JsonElement token) => new(Kind.Auth, default, default, default, default, token);

    public Kind Type { get; }

    public int Code { get; }
    public string? Time { get; }
    public string? Status { get;  }
    public string? Detail { get;  }
    public JsonElement Result { get; }

    public bool IsDefault => MemoryHelper.Compare(in this, default) == 0;

    public IResult ToResult() {
        return TryGetValue(out OkResult ok, out ErrorResult err) ? ok : err;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue([NotNullWhen(true)] out OkResult ok, [NotNullWhen(false)] out ErrorResult err) {
        if (Type is Kind.Ok) {
            ok = OkResult.From(Result.IntoSingle());
            err = default;
            return true;
        }

        ok = default;
        err = new ErrorResult(-1, Status, Detail);
        return false;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue([NotNullWhen(true)] out OkResult ok) {
        if (Status.Equals(OK, StringComparison.OrdinalIgnoreCase)) {
            ok = OkResult.From(Result.IntoSingle());
            return true;
        }

        ok = default;
        return false;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetError([NotNullWhen(true)] out ErrorResult err) {
        if (Status.Equals(OK, StringComparison.OrdinalIgnoreCase)) {
            err = default;
            return false;
        }

        err = new ErrorResult(-1, Status, Detail);
        return true;
    }

    public enum Kind {
        Ok,
        Error,
        TransportError,
        Auth,
        Unknown,
    }

}
