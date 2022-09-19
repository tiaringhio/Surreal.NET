namespace SurrealDB.Models;

public enum ResultKind : byte {
    Object,
    Array,
    None,
    String,
    SignedInteger,
    UnsignedInteger,
    Float,
    Boolean
}