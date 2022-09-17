using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Surreal.Net;

/// <summary>
/// Available authentication methods
/// </summary>
public enum Auth : byte
{
    None = 0,
    Basic,
    JsonWebToken,
}

public struct SurrealConfig
{
    private bool _validated;

    /// <summary>
    /// Flag indicating a validated config.
    /// </summary>
    public bool IsValidated => _validated;

    /// <summary>
    /// Remote database server endpoint (address and port) to connect to.
    /// </summary>
    public IPEndPoint? Endpoint { get; set; }

    /// <summary>
    /// Optional: The database to export the data from.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// Optional: The namespace to export the data from.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// The authentication method to use.
    /// </summary>
    public Auth Authentication { get; set; }

    /// <summary>
    /// Database authentication username to use when connecting.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///  Database authentication password to use when connecting.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Database authentication Json Web Token to use when connecting.
    /// </summary>
    public string? JsonWebToken { get; set; }

    /// <summary>
    /// Optional: Defines the RPC endpoint to use
    /// </summary>
    /// <remarks>
    /// If not specified computed using <see cref="Endpoint"/>.
    /// This option can be used to override the computed value.
    /// </remarks>
    public Uri? RpcEndpoint { get; set; }

    /// <summary>
    /// The <see cref="Uri"/> of the rest endpoint of the REST client
    /// </summary>
    /// <remarks>
    /// If not specified computed using <see cref="Endpoint"/>.
    /// This option can be used to override the computed value.
    /// </remarks>
    public Uri? RestEndpoint { get; set; }

    /// <summary>
    /// Begins configuration of a <see cref="SurrealConfig"/> with fluent api.
    /// </summary>
    /// <returns></returns>
    public static SurrealConfigBuilder.Basic Create() => SurrealConfigBuilder.Create();

    /// <summary>
    /// Marks the configuration as validated.
    /// </summary>
    public void MarkAsValidated()
    {
        _validated = true;
    }

    [Pure]
    public void ThrowIfInvalid()
    {
        if (!_validated)
        {
            throw new InvalidConfigException("The configuration is not marked as valid.");
        }
    }
}

/// <summary>
/// Component that configures a <see cref="SurrealConfig"/>.
/// </summary>
/// <remarks>
/// A <see cref="IConfigBuilder"/> may not be directly instantiated in user code!
/// </remarks>
public interface IConfigBuilder
{
    /// <summary>
    /// Returns the <see cref="IConfigBuilder"/> that seeded this instance
    /// </summary>
    public IConfigBuilder? Parent { get; }

    /// <summary>
    /// Configures the <see cref="SurrealConfig"/> with the current settings
    /// </summary>
    /// <exception cref="InvalidConfigException">If the configuration of this instance if faulty</exception>
    public void Configure(ref SurrealConfig config);
}

/// <summary>
/// Contains logic to build a <see cref="SurrealConfig"/> in the form of <see cref="IConfigBuilder"/> components and extensions methods.
/// </summary>
public static class SurrealConfigBuilder
{
    /// <summary>
    /// Begins configuration of a <see cref="SurrealConfig"/> with fluent api.
    /// </summary>
    public static Basic Create() => new(null);

    /// <summary>
    /// Returns the configured <see cref="SurrealConfig"/> by applying the entire <see cref="IConfigBuilder"/> chain.
    /// </summary>
    /// <exception cref="InvalidConfigException">If the configuration is faulty.</exception>
    public static SurrealConfig Build(this IConfigBuilder? builder)
    {
        InvalidConfigException.ThrowIf(builder is null, "", "Empty configuration is not allowed.");

        SurrealConfig config = default;

        Stack<IConfigBuilder> backlog = new();
        backlog.Push(builder);
        while (builder.Parent is not null)
        {
            backlog.Push(builder.Parent);
            builder = builder.Parent;
        }

        while (backlog.TryPop(out builder))
        {
            builder.Configure(ref config);
        }

        config.MarkAsValidated();
        return config;
    }

    /// <inheritdoc cref="BasicAuth"/>
    public static BasicAuth WithBasicAuth(this IConfigBuilder config, string? username = null, string? password = null) => new(config) { Username = username, Password = password };

    /// <inheritdoc cref="JwtAuth"/>
    public static JwtAuth WithJwtAuth(this IConfigBuilder config, string? token = null) => new(config) { Token = token };

    /// <inheritdoc cref="UseRpc"/>
    public static UseRpc WithRpc(this IConfigBuilder config, bool insecure = false) => new(config) { Insecure = insecure };

