using AuthApi.Application.Common.Interfaces;

namespace AuthApi.Infrastructure.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
