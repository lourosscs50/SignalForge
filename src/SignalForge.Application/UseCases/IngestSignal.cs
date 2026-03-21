using SignalForge.Application;
using SignalForge.Contracts.Signals;
using SignalForge.Domain;

namespace SignalForge.Application.UseCases;

public static class IngestSignal
{
    public sealed class Handler(ISignalRepository signals, ISignalEvaluationService evaluation)
    {
        public async Task<SignalResponse> HandleAsync(IngestSignalRequest request, CancellationToken cancellationToken)
        {
            var occurredAtUtc = request.TimestampUtc.UtcDateTime;
            var ingestedAtUtc = DateTime.UtcNow;

            var signal = new Signal(
                Id: Guid.NewGuid(),
                Source: request.Source,
                Type: request.Type,
                Payload: "",
                Value: request.Value,
                OccurredAtUtc: occurredAtUtc,
                IngestedAtUtc: ingestedAtUtc
            );

            await signals.AddAsync(signal, cancellationToken);
            await evaluation.EvaluateAsync(signal, cancellationToken);

            return new SignalResponse(
                Id: signal.Id,
                Source: signal.Source,
                Type: signal.Type,
                TimestampUtc: new DateTimeOffset(signal.OccurredAtUtc, TimeSpan.Zero),
                Value: signal.Value
            );
        }
    }
}