    /// <summary>
    /// Basic options, such as the remote, database and namespace of the <see cref="SurrealConfig"/>
    /// </summary>
    public sealed class Basic : IConfigBuilder
    {
        internal Basic(IConfigBuilder? parent)
        {
            Parent = parent;
        }

        /// <inheritdoc />
        public IConfigBuilder? Parent { get; }

        /// <inheritdoc cref="SurrealConfig.Endpoint"/>
        public IPEndPoint? Endpoint { get; set; }

        /// <inheritdoc cref="SurrealConfig.Database"/>
        public string? Database { get; set; }

        /// <inheritdoc cref="SurrealConfig.Namespace"/>
        public string? Namespace { get; set; }

        /// <inheritdoc cref="SurrealConfig.Endpoint"/>
        public Basic WithEndpoint(IPEndPoint endpoint)
        {
            Endpoint = endpoint;
            return this;
        }

        /// <inheritdoc cref="SurrealConfig.Endpoint"/>
        public Basic WithEndpoint(in ReadOnlySpan<char> endpoint, Func<IPEndPoint>? fallback = default)
        {
            if (fallback is null)
            {
                Endpoint = IPEndPoint.Parse(endpoint);
                return this;
            }

            if (!IPEndPoint.TryParse(endpoint, out IPEndPoint? ip))
            {
                ip = fallback();
            }

            return WithEndpoint(ip);
        }


        /// <inheritdoc cref="SurrealConfig.Endpoint"/>
        public Basic WithAddress(IPAddress address)
        {
            if (Endpoint is null)
            {
                Endpoint = new(address, 0);
            }
            else
            {
                Endpoint.Address = address;
            }

            return this;
        }


        /// <inheritdoc cref="SurrealConfig.Endpoint"/>
        public Basic WithAddress(in ReadOnlySpan<char> address, Func<IPAddress>? fallback = default)
        {
            IPAddress? ip;
            if (fallback is null)
            {
                ip = IPAddress.Parse(address);
            }
            else if (!IPAddress.TryParse(address, out ip))
            {
                ip = fallback();
            }

            return WithAddress(ip);
        }


        /// <inheritdoc cref="SurrealConfig.Endpoint"/>
        public Basic WithPort(in int port)
        {
            if (Endpoint is null)
            {
                Endpoint = new(IPAddress.Loopback, port);
            }
            else
            {
                Endpoint.Port = port;
            }

            return this;
        }

        /// <inheritdoc cref="SurrealConfig.Database"/>
        public Basic WithDatabase(string database)
        {
            Database = database;
            return this;
        }

        /// <inheritdoc cref="SurrealConfig.Database"/>
        public Basic WithDatabase(in ReadOnlySpan<char> database) => WithDatabase(database.ToString());

        /// <inheritdoc cref="SurrealConfig.Namespace"/>
        public Basic WithNamespace(string ns)
        {
            Namespace = ns;
            return this;
        }

        /// <inheritdoc cref="SurrealConfig.Namespace"/>
        public Basic WithNamespace(in ReadOnlySpan<char> ns) => WithDatabase(ns.ToString());

        /// <inheritdoc />
        public void Configure(ref SurrealConfig config)
        {
            InvalidConfigException.ThrowIfNull(Endpoint, "Remote cannot be null");
            config.Endpoint = Endpoint;
            config.Database = Database;
            config.Namespace = Namespace;
        }
    }

    /// <summary>
    /// Configures the <see cref="SurrealConfig"/> with User and Password authentication
    /// </summary>
    /// <remarks>
    /// Important:
    /// Do not ever use this on a client sided application!
    /// The password is stored in plaintext in the heap, and can be obtained by a 3rd party!
    /// </remarks>
    public sealed class BasicAuth : IConfigBuilder
    {
        internal BasicAuth(IConfigBuilder? parent)
        {
            Parent = parent;
        }

        /// <inheritdoc />
        public IConfigBuilder? Parent { get; }

        /// <inheritdoc cref="SurrealConfig.Username"/>
        public string? Username { get; set; }

        /// <inheritdoc cref="SurrealConfig.Password"/>
        public string? Password { get; set; }


        public BasicAuth WithUser(string user)
        {
            Username = user;
            return this;
        }

        /// <inheritdoc cref="SurrealConfig.Username"/>
        public BasicAuth WithUser(ReadOnlySpan<char> user) => WithUser(user.ToString());

        /// <inheritdoc cref="SurrealConfig.Password"/>
        public BasicAuth WithPassword(string password)
        {
            Password = password;
            return this;
        }

        /// <inheritdoc cref="SurrealConfig.Password"/>
        public BasicAuth WithPassword(ReadOnlySpan<char> password) => WithPassword(password.ToString());

