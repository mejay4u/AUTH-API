namespace AuthApi.Domain.Common;

/// <summary>
/// Classifies a domain <see cref="Error"/> so the API layer can map it to an HTTP status code
/// without the Application/Domain layers ever depending on ASP.NET Core.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,

    /// <summary>A dependency we call out to could not be reached (maps to 503).</summary>
    Unavailable = 6,

    /// <summary>A dependency we call out to did not answer in time (maps to 504).</summary>
    Timeout = 7
}
