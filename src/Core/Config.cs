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
    /// Begins configuration of a <see cref="SurrealConfig"/> with fluent api.
    /// </summary>
    /// <returns></returns>
    public static SurrealConfigBuilder.Remote Create() => SurrealConfigBuilder.Create();
    
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
    public static Remote Create() => new(null);
    
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
    public static BasicAuth WithBasicAuth(this Remote config) => new(config);

    /// <inheritdoc cref="JwtAuth"/>
    public static JwtAuth WithJwtAuth(this Remote config, string? token = null) => new JwtAuth(config).WithToken(token);
    
    /// <summary>
    /// The remote, database and namespace of the <see cref="SurrealConfig"/>
    /// </summary>
    public sealed class Remote : IConfigBuilder
    {
        internal Remote(IConfigBuilder? parent)
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
        public Remote WithEndpoint(IPEndPoint endpoint)
        {
            Endpoint = endpoint;
            return this;
        }

        /// <inheritdoc cref="SurrealConfig.Endpoint"/>
        public Remote WithEndpoint(in ReadOnlySpan<char> endpoint, Func<IPEndPoint>? fallback = default)
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
        public Remote WithAddress(IPAddress address)
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
        public Remote WithAddress(in ReadOnlySpan<char> address, Func<IPAddress>? fallback = default)
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
        public Remote WithPort(in int port)
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
        public Remote WithDatabase(string database)
        {
            Database = database;
            return this;
        }

        /// <inheritdoc cref="SurrealConfig.Database"/>
        public Remote WithDatabase(in ReadOnlySpan<char> database) => WithDatabase(database.ToString());
        
        /// <inheritdoc cref="SurrealConfig.Namespace"/>
        public Remote WithNamespace(string ns)
        {
            Namespace = ns;
            return this;
        }

        /// <inheritdoc cref="SurrealConfig.Namespace"/>
        public Remote WithNamespace(in ReadOnlySpan<char> ns) => WithDatabase(ns.ToString());

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

    public InvalidConfigException(string? propertyName,string? message) : base(message)
    {
        PropertyName = propertyName;
    }

    public InvalidConfigException(string? propertyName, string? message, Exception? innerException) : base(message, innerException)
    {
        PropertyName = propertyName;
    }

    public static void ThrowIf([DoesNotReturnIf(true)] bool condition, string propertyName,
        string? message = null, Exception? innerException = null)
    {
        if (condition)
        {
            throw new InvalidConfigException(propertyName, message, innerException);
        }
    }

    public static void ThrowIfNull(object? value, string? message = null, Exception? innerException = null,
        [CallerArgumentExpression("value")] string propertyName = "")
    {
        ThrowIf(value is null, propertyName, message, innerException);
    }
    
    public static void ThrowIfNullOrWhitespace(string? value, string? message = null, Exception? innerException = null,
        [CallerArgumentExpression("value")] string propertyName = "")
    {
        ThrowIf(String.IsNullOrWhiteSpace(value), propertyName, message, innerException);
    }
}