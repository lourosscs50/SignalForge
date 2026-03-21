using SignalForge.Contracts.Signals;

namespace SignalForge.Application.UseCases;

public static class ListSignals
{
    public sealed class Handler(ISignalRepository signals)
    {
        public async Task<IReadOnlyList<SignalResponse>> HandleAsync(CancellationToken cancellationToken)
        {
            var list = await signals.ListAsync(cancellationToken);

            return list
                .Select(s => new SignalResponse(
                    Id: s.Id,
                    Source: s.Source,
                    Type: s.Type,
                    TimestampUtc: new DateTimeOffset(s.OccurredAtUtc, TimeSpan.Zero)
                ))
                .ToList();
        }
    }
}

