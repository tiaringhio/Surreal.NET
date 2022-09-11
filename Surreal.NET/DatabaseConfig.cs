using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Surreal.NET;

public enum Auth : byte
{
    None = 0,
    Basic,
}

public struct DatabaseConfig
{
    public IPAddress? Host { get; set; }
    public string? Database { get; set; }
    public string? Namespace { get; set; }
    public Auth Authentication { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public interface IConfigBuilder
{
    /// <summary>
    /// Returns the <see cref="IConfigBuilder"/> that seeded this instance.
    /// </summary>
    public IConfigBuilder? Parent { get; }

    /// <summary>
    /// Configures the <see cref="DatabaseConfig"/> with the current settings.
    /// </summary>
    /// <exception cref="InvalidConfigException">If the configuration of this instance if faulty.</exception>
    public void Configure(ref DatabaseConfig config);
}

public static class ConfigBuilder
{
    public static DatabaseConfig Build(this IConfigBuilder builder)
    {
        DatabaseConfig config = default;
        
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
        return config;
    }

    public static Endpoint Create() => new(null);
    
    /// <summary>
    /// The remote, database and namespace of the <see cref="DatabaseConfig"/>.
    /// </summary>
    public class Endpoint : IConfigBuilder
    {
        internal Endpoint(IConfigBuilder? parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// Remote database server url to connect to.
        /// </summary>
        public IPAddress? Remote { get; set; }

        /// <summary>
        /// The database to export the data from
        /// </summary>
        public string? Database { get; set; }

        /// <summary>
        /// The namespace to export the data from
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Remote database server url to connect to.
        /// </summary>
        public Endpoint WithRemote(in ReadOnlySpan<char> address, Func<IPAddress>? fallback = default)
        {
            if (fallback is null)
            {
                Remote = IPAddress.Parse(address);
                return this;
            }

            if (!IPAddress.TryParse(address, out var ip))
            {
                ip = fallback();
            }

            Remote = ip;
            return this;
        }

        /// <summary>
        /// The database to export the data from
        /// </summary>
        public Endpoint WithDatabase(string database)
        {
            Database = database;
            return this;
        }

        /// <summary>
        /// The database to export the data from
        /// </summary>
        public Endpoint WithDatabase(in ReadOnlySpan<char> database) => WithDatabase(database.ToString());


        /// <summary>
        /// The namespace to export the data from
        /// </summary>
        public Endpoint WithNamespace(string ns)
        {
            Namespace = ns;
            return this;
        }


        /// <summary>
        /// The namespace to export the data from
        /// </summary>
        public Endpoint WithNamespace(in ReadOnlySpan<char> ns) => WithDatabase(ns.ToString());

        /// <summary>
        /// Adds basic authentication to the configuration 
        /// </summary>
        public BasicAuth WithBasicAuth() => new(this);

        /// <inheritdoc />
        public IConfigBuilder? Parent { get; }

        /// <inheritdoc />
        public void Configure(ref DatabaseConfig config)
        {
            InvalidConfigException.ThrowIfNull(Remote, "Remote cannot be null");
            InvalidConfigException.ThrowIfNullOrWhitespace(Database, "Database cannot be null or whitespace");
            InvalidConfigException.ThrowIfNullOrWhitespace(Namespace, "Namespace cannot be null or whitespace");
            config.Host = Remote;
            config.Database = Database;
            config.Namespace = Namespace;
        }
    }

    /// <summary>
    /// Configures the <see cref="DatabaseConfig"/> with User and Password authentication. 
    /// </summary>
    public class BasicAuth : IConfigBuilder
    {
        internal BasicAuth(IConfigBuilder? parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// Database authentication username to use when connecting
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        ///  Database authentication password to use when connecting
        /// </summary>
        public string? Password { get; set; }


        /// <summary>
        /// Database authentication username to use when connecting
        /// </summary>
        public BasicAuth WithUser(string user)
        {
            Username = user;
            return this;
        }

        /// <summary>
        /// Database authentication username to use when connecting
        /// </summary>
        public BasicAuth WithUser(ReadOnlySpan<char> user) => WithUser(user.ToString());

        /// <summary>
        ///  Database authentication password to use when connecting
        /// </summary>
        public BasicAuth WithPassword(string password)
        {
            Password = password;
            return this;
        }

        /// <summary>
        ///  Database authentication password to use when connecting
        /// </summary>
        public BasicAuth WithPassword(ReadOnlySpan<char> password) => WithPassword(password.ToString());

        /// <inheritdoc />
        public IConfigBuilder? Parent { get; }

        /// <inheritdoc />
        public void Configure(ref DatabaseConfig config)
        {
            InvalidConfigException.ThrowIfNullOrWhitespace(Username, "Username cannot be null or whitespace");
            config.Authentication = Auth.Basic;
            config.Username = Username;
            config.Password = Password;
        }
    }
}


public class InvalidConfigException : Exception
{
    public string? PropertyName { get; set; }

    public InvalidConfigException()
    {
    }

    protected InvalidConfigException(SerializationInfo info, StreamingContext context) : base(info, context)
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