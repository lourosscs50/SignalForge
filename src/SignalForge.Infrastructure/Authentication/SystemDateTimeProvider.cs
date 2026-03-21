using SignalForge.Application;

namespace SignalForge.Infrastructure.Authentication;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

