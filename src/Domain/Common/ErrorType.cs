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
    Forbidden = 5
}
