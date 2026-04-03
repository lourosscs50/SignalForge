using SignalForge.Application;
using SignalForge.Application.Evaluation;
using SignalForge.Application.Queries;
using SignalForge.Contracts;
using SignalForge.Contracts.Automation;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

public sealed class SignalEvaluationServiceAutomationTests
{
    private sealed class FixedClock(DateTime utc) : IDateTimeProvider
    {
        public DateTime UtcNow => utc;
    }

    private sealed class MatchAllEvaluator : IRuleEvaluator
    {
        public bool CanEvaluate(Rule rule) => true;
        public bool IsMatch(Rule rule, Signal signal) => true;
    }

    private sealed class EvalRulesRepo : IRuleRepository
    {
        public Rule Rule { get; }

        public EvalRulesRepo(Rule rule) => Rule = rule;

        public Task AddAsync(Rule rule, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Rule?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Rule?>(Rule.Id == id ? Rule : null);

        public Task UpdateAsync(Rule rule, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<Rule>> ListActiveAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Rule>>([Rule]);

        public Task<PagedResult<Rule>> ListPagedAsync(RuleListQuery query, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class EvalAlertsRepo : IAlertRepository
    {
        public List<Alert> Added { get; } = [];

        public Task AddAsync(Alert alert, CancellationToken cancellationToken)
        {
            Added.Add(alert);
            return Task.CompletedTask;
        }

        public Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Alert?>(Added.FirstOrDefault(a => a.Id == id));

        public Task UpdateAsync(Alert alert, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<PagedResult<Alert>> ListPagedAsync(AlertListQuery query, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Alert>> ListAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Alert>>(Added);

        public Task<IReadOnlyList<Alert>> ListByRuleIdAsync(Guid ruleId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Alert>>(Added.Where(a => a.RuleId == ruleId).ToList());
    }

    [Fact]
    public async Task EvaluateAsync_creates_alert_publishes_lifecycle_and_trigger_once_per_match()
    {
        var t = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        var rule = new Rule(Guid.NewGuid(), "E", RuleTypes.SignalTypeEquals, "t", true, false, t);
        var rules = new EvalRulesRepo(rule);
        var alerts = new EvalAlertsRepo();
        var life = new CapturingLifecyclePublisher();
        var trig = new CapturingTriggerPublisher();
        var coord = AutomationTestHarness.CreateCoordinator(life, trig);
        var svc = new SignalEvaluationService(
            rules,
            alerts,
            new FixedClock(t),
            [new MatchAllEvaluator()],
            coord);

        var signal = new Signal(Guid.NewGuid(), "s", "t", "", 1.0, t, t);

        await svc.EvaluateAsync(signal, CancellationToken.None);

        Assert.Single(alerts.Added);
        Assert.Single(life.Events);
        Assert.Equal(AlertLifecycleTransitionType.AlertCreated, life.Events[0].LifecycleTransitionType);
        Assert.Equal(rule.Name, life.Events[0].RuleName);
        Assert.Single(trig.Triggers);
        Assert.Equal(nameof(AlertLifecycleTransitionType.AlertCreated), trig.Triggers[0].TriggerType);
        Assert.Equal(AlertMappings.StatusOpen, trig.Triggers[0].CurrentStatus);
    }
}
