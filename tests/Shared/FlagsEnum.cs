namespace SurrealDB.Shared.Tests;

[Flags]
public enum FlagsEnum {
    None = 0,
    First = 1 << 0,
    Second = 1 << 1,
    Third = 1 << 2,
    Fourth = 1 << 3,
    All = First | Second | Third | Fourth,
}
