using System.Text.Json;

namespace SurrealDB.Common;
internal static  class ResponseExtensions {
    public static JsonElement IntoSingle(this JsonElement root) {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() > 1) {
            return root;
        }

        var en = root.EnumerateArray();
        while (en.MoveNext()) {
            JsonElement cur = en.Current;
            // Return the first not null element
            if (cur.ValueKind is not JsonValueKind.Null or JsonValueKind.Undefined) {
                return cur;
            }
        }
        // No content in the array.
        return default;
    }
}
