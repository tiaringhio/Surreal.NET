using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace SurrealDB.Configuration;

/// <summary>
///     Exception thrown if a <see cref="IConfigBuilder" /> contained invalid configuration.
/// </summary>
public sealed class InvalidConfigException : Exception {
    public InvalidConfigException() {
    }

    private InvalidConfigException(
        SerializationInfo info,
        StreamingContext context) : base(info, context) {
    }

    public InvalidConfigException(string? message) : base(message) {
    }

    public InvalidConfigException(
        string? message,
        Exception? innerException) : base(message, innerException) {
    }

    public InvalidConfigException(
        string? propertyName,
        string? message) : base(message) {
        PropertyName = propertyName;
    }

    public InvalidConfigException(
        string? propertyName,
        string? message,
        Exception? innerException) : base(message, innerException) {
        PropertyName = propertyName;
    }

    public string? PropertyName { get; set; }

    [DebuggerStepThrough]
    public static void ThrowIf(
        [DoesNotReturnIf(true)] bool condition,
        string propertyName,
        string? message = null,
        Exception? innerException = null) {
        if (condition) {
            Throw(propertyName, message, innerException);
        }
    }

    [DoesNotReturn, DebuggerStepThrough,]
    public static void Throw(
        string propertyName,
        string? message = null,
        Exception? innerException = null) {
        throw new InvalidConfigException(propertyName, message, innerException);
    }

    [DebuggerStepThrough]
    public static void ThrowIfNull(
        object? value,
        string? message = null,
        Exception? innerException = null,
        [CallerArgumentExpression("value")] string propertyName = "") {
        ThrowIf(value is null, propertyName, message ?? $"{value} cannot be null", innerException);
    }

    [DebuggerStepThrough]
    public static void ThrowIfNullOrWhitespace(
        string? value,
        string? message = null,
        Exception? innerException = null,
        [CallerArgumentExpression("value")] string propertyName = "") {
        ThrowIf(string.IsNullOrWhiteSpace(value), propertyName, message ?? $"{value} cannot be null or whitespace", innerException);
    }
}