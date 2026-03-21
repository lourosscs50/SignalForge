using SignalForge.Application;
using SignalForge.Domain;

namespace SignalForge.Infrastructure;

public sealed class InMemoryRuleRepository : IRuleRepository
{
    private readonly List<Rule> _rules = [];

    public Task AddAsync(Rule rule, CancellationToken cancellationToken)
    {
        _rules.Add(rule);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Rule>> ListActiveAsync(CancellationToken cancellationToken) =>
        Task.FromResult((IReadOnlyList<Rule>)_rules.Where(r => r.IsActive).ToList());
}

public sealed class InMemorySignalRepository : ISignalRepository
{
    private readonly List<Signal> _signals = [];

    public Task AddAsync(Signal signal, CancellationToken cancellationToken)
    {
        _signals.Add(signal);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Signal>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult((IReadOnlyList<Signal>)_signals.ToList());
}

public sealed class InMemoryAlertRepository : IAlertRepository
{
    private readonly List<Alert> _alerts = [];

    public Task AddAsync(Alert alert, CancellationToken cancellationToken)
    {
        _alerts.Add(alert);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Alert>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult((IReadOnlyList<Alert>)_alerts.ToList());
}
