// using System.Text.Json;
//
// using SurrealDB.Common;
// using SurrealDB.Models.DriverResult;
//
// namespace SurrealDB.Models;
//
// public readonly partial struct DriverResponse {
//     public bool TryGetFirstError(out ErrorResult err) {
//         foreach (ErrorResult res in Errors) {
//             err = res;
//             return true;
//         }
//
//         err = default;
//         return false;
//     }
//
//     public ErrorResult FirstError => TryGetFirstError(out ErrorResult err) ? err : default;
//
//     /// <summary>
//     /// Returns the first non null <see cref="ResultValue"/> in the result set
//     /// </summary>
//     public bool TryGetFirstValue(out ResultValue ok) {
//         foreach (OkResult res in Oks) {
//             if (res.Value.Inner.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null)) {
//                 ok = res.Value;
//                 return true;
//             }
//         }
//
//         ok = default;
//         return false;
//     }
//
//     public ResultValue First => TryGetFirstValue(out ResultValue ok) ? ok : default;
//
//     public bool TryGetSingleError(out ErrorResult err) {
//         ErrorIterator en = Errors;
//         return SequenceHelper.TrySingle(ref en, out err);
//     }
//
//     public ErrorResult SingleError => TryGetSingleError(out ErrorResult err) ? err : default;
//
//     public bool TryGetSingleValue(out ResultValue value) {
//         OkIterator en = Oks;
//         bool success = SequenceHelper.TrySingle(ref en, out OkResult ok);
//         value = ok.Value;
//         return success;
//     }
//
//     public ResultValue Single => TryGetSingleValue(out ResultValue ok) ? ok : default;
// }
