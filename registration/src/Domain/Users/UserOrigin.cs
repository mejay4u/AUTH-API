namespace Registration.Domain.Users;

/// <summary>How a <see cref="User"/> record came to exist.</summary>
public static class UserOrigin
{
    /// <summary>Created from a Descope self-service registration (pushed to us).</summary>
    public const string Registration = "Registration";

    /// <summary>Created by JIT migration of a legacy member on first login.</summary>
    public const string Migrated = "Migrated";
}