        /// <inheritdoc />
        public void Configure(ref SurrealConfig config)
        {
            InvalidConfigException.ThrowIfNullOrWhitespace(Username, "Username cannot be null or whitespace");
            config.Authentication = Auth.Basic;
            config.Username = Username;
            config.Password = Password;
        }
    }

    /// <summary>
    /// Configures the <see cref="SurrealConfig"/> with Json Web Token Authentication
    /// </summary>
    public sealed class JwtAuth : IConfigBuilder
    {
        internal JwtAuth(IConfigBuilder? parent)
        {
            Parent = parent;
        }

        /// <inheritdoc />
        public IConfigBuilder? Parent { get; }

        /// <inheritdoc cref="SurrealConfig.JsonWebToken"/>
        public string? Token { get; set; }

        /// <inheritdoc cref="SurrealConfig.JsonWebToken"/>
        public JwtAuth WithToken(string? jwt)
        {
            Token = jwt;
            return this;
        }

        /// <inheritdoc />
        public void Configure(ref SurrealConfig config)
        {
            InvalidConfigException.ThrowIfNullOrWhitespace(Token, "Invalid Json Web Token");
            config.Authentication = Auth.JsonWebToken;
            config.JsonWebToken = Token;
        }
    }

    /// <summary>
    /// Configures the <see cref="SurrealConfig"/> to use the rpc endpoint
    /// </summary>
    public sealed class UseRpc : IConfigBuilder
    {
        internal UseRpc(IConfigBuilder? parent)
        {
            Parent = parent;
        }

        public IConfigBuilder? Parent { get; }

        /// <summary>
        /// Optional: Determines whether to disable TLS for the RPC connection.
        /// `false` uses the `wss` protocol, `true` uses `ws`.
        /// </summary>
        /// <remarks>
        /// This is not recommended, and should only be used for testing purposes
        /// </remarks>
        public bool Insecure { get; set; }

        /// <inheritdoc cref="SurrealConfig.RpcUrl" />
        public Uri? RpcUrl { get; set; }

        /// <inheritdoc cref="SurrealConfig.RpcUrl"/>
        public UseRpc WithRpcUrl(Uri rpcUrl)
        {
            RpcUrl = rpcUrl;
            return this;
        }

        /// <inheritdoc cref="Insecure" />
        public UseRpc WithRpcInsecure(bool insecure)
        {
            Insecure = insecure;
            return this;
        }

        /// <summary>
        /// Creates the <see cref="Uri"/> used for the rpc websocket based on the specified <see cref="EndPoint"/>.
        /// </summary>
        public static Uri GetUri(EndPoint endPoint, bool insecure = false) => insecure
            ? new Uri($"ws://{endPoint}/rpc/")
            : new Uri($"wss://{endPoint}/rpc/");


        public void Configure(ref SurrealConfig config)
        {
            config.RpcEndpoint = RpcUrl ?? GetUri(config.Endpoint!, Insecure);
        }
    }
}

/// <summary>
/// Exception thrown if a <see cref="IConfigBuilder"/> contained invalid configuration.
/// </summary>
public sealed class InvalidConfigException : Exception
{
    public string? PropertyName { get; set; }

    public InvalidConfigException()
    {
    }

    private InvalidConfigException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public InvalidConfigException(string? message) : base(message)
    {
    }

    public InvalidConfigException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public InvalidConfigException(string? propertyName, string? message) : base(message)
    {
        PropertyName = propertyName;
    }

    public InvalidConfigException(string? propertyName, string? message, Exception? innerException) : base(message, innerException)
    {
        PropertyName = propertyName;
    }

    [DebuggerStepThrough]
    public static void ThrowIf([DoesNotReturnIf(true)] bool condition, string propertyName,
        string? message = null, Exception? innerException = null)
    {
        if (condition)
        {
            Throw(propertyName, message, innerException);
        }
    }

    [DoesNotReturn, DebuggerStepThrough]
    public static void Throw(string propertyName, string? message = null, Exception? innerException = null)
    {
        throw new InvalidConfigException(propertyName, message, innerException);
    }

    [DebuggerStepThrough]
    public static void ThrowIfNull(object? value, string? message = null, Exception? innerException = null,
        [CallerArgumentExpression("value")] string propertyName = "")
    {
        ThrowIf(value is null, propertyName, message ?? $"{value} cannot be null", innerException);
    }

    [DebuggerStepThrough]
    public static void ThrowIfNullOrWhitespace(string? value, string? message = null, Exception? innerException = null,
        [CallerArgumentExpression("value")] string propertyName = "")
    {
        ThrowIf(String.IsNullOrWhiteSpace(value), propertyName, message ?? $"{value} cannot be null or whitespace", innerException);
    }
}