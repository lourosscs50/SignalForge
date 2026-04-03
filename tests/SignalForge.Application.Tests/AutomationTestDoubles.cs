using SignalForge.Application;
using SignalForge.Application.Automation;
using SignalForge.Application.Queries;
using SignalForge.Contracts;
using SignalForge.Contracts.Automation;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

internal sealed class CapturingLifecyclePublisher : IAlertLifecycleEventPublisher
{
    public List<AlertLifecycleEvent> Events { get; } = [];

    public Task PublishAsync(AlertLifecycleEvent lifecycleEvent, CancellationToken cancellationToken = default)
    {
        Events.Add(lifecycleEvent);
        return Task.CompletedTask;
    }
}

internal sealed class CapturingTriggerPublisher : IControlAutomationTriggerPublisher
{
    public List<ControlAutomationTriggerRequest> Triggers { get; } = [];

    public Task PublishAsync(ControlAutomationTriggerRequest request, CancellationToken cancellationToken = default)
    {
        Triggers.Add(request);
        return Task.CompletedTask;
    }
}

internal sealed class FakeRuleRepositoryForLifecycle : IRuleRepository
{
    public required Rule Rule { get; init; }

    public Task AddAsync(Rule rule, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<Rule?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rule.Id == id ? Rule : null);

    public Task UpdateAsync(Rule rule, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyList<Rule>> ListActiveAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Rule>>([Rule]);

    public Task<PagedResult<Rule>> ListPagedAsync(RuleListQuery query, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

internal static class AutomationTestHarness
{
    public static AlertLifecycleAutomationCoordinator CreateCoordinator(
        CapturingLifecyclePublisher life,
        CapturingTriggerPublisher trigger,
        IAlertAutomationPolicy? policy = null) =>
        new(life, policy ?? new DefaultAlertAutomationPolicy(), trigger);
}
