using System.Text.Json;

namespace SurrealDB.Models;

public readonly partial struct DriverResponse {
    public bool TryGetFirstError(out ErrorResult err) {
        foreach (ErrorResult res in Errors) {
            err = res;
            return true;
        }

        err = default;
        return false;
    }

    public ErrorResult FirstError => TryGetFirstError(out ErrorResult err) ? err : default;

    /// <summary>
    /// Returns the first non null <see cref="OkResult"/> in the result set
    /// </summary>
    public bool TryGetFirstOk(out OkResult ok) {
        {
            foreach (OkResult res in Oks) {
                if (res.Inner.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null)) {
                    ok = res;
                    return true;
                }
            }

            ok = default;
            return false;
        }
    }

    public OkResult FirstOk => TryGetFirstOk(out OkResult ok) ? ok : default;

    public bool TryGetSingleError(out ErrorResult err) {
        // net7 RJU fixes the enumerator boxing issue, therefore use foreach
        err = default;
        bool success = false;
        foreach (ErrorResult cur in Errors) {
            if (!success) {
                // at least one
                success = true;
                err = cur;
            } else {
                // more then one
                err = default;
                return false;
            }
        }

        return success;
    }

    public ErrorResult SingleError => TryGetSingleError(out ErrorResult err) ? err : default;

    public bool TryGetSingleOk(out OkResult ok) {
        // net7 RJU fixes the enumerator boxing issue, therefore use foreach
        ok = default;
        bool success = false;
        foreach (OkResult cur in Oks) {
            if (!success) {
                // at least one
                success = true;
                ok = cur;
            } else {
                // more then one
                ok = default;
                return false;
            }
        }

        return success;
    }

    public OkResult SingleOk => TryGetSingleOk(out OkResult ok) ? ok : default;
}
