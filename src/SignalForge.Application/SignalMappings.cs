using SignalForge.Contracts.Signals;
using SignalForge.Domain;

namespace SignalForge.Application;

internal static class SignalMappings
{
    public static SignalResponse ToResponse(Signal signal) =>
        new(
            signal.Id,
            signal.Source,
            signal.Type,
            new DateTimeOffset(signal.OccurredAtUtc, TimeSpan.Zero),
            signal.Value);
}
