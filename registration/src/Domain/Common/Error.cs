namespace Registration.Domain.Common;

/// <summary>
/// A strongly-typed, serializable error. Using a value object (instead of throwing exceptions for
/// expected outcomes such as "email already registered") follows the Result pattern — predictable
/// control flow, no exceptions for business-rule violations.
/// </summary>
public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);
    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);
    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);
    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string description) => new(code, description, ErrorType.Forbidden);
    public static Error TooManyRequests(string code, string description) => new(code, description, ErrorType.TooManyRequests);
}
