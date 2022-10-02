using SurrealDB.Models;

using System.Net;

namespace SurrealDB.Models;

/// <summary>
/// The base object for a Signup request.
/// Inherit from this object to build your own login request based on the you want to supply when signing up
/// </summary>
/// <param name="NS">Namespace</param>
/// <param name="DB">Database</param>
/// <param name="SC">Scope</param>
public abstract record SignupRequestBase(
    string NS,
    string DB,
    string SC);

/// <summary>
/// The base object for a Signin request.
/// </summary>
public abstract record SigninRequestBase();

/// <summary>
/// A Signin request using the "Basic" authentication scheme (RFC 7617).
/// </summary>
public record BasicSigninRequest(
    string user,
    string pass
    ) : SigninRequestBase;
