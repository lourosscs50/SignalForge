using SignalForge.Application;
using SignalForge.Contracts.Signals;

namespace SignalForge.Application.UseCases;

public static class GetSignal
{
    public sealed class Handler(ISignalRepository signals)
    {
        public async Task<SignalResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
        {
            var signal = await signals.GetByIdAsync(id, cancellationToken);
            return signal is null ? null : SignalMappings.ToResponse(signal);
        }
    }
}
