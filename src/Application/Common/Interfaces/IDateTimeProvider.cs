namespace AuthApi.Application.Common.Interfaces;

/// <summary>Abstracts the system clock so token lifetimes and lockout logic are deterministically testable.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
